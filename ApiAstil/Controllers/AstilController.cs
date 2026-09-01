using ApiAstil.Services;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace ApiAstil.Controllers
{
    [ApiController]
    [Route("api/facturacion")]
    public class AstilController : ControllerBase
    {
        private readonly ISqlDataService _sqlDataService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AstilController(ISqlDataService sqlDataService,
             IHttpClientFactory httpClientFactory,
             IConfiguration configuration)
        {
            _sqlDataService = sqlDataService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene todos los productos
        /// </summary>
        /// <returns>Lista de productos</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Producto>> GetProductos()
        {
            var productos = new List<Producto>
        {
            new Producto { Id = 1, Nombre = "Laptop", Precio = 1200.50m },
            new Producto { Id = 2, Nombre = "Mouse", Precio = 25.99m }
        };
            return Ok(productos);
        }

        [HttpPost("crearegistro")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreaRegistro([FromBody] JsonElement requestBody)
        {
            // Convertir el JsonElement a string para pasarlo al SP
            string jsonString = requestBody.GetRawText();

            var rowsAffected = await _sqlDataService.ExecuteNonQueryAsync(
                "sp_Create_peticionVentaExterna",
                ("@ventaExterna", jsonString));

            return Content("\"1\"", "application/json");
        }

        [HttpPost("procesar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Procesar([FromBody] JsonElement requestBody)
        {
            // Extraer el valor de num_doc
            //string numDoc = requestBody.GetProperty("num_doc").GetString() ?? string.Empty;
            string jsonString = requestBody.GetRawText();

            // Usar ExecuteStoredProcedureAsync para obtener las dos columnas del SP
            var dataTable = await _sqlDataService.ExecuteStoredProcedureAsync(
                "sp_Create_ventaExterna",
                ("@ventaExterna", jsonString));

            // Verificar si hay resultados
            if (dataTable.Rows.Count == 0)
            {
                return BadRequest(new { mensaje = "El procedimiento no retornó resultados" });
            }

            // Obtener la primera fila (asumiendo que el SP retorna solo una fila)
            var row = dataTable.Rows[0];

            var idVenta = row["idVenta"] != DBNull.Value ? Convert.ToInt32(row["idVenta"]) : 0;
            var idMetodoDian = row["idMetodoDian"] != DBNull.Value ? Convert.ToInt32(row["idMetodoDian"]) : 0;

            var apiResponse = await InvocarApiExterna(idVenta, idMetodoDian);

            return Content(apiResponse ?? "{}", "application/json");
        }

        /// <summary>
        /// Método privado para invocar la API externa
        /// </summary>
        private async Task<string> InvocarApiExterna(int idventa, int idmetododian)
        {
            try
            {
                // Crear HttpClient desde el factory
                var httpClient = _httpClientFactory.CreateClient();

                // Obtener configuración de la API externa
                var baseUrl = _configuration["ExternalApi:BaseUrl"] ?? "https://api.ejemplo.com";
                var endpoint = String.Empty;
                if (idmetododian == 1)
                {
                    endpoint = _configuration["ExternalApi:Endpoint"] ?? "/enviar-dian";
                }
                else if (idmetododian == 3)
                {
                    endpoint = _configuration["ExternalApi:EndpointNC"] ?? "/enviar-nota-dian";
                }

                var apiKey = _configuration["ExternalApi:ApiKey"];

                // Configurar la URL completa
                var url = $"{baseUrl}{endpoint}";

                // Configurar headers
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Agregar API Key si existe
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    // O si usa otro formato:
                    // httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }

                // Crear el objeto JSON con los parámetros
                var requestData = new
                {
                    idventa,
                    idmetododian
                };

                // Serializar a JSON
                var jsonData = JsonSerializer.Serialize(requestData);

                // Crear el contenido JSON
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Hacer la llamada POST a la API externa
                var response = await httpClient.PostAsync(url, content);

                // Verificar si la respuesta es exitosa
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
                else
                {
                    // Manejar error de la API externa
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Serialize(new
                    {
                        error = true,
                        statusCode = (int)response.StatusCode,
                        mensaje = "Error al invocar la API externa",
                        detalle = errorContent,
                        url = url,
                        content = jsonData
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                // Manejar errores de conexión
                return JsonSerializer.Serialize(new
                {
                    error = true,
                    mensaje = "Error de conexión con la API externa",
                    detalle = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Manejar otros errores
                return JsonSerializer.Serialize(new
                {
                    error = true,
                    mensaje = "Error inesperado al invocar la API externa",
                    detalle = ex.Message                    
                });
            }
        }
    }

    public record Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
    }
}
