using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class ContextoVacantes : DbContext
{
    public DbSet<PropuestaIngresos> PropuestasIngresos => Set<PropuestaIngresos>();
    public DbSet<OcupacionPlaza> OcupacionesPlazas => Set<OcupacionPlaza>();
    public DbSet<AceptacionTurnoFamiliar> AceptacionesTurno => Set<AceptacionTurnoFamiliar>();
    public DbSet<IngresoConfirmado> IngresosConfirmados => Set<IngresoConfirmado>();
    public DbSet<Sorteo> Sorteos => Set<Sorteo>();
    public DbSet<EvaluacionPrioridad> EvaluacionesPrioridad => Set<EvaluacionPrioridad>();
    public DbSet<CasoRevision> CasosRevision => Set<CasoRevision>();
    public DbSet<HistorialCasoRevision> CambiosCasosRevision => Set<HistorialCasoRevision>();

    public ContextoVacantes(DbContextOptions<ContextoVacantes> opciones)
        : base(opciones) { }

    public override int SaveChanges(bool aceptarCambiosAlGuardar)
    {
        ProtegerPropuestas();
        return base.SaveChanges(aceptarCambiosAlGuardar);
    }

    public override Task<int> SaveChangesAsync(bool aceptarCambiosAlGuardar, CancellationToken tokenCancelacion = default)
    {
        ProtegerPropuestas();
        return base.SaveChangesAsync(aceptarCambiosAlGuardar, tokenCancelacion);
    }

    private void ProtegerPropuestas()
    {
        if (ChangeTracker.Entries<PropuestaIngresos>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Las propuestas históricas no se modifican ni se eliminan. Generá otra propuesta.");
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("vacantes");

        var propuesta = modelo.Entity<PropuestaIngresos>();
        propuesta.ToTable("propuestas_ingresos");
        propuesta.HasKey(x => x.Id);
        propuesta.Property(x => x.Id).ValueGeneratedNever();
        propuesta.Property(x => x.Motivo).IsRequired();
        propuesta.Property(x => x.HuellaEntradas).HasMaxLength(64).IsRequired();
        propuesta.Property(x => x.Entradas).HasColumnType("jsonb").IsRequired();
        propuesta.Property(x => x.Resultados).HasColumnType("jsonb").IsRequired();
        propuesta.Property(x => x.PropuestaAnteriorId);
        propuesta.HasIndex(x => new { x.PeriodoId, x.Fecha });

        var ocupacion = modelo.Entity<OcupacionPlaza>();
        ocupacion.ToTable("ocupaciones_plazas");
        ocupacion.HasKey(x => x.Id);
        ocupacion.Property(x => x.Id).ValueGeneratedNever();
        ocupacion.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
        ocupacion.Property(x => x.Revision).IsConcurrencyToken();
        ocupacion.HasIndex(x => new { x.PeriodoId, x.AspiranteId }).IsUnique().HasFilter("\"Activa\" = true");

        var aceptacion = modelo.Entity<AceptacionTurnoFamiliar>();
        aceptacion.ToTable("aceptaciones_turno");
        aceptacion.HasKey(x => x.Id);
        aceptacion.Property(x => x.Id).ValueGeneratedNever();
        aceptacion.Property(x => x.Referencia).HasMaxLength(250);
        aceptacion.HasIndex(x => new { x.PropuestaId, x.SolicitudId, x.TurnoId }).IsUnique();

        var ingreso = modelo.Entity<IngresoConfirmado>();
        ingreso.ToTable("ingresos_confirmados");
        ingreso.HasKey(x => x.Id);
        ingreso.Property(x => x.Id).ValueGeneratedNever();
        ingreso.Property(x => x.Revision).IsConcurrencyToken();
        ingreso.HasIndex(x => new { x.PeriodoId, x.AspiranteId }).IsUnique();

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
