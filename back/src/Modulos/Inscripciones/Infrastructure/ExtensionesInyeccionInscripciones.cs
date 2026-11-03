using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Servicios;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Inscripciones.Infrastructure;

public static class ExtensionesInyeccionInscripciones
{
    public static IServiceCollection AgregarModuloInscripciones(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var conexion = configuracion.GetConnectionString("Inscripciones") ?? configuracion.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conexion)) throw new InvalidOperationException("Falta ConnectionStrings:Inscripciones.");
        servicios.AddDbContext<ContextoInscripciones>(opciones => opciones.UseNpgsql(conexion,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "inscripciones")));
        servicios.AddScoped<IRepositorioPersonasInscripciones, RepositorioPersonasInscripciones>();
        servicios.AddScoped<IModuloPersonasInscripciones, ServicioPersonasInscripciones>();
        servicios.AddScoped<IRepositorioSolicitudes, RepositorioSolicitudes>();
        servicios.AddScoped<IModuloSolicitudes, ServicioSolicitudes>();
        servicios.AddScoped<IRepositorioAntecedentesSolicitud, RepositorioAntecedentesSolicitud>();
        servicios.AddScoped<IModuloAntecedentesSolicitud, ServicioAntecedentesSolicitud>();
        servicios.AddScoped<IRepositorioCorreccionesSolicitudes, RepositorioCorreccionesSolicitudes>();
        servicios.AddScoped<IModuloCorreccionesSolicitudes, ServicioCorreccionesSolicitudes>();
        servicios.AddScoped<IRepositorioUsoDatosSolicitud, RepositorioUsoDatosSolicitud>();
        servicios.AddScoped<IModuloUsoDatosSolicitud, ServicioUsoDatosSolicitud>();
        servicios.AddScoped<IRepositorioDuplicadosSolicitud, RepositorioDuplicadosSolicitud>();
        servicios.AddScoped<IModuloDuplicadosSolicitud, ServicioDuplicadosSolicitud>();
        servicios.AddScoped<IRepositorioDistancias, RepositorioDistancias>();
        servicios.AddScoped<IModuloDistancias, ServicioDistancias>();
        return servicios;
    }
}
