using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = RolesPersonal.AdministradorDelSistema)]
public class UsuariosController : ControllerBase
{
    private readonly IModuloUsuarios _moduloUsuarios;

    public UsuariosController(IModuloUsuarios moduloUsuarios)
    {
        _moduloUsuarios = moduloUsuarios;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DatosUsuarioPersonal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUsuarios(CancellationToken cancellationToken)
    {
        var usuarios = await _moduloUsuarios.ListarUsuariosAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DatosUsuarioPersonal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _moduloUsuarios.ObtenerUsuarioPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return NotFound(new { mensaje = $"No se encontró el usuario con identificador '{id}'." });

        return Ok(usuario);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioPersonalPeticion peticion, CancellationToken cancellationToken)
    {
        try
        {
            var usuarioId = await _moduloUsuarios.CrearUsuarioAsync(peticion, cancellationToken);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = usuarioId }, new { id = usuarioId });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoUsuarioPeticion peticion, CancellationToken cancellationToken)
    {
        var exito = await _moduloUsuarios.CambiarEstadoUsuarioAsync(id, peticion.Activo, cancellationToken);
        if (!exito)
            return NotFound(new { mensaje = $"No se encontró el usuario con identificador '{id}'." });

        return NoContent();
    }

    [HttpPost("{id:guid}/accesos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AsignarAcceso(Guid id, [FromBody] AsignarAccesoInstitucionPeticion peticion, CancellationToken cancellationToken)
    {
        try
        {
            var exito = await _moduloUsuarios.AsignarAccesoInstitucionAsync(id, peticion, cancellationToken);
            if (!exito)
                return NotFound(new { mensaje = $"No se encontró el usuario con identificador '{id}'." });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/accesos/{institucionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevocarAcceso(Guid id, Guid institucionId, CancellationToken cancellationToken)
    {
        var exito = await _moduloUsuarios.RevocarAccesoInstitucionAsync(id, institucionId, cancellationToken);
        if (!exito)
            return NotFound(new { mensaje = "No se encontró el usuario o no poseía acceso asignado a dicha institución." });

        return NoContent();
    }
}
