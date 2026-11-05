using Npgsql;

namespace Jardin.Configuracion.Pruebas;

public sealed record RegistroEstructura(
    string Tipo,
    string Esquema,
    string Tabla,
    string Nombre,
    string Definicion);

// Compara estructura física, sin leer valores de las personas ni cambiar el modelo.
public static class EstructuraPostgres
{
    public static async Task<IReadOnlyList<RegistroEstructura>> LeerAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction? transaccion,
        string[] esquemas)
    {
        const string sql = """
            WITH tablas AS (
              SELECT c.oid,n.nspname,c.relname,c.relkind FROM pg_class c
              JOIN pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname=ANY(@esquemas) AND c.relkind IN ('r','p','v','m') AND c.relname<>'__EFMigrationsHistory'
            )
            SELECT 'tabla',nspname,relname,relname,relkind::text FROM tablas
            UNION ALL
            SELECT 'columna',t.nspname,t.relname,a.attname,
              concat_ws('|',format_type(a.atttypid,a.atttypmod),a.attnotnull::text,a.attidentity,a.attgenerated,
                COALESCE(coll.collname,''),COALESCE(pg_get_expr(d.adbin,d.adrelid),''))
              FROM tablas t JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum>0 AND NOT a.attisdropped
              LEFT JOIN pg_attrdef d ON d.adrelid=t.oid AND d.adnum=a.attnum
              LEFT JOIN pg_collation coll ON coll.oid=a.attcollation
            UNION ALL
            SELECT 'restriccion',t.nspname,t.relname,c.conname,
              concat_ws('|',pg_get_constraintdef(c.oid,true),c.convalidated::text,c.condeferrable::text,c.condeferred::text)
              FROM tablas t JOIN pg_constraint c ON c.conrelid=t.oid
            UNION ALL
            SELECT 'indice',t.nspname,t.relname,c.relname,
              concat_ws('|',pg_get_indexdef(i.indexrelid),i.indisvalid::text,i.indisready::text)
              FROM tablas t JOIN pg_index i ON i.indrelid=t.oid JOIN pg_class c ON c.oid=i.indexrelid
            UNION ALL
            SELECT 'trigger',t.nspname,t.relname,g.tgname,pg_get_triggerdef(g.oid,true)
              FROM tablas t JOIN pg_trigger g ON g.tgrelid=t.oid AND NOT g.tgisinternal
            ORDER BY 1,2,3,4,5
            """;
        await using var comando = new NpgsqlCommand(sql, conexion, transaccion);
        comando.Parameters.AddWithValue("esquemas", esquemas);
        await using var lector = await comando.ExecuteReaderAsync();
        var resultado = new List<RegistroEstructura>();
        while (await lector.ReadAsync()) resultado.Add(new RegistroEstructura(
            lector.GetString(0),
            lector.GetString(1),
            lector.GetString(2),
            lector.GetString(3),
            lector.GetString(4)));
        return resultado;
    }

    public static async Task<IReadOnlyDictionary<string, string>> HuellasDatosAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction transaccion,
        IReadOnlyList<RegistroEstructura> estructura)
    {
        var huellas = new Dictionary<string, string>();
        foreach (var tabla in estructura.Where(x => x.Tipo == "tabla"))
        {
            var nombre = $"{Citar(tabla.Esquema)}.{Citar(tabla.Tabla)}";
            var sql = $"SELECT count(*)::text || ':' || md5(COALESCE(string_agg(md5(to_jsonb(fila)::text),'' ORDER BY md5(to_jsonb(fila)::text)),'')) FROM {nombre} fila";
            await using var comando = new NpgsqlCommand(sql, conexion, transaccion);
            huellas[nombre] = (string)(await comando.ExecuteScalarAsync())!;
        }
        return huellas;
    }
    public static string Citar(string identificador) => "\"" + identificador.Replace("\"", "\"\"") + "\"";
}
