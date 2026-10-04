using System.Text.Json.Serialization;

namespace Soenneker.DocTr.Util.Models;

/// <summary>A page and its recognized blocks.</summary>
public sealed class DocTrPage
{
    /// <summary>Zero-based page index.</summary>
    [JsonPropertyName("page_idx")]
    public int PageIndex { get; init; }

    /// <summary>Page dimensions in pixels, ordered as height then width.</summary>
    public int[] Dimensions { get; init; } = [];

    /// <summary>Estimated rotation, when requested.</summary>
    public DocTrOrientation? Orientation { get; init; }

    /// <summary>Estimated language, when requested.</summary>
    public DocTrLanguage? Language { get; init; }

    /// <summary>Recognized blocks in reading order.</summary>
    public DocTrBlock[] Blocks { get; init; } = [];
}
