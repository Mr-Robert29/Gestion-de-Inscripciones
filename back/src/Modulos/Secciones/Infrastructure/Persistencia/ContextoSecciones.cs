using System.Text.Json;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class ContextoSecciones : DbContext
{
    public DbSet<Seccion> Secciones => Set<Seccion>();
    public DbSet<PropuestaDistribucion> PropuestasDistribucion => Set<PropuestaDistribucion>();
    public DbSet<AsignacionPropuesta> AsignacionesPropuesta => Set<AsignacionPropuesta>();
    public DbSet<AsignacionSeccion> AsignacionesSeccion => Set<AsignacionSeccion>();

    public ContextoSecciones(DbContextOptions<ContextoSecciones> opciones)
        : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("secciones");

        builder.Entity<Seccion>(b =>
        {
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
                    value => JsonSerializer.Deserialize<List<int>>(value, (JsonSerializerOptions?)null) ?? new List<int>());
            b.HasIndex(x => new { x.PeriodoId, x.AulaId, x.TurnoId });
        });

        builder.Entity<PropuestaDistribucion>(b =>
        {
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
