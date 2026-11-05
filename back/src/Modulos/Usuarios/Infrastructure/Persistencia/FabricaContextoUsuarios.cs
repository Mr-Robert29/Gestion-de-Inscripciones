using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jardin.Usuarios.Infrastructure.Persistencia;

// Permite generar migraciones sin iniciar la API ni crear cuentas.
public sealed class FabricaContextoUsuarios : IDesignTimeDbContextFactory<ContextoUsuarios>
{
    // Implementa IDesignTimeDbContextFactory<ContextoUsuarios>.CreateDbContext para las herramientas de EF.
    public ContextoUsuarios CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Usuarios")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=jardin_diseno;Username=postgres";
        var opciones = new DbContextOptionsBuilder<ContextoUsuarios>()
            .UseNpgsql(
                conexion,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "usuarios"))
            .Options;
        return new ContextoUsuarios(opciones);
    }
}
