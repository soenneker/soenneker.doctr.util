using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.DocTr.Util.Models;

namespace Soenneker.DocTr.Util.Tests;

public sealed class DocTrWorkerTests
{
    // Exercise the actual embedded worker and process protocol without downloading models in routine tests.
    internal static string GetScript()
    {
        using Stream resource = typeof(DocTrUtil).Assembly.GetManifestResourceStream("Soenneker.DocTr.Util.Resources.doctr_worker.py")!;
        using var reader = new StreamReader(resource);
        return """
               import contextlib, os, sys, types
               class Document:
                   def __init__(self, paths): self.paths = paths
                   def render(self): return "\n".join(self.paths)
                   def export(self):
                       return {"pages": [{"page_idx": i, "dimensions": [100, 200],
                           "orientation": {"value": None, "confidence": None},
                           "language": {"value": "en", "confidence": 0.95},
                           "blocks": [{"objectness_score": 0.9, "geometry": [[0,0],[1,1]],
                               "lines": [{"objectness_score": 0.9, "words": [{"value": p,
                                   "confidence": 0.98, "objectness_score": 0.9,
                                   "crop_orientation": {"value": 0, "confidence": None},
                                   "geometry": [[0,0],[1,0],[1,1],[0,1]]}]}]}]}
                           for i, p in enumerate(self.paths)]}
               class Model:
                   def to(self, device): return self
                   def eval(self): return self
                   def __call__(self, paths):
                       if paths == ["fail"]: raise ValueError("bad image")
                       if paths == ["hang"]:
                           import time
                           time.sleep(60)
                       print("diagnostic output")
                       os.write(1, b"native diagnostic output\n")
                       return Document(paths)
               def predictor(**kwargs):
                   assert kwargs["pretrained"] is True
                   assert kwargs["det_arch"] == "fast_base"
                   print("loading model")
                   return Model()
               class DocumentFile:
                   @staticmethod
                   def from_pdf(path): return ["PDF:" + path]
                   @staticmethod
                   def from_images(paths): return paths
               sys.modules["torch"] = types.SimpleNamespace(device=lambda x: x, inference_mode=contextlib.nullcontext)
               sys.modules["doctr"] = types.ModuleType("doctr")
               sys.modules["doctr.io"] = types.SimpleNamespace(DocumentFile=DocumentFile)
               sys.modules["doctr.models"] = types.SimpleNamespace(ocr_predictor=predictor)

               """ + reader.ReadToEnd();
    }

    [Test]
    public async Task ReusesWorkerAndPreservesUnicodePageOrderAndRotatedGeometry(CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var worker = await DocTrWorker.Start(PythonTestUtil.CreateUtil(), await PythonTestUtil.GetInterpreter(), cancellationToken, GetScript());
        await worker.Initialize(new DocTrOptions(), timeout.Token);
        string[] paths = ["C:\\folder with spaces\\résumé \"one\".png", "/tmp/中文.png"];
        DocTrResult result = await worker.Recognize(new DocTrRequest { Kind = "images", Paths = paths }, timeout.Token);
        await Assert.That(result.Text).IsEqualTo(string.Join("\n", paths));
        await Assert.That(result.Document.Pages.Length).IsEqualTo(2);
        await Assert.That(result.Document.Pages[1].PageIndex).IsEqualTo(1);
        DocTrWord word = result.Document.Pages[1].Blocks[0].Lines[0].Words[0];
        await Assert.That(word.Value).IsEqualTo(paths[1]);
        await Assert.That(word.Geometry.Length).IsEqualTo(4);
        await Assert.That(word.Confidence).IsEqualTo(0.98);
        await Assert.That(word.ObjectnessScore).IsEqualTo(0.9);
        await Assert.That(result.Document.Pages[0].Orientation!.Value).IsNull();
        result = await worker.Recognize(new DocTrRequest { Kind = "pdf", Paths = ["/tmp/test.PDF"] }, timeout.Token);
        await Assert.That(result.Text).IsEqualTo("PDF:/tmp/test.PDF");
    }

    [Test]
    public async Task PythonFailureIsReturnedWithItsCause(CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var worker = await DocTrWorker.Start(PythonTestUtil.CreateUtil(), await PythonTestUtil.GetInterpreter(), cancellationToken, GetScript());
        await worker.Initialize(new DocTrOptions(), timeout.Token);
        try
        {
            await worker.Recognize(new DocTrRequest { Kind = "images", Paths = ["fail"] }, timeout.Token);
            throw new Exception("Expected a Python error.");
        }
        catch (InvalidOperationException error)
        {
            await Assert.That(error.Message).Contains("ValueError: bad image");
        }
    }

    [Test]
    public async Task UnexpectedExitIncludesStderr(CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var worker = await DocTrWorker.Start(PythonTestUtil.CreateUtil(), await PythonTestUtil.GetInterpreter(), cancellationToken, "import sys; sys.stdin.readline(); print('missing dependency', file=sys.stderr); sys.exit(7)");
        try
        {
            await worker.Initialize(new DocTrOptions(), timeout.Token);
            throw new Exception("Expected an unexpected process exit.");
        }
        catch (InvalidOperationException error)
        {
            await Assert.That(error.Message).Contains("code 7");
            await Assert.That(error.Message).Contains("missing dependency");
        }
    }

    [Test]
    public async Task CancellationAndDisposalStopAHungWorker(CancellationToken cancellationToken)
    {
        using var startup = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var worker = await DocTrWorker.Start(PythonTestUtil.CreateUtil(), await PythonTestUtil.GetInterpreter(), cancellationToken, GetScript());
        try
        {
            await worker.Initialize(new DocTrOptions(), startup.Token);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            try
            {
                await worker.Recognize(new DocTrRequest { Kind = "images", Paths = ["hang"] }, cancellation.Token);
                throw new Exception("Expected cancellation.");
            }
            catch (OperationCanceledException) { }
        }
        finally
        {
            await worker.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
