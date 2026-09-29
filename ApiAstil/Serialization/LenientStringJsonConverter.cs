using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiAstil.Serialization
{
    /// <summary>
    /// Reads a JSON value of any kind (string, number, boolean, null) as a string.
    /// Used for fields on external APIs (like Factura1) whose real type has not
    /// been verified against a live response, so an unexpected JSON type does
    /// not throw a JsonException.
    /// </summary>
    public class LenientStringJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                _ => JsonDocument.ParseValue(ref reader).RootElement.GetRawText()
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
