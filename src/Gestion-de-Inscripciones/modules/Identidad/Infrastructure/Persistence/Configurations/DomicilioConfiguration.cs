using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Configurations
{
    internal class DomicilioConfiguration : IEntityTypeConfiguration<Domicilio>
    {
        public void Configure(EntityTypeBuilder<Domicilio> builder)
        {
            builder.ToTable("Domicilios");
            
            builder.HasKey(d => d.IdDomicilio);

            builder.Property(d => d.Calle).IsRequired().HasMaxLength(150);
            builder.Property(d => d.Numero).IsRequired().HasMaxLength(20);
            builder.Property(d => d.Piso).HasMaxLength(10);
            builder.Property(d => d.Departamento).HasMaxLength(10);
            builder.Property(d => d.Barrio).HasMaxLength(100);
            builder.Property(d => d.CodigoPostal).HasMaxLength(20);
            
            // Para PostgreSQL u otros motores, usamos precisión para las coordenadas
            builder.Property(d => d.Latitud).HasColumnType("numeric(18,9)");
            builder.Property(d => d.Longitud).HasColumnType("numeric(18,9)");
        }
    }
}
