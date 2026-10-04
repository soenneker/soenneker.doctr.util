using System.Text.Json.Serialization;

namespace Soenneker.DocTr.Util.Models;

/// <summary>A text line containing words.</summary>
public sealed class DocTrLine
{
    /// <summary>Normalized page coordinates: two [x,y] corners, or four [x,y] polygon vertices.</summary>
    public double[][] Geometry { get; init; } = [];

    /// <summary>Detection confidence.</summary>
    [JsonPropertyName("objectness_score")]
    public double ObjectnessScore { get; init; }

    /// <summary>Recognized words.</summary>
    public DocTrWord[] Words { get; init; } = [];
}
