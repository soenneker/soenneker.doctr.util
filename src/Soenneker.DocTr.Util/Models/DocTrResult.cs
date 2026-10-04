namespace Soenneker.DocTr.Util.Models;

/// <summary>Rendered text and the hierarchical docTR export.</summary>
public sealed class DocTrResult
{
    /// <summary>Text rendered by docTR, preserving its line, block, and page separators.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Pages and their recognized text regions.</summary>
    public DocTrDocument Document { get; init; } = new();
}
