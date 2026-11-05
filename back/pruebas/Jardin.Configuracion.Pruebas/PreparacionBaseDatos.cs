using Jardin.Api;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Secciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

// Instalación inicial explícita: no se ejecuta al arrancar la API ni sobre una base con tablas del proyecto.
public static class PreparacionBaseDatos
{
    public static async Task EjecutarAsync(bool aplicar)
    {
        var configuracion = new ConfigurationBuilder().AddUserSecrets<PuntoEntradaApi>(optional: false).Build();
        var conexion = configuracion.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta DefaultConnection en los secretos de Jardin.Api.");
        var destino = new NpgsqlConnectionStringBuilder(conexion);
        if (destino.Database != "jardin_inscripciones")
            throw new InvalidOperationException("Este instalador inicial solo admite la base jardin_inscripciones.");
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "Jardin.sln"))) carpeta = carpeta.Parent;
        if (carpeta is null) throw new InvalidOperationException("No se encontró la carpeta back.");
        var salida = Path.Combine(carpeta.FullName, "instalacion");
        Directory.CreateDirectory(salida);
        var opcionesConfiguracion = new DbContextOptionsBuilder<ContextoConfiguracion>().UseNpgsql(
            conexion,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "configuracion")).Options;
        var opcionesInscripciones = new DbContextOptionsBuilder<ContextoInscripciones>().UseNpgsql(
            conexion,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "inscripciones")).Options;
        await using var usuarios = new ContextoUsuarios(new DbContextOptionsBuilder<ContextoUsuarios>().UseNpgsql(conexion).Options);
        await using var configuracionDb = new ContextoConfiguracion(opcionesConfiguracion);
        await using var inscripciones = new ContextoInscripciones(opcionesInscripciones);
        await using var vacantes = new ContextoVacantes(new DbContextOptionsBuilder<ContextoVacantes>().UseNpgsql(conexion).Options);
        await using var secciones = new ContextoSecciones(new DbContextOptionsBuilder<ContextoSecciones>().UseNpgsql(conexion).Options);
        var scripts = new List<(string Nombre, string Sql)>();
        foreach (var (nombre, contexto, tieneMigraciones) in new (string, DbContext, bool)[]
        {
            ("01_usuarios.sql", usuarios, false),
            ("02_configuracion.sql", configuracionDb, true),
            ("03_inscripciones.sql", inscripciones, true),
            ("04_vacantes.sql", vacantes, false),
            ("05_secciones.sql", secciones, false)
        })
        {
            var sql = tieneMigraciones
                ? contexto.GetService<IMigrator>().GenerateScript(
                    options: MigrationsSqlGenerationOptions.Idempotent | MigrationsSqlGenerationOptions.NoTransactions)
                : contexto.Database.GenerateCreateScript();
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
