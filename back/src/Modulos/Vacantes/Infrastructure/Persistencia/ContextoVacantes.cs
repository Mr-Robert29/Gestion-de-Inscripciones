using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class ContextoVacantes : DbContext
{
    public DbSet<Sorteo> Sorteos => Set<Sorteo>();

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
    }
}
