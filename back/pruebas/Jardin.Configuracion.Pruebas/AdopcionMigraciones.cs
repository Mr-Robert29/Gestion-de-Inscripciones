using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

public static class AdopcionMigraciones
{
    private static readonly string[] Esquemas = ["usuarios", "vacantes", "secciones"];

    public static async Task AdoptarAsync(string conexion)
    {
        await using var contextos = new ContextosMigraciones(conexion);
        var modulos = contextos.Modulos.Where(x => Esquemas.Contains(x.Esquema)).ToArray();
        var iniciales = modulos.ToDictionary(x => x.Esquema, x => x.Contexto.Database.GetMigrations().First());
        var completados = true;
        foreach (var modulo in modulos)
            completados &= (await modulo.Contexto.Database.GetAppliedMigrationsAsync()).Contains(iniciales[modulo.Esquema]);
        if (completados)
        {
            Console.WriteLine("Las tres migraciones iniciales ya están registradas. No se modifica la base.");
            return;
        }

        // La referencia se obtiene ejecutando las migraciones iniciales, no suponiendo que las tablas coinciden.
        await using var temporal = await BaseTemporalMigraciones.CrearAsync(conexion);
        await using (var referencia = new ContextosMigraciones(temporal.Conexion))
        {
            foreach (var modulo in referencia.Modulos.Where(x => Esquemas.Contains(x.Esquema)))
                await modulo.Contexto.GetService<IMigrator>().MigrateAsync(iniciales[modulo.Esquema]);
        }
        await using var conexionReferencia = new NpgsqlConnection(temporal.Conexion);
        await conexionReferencia.OpenAsync();
        var esperada = await EstructuraPostgres.LeerAsync(conexionReferencia, null, Esquemas);
        await conexionReferencia.CloseAsync();

        await using var destino = new NpgsqlConnection(conexion);
        await destino.OpenAsync();
        await using var transaccion = await destino.BeginTransactionAsync();
        await using (var bloqueo = new NpgsqlCommand("SELECT pg_advisory_xact_lock(2026110501)", destino, transaccion))
            await bloqueo.ExecuteNonQueryAsync();
        var actual = await EstructuraPostgres.LeerAsync(destino, transaccion, Esquemas);
        foreach (var tabla in actual.Where(x => x.Tipo == "tabla"))
        {
            var nombre = $"{EstructuraPostgres.Citar(tabla.Esquema)}.{EstructuraPostgres.Citar(tabla.Tabla)}";
            await using var bloquearTabla = new NpgsqlCommand($"LOCK TABLE {nombre} IN SHARE MODE", destino, transaccion);
            await bloquearTabla.ExecuteNonQueryAsync();
        }
        actual = await EstructuraPostgres.LeerAsync(destino, transaccion, Esquemas);
        if (!actual.SequenceEqual(esperada))
        {
            var diferencia = actual.Except(esperada).Concat(esperada.Except(actual)).First();
            throw new InvalidOperationException($"La estructura no coincide: {diferencia.Esquema}.{diferencia.Tabla}, {diferencia.Tipo} {diferencia.Nombre}. No se registró ninguna migración.");
        }
        var antes = await EstructuraPostgres.HuellasDatosAsync(destino, transaccion, actual);
        foreach (var modulo in modulos)
        {
            var historial = modulo.Contexto.GetService<IHistoryRepository>();
            await using (var crear = new NpgsqlCommand(historial.GetCreateIfNotExistsScript(), destino, transaccion))
                await crear.ExecuteNonQueryAsync();
            var identificador = iniciales[modulo.Esquema];
            var tabla = $"{EstructuraPostgres.Citar(modulo.Esquema)}.\"__EFMigrationsHistory\"";
            var registradas = new List<string>();
            await using (var leer = new NpgsqlCommand($"SELECT \"MigrationId\" FROM {tabla}", destino, transaccion))
            await using (var lector = await leer.ExecuteReaderAsync())
                while (await lector.ReadAsync()) registradas.Add(lector.GetString(0));
            if (registradas.Except(modulo.Contexto.Database.GetMigrations()).Any())
                throw new InvalidOperationException("Hay migraciones desconocidas en el historial. No se modifica la base.");
            if (!registradas.Contains(identificador))
            {
                var version = modulo.Contexto.GetService<IMigrationsAssembly>().ModelSnapshot!.Model.GetProductVersion()
                    ?? throw new InvalidOperationException("Falta la versión del snapshot de EF.");
                var sql = historial.GetInsertScript(new HistoryRow(identificador, version));
                await using var insertar = new NpgsqlCommand(sql, destino, transaccion);
                await insertar.ExecuteNonQueryAsync();
            }
        }
        var despues = await EstructuraPostgres.HuellasDatosAsync(destino, transaccion, actual);
        if (antes.Count != despues.Count || antes.Any(x => !despues.TryGetValue(x.Key, out var huella) || huella != x.Value))
            throw new InvalidOperationException("La comprobación de datos no coincide; se revierte el registro de migraciones.");
        if (!actual.SequenceEqual(await EstructuraPostgres.LeerAsync(destino, transaccion, Esquemas)))
            throw new InvalidOperationException("Se detectó un cambio inesperado de estructura; se revierte la adopción.");
        await transaccion.CommitAsync();
        Console.WriteLine("Usuarios, Vacantes y Secciones adoptados: solo se crearon sus historiales y se registraron las migraciones iniciales. Estructura y datos preservados.");
    }
}
