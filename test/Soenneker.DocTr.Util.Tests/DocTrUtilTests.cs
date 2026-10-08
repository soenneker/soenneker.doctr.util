using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.DocTr.Util.Abstract;
using Soenneker.DocTr.Util.Registrars;
using System.Threading;

namespace Soenneker.DocTr.Util.Tests;

public sealed class DocTrUtilTests
{
    [Test]
    public async Task ModelReuseIsEnabledByDefaultAndCanBeDisabled(CancellationToken cancellationToken)
    {
        string path = Path.GetTempFileName();
        try
        {
            foreach (bool reuse in new[] { true, false })
            {
                var python = new DocTrTestPythonUtil(await PythonTestUtil.GetInterpreter());
                await using var util = new DocTrUtil(python, new DocTrOptions { InstallDependencies = false, ReuseModels = reuse });
                await util.Recognize(path, cancellationToken: cancellationToken);
                await util.Recognize(path, cancellationToken: cancellationToken);
                await Assert.That(python.SessionCount).IsEqualTo(reuse ? 1 : 2);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task MissingDocumentDoesNotStartPython(CancellationToken cancellationToken)
    {
        await using var util = new DocTrUtil(new UnavailablePythonUtil());
        try
        {
            await util.Recognize(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"), cancellationToken: cancellationToken);
            throw new Exception("Expected a missing document error.");
        }
        catch (FileNotFoundException) { }
    }

    [Test]
    public async Task EmptyImagesAreRejected(CancellationToken cancellationToken)
    {
        await using var util = new DocTrUtil(new UnavailablePythonUtil());
        try
        {
            await util.RecognizeImages([], cancellationToken: cancellationToken);
            throw new Exception("Expected an empty image collection error.");
        }
        catch (ArgumentException) { }
    }

    [Test]
    public async Task PdfCannotBePassedAsAnImage(CancellationToken cancellationToken)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".PDF");
        await File.WriteAllTextAsync(path, "not a real PDF", cancellationToken: cancellationToken);
        try
        {
            await using var util = new DocTrUtil(new UnavailablePythonUtil());
            try
            {
                await util.RecognizeImages([path], cancellationToken: cancellationToken);
                throw new Exception("Expected a PDF validation error.");
            }
            catch (ArgumentException) { }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task DisposalIsIdempotentAndRejectsNewCalls(CancellationToken cancellationToken)
    {
        var util = new DocTrUtil(new UnavailablePythonUtil());
        await Task.WhenAll(util.DisposeAsync().AsTask(), util.DisposeAsync().AsTask());
        try
        {
            await util.EnsureInstalled(cancellationToken: cancellationToken);
            throw new Exception("Expected a disposed utility error.");
        }
        catch (ObjectDisposedException) { }
    }

    [Test]
    public async Task RegistrationPreservesOptionsAndResolvesBothLifetimes(CancellationToken cancellationToken)
    {
        foreach (bool scoped in new[] { false, true })
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var options = new DocTrOptions { DetectionArchitecture = "db_resnet50" };
            services.AddSingleton(options);
            if (scoped)
                services.AddDocTrUtilAsScoped();
            else
                services.AddDocTrUtilAsSingleton();
            await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            await Assert.That(scope.ServiceProvider.GetRequiredService<IDocTrUtil>()).IsNotNull();
            await Assert.That(scope.ServiceProvider.GetRequiredService<DocTrOptions>()).IsSameReferenceAs(options);
        }
    }
}
