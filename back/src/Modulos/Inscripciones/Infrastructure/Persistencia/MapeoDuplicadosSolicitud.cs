using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

internal static class MapeoDuplicadosSolicitud
{
    public static void Configurar(ModelBuilder modelo)
    {
        var alerta = modelo.Entity<PosibleDuplicado>();
        alerta.ToTable("posibles_duplicados");
        alerta.HasKey(a => a.Id);
        alerta.Property(a => a.Id).ValueGeneratedNever();
        alerta.Property(a => a.PerfilDeteccion).IsRequired();
        alerta.Property(a => a.HuellaDatos).IsRequired();
        alerta.Property(a => a.DatosEvaluados).IsRequired();
        alerta.Property(a => a.Criterios).HasConversion<string>();
        alerta.Property(a => a.Estado).HasConversion<string>();
        alerta.Property(a => a.Revision).IsConcurrencyToken();
        alerta.HasOne<SolicitudInscripcion>().WithMany().HasForeignKey(a => a.SolicitudAId).OnDelete(DeleteBehavior.Restrict);
        alerta.HasOne<SolicitudInscripcion>().WithMany().HasForeignKey(a => a.SolicitudBId).OnDelete(DeleteBehavior.Restrict);
        alerta.HasIndex(a => new { a.SolicitudAId, a.SolicitudBId }).IsUnique().HasDatabaseName("UX_Duplicado_ParSolicitudes");
        alerta.HasIndex(a => new { a.InstitucionId, a.PeriodoId, a.Estado });
    }
}
