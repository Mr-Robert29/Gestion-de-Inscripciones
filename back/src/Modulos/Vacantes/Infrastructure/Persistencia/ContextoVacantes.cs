using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class ContextoVacantes : DbContext
{
    public DbSet<Sorteo> Sorteos => Set<Sorteo>();
    public DbSet<EvaluacionPrioridad> EvaluacionesPrioridad => Set<EvaluacionPrioridad>();
    public DbSet<CasoRevision> CasosRevision => Set<CasoRevision>();
    public DbSet<HistorialCasoRevision> CambiosCasosRevision => Set<HistorialCasoRevision>();

    public ContextoVacantes(DbContextOptions<ContextoVacantes> opciones)
        : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("vacantes");

        var sorteo = modelo.Entity<Sorteo>();
        sorteo.ToTable("sorteos");
        sorteo.HasKey(x => x.Id);
        sorteo.Property(x => x.Id).ValueGeneratedNever();
        sorteo.Property(x => x.PeriodoId).IsRequired();
        sorteo.Property(x => x.Sala).IsRequired();
        sorteo.Property(x => x.Numero).IsRequired();
        sorteo.Property(x => x.Fecha).IsRequired();
        sorteo.Property(x => x.Referencia).IsRequired().HasMaxLength(250);
        sorteo.Property(x => x.Version).IsRequired();
        sorteo.Property(x => x.Vigente).IsRequired();
        sorteo.Property(x => x.UsuarioId).IsRequired();
        sorteo.Property(x => x.FechaRegistro).IsRequired();
        sorteo.HasIndex(x => new { x.PeriodoId, x.Sala, x.Version }).IsUnique();
        sorteo.HasIndex(x => new { x.PeriodoId, x.Sala })
            .IsUnique()
            .HasFilter("\"Vigente\" = true");

        var evaluacion = modelo.Entity<EvaluacionPrioridad>();
        evaluacion.ToTable("evaluaciones_prioridad");
        evaluacion.HasKey(x => x.Id);
        evaluacion.Property(x => x.Id).ValueGeneratedNever();
        evaluacion.Property(x => x.PeriodoId).IsRequired();
        evaluacion.Property(x => x.VersionReglasId).IsRequired();
        evaluacion.Property(x => x.UsuarioId).IsRequired();
        evaluacion.Property(x => x.RevisionPeriodo).IsRequired();
        evaluacion.Property(x => x.Fecha).IsRequired();
        evaluacion.Property(x => x.Datos).IsRequired().HasColumnType("jsonb");
        evaluacion.HasIndex(x => new { x.PeriodoId, x.Fecha });

        var caso = modelo.Entity<CasoRevision>();
        caso.ToTable("casos_revision");
        caso.HasKey(x => x.Id);
        caso.Property(x => x.Id).ValueGeneratedNever();
        caso.Property(x => x.PeriodoId).IsRequired();
        caso.Property(x => x.Sala).IsRequired();
        caso.Property(x => x.Tipo).HasConversion<string>().IsRequired();
        caso.Property(x => x.Estado).HasConversion<string>().IsRequired();
        caso.Property(x => x.SolicitudesAfectadas).IsRequired().HasColumnType("jsonb");
        caso.Property(x => x.OrdenDecidido).IsRequired().HasColumnType("jsonb");
        caso.Property(x => x.HuellaOrigen).IsRequired().HasMaxLength(500);
        caso.Property(x => x.MotivoResolucion).HasMaxLength(1000);
        caso.Property(x => x.FechaCreacion).IsRequired();
        caso.Property(x => x.Revision).IsConcurrencyToken();
        caso.HasIndex(x => new { x.PeriodoId, x.Sala, x.Tipo, x.HuellaOrigen }).IsUnique();
        caso.HasIndex(x => new { x.PeriodoId, x.Estado });

        var historialCaso = modelo.Entity<HistorialCasoRevision>();
        historialCaso.ToTable("historial_casos_revision");
        historialCaso.HasKey(x => x.Id);
        historialCaso.Property(x => x.Id).ValueGeneratedNever();
        historialCaso.Property(x => x.CasoRevisionId).IsRequired();
        historialCaso.Property(x => x.Accion).IsRequired().HasMaxLength(100);
        historialCaso.Property(x => x.EstadoAnterior).HasConversion<string>().IsRequired();
        historialCaso.Property(x => x.EstadoNuevo).HasConversion<string>().IsRequired();
        historialCaso.Property(x => x.OrdenAnterior).IsRequired().HasColumnType("jsonb");
        historialCaso.Property(x => x.OrdenNueva).IsRequired().HasColumnType("jsonb");
        historialCaso.Property(x => x.Motivo).IsRequired().HasMaxLength(1000);
        historialCaso.Property(x => x.UsuarioId).IsRequired();
        historialCaso.Property(x => x.Fecha).IsRequired();
        historialCaso.HasIndex(x => new { x.CasoRevisionId, x.Fecha });
    }
}
