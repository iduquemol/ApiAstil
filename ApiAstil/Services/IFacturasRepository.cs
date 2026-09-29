using ApiAstil.Models;

namespace ApiAstil.Services
{
    public interface IFacturasRepository
    {
        Task<IEnumerable<FacturaRecord>> GetFacturasAsync(DateOnly fechaIni, DateOnly fechaFin);
        Task<string?> GenerarFacturaXmlAsync(string folio);

        Task GuardarRespuestaFacturaAsync(
            int anoDoc,
            int perDoc,
            string tipo,
            int numero,
            string? codigoError,
            string? valorError,
            string? cufe,
            string docRequest);
    }
}
