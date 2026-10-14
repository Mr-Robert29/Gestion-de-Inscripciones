using Jardin.Usuarios.Application.Abstracciones;
using Microsoft.AspNetCore.Identity;

namespace Jardin.Usuarios.Infrastructure.Identidad;

public class AdaptadorIdentidad : IServicioIdentidad
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public AdaptadorIdentidad(
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool Exito, string? Error)> CrearCuentaAsync(
        Guid usuarioId,
        string correoElectronico,
        string clave,
        string rolGlobal,
        CancellationToken cancellationToken = default)
    {
        var user = new IdentityUser<Guid>
        {
            Id = usuarioId,
            UserName = correoElectronico,
            Email = correoElectronico,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, clave);
        if (!result.Succeeded)
        {
            var primerError = result.Errors.FirstOrDefault()?.Description ?? "Error al crear la cuenta de usuario.";
            return (false, primerError);
        }

        if (!await _roleManager.RoleExistsAsync(rolGlobal))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(rolGlobal));
        }

        await _userManager.AddToRoleAsync(user, rolGlobal);
        return (true, null);
    }

    public async Task<(bool Valido, Guid UsuarioId, IReadOnlyList<string> Roles)> ValidarCredencialesAsync(
        string correoElectronico,
        string clave,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(correoElectronico);
        if (user is null)
            return (false, Guid.Empty, Array.Empty<string>());

        var claveValida = await _userManager.CheckPasswordAsync(user, clave);
        if (!claveValida)
            return (false, Guid.Empty, Array.Empty<string>());

        var roles = await _userManager.GetRolesAsync(user);
        return (true, user.Id, roles.ToList());
    }
}
