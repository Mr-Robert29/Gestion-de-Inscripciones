using System.Security.Claims;
using Jardin.Usuarios.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AutenticacionController : ControllerBase
{
    private readonly IModuloUsuarios _moduloUsuarios;

    public AutenticacionController(IModuloUsuarios moduloUsuarios)
    {
        _moduloUsuarios = moduloUsuarios;
    }

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAutenticacion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion([FromBody] IniciarSesionPeticion peticion, CancellationToken cancellationToken)
    {
        var resultado = await _moduloUsuarios.IniciarSesionAsync(peticion, cancellationToken);
        if (resultado is null)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas o cuenta de usuario deshabilitada." });
        }

        return Ok(resultado);
    }

    [HttpGet("mi-acceso")]
    [Authorize]
    [ProducesResponseType(typeof(DatosUsuarioSesion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMiAcceso(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var usuarioId))
        {
            return Unauthorized(new { mensaje = "Identificador de usuario inválido en el token." });
        }

        var sesion = await _moduloUsuarios.ObtenerSesionActualAsync(usuarioId, cancellationToken);
        if (sesion is null)
        {
            return Unauthorized(new { mensaje = "No se encontró la información de la cuenta activa." });
        }

        return Ok(sesion);
    }
}
