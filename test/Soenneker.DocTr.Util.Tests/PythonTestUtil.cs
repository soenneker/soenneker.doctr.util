using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Python.Util.Abstract;
using Soenneker.Python.Util.Registrars;

namespace Soenneker.DocTr.Util.Tests;

internal static class PythonTestUtil
{
    public static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPythonUtilAsSingleton();
        return services.BuildServiceProvider();
    }
    public static async ValueTask<string> GetInterpreter()
    {
        if (Environment.GetEnvironmentVariable("DOCTR_TEST_PYTHON") is { Length: > 0 } interpreter)
            return interpreter;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using ServiceProvider provider = CreateProvider();
        return (await provider.GetRequiredService<IPythonUtil>().Run(OperatingSystem.IsWindows() ? "py" : "python3",
            OperatingSystem.IsWindows()
                ? ["-3.11", "-c", "import sys; print(sys.executable)"]
                : ["-c", "import sys; print(sys.executable)"], cancellationToken: timeout.Token)).Trim();
    }
}
