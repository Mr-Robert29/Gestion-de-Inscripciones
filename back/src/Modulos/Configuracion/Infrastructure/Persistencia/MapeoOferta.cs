using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

// Mantiene los mapeos de HU-03 separados del contexto general.
internal static class MapeoOferta
{
    public static void Configurar(ModelBuilder modelo)
    {
        var aula = modelo.Entity<Aula>();
        aula.ToTable("aulas");
        aula.HasKey(a => a.Id);
        aula.Property(a => a.Id).ValueGeneratedNever();
        aula.Property(a => a.Nombre).IsRequired();
        aula.HasOne<Institucion>().WithMany().HasForeignKey(a => a.InstitucionId).OnDelete(DeleteBehavior.Restrict);

        var turno = modelo.Entity<Turno>();
        turno.ToTable("turnos");
        turno.HasKey(t => t.Id);
        turno.Property(t => t.Id).ValueGeneratedNever();
        turno.Property(t => t.Nombre).IsRequired();
        turno.HasOne<Institucion>().WithMany().HasForeignKey(t => t.InstitucionId).OnDelete(DeleteBehavior.Restrict);

        var oferta = modelo.Entity<OfertaAulaTurno>();
        oferta.ToTable("ofertas_aula_turno");
        oferta.HasKey(o => o.Id);
        oferta.Property(o => o.Id).ValueGeneratedNever();
        oferta.Property(o => o.Revision).IsConcurrencyToken();
        oferta.Property(o => o.ReferenciaCapacidad).IsRequired();
        oferta.Property(o => o.OrigenOcupaciones).IsRequired();
        oferta.Ignore(o => o.CapacidadNetaInicial);
        oferta.HasIndex(o => new { o.PeriodoId, o.AulaId, o.TurnoId }).IsUnique().HasDatabaseName("UX_Oferta_Periodo_Aula_Turno");
        oferta.HasOne<PeriodoInscripcion>().WithMany().HasForeignKey(o => o.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        oferta.HasOne<Aula>().WithMany().HasForeignKey(o => o.AulaId).OnDelete(DeleteBehavior.Restrict);
        oferta.HasOne<Turno>().WithMany().HasForeignKey(o => o.TurnoId).OnDelete(DeleteBehavior.Restrict);
        oferta.OwnsMany(o => o.Ocupaciones, ocupacion =>
        {
            ocupacion.ToTable("ocupaciones_iniciales");
            ocupacion.WithOwner().HasForeignKey("OfertaId");
            ocupacion.Property(o => o.Sala).ValueGeneratedNever();
            ocupacion.HasKey("OfertaId", nameof(OcupacionInicial.Sala));
        });
        oferta.Navigation(o => o.Ocupaciones).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
