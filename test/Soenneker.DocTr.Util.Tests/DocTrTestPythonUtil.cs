using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Python.Util.Abstract;

namespace Soenneker.DocTr.Util.Tests;

internal sealed class DocTrTestPythonUtil(string interpreter) : IPythonUtil
{
    public int SessionCount { get; private set; }
    public ValueTask<string> GetPythonPath(string pythonCommand = "python", CancellationToken cancellationToken = default) => ValueTask.FromResult(interpreter);
    public ValueTask<string> EnsureInstalled(string minVersion = "3.11", bool installIfMissing = true, CancellationToken cancellationToken = default) => ValueTask.FromResult(interpreter);
    public ValueTask TryInstall(Version min, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<string> Run(string pythonPath, IEnumerable<string> arguments, TimeSpan? timeout = null, CancellationToken cancellationToken = default) => ValueTask.FromResult(string.Empty);
    public ValueTask<string> RunScript(string pythonPath, string script, IEnumerable<string>? arguments = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<IPythonSession> StartSession(string pythonPath, string script, CancellationToken cancellationToken = default)
    {
        SessionCount++;
        return PythonTestUtil.CreateUtil().StartSession(pythonPath, DocTrWorkerTests.GetScript(), cancellationToken);
    }
    public ValueTask<string> EnsureVirtualEnvironment(string pythonPath, string directory, TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask InstallPackages(string pythonPath, IEnumerable<string> packages, string? indexUrl = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
