using Jardin.Secciones.Domain.Entidades;
using Jardin.Secciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain.Entidades;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

// Pruebas en bases PostgreSQL creadas y descartadas aquí, sin insertar datos ficticios en la base del usuario.
public static class PruebasMigracionesPostgres
{
    public static async Task EjecutarAsync(string conexion)
    {
        await using (var nueva = await BaseTemporalMigraciones.CrearAsync(conexion))
        {
            await using (var contextos = new ContextosMigraciones(nueva.Conexion))
            await using (var baseNueva = new NpgsqlConnection(nueva.Conexion))
            {
                await baseNueva.OpenAsync();
                await using var transaccion = await baseNueva.BeginTransactionAsync();
                foreach (var modulo in contextos.Modulos)
                {
                    var sql = modulo.Contexto.GetService<IMigrator>().GenerateScript(
                        options: MigrationsSqlGenerationOptions.Idempotent | MigrationsSqlGenerationOptions.NoTransactions);
                    await using var aplicar = new NpgsqlCommand(sql, baseNueva, transaccion);
                    await aplicar.ExecuteNonQueryAsync();
                }
                await transaccion.CommitAsync();
            }
            await GestionMigraciones.RevisarAsync(nueva.Conexion, true);
            await GestionMigraciones.RevisarAsync(nueva.Conexion, true);
            await ComprobarEstadoAsync(nueva.Conexion);
            Console.WriteLine("Correcto: base nueva creada y actualización repetida sin migraciones pendientes.");
        }
        await using var existente = await BaseTemporalMigraciones.CrearAsync(conexion);
        await using (var contextos = new ContextosMigraciones(existente.Conexion))
        {
            foreach (var modulo in contextos.Modulos)
            {
                if (modulo.Esquema is "configuracion" or "inscripciones")
                    await modulo.Contexto.Database.MigrateAsync();
                else
                    await modulo.Contexto.Database.ExecuteSqlRawAsync(modulo.Contexto.Database.GenerateCreateScript());
            }
            var usuarios = (ContextoUsuarios)contextos.Modulos.Single(x => x.Esquema == "usuarios").Contexto;
            usuarios.UsuariosPersonal.Add(new UsuarioPersonal(Guid.NewGuid(), "Persona ficticia", "migraciones@prueba.example"));
            await usuarios.SaveChangesAsync();
            var vacantes = (ContextoVacantes)contextos.Modulos.Single(x => x.Esquema == "vacantes").Contexto;
            vacantes.Sorteos.Add(new Sorteo(
                Guid.NewGuid(),
                5,
                347,
                new DateOnly(2026, 11, 5),
                "Acta ficticia de migraciones",
                1,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));
            await vacantes.SaveChangesAsync();
            var secciones = (ContextoSecciones)contextos.Modulos.Single(x => x.Esquema == "secciones").Contexto;
            secciones.Secciones.Add(new Seccion(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sección ficticia",
                [5],
                2));
            await secciones.SaveChangesAsync();
        }
        await using var baseDatos = new NpgsqlConnection(existente.Conexion);
        await baseDatos.OpenAsync();
        await using (var alterar = new NpgsqlCommand("ALTER TABLE vacantes.sorteos ADD COLUMN \"ColumnaAjena\" integer", baseDatos))
            await alterar.ExecuteNonQueryAsync();
        var rechazo = false;
        try { await AdopcionMigraciones.AdoptarAsync(existente.Conexion); }
        catch (InvalidOperationException error) when (error.Message.Contains("La estructura no coincide", StringComparison.Ordinal))
        { rechazo = true; }
        if (!rechazo) throw new InvalidOperationException("No se bloqueó la adopción de una estructura incompatible.");
        await using (var contextos = new ContextosMigraciones(existente.Conexion))
            foreach (var modulo in contextos.Modulos.Where(x => x.Esquema is "usuarios" or "vacantes" or "secciones"))
                if ((await modulo.Contexto.Database.GetAppliedMigrationsAsync()).Any())
                    throw new InvalidOperationException("Un rechazo dejó historiales parciales.");
        Console.WriteLine("Correcto: estructura distinta rechazada sin registrar historiales parciales.");
        // Solo retira la columna ficticia agregada arriba, dentro de la base temporal propia.
        await using (var restaurar = new NpgsqlCommand("ALTER TABLE vacantes.sorteos DROP COLUMN \"ColumnaAjena\"", baseDatos))
            await restaurar.ExecuteNonQueryAsync();
        await AdopcionMigraciones.AdoptarAsync(existente.Conexion);
        await AdopcionMigraciones.AdoptarAsync(existente.Conexion);
        await GestionMigraciones.RevisarAsync(existente.Conexion, true);
        await ComprobarEstadoAsync(existente.Conexion);
        await using (var contextos = new ContextosMigraciones(existente.Conexion))
        {
            if (await ((ContextoUsuarios)contextos.Modulos.Single(x => x.Esquema == "usuarios").Contexto).UsuariosPersonal.CountAsync() != 1 ||
                await ((ContextoVacantes)contextos.Modulos.Single(x => x.Esquema == "vacantes").Contexto).Sorteos.CountAsync() != 1 ||
                await ((ContextoSecciones)contextos.Modulos.Single(x => x.Esquema == "secciones").Contexto).Secciones.CountAsync() != 1)
                throw new InvalidOperationException("No se conservaron los registros de prueba.");
        }
        await baseDatos.CloseAsync();
        Console.WriteLine("Correcto: adopción y reintento conservan datos; cinco módulos con EF Core sin cambios de modelo pendientes.");
    }

    private static async Task ComprobarEstadoAsync(string conexion)
    {
        await using var contextos = new ContextosMigraciones(conexion);
        foreach (var modulo in contextos.Modulos)
        {
            if (!(await modulo.Contexto.Database.GetAppliedMigrationsAsync()).Any() ||
                (await modulo.Contexto.Database.GetPendingMigrationsAsync()).Any() ||
                modulo.Contexto.Database.HasPendingModelChanges())
                throw new InvalidOperationException($"Estado incompleto de migraciones en {modulo.Esquema}.");
        }
    }
}
