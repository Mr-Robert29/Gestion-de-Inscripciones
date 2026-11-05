using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jardin.Secciones.Infrastructure.Persistencia;

// Permite generar migraciones sin iniciar la API ni crear cuentas.
public sealed class FabricaContextoSecciones : IDesignTimeDbContextFactory<ContextoSecciones>
{
    // Implementa IDesignTimeDbContextFactory<ContextoSecciones>.CreateDbContext para las herramientas de EF.
    public ContextoSecciones CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Secciones")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=jardin_diseno;Username=postgres";
        var opciones = new DbContextOptionsBuilder<ContextoSecciones>()
            .UseNpgsql(
                conexion,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "secciones"))
            .Options;
        return new ContextoSecciones(opciones);
    }
}
