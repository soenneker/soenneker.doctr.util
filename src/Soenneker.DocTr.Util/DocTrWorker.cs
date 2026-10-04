using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.DocTr.Util.Models;
using Soenneker.Python.Util.Abstract;

namespace Soenneker.DocTr.Util;

internal sealed class DocTrWorker : IAsyncDisposable
{
    private readonly IPythonSession _session;

    private DocTrWorker(IPythonSession session) => _session = session;

    public static async ValueTask<DocTrWorker> Start(IPythonUtil pythonUtil, string interpreter,
        CancellationToken lifetimeToken, string? script = null)
    {
        if (script is null)
        {
            using Stream resource = typeof(DocTrWorker).Assembly.GetManifestResourceStream("Soenneker.DocTr.Util.Resources.doctr_worker.py")
                ?? throw new InvalidOperationException("The embedded docTR script is missing.");
            using var reader = new StreamReader(resource);
            script = reader.ReadToEnd();
        }
        return new DocTrWorker(await pythonUtil.StartSession(interpreter, script, lifetimeToken).ConfigureAwait(false));
    }

    public async ValueTask Initialize(DocTrOptions options, CancellationToken cancellationToken)
    {
        using JsonDocument response = await Exchange(JsonSerializer.Serialize(options, DocTrJsonContext.Default.DocTrOptions), cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.TryGetProperty("ready", out JsonElement ready) || ready.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException("docTR did not report readiness.");
    }

    public async ValueTask<DocTrResult> Recognize(DocTrRequest request, CancellationToken cancellationToken)
    {
        using JsonDocument response = await Exchange(JsonSerializer.Serialize(request, DocTrJsonContext.Default.DocTrRequest), cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.TryGetProperty("result", out JsonElement result))
            throw new InvalidOperationException("docTR returned an invalid response.");
        return result.Deserialize(DocTrJsonContext.Default.DocTrResult) ?? throw new InvalidOperationException("docTR returned an empty result.");
    }

    private async ValueTask<JsonDocument> Exchange(string input, CancellationToken cancellationToken)
    {
        JsonDocument response = JsonDocument.Parse(await _session.Exchange(input, cancellationToken).ConfigureAwait(false));
        if (response.RootElement.TryGetProperty("error", out JsonElement error))
        {
            string? message = error.GetString();
            response.Dispose();
            throw new InvalidOperationException($"docTR OCR failed: {message}");
        }
        return response;
    }

    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
