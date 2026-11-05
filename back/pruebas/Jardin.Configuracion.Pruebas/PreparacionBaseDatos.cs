using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

// Instalación inicial explícita: no se ejecuta al arrancar la API ni sobre una base con tablas del proyecto.
public static class PreparacionBaseDatos
{
    public static async Task EjecutarAsync(bool aplicar)
    {
        var conexion = ContextosMigraciones.LeerConexion();
        var destino = new NpgsqlConnectionStringBuilder(conexion);
        if (destino.Database != "jardin_inscripciones")
            throw new InvalidOperationException("Este instalador inicial solo admite la base jardin_inscripciones.");
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "Jardin.sln"))) carpeta = carpeta.Parent;
        if (carpeta is null) throw new InvalidOperationException("No se encontró la carpeta back.");
        var salida = Path.Combine(carpeta.FullName, "instalacion");
        Directory.CreateDirectory(salida);
        await using var contextos = new ContextosMigraciones(conexion);
        var scripts = new List<(string Nombre, string Sql)>();
        var numero = 0;
        foreach (var modulo in contextos.Modulos)
        {
            var nombre = $"{++numero:00}_{modulo.Esquema}.sql";
            var sql = modulo.Contexto.GetService<IMigrator>().GenerateScript(
                options: MigrationsSqlGenerationOptions.Idempotent | MigrationsSqlGenerationOptions.NoTransactions);
            await File.WriteAllTextAsync(Path.Combine(salida, nombre), sql);
            scripts.Add((nombre, sql));
            Console.WriteLine($"Preparado {nombre}, sin credenciales.");
        }
        if (!aplicar) return;
        await using var baseDatos = new NpgsqlConnection(conexion);
        await baseDatos.OpenAsync();
        await using var transaccion = await baseDatos.BeginTransactionAsync();
        await using (var bloqueo = new NpgsqlCommand("SELECT pg_advisory_xact_lock(2026110501)", baseDatos, transaccion))
            await bloqueo.ExecuteNonQueryAsync();
        const string consulta = "SELECT count(*) FROM information_schema.tables WHERE table_schema IN ('usuarios','configuracion','inscripciones','vacantes','secciones')";
        await using (var comprobacion = new NpgsqlCommand(consulta, baseDatos, transaccion))
        {
            if (Convert.ToInt32(await comprobacion.ExecuteScalarAsync()) != 0)
                throw new InvalidOperationException("La base ya tiene tablas del proyecto. No se aplica la instalación inicial ni se borra nada.");
        }
        foreach (var script in scripts)
        {
            await using var comando = new NpgsqlCommand(script.Sql, baseDatos, transaccion) { CommandTimeout = 120 };
            await comando.ExecuteNonQueryAsync();
            Console.WriteLine($"Aplicado {script.Nombre}.");
        }
        await transaccion.CommitAsync();
        Console.WriteLine("Instalación inicial completada en una transacción. No se borraron tablas ni registros.");
    }
}
