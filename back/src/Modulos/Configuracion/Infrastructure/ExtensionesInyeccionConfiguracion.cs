using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Application.Servicios;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Infrastructure;

public static class ExtensionesInyeccionConfiguracion
{
    public static IServiceCollection AgregarModuloConfiguracion(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var conexion = configuracion.GetConnectionString("Configuracion") ?? configuracion.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conexion)) throw new InvalidOperationException("Falta ConnectionStrings:Configuracion.");
        servicios.AddDbContext<ContextoConfiguracion>(opciones =>
            opciones.UseNpgsql(conexion, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "configuracion")));
        servicios.AddScoped<IRepositorioInstituciones, RepositorioInstituciones>();
        servicios.AddScoped<IModuloConfiguracion, ServicioConfiguracion>();
        servicios.AddScoped<IRepositorioPeriodos, RepositorioPeriodos>();
        servicios.AddScoped<IModuloPeriodos, ServicioPeriodos>();
        return servicios;
    }
}
