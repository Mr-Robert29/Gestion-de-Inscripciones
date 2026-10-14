using Jardin.Usuarios.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Usuarios.Infrastructure.Persistencia;

public class ContextoUsuarios : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public DbSet<UsuarioPersonal> UsuariosPersonal => Set<UsuarioPersonal>();
    public DbSet<AccesoInstitucion> AccesosInstitucion => Set<AccesoInstitucion>();

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

            b.HasMany(u => u.Accesos)
             .WithOne()
             .HasForeignKey(a => a.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AccesoInstitucion>(b =>
        {
            b.ToTable("accesos_institucion");
            b.HasKey(a => a.Id);
            b.Property(a => a.UsuarioId).IsRequired();
            b.Property(a => a.InstitucionId).IsRequired();
            b.Property(a => a.Rol).HasMaxLength(50).IsRequired();
            b.HasIndex(a => new { a.UsuarioId, a.InstitucionId }).IsUnique();
        });
    }
}
