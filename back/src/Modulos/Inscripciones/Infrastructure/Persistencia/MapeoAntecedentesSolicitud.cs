using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

internal static class MapeoAntecedentesSolicitud
{
    public static void Configurar(ModelBuilder modelo)
    {
        var documento = modelo.Entity<DocumentacionSolicitud>();
        documento.ToTable("documentacion_solicitud");
        documento.HasKey(d => d.Id);
        documento.Property(d => d.Id).ValueGeneratedNever();
        documento.Property(d => d.Tipo).IsRequired();
        documento.Property(d => d.Estado).HasConversion<string>();
        documento.HasOne<SolicitudInscripcion>().WithMany(s => s.Documentos).HasForeignKey(d => d.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        var condicion = modelo.Entity<CondicionPrioridad>();
        condicion.ToTable("condiciones_prioridad");
        condicion.HasKey(c => c.Id);
        condicion.Property(c => c.Id).ValueGeneratedNever();
        condicion.Property(c => c.Tipo).IsRequired();
        condicion.Property(c => c.ValorDeclarado).IsRequired();
        condicion.Property(c => c.EstadoVerificacion).HasConversion<string>();
        condicion.HasOne<SolicitudInscripcion>().WithMany(s => s.Condiciones).HasForeignKey(c => c.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<SolicitudInscripcion>().Navigation(s => s.Documentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        modelo.Entity<SolicitudInscripcion>().Navigation(s => s.Condiciones).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
