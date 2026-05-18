using System.Text.Json;
using System.Text.Json.Serialization;
using OpenFeature.Providers.Ofrep.Models;

namespace OpenFeature.Providers.Ofrep.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(OfrepRequest))]
[JsonSerializable(typeof(OfrepResponse<JsonElement>))]
internal sealed partial class OfrepJsonSerializerContext : JsonSerializerContext
{
}
