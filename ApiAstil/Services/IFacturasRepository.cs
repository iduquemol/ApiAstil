using ApiAstil.Models;

namespace ApiAstil.Services
{
    public interface IFacturasRepository
    {
        Task<IEnumerable<FacturaRecord>> GetFacturasAsync(DateOnly fechaIni, DateOnly fechaFin);
    }
}
