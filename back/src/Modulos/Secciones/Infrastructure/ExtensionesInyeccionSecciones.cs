using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Application.Servicios;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Infrastructure.Configuracion;
using Jardin.Secciones.Infrastructure.Persistencia;
using Jardin.Vacantes.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Secciones.Infrastructure;

public static class ExtensionesInyeccionSecciones
{
    public static IServiceCollection AgregarModuloSecciones(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("DefaultConnection")
            ?? configuracion.GetConnectionString("Postgres")
            ?? "Host=localhost;Database=jardin_inscripciones;Username=postgres;Password=postgres";

        servicios.AddDbContext<ContextoSecciones>(opciones =>
        {
            opciones.UseNpgsql(cadenaConexion, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "secciones"));
        });

        servicios.AddScoped<IRepositorioSecciones, RepositorioSecciones>();
        servicios.AddScoped<IRepositorioDistribuciones, RepositorioDistribuciones>();
        servicios.AddScoped<IProveedorOfertaSecciones, ProveedorOfertaSeccionesPorConfiguracion>();
        servicios.AddScoped<IModuloSecciones, ServicioOrganizacion>();
        return servicios;
    }
}
