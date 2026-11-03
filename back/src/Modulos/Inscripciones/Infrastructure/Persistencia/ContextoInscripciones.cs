using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class ContextoInscripciones : DbContext
{
    public DbSet<DocumentacionSolicitud> DocumentosSolicitud => Set<DocumentacionSolicitud>();
    public DbSet<CondicionPrioridad> CondicionesPrioridad => Set<CondicionPrioridad>();
    public DbSet<UsoDatosSolicitud> UsosDatosSolicitud => Set<UsoDatosSolicitud>();
    public DbSet<SolicitudInscripcion> Solicitudes => Set<SolicitudInscripcion>();
    public DbSet<Aspirante> Aspirantes => Set<Aspirante>();
    public DbSet<Responsable> Responsables => Set<Responsable>();
    public DbSet<VinculoResponsable> Vinculos => Set<VinculoResponsable>();
    public DbSet<RegistroAspiranteInstitucion> AspirantesInstituciones => Set<RegistroAspiranteInstitucion>();
    public DbSet<RegistroResponsableInstitucion> ResponsablesInstituciones => Set<RegistroResponsableInstitucion>();
    public DbSet<CambioInscripciones> Cambios => Set<CambioInscripciones>();

    public ContextoInscripciones(DbContextOptions<ContextoInscripciones> opciones) : base(opciones) { }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("inscripciones");
        modelo.HasDbFunction(typeof(TextoBusquedaInscripciones).GetMethod(nameof(TextoBusquedaInscripciones.Mayusculas))!)
            .HasName("upper").IsBuiltIn();
        MapeoSolicitudes.Configurar(modelo);
        MapeoAntecedentesSolicitud.Configurar(modelo);
        var uso = modelo.Entity<UsoDatosSolicitud>();
        uso.ToTable("usos_datos_solicitud");
        uso.HasKey(u => u.Id);
        uso.Property(u => u.Id).ValueGeneratedNever();
        uso.Property(u => u.Tipo).IsRequired();
        uso.HasOne<SolicitudInscripcion>().WithMany().HasForeignKey(u => u.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        uso.HasIndex(u => new { u.SolicitudId, u.ReferenciaId, u.Tipo, u.RevisionDatos, u.RevisionDomicilio })
            .IsUnique().HasDatabaseName("UX_UsoSolicitud_Referencia_Revision");
        var aspirante = modelo.Entity<Aspirante>();
        aspirante.ToTable("aspirantes");
        aspirante.HasKey(a => a.Id);
        aspirante.Property(a => a.Id).ValueGeneratedNever();
        aspirante.Property(a => a.Nombre).IsRequired();
        aspirante.Property(a => a.Apellido).IsRequired();
        aspirante.HasIndex(a => a.Dni);
        aspirante.Navigation(a => a.Vinculos).UsePropertyAccessMode(PropertyAccessMode.Field);
        var responsable = modelo.Entity<Responsable>();
        responsable.ToTable("responsables");
        responsable.HasKey(r => r.Id);
        responsable.Property(r => r.Id).ValueGeneratedNever();
        responsable.Property(r => r.Nombre).IsRequired();
        responsable.Property(r => r.Apellido).IsRequired();
        responsable.Property(r => r.Telefono).IsRequired();
        // Documento no es una clave de identidad: no fusionamos personas automáticamente.
        responsable.HasIndex(r => r.Documento);
        var vinculo = modelo.Entity<VinculoResponsable>();
        vinculo.ToTable("vinculos_responsables");
        vinculo.HasKey(v => new { v.AspiranteId, v.ResponsableId });
        vinculo.Property(v => v.TipoVinculo).IsRequired();
        vinculo.HasOne<Aspirante>().WithMany(a => a.Vinculos).HasForeignKey(v => v.AspiranteId).OnDelete(DeleteBehavior.Restrict);
        vinculo.HasOne(v => v.Responsable).WithMany().HasForeignKey(v => v.ResponsableId).OnDelete(DeleteBehavior.Restrict);
        var registroAspirante = modelo.Entity<RegistroAspiranteInstitucion>();
        registroAspirante.ToTable("aspirantes_instituciones");
        registroAspirante.HasKey(r => new { r.InstitucionId, r.AspiranteId });
        registroAspirante.HasOne<Aspirante>().WithMany().HasForeignKey(r => r.AspiranteId).OnDelete(DeleteBehavior.Restrict);
        var registroResponsable = modelo.Entity<RegistroResponsableInstitucion>();
        registroResponsable.ToTable("responsables_instituciones");
        registroResponsable.HasKey(r => new { r.InstitucionId, r.ResponsableId });
        registroResponsable.HasOne<Responsable>().WithMany().HasForeignKey(r => r.ResponsableId).OnDelete(DeleteBehavior.Restrict);
        // InstitucionId es una referencia externa: no hay FK ni acceso a tablas de Configuración.
        var cambio = modelo.Entity<CambioInscripciones>();
        cambio.ToTable("historial_personas");
        cambio.HasKey(c => c.Id);
        cambio.Property(c => c.Id).ValueGeneratedNever();
        cambio.Property(c => c.Accion).IsRequired();
        cambio.Property(c => c.Datos).IsRequired();
        cambio.HasIndex(c => c.InstitucionId);
    }
}
