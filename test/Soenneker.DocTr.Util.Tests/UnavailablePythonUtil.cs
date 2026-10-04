using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Soenneker.Python.Util.Abstract;

namespace Soenneker.DocTr.Util.Tests;

internal sealed class UnavailablePythonUtil : IPythonUtil
{
    public ValueTask<string> GetPythonPath(string pythonCommand = "python", CancellationToken cancellationToken = default)
        => throw new Exception("Python must not be started by input validation.");

    public ValueTask<string> EnsureInstalled(string minVersion = "3.11", bool installIfMissing = true, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not be started by input validation.");

    public ValueTask TryInstall(Version min, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not be installed by input validation.");

    public ValueTask<string> Run(string pythonPath, IEnumerable<string> arguments, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not run during validation.");
    public ValueTask<string> RunScript(string pythonPath, string script, IEnumerable<string>? arguments = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not run during validation.");
    public ValueTask<IPythonSession> StartSession(string pythonPath, string script, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not run during validation.");
    public ValueTask<string> EnsureVirtualEnvironment(string pythonPath, string directory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not run during validation.");
    public ValueTask InstallPackages(string pythonPath, IEnumerable<string> packages, string? indexUrl = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => throw new Exception("Python must not run during validation.");
}
