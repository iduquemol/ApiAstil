namespace ApiAstil.Models
{
    /// <summary>
    /// Raw shape of one element in the JSON array returned by
    /// usr_sp_itq_consulta_fe's single "venta" column. Mirrors the SP's
    /// actual (inconsistent) key casing and untrimmed/numeric values;
    /// FacturasRepository maps this to the public FacturaRecord.
    /// </summary>
    internal record FacturaJson
    {
        public int Marca { get; init; }
        public DateOnly Fecha { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public int Numero { get; init; }
        public string NitCliente { get; init; } = string.Empty;
        public string NombreCliente { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public int Estado { get; init; }
    }
}
