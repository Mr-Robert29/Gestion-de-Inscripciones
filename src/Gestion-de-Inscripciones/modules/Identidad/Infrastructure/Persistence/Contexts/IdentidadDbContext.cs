using Microsoft.EntityFrameworkCore;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Contexts
{
    public class IdentidadDbContext : DbContext
    {
        public const string Schema = "identidad";

        public IdentidadDbContext(DbContextOptions<IdentidadDbContext> options) : base(options)
        {
        }

        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Responsable> Responsables { get; set; }
        public DbSet<Domicilio> Domicilios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentidadDbContext).Assembly, 
                type => type.Namespace != null && type.Namespace.Contains("Identidad.Infrastructure.Persistence.Configurations"));
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
