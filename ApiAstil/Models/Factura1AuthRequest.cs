namespace ApiAstil.Models
{
    public class Factura1AuthRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class Factura1AuthResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}