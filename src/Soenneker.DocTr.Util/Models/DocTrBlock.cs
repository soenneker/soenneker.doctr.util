using System.Text.Json.Serialization;

namespace Soenneker.DocTr.Util.Models;

/// <summary>A text block containing lines.</summary>
public sealed class DocTrBlock
{
    /// <summary>Normalized page coordinates: two [x,y] corners, or four [x,y] polygon vertices for rotated text.</summary>
    public double[][] Geometry { get; init; } = [];

    /// <summary>Detection confidence.</summary>
    [JsonPropertyName("objectness_score")]
    public double ObjectnessScore { get; init; }

    /// <summary>Recognized lines.</summary>
    public DocTrLine[] Lines { get; init; } = [];
}
