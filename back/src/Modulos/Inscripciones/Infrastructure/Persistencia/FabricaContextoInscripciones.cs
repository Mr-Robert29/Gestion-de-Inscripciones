using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class FabricaContextoInscripciones : IDesignTimeDbContextFactory<ContextoInscripciones>
{
    // Implementa IDesignTimeDbContextFactory<ContextoInscripciones>.CreateDbContext: no inicia la API ni sus cuentas.
    public ContextoInscripciones CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Inscripciones")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=jardin_diseno;Username=postgres";
        var opciones = new DbContextOptionsBuilder<ContextoInscripciones>()
            .UseNpgsql(conexion, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "inscripciones")).Options;
        return new ContextoInscripciones(opciones);
    }
}
