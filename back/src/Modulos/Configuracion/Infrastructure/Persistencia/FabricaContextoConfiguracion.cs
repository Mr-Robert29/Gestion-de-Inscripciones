using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

// Permite generar migraciones sin arrancar la API ni ejecutar su inicialización de cuentas.
public sealed class FabricaContextoConfiguracion : IDesignTimeDbContextFactory<ContextoConfiguracion>
{
    // Implementa IDesignTimeDbContextFactory<ContextoConfiguracion>.CreateDbContext.
    public ContextoConfiguracion CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Configuracion")
            ?? "Host=localhost;Database=jardin_diseno;Username=postgres";
        var opciones = new DbContextOptionsBuilder<ContextoConfiguracion>()
            .UseNpgsql(conexion, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "configuracion")).Options;
        return new ContextoConfiguracion(opciones);
    }
}
