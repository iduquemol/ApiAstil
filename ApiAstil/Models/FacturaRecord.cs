namespace ApiAstil.Models
{
    public record FacturaRecord
    {
        public bool Marca { get; init; }
        public DateOnly Fecha { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public int Numero { get; init; }
        public string NitCliente { get; init; } = string.Empty;
        public string NombreCliente { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public int Estado { get; init; }
    }
}
