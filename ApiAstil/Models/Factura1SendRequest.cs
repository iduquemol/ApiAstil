using System.Text.Json.Serialization;

namespace ApiAstil.Models
{
    public class Factura1SendRequest
    {
        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string Contrasena { get; set; } = string.Empty;

        [JsonPropertyName("sucursal")]
        public string Sucursal { get; set; } = string.Empty;

        [JsonPropertyName("base64doc")]
        public string Base64doc { get; set; } = string.Empty;
    }
}