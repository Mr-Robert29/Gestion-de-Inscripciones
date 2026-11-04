using System.Text.Json;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class ContextoSecciones : DbContext
{
    public DbSet<CambioComposicionSeccion> CambiosComposicion => Set<CambioComposicionSeccion>();
    public DbSet<Seccion> Secciones => Set<Seccion>();
    public DbSet<PropuestaDistribucion> PropuestasDistribucion => Set<PropuestaDistribucion>();
    public DbSet<AsignacionPropuesta> AsignacionesPropuesta => Set<AsignacionPropuesta>();
    public DbSet<AsignacionSeccion> AsignacionesSeccion => Set<AsignacionSeccion>();
    public DbSet<IngresoRetiradoSecciones> IngresosRetirados => Set<IngresoRetiradoSecciones>();
    public DbSet<CambioBajaAsignacion> CambiosBajaAsignaciones => Set<CambioBajaAsignacion>();

    public ContextoSecciones(DbContextOptions<ContextoSecciones> opciones)
        : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("secciones");
        builder.Entity<CambioComposicionSeccion>(b =>
        {
            b.ToTable("cambios_composicion");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.HasIndex(x => new { x.SeccionId, x.RevisionNueva }).IsUnique();
        });
        builder.Entity<CambioBajaAsignacion>(b =>
        {
            b.ToTable("cambios_baja_asignaciones");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.HasIndex(x => x.IngresoId);
        });
        builder.Entity<IngresoRetiradoSecciones>(b =>
        {
            b.ToTable("ingresos_retirados");
            b.HasKey(x => x.IngresoId);
            b.Property(x => x.IngresoId).ValueGeneratedNever();
            b.Property(x => x.Motivo).IsRequired();
        });

        builder.Entity<Seccion>(b =>
        {
            b.Ignore(x => x.EdadesAdmitidas);
            b.ToTable("secciones");
            b.HasKey(x => x.Id);
            b.Property(x => x.PeriodoId).IsRequired();
            b.Property(x => x.AulaId).IsRequired();
            b.Property(x => x.TurnoId).IsRequired();
            b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Capacidad).IsRequired();
            b.Property(x => x.Revision).IsRequired();
            b.Property(x => x.Activa).IsRequired();
            b.Property<List<int>>("_edadesAdmitidas")
                .HasColumnName("edades_admitidas")
                .IsRequired()
                .HasConversion(
                    value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    value => JsonSerializer.Deserialize<List<int>>(value, (JsonSerializerOptions?)null) ?? new List<int>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<int>>(
                    (primera, segunda) => primera != null && segunda != null && primera.SequenceEqual(segunda),
                    edades => edades.Aggregate(0, (actual, edad) => HashCode.Combine(actual, edad)),
                    edades => edades.ToList()));
            b.HasIndex(x => new { x.PeriodoId, x.AulaId, x.TurnoId });
        });

        builder.Entity<PropuestaDistribucion>(b =>
        {
            b.Ignore(x => x.Asignaciones);
            b.ToTable("propuestas_distribucion");
            b.HasKey(x => x.Id);
            b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.FechaCreacion).IsRequired();
            b.Property(x => x.CreadaPor).IsRequired();
            b.HasIndex(x => new { x.PeriodoId, x.Version }).IsUnique();
            b.HasMany<AsignacionPropuesta>("_asignaciones")
                .WithOne()
                .HasForeignKey(x => x.PropuestaDistribucionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AsignacionPropuesta>(b =>
        {
            b.ToTable("asignaciones_propuesta");
            b.HasKey(x => x.Id);
            b.Property(x => x.MotivoSinSeccion).HasMaxLength(500);
            b.Property(x => x.MotivoModificacion).HasMaxLength(500);
            b.HasIndex(x => new { x.PropuestaDistribucionId, x.IngresoId }).IsUnique();
        });

        builder.Entity<AsignacionSeccion>(b =>
        {
            b.ToTable("asignaciones_seccion");
            b.HasKey(x => x.Id);
            b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Fecha).IsRequired();
            b.Property(x => x.UsuarioId).IsRequired();
            b.Property(x => x.Motivo).HasMaxLength(500);
            b.HasIndex(x => x.IngresoId).IsUnique().HasFilter("\"Estado\" = 'Activa'");
        });
    }
}
