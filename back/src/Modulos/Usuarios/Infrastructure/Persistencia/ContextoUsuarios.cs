using Jardin.Usuarios.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Usuarios.Infrastructure.Persistencia;

public class ContextoUsuarios : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public DbSet<UsuarioPersonal> UsuariosPersonal => Set<UsuarioPersonal>();
    public DbSet<AccesoInstitucion> AccesosInstitucion => Set<AccesoInstitucion>();
    public DbSet<IdentidadPersonal> IdentidadesPersonal => Set<IdentidadPersonal>();
    public DbSet<EventoAuditoriaPersonal> AuditoriaPersonal => Set<EventoAuditoriaPersonal>();

    public ContextoUsuarios(DbContextOptions<ContextoUsuarios> opciones) : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("usuarios");

        builder.Entity<UsuarioPersonal>(b =>
        {
            b.ToTable("personal");
            b.HasKey(u => u.Id);
            b.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
            b.Property(u => u.CorreoElectronico).HasMaxLength(200).IsRequired();
            b.HasIndex(u => u.CorreoElectronico).IsUnique();
            b.Property(u => u.Activo).IsRequired();
            b.Property(u => u.DebeCambiarClave).IsRequired();
            b.HasOne(u => u.IdentidadPersonal)
             .WithMany()
             .HasForeignKey(u => u.IdentidadPersonalId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(u => u.IdentidadPersonalId);

            b.HasMany(u => u.Accesos)
             .WithOne()
             .HasForeignKey(a => a.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentidadPersonal>(b =>
        {
            b.ToTable("identidades_personal");
            b.HasKey(i => i.Id);
            b.Property(i => i.DniNormalizado).HasMaxLength(8);
            b.HasIndex(i => i.DniNormalizado).IsUnique();
        });

        builder.Entity<EventoAuditoriaPersonal>(b =>
        {
            b.ToTable("auditoria_personal");
            b.HasKey(e => e.Id);
            b.Property(e => e.Accion).HasMaxLength(80).IsRequired();
            b.Property(e => e.Detalle).HasMaxLength(500).IsRequired();
            b.Property(e => e.FechaUtc).IsRequired();
            b.HasIndex(e => e.FechaUtc);
        });

        builder.Entity<AccesoInstitucion>(b =>
        {
            b.ToTable("accesos_institucion");
            b.HasKey(a => a.Id);
            // El dominio genera el Id; EF debe insertar los accesos nuevos, no intentar actualizarlos.
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.UsuarioId).IsRequired();
            b.Property(a => a.InstitucionId).IsRequired();
            b.Property(a => a.Rol).HasMaxLength(50).IsRequired();
            b.HasIndex(a => new { a.UsuarioId, a.InstitucionId }).IsUnique();
        });
    }
}
