using System.Text.Json;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class ContextoSecciones : DbContext
{
    public DbSet<Seccion> Secciones => Set<Seccion>();

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
    }
}
