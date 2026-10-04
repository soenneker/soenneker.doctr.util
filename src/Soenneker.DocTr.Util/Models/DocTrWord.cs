using System.Text.Json.Serialization;

namespace Soenneker.DocTr.Util.Models;

/// <summary>A recognized word and its location and confidence.</summary>
public sealed class DocTrWord
{
    /// <summary>Recognized text.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Text recognition confidence from zero to one.</summary>
    public double Confidence { get; init; }

    /// <summary>Normalized page coordinates: two [x,y] corners, or four [x,y] polygon vertices.</summary>
    public double[][] Geometry { get; init; } = [];

    /// <summary>Text detection confidence.</summary>
    [JsonPropertyName("objectness_score")]
    public double ObjectnessScore { get; init; }

    /// <summary>Estimated word crop rotation.</summary>
    [JsonPropertyName("crop_orientation")]
    public DocTrOrientation? CropOrientation { get; init; }
}
