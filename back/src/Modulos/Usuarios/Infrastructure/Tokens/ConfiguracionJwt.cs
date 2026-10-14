namespace Jardin.Usuarios.Infrastructure.Tokens;

public class ConfiguracionJwt
{
    public const string Seccion = "Jwt";
    public string ClaveSecreta { get; set; } = "ClaveSecretaSuperSeguraParaJardinInscripciones2026!";
    public string Emisor { get; set; } = "Jardin.Api";
    public string Audiencia { get; set; } = "Jardin.Frontend";
    public int MinutosExpiracion { get; set; } = 15;
}
