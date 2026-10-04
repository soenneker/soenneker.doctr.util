using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.DocTr.Util.Models;

namespace Soenneker.DocTr.Util.Abstract;

/// <summary>
/// A cross-platform docTR OCR utility for .NET using Python.
/// </summary>
public interface IDocTrUtil : IAsyncDisposable
{
    /// <summary>
    /// Prepares the Python environment and verifies docTR. By default, dependencies are installed in an isolated
    /// virtual environment. Python itself is only installed when explicitly enabled in <see cref="DocTrOptions"/>.
    /// OCR calls also perform this initialization automatically, once per utility instance.
    /// </summary>
    /// <returns>The absolute path to the interpreter used for OCR.</returns>
    ValueTask<string> EnsureInstalled(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recognizes a local PDF or image, returning rendered text and structured OCR data.
    /// The first call loads pretrained models and may download their weights. Subsequent calls reuse the models
    /// unless <see cref="DocTrOptions.ReuseModels"/> is disabled.
    /// Requests on one instance are serialized. Cancellation or timeout stops the worker; the next call restarts it.
    /// </summary>
    /// <param name="path">Path to an existing PDF or image file.</param>
    /// <param name="cancellationToken">Cancels initialization, model loading, and inference.</param>
    ValueTask<DocTrResult> Recognize(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recognizes local images as an ordered, multi-page document. At least one image is required.
    /// Uses the same initialization, worker reuse, and cancellation behavior as <see cref="Recognize"/>.
    /// </summary>
    /// <param name="paths">Image paths in page order; PDF files are not accepted.</param>
    /// <param name="cancellationToken">Cancels initialization, model loading, and inference.</param>
    ValueTask<DocTrResult> RecognizeImages(IEnumerable<string> paths, CancellationToken cancellationToken = default);
}
