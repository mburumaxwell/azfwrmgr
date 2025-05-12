using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureFwrMgr;

[JsonSerializable(typeof(FirewallManagerConfig))]

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,

    // Ignore default values to reduce the data sent after serialization
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

    // Do not indent content to reduce data usage
    WriteIndented = false,

    // Use SnakeCase because it is what the server provides
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,

    Converters = [
        typeof(Tingle.Extensions.Primitives.Converters.JsonIPAddressConverter),
        typeof(Tingle.Extensions.Primitives.Converters.JsonIPNetworkConverter),
        typeof(JsonIPNetwork2Converter),
    ]
)]
internal partial class AzureFwrMgrSerializerContext : JsonSerializerContext { }

internal class JsonIPNetwork2Converter : JsonConverter<IPNetwork2>
{
    private static readonly PropertyInfo? s_JsonException_AppendPathInformation
        = typeof(JsonException).GetProperty("AppendPathInformation", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <inheritdoc/>
    public override IPNetwork2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
        {
            JsonException jsonException = new($"The JSON value could not be converted to {typeof(IPNetwork2)}.");
            s_JsonException_AppendPathInformation?.SetValue(jsonException, true);
            throw jsonException;
        }

        var value = reader.GetString()!;

        try
        {
            return IPNetwork2.Parse(value);
        }
        catch (Exception ex)
        {
            JsonException jsonException = new(
                $"The JSON value '{value}' could not be converted to {typeof(IPNetwork)}.",
                ex);
            s_JsonException_AppendPathInformation?.SetValue(jsonException, true);
            throw jsonException;
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IPNetwork2 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
