using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

// Cada módulo conserva sus tablas; los objetos de valor se guardan junto a la institución.
public sealed class ContextoConfiguracion : DbContext
{
    public DbSet<Institucion> Instituciones => Set<Institucion>();
    public DbSet<CambioInstitucion> Cambios => Set<CambioInstitucion>();
    public DbSet<UsoUbicacion> UsosUbicacion => Set<UsoUbicacion>();
    public DbSet<PeriodoInscripcion> Periodos => Set<PeriodoInscripcion>();

    public ContextoConfiguracion(DbContextOptions<ContextoConfiguracion> opciones) : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("configuracion");
        var periodo = modelo.Entity<PeriodoInscripcion>();
        periodo.ToTable("periodos");
        periodo.HasKey(p => p.Id);
        periodo.Property(p => p.Id).ValueGeneratedNever();
        periodo.Property(p => p.Estado).HasConversion<string>().IsRequired();
        periodo.Property(p => p.Revision).IsConcurrencyToken();
        periodo.HasOne<Institucion>().WithMany().HasForeignKey(p => p.InstitucionId).OnDelete(DeleteBehavior.Restrict);
        periodo.HasIndex(p => p.InstitucionId);
        var institucion = modelo.Entity<Institucion>();
        institucion.ToTable("instituciones");
        institucion.HasKey(i => i.Id);
        institucion.Property(i => i.Id).ValueGeneratedNever();
        institucion.Property(i => i.Nombre).IsRequired();
        institucion.Property(i => i.TelefonoContacto).IsRequired();
        institucion.Property(i => i.RevisionDatos).IsConcurrencyToken();
        institucion.OwnsOne(i => i.Domicilio, domicilio =>
        {
            domicilio.Property(d => d.Calle).IsRequired();
            domicilio.Property(d => d.Numero).IsRequired();
            domicilio.Property(d => d.Localidad).IsRequired();
            domicilio.Property(d => d.Provincia).IsRequired();
            domicilio.Property(d => d.CodigoPostal);
            domicilio.Property(d => d.Complemento);
            domicilio.Property(d => d.Piso);
            domicilio.Property(d => d.Departamento);
            domicilio.Property(d => d.Barrio);
        });
        institucion.Navigation(i => i.Domicilio).IsRequired();
        institucion.OwnsOne(i => i.Coordenadas, coordenadas =>
        {
            coordenadas.Property(c => c.Latitud).IsRequired();
            coordenadas.Property(c => c.Longitud).IsRequired();
        });

        var cambio = modelo.Entity<CambioInstitucion>();
        cambio.ToTable("historial_instituciones");
        cambio.HasKey(c => c.Id);
        cambio.Property(c => c.Id).ValueGeneratedNever();
        cambio.Property(c => c.Accion).IsRequired();
        cambio.Property(c => c.Motivo).IsRequired();
        cambio.Property(c => c.DatosNuevos).IsRequired();
        cambio.HasOne<Institucion>().WithMany().HasForeignKey(c => c.InstitucionId).OnDelete(DeleteBehavior.Restrict);
        cambio.HasIndex(c => c.InstitucionId);

        var uso = modelo.Entity<UsoUbicacion>();
        uso.ToTable("usos_ubicacion");
        uso.HasKey(u => u.Id);
        uso.Property(u => u.Id).ValueGeneratedNever();
        uso.Property(u => u.Tipo).IsRequired();
        uso.HasOne<Institucion>().WithMany().HasForeignKey(u => u.InstitucionId).OnDelete(DeleteBehavior.Restrict);
        uso.HasIndex(u => new { u.InstitucionId, u.ReferenciaId, u.Tipo, u.RevisionUbicacion }).IsUnique();
    }
}
