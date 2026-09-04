using ApiAstil.Models;
using ApiAstil.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAstil.Controllers
{
    [ApiController]
    [Route("api/facturas")]
    public class FacturasController : ControllerBase
    {
        private readonly IFacturasRepository _facturasRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public FacturasController(
            IFacturasRepository facturasRepository,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _facturasRepository = facturasRepository;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Envía el XML de una factura a Factura1
        /// </summary>
        private async Task<HttpResponseMessage> EnviarFacturaApiAsync(string xmlContent, string token)
        {
            var baseUrl = _configuration["Factura1:BaseUrl"];
            var requestUrl = $"{baseUrl}/v2/factura";

            // 1. Convertir el contenido del XML a Base64
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
            var base64Xml = Convert.ToBase64String(xmlBytes);

            // 2. Armar el cuerpo del JSON
            var payload = new Factura1SendRequest
            {
                Sucursal = _configuration["Factura1:Sucursal"] ?? "1", // Ajusta el valor según corresponda
                Base64doc = base64Xml
            };

            // 3. Configurar la petición con la cabecera Authorization
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            // Si el proveedor pide el formato Bearer estándar:
            request.Headers.TryAddWithoutValidation("Authorization", token);
            // Si exige la cadena directa sin "Bearer ", usa esta alternativa en su lugar:
            // request.Headers.TryAddWithoutValidation("Authorization", token);

            request.Content = JsonContent.Create(payload);

            // 4. Enviar solicitud
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Obtiene las facturas en un rango de fechas
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FacturaRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<FacturaRecord>>> GetFacturas(
            [FromQuery] DateOnly? fechaIni,
            [FromQuery] DateOnly? fechaFin)
        {
            if (fechaIni is null || fechaFin is null)
            {
                return BadRequest(new { mensaje = "fechaIni y fechaFin son requeridos." });
            }

            if (fechaIni > fechaFin)
            {
                return BadRequest(new { mensaje = "fechaIni no puede ser posterior a fechaFin." });
            }

            var facturas = await _facturasRepository.GetFacturasAsync(fechaIni.Value, fechaFin.Value);
            return Ok(facturas);
        }
    

        /// <summary>
        /// Genera el XML de una factura a partir de su Folio
        /// </summary>
        [HttpGet("generar-xml/{folio}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<string>> GenerarFacturaXml(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio))
            {
                return BadRequest(new { mensaje = "El folio es requerido." });
            }

            var xmlData = await _facturasRepository.GenerarFacturaXmlAsync(folio);

            if (string.IsNullOrEmpty(xmlData))
            {
                return NotFound(new { mensaje = $"No se encontró información o XML para el folio: {folio}" });
            }

            return Ok(xmlData);
        }

        [HttpGet("probar-token")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> ProbarToken()
        {
            var token = await CallExternalApiAsync();

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { mensaje = "No se pudo obtener el token de Factura1. Revisa las credenciales o la URL en appsettings.json." });
            }

            return Ok(new { token });
        }

        private async Task<string?> CallExternalApiAsync()
        {
            var baseUrl = _configuration["Factura1:BaseUrl"];
            var authEndpoint = _configuration["Factura1:AuthEndpoint"];
            var username = _configuration["Factura1:Username"];
            var password = _configuration["Factura1:Password"];

            var authPayload = new Factura1AuthRequest
            {
                Username = username ?? string.Empty,
                Password = password ?? string.Empty
            };

            var requestUrl = $"{baseUrl}{authEndpoint}";
            var response = await _httpClient.PostAsJsonAsync(requestUrl, authPayload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Factura1AuthResponse>();
                return result?.Token;
            }

            return null;
        }

        [HttpPost("enviar-xml/{folio}")]
        public async Task<IActionResult> EnviarFacturaXml(string folio)
        {
            // 1. Obtener XML del repositorio
            var xmlData = await _facturasRepository.GenerarFacturaXmlAsync(folio);
            if (string.IsNullOrEmpty(xmlData))
            {
                return NotFound(new { mensaje = $"No se encontró XML para el folio: {folio}" });
            }

            // 2. Obtener Token
            var token = await CallExternalApiAsync();
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { mensaje = "No se pudo autenticar con Factura1." });
            }

            // 3. Enviar a Factura1
            var response = await EnviarFacturaApiAsync(xmlData, token);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { mensaje = "Factura enviada correctamente", respuesta = responseBody });
            }

            return StatusCode((int)response.StatusCode, new { mensaje = "Error al enviar factura", detalle = responseBody });
        }

    }
}



