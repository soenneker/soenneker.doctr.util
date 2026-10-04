using System.Text.Json.Serialization;
using Soenneker.DocTr.Util.Models;

namespace Soenneker.DocTr.Util;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(DocTrResult))]
[JsonSerializable(typeof(DocTrOptions))]
[JsonSerializable(typeof(DocTrRequest))]
internal sealed partial class DocTrJsonContext : JsonSerializerContext;
