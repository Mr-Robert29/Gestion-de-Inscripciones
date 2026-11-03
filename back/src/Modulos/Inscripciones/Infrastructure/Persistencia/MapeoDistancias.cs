using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

internal static class MapeoDistancias
{
    public static void Configurar(ModelBuilder modelo)
    {
        var entidad = modelo.Entity<MedicionDistancia>();
        entidad.ToTable("mediciones_distancia");
        entidad.HasKey(m => m.Id);
        entidad.Property(m => m.Id).ValueGeneratedNever();
        entidad.Property(m => m.DistanciaMetros).HasColumnType("numeric");
        entidad.HasIndex(m => new { m.InstitucionId, m.SolicitudId });
        entidad.HasIndex(m => new { m.SolicitudId, m.NumeroRegistro }).IsUnique().HasDatabaseName("UX_Distancia_Solicitud_Numero");
        entidad.HasOne<SolicitudInscripcion>().WithMany().HasForeignKey(m => m.SolicitudId).OnDelete(DeleteBehavior.Restrict);
    }
}
