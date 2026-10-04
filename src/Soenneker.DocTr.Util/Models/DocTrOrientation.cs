namespace Soenneker.DocTr.Util.Models;

/// <summary>A predicted rotation, with nullable values when orientation was not estimated.</summary>
public sealed class DocTrOrientation
{
    /// <summary>Rotation in degrees.</summary>
    public double? Value { get; init; }

    /// <summary>Prediction confidence, when available.</summary>
    public double? Confidence { get; init; }
}
