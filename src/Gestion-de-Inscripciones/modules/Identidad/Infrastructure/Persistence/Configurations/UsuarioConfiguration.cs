using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Configurations
{
    internal class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            
            builder.HasKey(u => u.IdUsuario);

            builder.Property(u => u.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Rol).IsRequired().HasMaxLength(50);

            builder.Property("_historialAcciones")
            .HasColumnName("HistorialAcciones");

            builder.HasDiscriminator<string>("TipoUsuario")
                .HasValue<Director>("Director")
                .HasValue<Administrativo>("Administrativo")
                .HasValue<Maestro>("Maestro");
        }
    }
}
