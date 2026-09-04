using System.Text.Json.Serialization;

namespace ApiAstil.Models
{
    public class Factura1SendRequest
    {
        [JsonPropertyName("sucursal")]
        public string Sucursal { get; set; } = string.Empty;

        [JsonPropertyName("base64doc")]
        public string Base64doc { get; set; } = string.Empty;
    }
}