namespace Soenneker.DocTr.Util;

internal sealed class DocTrRequest
{
    public required string Kind { get; init; }
    public required string[] Paths { get; init; }
}
