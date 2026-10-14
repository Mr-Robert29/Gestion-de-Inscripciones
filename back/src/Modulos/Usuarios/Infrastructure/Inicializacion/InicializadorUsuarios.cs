using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Domain.Entidades;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Jardin.Usuarios.Infrastructure.Inicializacion;

public class InicializadorUsuarios
{
    private readonly ContextoUsuarios _contexto;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<InicializadorUsuarios> _logger;

    public InicializadorUsuarios(
        ContextoUsuarios contexto,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<InicializadorUsuarios> logger)
    {
        _contexto = contexto;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task InicializarAsync(CancellationToken cancellationToken = default)
    {
        foreach (var rol in RolesPersonal.Todos)
        {
            if (!await _roleManager.RoleExistsAsync(rol))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(rol));
                _logger.LogInformation("Rol '{Rol}' creado exitosamente.", rol);
            }
        }

        var correoRoot = "admin@jardin.gob.ar";
        var usuarioRoot = await _userManager.FindByEmailAsync(correoRoot);

        if (usuarioRoot is null)
        {
            var rootId = Guid.NewGuid();
            var identityUser = new IdentityUser<Guid>
            {
                Id = rootId,
                UserName = correoRoot,
                Email = correoRoot,
                EmailConfirmed = true
            };

            const string claveRoot = "Admin123*!";
            var creacionResult = await _userManager.CreateAsync(identityUser, claveRoot);

            if (creacionResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(identityUser, RolesPersonal.AdministradorDelSistema);

                var personalRoot = new UsuarioPersonal(rootId, "Administrador Root", correoRoot);
                await _contexto.UsuariosPersonal.AddAsync(personalRoot, cancellationToken);
                await _contexto.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Usuario Root inicial '{CorreoRoot}' creado exitosamente con rol '{Rol}'.",
                    correoRoot, RolesPersonal.AdministradorDelSistema);
            }
            else
            {
                _logger.LogWarning("No se pudo crear el usuario Root inicial: {Errores}",
                    string.Join(", ", creacionResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
