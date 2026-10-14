using Jardin.Usuarios.Application.Abstracciones;
using Jardin.Usuarios.Application.Servicios;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Infrastructure.Identidad;
using Jardin.Usuarios.Infrastructure.Inicializacion;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Jardin.Usuarios.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Usuarios.Infrastructure;

public static class ExtensionesInyeccionUsuarios
{
    public static IServiceCollection AgregarModuloUsuarios(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.Configure<ConfiguracionJwt>(configuracion.GetSection(ConfiguracionJwt.Seccion));

        var cadenaConexion = configuracion.GetConnectionString("DefaultConnection")
            ?? configuracion.GetConnectionString("Postgres")
            ?? "Host=localhost;Database=jardin_inscripciones;Username=postgres;Password=postgres";

        servicios.AddDbContext<ContextoUsuarios>(opciones =>
        {
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "usuarios");
            });
        });

        servicios.AddIdentityCore<IdentityUser<Guid>>(opciones =>
        {
            opciones.Password.RequireDigit = true;
            opciones.Password.RequireLowercase = false;
            opciones.Password.RequireUppercase = false;
            opciones.Password.RequireNonAlphanumeric = false;
            opciones.Password.RequiredLength = 6;
            opciones.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ContextoUsuarios>()
        .AddDefaultTokenProviders();

        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IServicioIdentidad, AdaptadorIdentidad>();
        servicios.AddSingleton<IGeneradorTokenJwt, GeneradorTokenJwt>();
        servicios.AddScoped<IModuloUsuarios, ServicioUsuarios>();
        servicios.AddScoped<InicializadorUsuarios>();

        return servicios;
    }
}
