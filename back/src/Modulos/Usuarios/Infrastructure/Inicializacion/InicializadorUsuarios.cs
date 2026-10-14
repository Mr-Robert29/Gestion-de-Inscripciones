using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Domain.Entidades;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jardin.Usuarios.Infrastructure.Inicializacion;

public class InicializadorUsuarios
{
    private readonly ContextoUsuarios _contexto;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<InicializadorUsuarios> _logger;

    public InicializadorUsuarios(
        ContextoUsuarios contexto,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuracion,
        ILogger<InicializadorUsuarios> logger)
    {
        _contexto = contexto;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuracion = configuracion;
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

        var correoRoot = _configuracion["UsuarioRoot:Correo"]
            ?? Environment.GetEnvironmentVariable("UsuarioRoot__Correo")
            ?? "admin@jardin.gob.ar";

        var claveRoot = _configuracion["UsuarioRoot:Clave"]
            ?? Environment.GetEnvironmentVariable("UsuarioRoot__Clave");

        var nombreRoot = _configuracion["UsuarioRoot:Nombre"]
            ?? "Administrador Root";

        if (string.IsNullOrWhiteSpace(claveRoot))
        {
            _logger.LogWarning("No se ha configurado la contraseña para el usuario Root inicial ('UsuarioRoot:Clave' o variable de entorno 'UsuarioRoot__Clave'). Se omite la creación del usuario inicial.");
            return;
        }

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

            var creacionResult = await _userManager.CreateAsync(identityUser, claveRoot);

            if (creacionResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(identityUser, RolesPersonal.AdministradorDelSistema);

                var personalRoot = new UsuarioPersonal(rootId, nombreRoot, correoRoot);
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
