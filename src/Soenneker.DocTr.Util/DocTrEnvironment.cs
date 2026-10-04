using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Python.Util.Abstract;

namespace Soenneker.DocTr.Util;

internal static class DocTrEnvironment
{
    public static async ValueTask<string> Prepare(IPythonUtil pythonUtil, DocTrOptions options, CancellationToken cancellationToken)
    {
        string python = options.PythonCommand is { } command
            ? await pythonUtil.GetPythonPath(command, cancellationToken).ConfigureAwait(false)
            : await pythonUtil.EnsureInstalled(options.PythonVersion, options.InstallPythonIfMissing, cancellationToken).ConfigureAwait(false);

        const string versionCheck = "import sys; assert sys.version_info >= (3, 11), 'docTR requires Python 3.11 or newer'";
        await pythonUtil.Run(python, ["-c", versionCheck], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (options.InstallDependencies)
        {
            string directory = Path.GetFullPath(options.EnvironmentDirectory);
            Directory.CreateDirectory(directory);
            // Coordinate pip and venv creation across utility instances and processes sharing this directory.
            await using FileStream installationLock = await AcquireLock(directory + ".install.lock", cancellationToken).ConfigureAwait(false);
            python = await pythonUtil.EnsureVirtualEnvironment(python, directory, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (options.Device == "cpu")
                await pythonUtil.InstallPackages(python, ["torch", "torchvision"], "https://download.pytorch.org/whl/cpu",
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            await pythonUtil.InstallPackages(python, [$"python-doctr=={options.DocTrVersion}"],
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await pythonUtil.Run(python, ["-c", "from doctr.io import DocumentFile; from doctr.models import ocr_predictor; import torch"],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return python;
    }

    private static async ValueTask<FileStream> AcquireLock(string path, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
