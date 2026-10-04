using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Soenneker.Python.Util;
using Soenneker.Utils.Process;

namespace Soenneker.DocTr.Util.Tests;

internal static class PythonTestUtil
{
    public static PythonUtil CreateUtil() => new(new ProcessUtil(NullLogger<ProcessUtil>.Instance),
        NullLogger<PythonUtil>.Instance, null!, null!);
    public static async ValueTask<string> GetInterpreter()
    {
        if (Environment.GetEnvironmentVariable("DOCTR_TEST_PYTHON") is { Length: > 0 } interpreter)
            return interpreter;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        return (await CreateUtil().Run(OperatingSystem.IsWindows() ? "py" : "python3",
            OperatingSystem.IsWindows()
                ? ["-3.11", "-c", "import sys; print(sys.executable)"]
                : ["-c", "import sys; print(sys.executable)"], cancellationToken: timeout.Token)).Trim();
    }
}
