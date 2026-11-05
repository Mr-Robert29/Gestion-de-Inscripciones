using Jardin.Api;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Secciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jardin.Configuracion.Pruebas;

// Los cinco módulos usan el mismo criterio de conexión e historial que la API.
public sealed class ContextosMigraciones : IAsyncDisposable
{
    public IReadOnlyList<(string Esquema, DbContext Contexto)> Modulos { get; }
    public ContextosMigraciones(string conexion)
    {
        Modulos = [
            ("usuarios", new ContextoUsuarios(Opciones<ContextoUsuarios>(conexion, "usuarios"))),
            ("configuracion", new ContextoConfiguracion(Opciones<ContextoConfiguracion>(conexion, "configuracion"))),
            ("inscripciones", new ContextoInscripciones(Opciones<ContextoInscripciones>(conexion, "inscripciones"))),
            ("vacantes", new ContextoVacantes(Opciones<ContextoVacantes>(conexion, "vacantes"))),
            ("secciones", new ContextoSecciones(Opciones<ContextoSecciones>(conexion, "secciones")))
        ];
    }
    private static DbContextOptions<T> Opciones<T>(string conexion, string esquema) where T : DbContext
        => new DbContextOptionsBuilder<T>().UseNpgsql(
            conexion,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", esquema)).Options;

    public static string LeerConexion()
    {
        var configuracion = new ConfigurationBuilder().AddUserSecrets<PuntoEntradaApi>(optional: false).Build();
        var conexion = configuracion.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta DefaultConnection en los secretos de Jardin.Api.");
        // Este ejecutable instala una única base. No redirigir silenciosamente módulos configurados por separado.
        foreach (var modulo in new[] { "Configuracion", "Inscripciones", "Vacantes" })
        {
            var especifica = configuracion.GetConnectionString(modulo);
            if (especifica is not null && new Npgsql.NpgsqlConnectionStringBuilder(especifica).ConnectionString !=
                new Npgsql.NpgsqlConnectionStringBuilder(conexion).ConnectionString)
                throw new InvalidOperationException("La herramienta conjunta requiere que todos los módulos utilicen DefaultConnection.");
        }
        return conexion;
    }

    // Implementa IAsyncDisposable.DisposeAsync: libera los contextos de la operación.
    public async ValueTask DisposeAsync()
    {
        foreach (var modulo in Modulos) await modulo.Contexto.DisposeAsync();
    }
}
