using Soenneker.DocTr.Util.Abstract;
using Soenneker.Atomics.ValueBools;
using Soenneker.Asyncs.Semaphores;
using Soenneker.DocTr.Util.Models;
using Soenneker.Python.Util.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.DocTr.Util;

public sealed class DocTrUtil : IDocTrUtil
{
    private readonly IPythonUtil _pythonUtil;
    private readonly DocTrOptions _options;
    private readonly AsyncSemaphore _gate = new(1);
    private readonly CancellationTokenSource _lifetime = new();
    private DocTrWorker? _worker;
    private string? _interpreter;
    private ValueAtomicBool _disposed;

    public DocTrUtil(IPythonUtil pythonUtil, DocTrOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pythonUtil);
        _pythonUtil = pythonUtil;
        _options = options ?? new DocTrOptions();
        ValidateOptions(_options);
    }

    public async ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        SemaphoreLease lease = await _gate.Acquire(linked.Token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed.Value, this);
            return await Prepare(linked.Token).ConfigureAwait(false);
        }
        finally
        {
            lease.Dispose();
        }
    }

    public ValueTask<DocTrResult> Recognize(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        string fullPath = ValidatePath(path);
        return RecognizeCore(new DocTrRequest
        {
            Kind = Path.GetExtension(fullPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "images",
            Paths = [fullPath]
        }, cancellationToken);
    }

    public ValueTask<DocTrResult> RecognizeImages(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed.Value, this);
        ArgumentNullException.ThrowIfNull(paths);
        string[] fullPaths = paths.Select(ValidatePath).ToArray();
        if (fullPaths.Length == 0)
            throw new ArgumentException("At least one image is required.", nameof(paths));
        if (fullPaths.Any(path => Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Use Recognize for PDF files.", nameof(paths));
        return RecognizeCore(new DocTrRequest { Kind = "images", Paths = fullPaths }, cancellationToken);
    }

    private async ValueTask<string> Prepare(CancellationToken cancellationToken)
    {
        if (_interpreter is not null)
            return _interpreter;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.InstallationTimeout);
        try
        {
            _interpreter = await DocTrEnvironment.Prepare(_pythonUtil, _options, timeout.Token).ConfigureAwait(false);
            return _interpreter;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"docTR environment initialization exceeded {_options.InstallationTimeout}.");
        }
    }

    private async ValueTask<DocTrResult> RecognizeCore(DocTrRequest request, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        SemaphoreLease lease = await _gate.Acquire(linked.Token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed.Value, this);
            string interpreter = await Prepare(linked.Token).ConfigureAwait(false);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
            timeout.CancelAfter(_options.RecognitionTimeout);
            try
            {
                if (_worker is null)
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    _worker = await DocTrWorker.Start(_pythonUtil, interpreter, _lifetime.Token).ConfigureAwait(false);
                    await _worker.Initialize(_options, timeout.Token).ConfigureAwait(false);
                }
                DocTrResult result = await _worker.Recognize(request, timeout.Token).ConfigureAwait(false);
                if (!_options.ReuseModels)
                    await StopWorker().ConfigureAwait(false);
                return result;
            }
            catch
            {
                // Discard the process after failure so no delayed response can reach a later caller.
                await StopWorker().ConfigureAwait(false);
                if (timeout.IsCancellationRequested && !linked.IsCancellationRequested)
                    throw new TimeoutException($"docTR recognition exceeded {_options.RecognitionTimeout}.");
                throw;
            }
        }
        finally
        {
            lease.Dispose();
        }
    }

    private async ValueTask StopWorker()
    {
        DocTrWorker? worker = _worker;
        _worker = null;
        if (worker is not null)
            await worker.DisposeAsync().ConfigureAwait(false);
    }

    private static string ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The document does not exist.", fullPath);
        return fullPath;
    }

    private static void ValidateOptions(DocTrOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PythonVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.EnvironmentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DocTrVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Device);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DetectionArchitecture);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RecognitionArchitecture);
        if (!Version.TryParse(options.DocTrVersion, out _))
            throw new ArgumentException("DocTrVersion must be a numeric release version.", nameof(options));
        if (options.InstallationTimeout <= TimeSpan.Zero || options.InstallationTimeout.TotalMilliseconds > uint.MaxValue - 1 ||
            options.RecognitionTimeout <= TimeSpan.Zero || options.RecognitionTimeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Timeouts must be positive and supported by CancellationTokenSource.");
    }

    public async ValueTask DisposeAsync()
    {
        // Cancel current and queued work before taking the gate to release the child process promptly.
        await _lifetime.CancelAsync().ConfigureAwait(false);
        SemaphoreLease lease = await _gate.Acquire().ConfigureAwait(false);
        try
        {
            if (!_disposed.TrySetTrue())
                return;
            await StopWorker().ConfigureAwait(false);
        }
        finally
        {
            // Keep the synchronization primitives alive so concurrent callers/disposers cannot race their disposal.
            lease.Dispose();
        }
    }
}
