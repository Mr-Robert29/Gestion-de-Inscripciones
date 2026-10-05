using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Configurations
{
    internal class ResponsableConfiguration : IEntityTypeConfiguration<Responsable>
    {
        public void Configure(EntityTypeBuilder<Responsable> builder)
        {
            builder.ToTable("Responsables");
            
            builder.HasKey(r => r.IdResponsable);
            
            builder.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Apellido).IsRequired().HasMaxLength(100);
            builder.Property(r => r.TipoDocumento).IsRequired().HasMaxLength(20);
            builder.Property(r => r.NroDocumento).IsRequired().HasMaxLength(20);
            builder.Property(r => r.Telefono).HasMaxLength(50);
            builder.Property(r => r.Email).HasMaxLength(150);
            builder.Property(r => r.Vinculo).HasMaxLength(50);
            builder.Property(r => r.EsTutor).IsRequired();
        }
    }
}
