using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

internal static class MapeoRevisionDomicilios
{
    public static void Configurar(ModelBuilder modelo)
    {
        var entidad = modelo.Entity<RevisionManualDomicilio>();
        entidad.ToTable("revisiones_domicilio");
        entidad.HasKey(r => r.Id);
        entidad.Property(r => r.Id).ValueGeneratedNever();
        entidad.Property(r => r.Estado).HasConversion<string>();
        entidad.HasIndex(r => new { r.InstitucionId, r.SolicitudId });
        entidad.HasIndex(r => new { r.SolicitudId, r.NumeroRegistro }).IsUnique().HasDatabaseName("UX_RevisionDomicilio_Solicitud_Numero");
        entidad.HasOne<SolicitudInscripcion>().WithMany().HasForeignKey(r => r.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        entidad.OwnsOne(r => r.Domicilio, d =>
        {
            d.Property(p => p.Calle).IsRequired();
            d.Property(p => p.Numero).IsRequired();
            d.Property(p => p.Localidad).IsRequired();
            d.Property(p => p.Provincia).IsRequired();
            d.Property(p => p.CodigoPostal);
            d.Property(p => p.Complemento);
            d.Property(p => p.Piso);
            d.Property(p => p.Departamento);
            d.Property(p => p.Barrio);
        });
        entidad.Navigation(r => r.Domicilio).IsRequired();
    }
}
