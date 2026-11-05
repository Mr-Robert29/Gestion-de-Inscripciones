using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

// Permite generar migraciones sin iniciar la API ni crear cuentas.
public sealed class FabricaContextoVacantes : IDesignTimeDbContextFactory<ContextoVacantes>
{
    // Implementa IDesignTimeDbContextFactory<ContextoVacantes>.CreateDbContext para las herramientas de EF.
    public ContextoVacantes CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Vacantes")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=jardin_diseno;Username=postgres";
        var opciones = new DbContextOptionsBuilder<ContextoVacantes>()
            .UseNpgsql(
                conexion,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "vacantes"))
            .Options;
        return new ContextoVacantes(opciones);
    }
}
