using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

internal static class ProteccionSecciones
{
    private static readonly SemaphoreSlim PuertaSqlite = new(1, 1);
    public static string Oferta(Guid periodoId, Guid aulaId, Guid turnoId) => $"oferta:{periodoId:N}:{aulaId:N}:{turnoId:N}";

    public static async Task BloquearAsync(
        ContextoSecciones contexto,
        string clave,
        CancellationToken tokenCancelacion)
    {
        if (!contexto.Database.IsNpgsql()) return;
        var numero = BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(clave)));
        await contexto.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({numero})", tokenCancelacion);
    }

    public static async Task<T> EjecutarAsync<T>(
        ContextoSecciones contexto,
        string clave,
        Func<Task<T>> operacion,
        CancellationToken tokenCancelacion)
    {
        var local = !contexto.Database.IsNpgsql();
        if (local) await PuertaSqlite.WaitAsync(tokenCancelacion);
        try
        {
            await using var transaccion = await contexto.Database.BeginTransactionAsync(tokenCancelacion);
            await BloquearAsync(contexto, clave, tokenCancelacion);
            var resultado = await operacion();
            await transaccion.CommitAsync(tokenCancelacion);
            return resultado;
        }
        finally { if (local) PuertaSqlite.Release(); }
    }
}
