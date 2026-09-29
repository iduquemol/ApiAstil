using System.Text;
using System.Text.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public FacturasController(
            IFacturasRepository facturasRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _facturasRepository = facturasRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
        /// Genera el XML de una factura a partir de su Folio (diagnostico/pruebas)
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

        /// <summary>
        /// Prueba la autenticacion con Factura1 de forma independiente (diagnostico/pruebas)
        /// </summary>
        [HttpGet("probar-token")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> ProbarToken()
        {
            var token = await ObtenerTokenFactura1Async();

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { mensaje = "No se pudo obtener el token de Factura1. Revisa las credenciales o la URL en appsettings.json." });
            }

            return Ok(new { token });
        }

        /// <summary>
        /// Genera el XML de una factura, autentica con Factura1 y la envia
        /// </summary>
        [HttpPost("enviar-xml/{folio}")]
        [ProducesResponseType(typeof(Factura1SendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EnviarFacturaXml(string folio, [FromBody] EnviarFacturaRequest request)
        {
            if (request.AnoDoc is null || request.PerDoc is null || string.IsNullOrWhiteSpace(request.Tipo) || request.Numero is null)
            {
                return BadRequest(new { mensaje = "anoDoc, perDoc, tipo y numero son requeridos." });
            }

            // 1. Obtener XML del repositorio
            var xmlData = await _facturasRepository.GenerarFacturaXmlAsync(folio);
            if (string.IsNullOrEmpty(xmlData))
            {
                return NotFound(new { mensaje = $"No se encontró XML para el folio: {folio}" });
            }

            // 2. Obtener Token
            var token = await ObtenerTokenFactura1Async();
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { mensaje = "No se pudo autenticar con Factura1." });
            }

            // 3. Enviar a Factura1
            var response = await EnviarFacturaAFactura1Async(xmlData, token);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { mensaje = "Error al enviar factura", detalle = responseBody });
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<Factura1SendResponse>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is not null)
                {
                    // 4. Guardar la respuesta - no debe ocultar un envío ya exitoso si falla
                    try
                    {
                        await _facturasRepository.GuardarRespuestaFacturaAsync(
                            request.AnoDoc.Value,
                            request.PerDoc.Value,
                            request.Tipo,
                            request.Numero.Value,
                            codigoError: parsed.Error,
                            valorError: parsed.Error,
                            cufe: parsed.Cufe,
                            docRequest: xmlData);
                    }
                    catch (Exception ex)
                    {
                        parsed.Advertencia = $"No se pudo guardar la respuesta internamente: {ex.Message}";
                    }
                }

                return Ok(parsed);
            }
            catch (JsonException)
            {
                return Ok(new { mensaje = "Factura enviada, pero la respuesta de Factura1 no tiene el formato esperado", detalle = responseBody });
            }
        }

        /// <summary>
        /// Obtiene un token de autenticacion de Factura1
        /// </summary>
        private async Task<string?> ObtenerTokenFactura1Async()
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
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(requestUrl, authPayload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Factura1AuthResponse>();
                return result?.Token;
            }

            return null;
        }

        /// <summary>
        /// Envía el XML de una factura a Factura1
        /// </summary>
        private async Task<HttpResponseMessage> EnviarFacturaAFactura1Async(string xmlContent, string token)
        {
             var baseUrl = _configuration["Factura1:BaseUrl"];
            var requestUrl = $"{baseUrl}/v2/factura";

            var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
            var base64Xml = Convert.ToBase64String(xmlBytes);

            var payload = new Factura1SendRequest
            {
                Usuario = _configuration["Factura1:Username"] ?? string.Empty,
                Contrasena = _configuration["Factura1:Password"] ?? string.Empty,
                Sucursal = _configuration["Factura1:Sucursal"] ?? "1",
                Base64doc = base64Xml
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", token);
            request.Content = JsonContent.Create(payload);

            var httpClient = _httpClientFactory.CreateClient();
            return await httpClient.SendAsync(request);
        }
    }
}
