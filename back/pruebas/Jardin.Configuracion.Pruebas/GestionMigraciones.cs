using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

public static class GestionMigraciones
{
    public static Task EjecutarAsync(bool actualizar) => RevisarAsync(ContextosMigraciones.LeerConexion(), actualizar);

    public static async Task RevisarAsync(string conexion, bool actualizar)
    {
        await using var contextos = new ContextosMigraciones(conexion);
        // Revisa todos los módulos antes de aplicar el primero: nunca ejecutar un inicial sobre tablas sin historial.
        await using var baseDatos = new NpgsqlConnection(conexion);
        await baseDatos.OpenAsync();
        foreach (var modulo in contextos.Modulos)
        {
            var aplicadas = (await modulo.Contexto.Database.GetAppliedMigrationsAsync()).ToArray();
            var conocidas = modulo.Contexto.Database.GetMigrations().ToArray();
            if (aplicadas.Except(conocidas).Any())
                throw new InvalidOperationException($"{modulo.Esquema}: el historial contiene migraciones que esta versión no conoce.");
            if (modulo.Contexto.Database.HasPendingModelChanges())
                throw new InvalidOperationException($"{modulo.Esquema}: el modelo tiene cambios sin una migración. Preparala antes de actualizar.");
            var pendientes = conocidas.Except(aplicadas).Count();
            Console.WriteLine($"{modulo.Esquema}: {aplicadas.Length} aplicadas, {pendientes} pendientes.");
            if (actualizar && aplicadas.Length == 0)
            {
                await using var consulta = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema=@esquema AND table_name<>'__EFMigrationsHistory'", baseDatos);
                consulta.Parameters.AddWithValue("esquema", modulo.Esquema);
                if (Convert.ToInt32(await consulta.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException($"{modulo.Esquema}: hay tablas sin historial. Usá primero --adoptar-base-existente; no se recrean ni se borran tablas.");
            }
        }
        if (!actualizar) return;
        foreach (var modulo in contextos.Modulos)
        {
            await modulo.Contexto.Database.MigrateAsync();
            Console.WriteLine($"{modulo.Esquema}: actualizado mediante EF Core.");
        }
    }
}
