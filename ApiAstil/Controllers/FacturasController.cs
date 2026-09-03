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

        public FacturasController(IFacturasRepository facturasRepository)
        {
            _facturasRepository = facturasRepository;
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
    }
}
