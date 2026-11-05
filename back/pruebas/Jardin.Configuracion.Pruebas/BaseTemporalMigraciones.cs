using Npgsql;

namespace Jardin.Configuracion.Pruebas;

// Solo crea y elimina bases descartables con nombre propio, generado aquí; nunca la base del usuario.
public sealed class BaseTemporalMigraciones : IAsyncDisposable
{
    private readonly string _conexionAdministrativa;
    private readonly string _nombre;
    public string Conexion { get; }
    private BaseTemporalMigraciones(string administrativa, string nombre)
    {
        _conexionAdministrativa = administrativa;
        _nombre = nombre;
        var datos = new NpgsqlConnectionStringBuilder(administrativa) { Database = nombre, Pooling = false };
        Conexion = datos.ConnectionString;
    }
    public static async Task<BaseTemporalMigraciones> CrearAsync(string conexion)
    {
        var datos = new NpgsqlConnectionStringBuilder(conexion) { Database = "postgres", Pooling = false };
        var nombre = "jardin_migraciones_prueba_" + Guid.NewGuid().ToString("N");
        var resultado = new BaseTemporalMigraciones(datos.ConnectionString, nombre);
        await using var baseDatos = new NpgsqlConnection(datos.ConnectionString);
        await baseDatos.OpenAsync();
        await using var comando = new NpgsqlCommand($"CREATE DATABASE {EstructuraPostgres.Citar(nombre)} TEMPLATE template0", baseDatos);
        await comando.ExecuteNonQueryAsync();
        return resultado;
    }
    // Implementa IAsyncDisposable.DisposeAsync: elimina exclusivamente la base descartable creada por esta instancia.
    public async ValueTask DisposeAsync()
    {
        if (!_nombre.StartsWith("jardin_migraciones_prueba_", StringComparison.Ordinal) || _nombre.Length != "jardin_migraciones_prueba_".Length + 32)
            throw new InvalidOperationException("El nombre no corresponde a una base temporal de migraciones.");
        await using var conexion = new NpgsqlConnection(_conexionAdministrativa);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP DATABASE {EstructuraPostgres.Citar(_nombre)}", conexion);
        await comando.ExecuteNonQueryAsync();
    }
}
