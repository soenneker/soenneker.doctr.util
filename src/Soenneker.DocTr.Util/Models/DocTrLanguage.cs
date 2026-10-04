namespace Soenneker.DocTr.Util.Models;

/// <summary>A predicted language, with nullable values when language was not estimated.</summary>
public sealed class DocTrLanguage
{
    /// <summary>Detected language code.</summary>
    public string? Value { get; init; }

    /// <summary>Prediction confidence, when available.</summary>
    public double? Confidence { get; init; }
}
