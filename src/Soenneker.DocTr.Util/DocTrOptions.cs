using System;
using System.IO;

namespace Soenneker.DocTr.Util;

/// <summary>Configures the Python environment and models for a utility instance. Register before adding the utility.</summary>
public sealed class DocTrOptions
{
    /// <summary>Python major/minor release to locate when <see cref="PythonCommand"/> is unset.</summary>
    public string PythonVersion { get; init; } = "3.11";

    /// <summary>Optional existing interpreter path or launcher command, such as "py -3.12" or "python3".</summary>
    public string? PythonCommand { get; init; }

    /// <summary>Allows Python.Util to invoke a platform package manager when the requested Python is missing.</summary>
    public bool InstallPythonIfMissing { get; init; }

    /// <summary>Creates a virtual environment and installs pinned docTR dependencies. Disable for a preconfigured interpreter.</summary>
    public bool InstallDependencies { get; init; } = true;

    /// <summary>Retains the Python session and loaded models between calls. Disable to use a fresh process per call.</summary>
    public bool ReuseModels { get; init; } = true;

    /// <summary>Virtual environment directory. Ignored when dependency installation is disabled.</summary>
    public string EnvironmentDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Soenneker", "DocTr", "venv");

    /// <summary>PyPI release to install. The default is the release supported by this wrapper.</summary>
    public string DocTrVersion { get; init; } = "1.1.0";

    /// <summary>Maximum time for environment initialization and dependency installation.</summary>
    public TimeSpan InstallationTimeout { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Maximum time per OCR call, including first-use model loading. Initialization has a separate timeout.</summary>
    public TimeSpan RecognitionTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>PyTorch device, for example "cpu" or "cuda:0". CPU installs use PyTorch's CPU wheel index.</summary>
    public string Device { get; init; } = "cpu";

    /// <summary>Text detection architecture.</summary>
    public string DetectionArchitecture { get; init; } = "fast_base";

    /// <summary>Text recognition architecture.</summary>
    public string RecognitionArchitecture { get; init; } = "crnn_vgg16_bn";

    /// <summary>Assumes horizontal text on straight pages. Disable to recognize rotated text.</summary>
    public bool AssumeStraightPages { get; init; } = true;

    /// <summary>Estimates page rotation and includes it in the result.</summary>
    public bool DetectOrientation { get; init; }

    /// <summary>Rotates pages before inference to correct uniform page rotation.</summary>
    public bool StraightenPages { get; init; }

    /// <summary>Estimates page language and includes it in the result.</summary>
    public bool DetectLanguage { get; init; }
}
