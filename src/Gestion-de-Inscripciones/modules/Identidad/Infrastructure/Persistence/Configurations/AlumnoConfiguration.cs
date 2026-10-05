using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Configurations
{
    internal class AlumnoConfiguration : IEntityTypeConfiguration<Alumno>
    {
        public void Configure(EntityTypeBuilder<Alumno> builder)
        {
            builder.ToTable("Alumnos");
            
            builder.HasKey(a => a.IdAlumno);
            
            builder.Property(a => a.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Apellido).IsRequired().HasMaxLength(100);
            builder.Property(a => a.TipoDocumento).IsRequired().HasMaxLength(20);
            builder.Property(a => a.NroDocumento).IsRequired().HasMaxLength(20);
            builder.Property(a => a.FechaNacimiento).IsRequired();
            builder.Property(a => a.Sexo).IsRequired().HasMaxLength(20);

            // Relacion con Domicilio (Asumiendo que un Alumno tiene un Domicilio y el foreign key esta en Alumno)
            builder.HasOne(a => a.Domicilio)
                   .WithMany()
                   .HasForeignKey("IdDomicilio");

            // Configurar la lista privada _responsables
            builder.HasMany(a => a.Responsables)
                   .WithMany()
                   .UsingEntity(j => j.ToTable("AlumnoResponsables"));
            
            // Para poder setear el campo de respaldo de la lista
            builder.Metadata.FindNavigation(nameof(Alumno.Responsables))
                   ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
