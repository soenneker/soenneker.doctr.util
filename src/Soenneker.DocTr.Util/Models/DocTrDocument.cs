namespace Soenneker.DocTr.Util.Models;

/// <summary>A recognized document, with pages in input order.</summary>
public sealed class DocTrDocument
{
    /// <summary>Recognized pages.</summary>
    public DocTrPage[] Pages { get; init; } = [];
}
