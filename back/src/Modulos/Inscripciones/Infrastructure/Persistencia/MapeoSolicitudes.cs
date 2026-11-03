using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

internal static class MapeoSolicitudes
{
    public static void Configurar(ModelBuilder modelo)
    {
        var solicitud = modelo.Entity<SolicitudInscripcion>();
        solicitud.ToTable("solicitudes");
        solicitud.HasKey(s => s.Id);
        solicitud.Property(s => s.Id).ValueGeneratedNever();
        solicitud.Property(s => s.EstadoRecepcion).HasConversion<string>();
        solicitud.Property(s => s.RevisionDatos).IsConcurrencyToken();
        solicitud.Property(s => s.RevisionDomicilio).HasDefaultValue(1);
        solicitud.Ignore(s => s.FueraDeTermino);
        solicitud.Ignore(s => s.RequiereRevisionEdad);
        solicitud.HasIndex(s => new { s.InstitucionId, s.PeriodoId });
        // No es único: las coincidencias se revisan en HU-10, no se borran ni rechazan automáticamente.
        solicitud.HasIndex(s => new { s.AspiranteId, s.PeriodoId });
        solicitud.HasOne<RegistroAspiranteInstitucion>().WithMany()
            .HasForeignKey(s => new { s.InstitucionId, s.AspiranteId }).OnDelete(DeleteBehavior.Restrict);
        solicitud.OwnsOne(s => s.DomicilioDeclarado, domicilio =>
        {
            domicilio.Property(d => d.Calle).IsRequired();
            domicilio.Property(d => d.Numero).IsRequired();
            domicilio.Property(d => d.Localidad).IsRequired();
            domicilio.Property(d => d.Provincia).IsRequired();
            domicilio.Property(d => d.CodigoPostal);
            domicilio.Property(d => d.Complemento);
            domicilio.Property(d => d.Piso);
            domicilio.Property(d => d.Departamento);
            domicilio.Property(d => d.Barrio);
        });
        solicitud.Navigation(s => s.DomicilioDeclarado).IsRequired();
        solicitud.OwnsOne(s => s.CalculoEdad, calculo =>
        {
            calculo.Property(c => c.FechaNacimientoUtilizada).IsRequired();
            calculo.Property(c => c.FechaCorteUtilizada).IsRequired();
            calculo.Property(c => c.EdadCalculada).IsRequired();
            calculo.Property(c => c.Sala);
        });
        solicitud.Navigation(s => s.CalculoEdad).IsRequired();
        solicitud.OwnsMany(s => s.TurnosAlternativos, alternativa =>
        {
            alternativa.ToTable("turnos_alternativos_solicitud");
            alternativa.WithOwner().HasForeignKey("SolicitudId");
            alternativa.Property(t => t.TurnoId).ValueGeneratedNever();
            alternativa.HasKey("SolicitudId", nameof(TurnoAlternativoSolicitud.TurnoId));
        });
        solicitud.Navigation(s => s.TurnosAlternativos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
