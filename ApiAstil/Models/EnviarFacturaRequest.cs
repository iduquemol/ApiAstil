namespace ApiAstil.Models
{
    public class EnviarFacturaRequest
    {
        public int? AnoDoc { get; set; }
        public int? PerDoc { get; set; }
        public string? Tipo { get; set; }
        public int? Numero { get; set; }
    }
}
