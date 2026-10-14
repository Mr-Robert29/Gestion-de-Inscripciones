using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jardin.Usuarios.Application.Abstracciones;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Jardin.Usuarios.Infrastructure.Tokens;

public class GeneradorTokenJwt : IGeneradorTokenJwt
{
    private readonly ConfiguracionJwt _configuracion;

    public GeneradorTokenJwt(IOptions<ConfiguracionJwt> opciones)
    {
        _configuracion = opciones.Value;
    }

    public (string Token, DateTimeOffset Expiracion) GenerarToken(
        Guid usuarioId,
        string correoElectronico,
        string nombre,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permisos,
        IReadOnlyList<Guid> institucionesPermitidas)
    {
        var expiracion = DateTimeOffset.UtcNow.AddMinutes(_configuracion.MinutosExpiracion);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, correoElectronico),
            new(JwtRegisteredClaimNames.Name, nombre),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var rol in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        foreach (var permiso in permisos)
        {
            claims.Add(new Claim("permiso", permiso));
        }

        foreach (var instId in institucionesPermitidas)
        {
            claims.Add(new Claim("institucion_id", instId.ToString()));
        }

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion.ClaveSecreta));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiracion.UtcDateTime,
            Issuer = _configuracion.Emisor,
            Audience = _configuracion.Audiencia,
            SigningCredentials = credenciales
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);
        var tokenString = handler.WriteToken(securityToken);

        return (tokenString, expiracion);
    }
}
