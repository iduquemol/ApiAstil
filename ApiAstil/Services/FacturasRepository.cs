using ApiAstil.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ApiAstil.Services
{
    public class FacturasRepository : IFacturasRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _connectionString;

        public FacturasRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<string?> GenerarFacturaXmlAsync(string folio)
        {
            using var connection = new SqlConnection(_connectionString);

            var xmlResult = await connection.QueryFirstOrDefaultAsync<string>(
                "usr_sp_itq_GenerarFacturaXML",
                new { Folio = folio },
                commandType: CommandType.StoredProcedure
            );

            return xmlResult;
        }

        public async Task<IEnumerable<FacturaRecord>> GetFacturasAsync(DateOnly fechaIni, DateOnly fechaFin)
        {
            using var connection = new SqlConnection(_connectionString);

            // usr_sp_itq_consulta_fe returns a single row with one JSON-text
            // column ("venta"), not a relational rowset - so it's read as a
            // scalar-per-row string rather than mapped with QueryAsync<T>.
            // Dapper also has no built-in DateOnly parameter support, hence
            // the DateTime conversion below.
            var venta = await connection.QuerySingleOrDefaultAsync<string?>(
                "usr_sp_itq_consulta_fe",
                new
                {
                    fecha_ini = fechaIni.ToDateTime(TimeOnly.MinValue),
                    fecha_fin = fechaFin.ToDateTime(TimeOnly.MinValue)
                },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrWhiteSpace(venta))
            {
                return Enumerable.Empty<FacturaRecord>();
            }

            var rawFacturas = JsonSerializer.Deserialize<List<FacturaJson>>(venta, JsonOptions)
                ?? new List<FacturaJson>();

            return rawFacturas.Select(raw => new FacturaRecord
            {
                Marca = raw.Marca != 0,
                Fecha = raw.Fecha,
                Tipo = raw.Tipo.Trim(),
                Numero = raw.Numero,
                NitCliente = raw.NitCliente,
                NombreCliente = raw.NombreCliente,
                Valor = raw.Valor,
                Estado = raw.Estado
            });
        }
    }
}
