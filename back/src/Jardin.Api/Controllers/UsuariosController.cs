using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Configuracion.Contracts;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = RolesPersonal.AdministradorDelSistema)]
public class UsuariosController : ControllerBase
{
    private readonly IModuloUsuarios _moduloUsuarios;
    private readonly IModuloConfiguracion _moduloConfiguracion;

    public UsuariosController(IModuloUsuarios moduloUsuarios, IModuloConfiguracion moduloConfiguracion)
    {
        _moduloUsuarios = moduloUsuarios;
        _moduloConfiguracion = moduloConfiguracion;
    }

    [HttpPost("personal-institucion")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearPersonalInstitucion(
        [FromBody] CrearPersonalInstitucionRootPeticion peticion,
        CancellationToken cancellationToken)
    {
        if (await _moduloConfiguracion.ConsultarInstitucionAsync(peticion.InstitucionId, cancellationToken) is null)
            return NotFound(new { mensaje = "No se encontró la institución seleccionada." });
        try
        {
            var solicitud = new CrearCuentaInstitucionPeticion(
                peticion.Nombre,
                peticion.CorreoElectronico,
                peticion.ClaveInicial,
                peticion.Dni);
            var usuarioId = await _moduloUsuarios.CrearCuentaInstitucionAsync(
                peticion.InstitucionId,
                solicitud,
                peticion.Rol,
                ActorId(),
                cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { id = usuarioId });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DatosUsuarioPersonal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUsuarios(CancellationToken cancellationToken)
    {
        var usuarios = await _moduloUsuarios.ListarUsuariosAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("auditoria")]
    [ProducesResponseType(typeof(IReadOnlyList<DatosAuditoriaPersonal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAuditoria(CancellationToken cancellationToken)
        => Ok(await _moduloUsuarios.ListarAuditoriaAsync(cancellationToken));

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

    [HttpGet("{id:guid}/dni")]
    [ProducesResponseType(typeof(DatosDniUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDni(Guid id, CancellationToken cancellationToken)
    {
        var datos = await _moduloUsuarios.ObtenerDniUsuarioAsync(id, cancellationToken);
        return datos is null
            ? NotFound(new { mensaje = "No se encontró la cuenta solicitada." })
            : Ok(datos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioPersonalPeticion peticion, CancellationToken cancellationToken)
    {
        try
        {
            var usuarioId = await _moduloUsuarios.CrearUsuarioAsync(peticion, cancellationToken);
            await _moduloUsuarios.RegistrarAuditoriaAsync(ActorId(), usuarioId, null, "Cuenta Root creada", "Se creó una cuenta global de Administrador del sistema.", cancellationToken);
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
        try
        {
            var exito = await _moduloUsuarios.CambiarEstadoUsuarioAsync(id, peticion.Activo, ActorId(), cancellationToken);
            if (!exito)
                return NotFound(new { mensaje = $"No se encontró el usuario con identificador '{id}'." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
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

            await _moduloUsuarios.RegistrarAuditoriaAsync(ActorId(), id, peticion.InstitucionId, "Acceso asignado", $"Se asignó el rol institucional {peticion.Rol}.", cancellationToken);
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
        var exito = await _moduloUsuarios.RevocarAccesoInstitucionAsync(id, institucionId, ActorId(), cancellationToken);
        if (!exito)
            return NotFound(new { mensaje = "No se encontró el usuario o no poseía acceso asignado a dicha institución." });

        return NoContent();
    }

    [HttpPost("{id:guid}/vincular-dni")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VincularDni(Guid id, [FromBody] VincularIdentidadPeticion peticion, CancellationToken cancellationToken)
    {
        try
        {
            await _moduloUsuarios.VincularDniAsync(id, peticion.Dni, ActorId(), cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);
}
