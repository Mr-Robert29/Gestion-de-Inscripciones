using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Servicios;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Vacantes.Infrastructure;

public static class ExtensionesInyeccionVacantes
{
    public static IServiceCollection AgregarModuloVacantes(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var conexion = configuracion.GetConnectionString("Vacantes")
            ?? configuracion.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Vacantes.");

        servicios.AddDbContext<ContextoVacantes>(opciones =>
            opciones.UseNpgsql(conexion, postgres =>
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", "vacantes")));
        servicios.AddScoped<IRepositorioSorteos, RepositorioSorteos>();
        servicios.AddScoped<IRepositorioEvaluacionesPrioridad, RepositorioEvaluacionesPrioridad>();
        servicios.AddScoped<IRepositorioCasosRevision, RepositorioCasosRevision>();
        servicios.AddScoped<IModuloSorteos, ServicioSorteos>();
        servicios.AddScoped<IModuloPrioridades, ServicioPrioridades>();
        return servicios;
    }
}
