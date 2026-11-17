using System.Security.Claims;
using Jardin.Configuracion.Contracts;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}/personal")]
public sealed class PersonalInstitucionController : ControllerBase
{
    private readonly IModuloUsuarios _usuarios;
    private readonly IModuloConfiguracion _configuracion;

    public PersonalInstitucionController(IModuloUsuarios usuarios, IModuloConfiguracion configuracion)
    {
        _usuarios = usuarios;
        _configuracion = configuracion;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DatosPersonalInstitucion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(Guid institucionId, CancellationToken cancellationToken)
    {
        if (!await PuedeAdministrarAsync(institucionId, cancellationToken)) return Forbid();
        try
        {
            return Ok(await _usuarios.ListarPersonalInstitucionAsync(institucionId, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(
        Guid institucionId,
        [FromBody] CrearCuentaInstitucionPeticion peticion,
        CancellationToken cancellationToken)
    {
        if (!await PuedeAdministrarAsync(institucionId, cancellationToken)) return Forbid();
        try
        {
            if (await _configuracion.ConsultarInstitucionAsync(institucionId, cancellationToken) is null) return NotFound();
            // El Directivo solo puede incorporar personal Administrativo a su propio jardín.
            var id = await _usuarios.CrearCuentaInstitucionAsync(institucionId, peticion, RolesPersonal.Administrativo, ActorId(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { id });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{usuarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revocar(
        Guid institucionId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await PuedeAdministrarAsync(institucionId, cancellationToken)) return Forbid();
        var personal = await _usuarios.ListarPersonalInstitucionAsync(institucionId, cancellationToken);
        var cuenta = personal.FirstOrDefault(x => x.UsuarioId == usuarioId);
        if (cuenta is null || cuenta.Rol != RolesPersonal.Administrativo)
            return NotFound(new { mensaje = "No se encontró una cuenta administrativa para esta institución." });
        if (!await _usuarios.RevocarAccesoInstitucionAsync(usuarioId, institucionId, ActorId(), cancellationToken)) return NotFound();
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

    private async Task<bool> PuedeAdministrarAsync(Guid institucionId, CancellationToken cancellationToken)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(claim, out var usuarioId)) return false;
        var sesion = await _usuarios.ObtenerSesionActualAsync(usuarioId, cancellationToken);
        if (sesion is null || !sesion.Activo) return false;
        if (sesion.Roles.Contains(RolesPersonal.AdministradorDelSistema)) return true;
        return sesion.Accesos.Any(a => a.InstitucionId == institucionId && a.Rol == RolesPersonal.Directivo);
    }
}
