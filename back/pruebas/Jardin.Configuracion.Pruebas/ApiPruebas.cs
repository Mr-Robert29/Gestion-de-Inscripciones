using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Api;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Domain.Entidades;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Jardin.Configuracion.Pruebas;

// Bases aisladas por módulo: nunca se usa la conexión local de la aplicación.
public sealed class ApiPruebas : WebApplicationFactory<PuntoEntradaApi>
{
    private readonly SqliteConnection _configuracion = new("Data Source=:memory:");
    private readonly SqliteConnection _inscripciones = new("Data Source=:memory:");
    private readonly SqliteConnection _usuarios = new("Data Source=:memory:");
    private readonly SqliteConnection _vacantes = new("Data Source=:memory:");
    private readonly string _contenidoApi;

    public ApiPruebas(string contenidoApi)
    {
        _contenidoApi = contenidoApi;
        _configuracion.Open();
        _inscripciones.Open();
        _usuarios.Open();
        _vacantes.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseContentRoot(_contenidoApi);
        constructor.UseEnvironment("Pruebas");
        constructor.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ClaveSecreta"] = "clave-ficticia-solo-pruebas-configuracion-2026",
            ["Jwt:Emisor"] = "Jardin.Api",
            ["Jwt:Audiencia"] = "Jardin.Frontend"
        }));
        constructor.ConfigureLogging(registro => registro.ClearProviders());
        constructor.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<ContextoConfiguracion>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoConfiguracion>>();
            servicios.RemoveAll<DbContextOptions<ContextoInscripciones>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoInscripciones>>();
            servicios.RemoveAll<DbContextOptions<ContextoUsuarios>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoUsuarios>>();
            servicios.RemoveAll<DbContextOptions<ContextoVacantes>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoVacantes>>();
            var conexionPostgres = Environment.GetEnvironmentVariable("HU01_POSTGRES_PRUEBAS");
            if (!string.IsNullOrWhiteSpace(conexionPostgres) &&
                new Npgsql.NpgsqlConnectionStringBuilder(conexionPostgres).Database?.StartsWith("hu01_pruebas_", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Usá una base exclusiva con prefijo hu01_pruebas_.");
            servicios.AddDbContext<ContextoConfiguracion>(opciones =>
            {
                if (string.IsNullOrWhiteSpace(conexionPostgres))
                    opciones.UseSqlite(_configuracion);
                else
                    opciones.UseNpgsql(conexionPostgres, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "configuracion"));
            });
            servicios.AddDbContext<ContextoInscripciones>(opciones =>
            {
                if (string.IsNullOrWhiteSpace(conexionPostgres))
                    opciones.UseSqlite(_inscripciones);
                else
                    opciones.UseNpgsql(conexionPostgres, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "inscripciones"));
            });
            servicios.AddDbContext<ContextoUsuarios>(opciones => opciones.UseSqlite(_usuarios));
            servicios.AddDbContext<ContextoVacantes>(opciones => opciones.UseSqlite(_vacantes));
        });
    }

    public async Task PrepararAsync()
    {
        using var alcance = Services.CreateScope();
        var contextoConfiguracion = alcance.ServiceProvider.GetRequiredService<ContextoConfiguracion>();
        if (contextoConfiguracion.Database.IsNpgsql())
            await contextoConfiguracion.Database.MigrateAsync();
        else
            await contextoConfiguracion.Database.EnsureCreatedAsync();
        var contextoInscripciones = alcance.ServiceProvider.GetRequiredService<ContextoInscripciones>();
        if (contextoInscripciones.Database.IsNpgsql())
            await contextoInscripciones.Database.MigrateAsync();
        else
            await contextoInscripciones.Database.EnsureCreatedAsync();
        await alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>().Database.EnsureCreatedAsync();
        await alcance.ServiceProvider.GetRequiredService<ContextoVacantes>().Database.EnsureCreatedAsync();
        var roles = alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var rol in RolesPersonal.Todos)
            await roles.CreateAsync(new IdentityRole<Guid>(rol));
        await CrearUsuarioAsync("duplicados@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("distancias@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("correcciones@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("antecedentes@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("solicitudes@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("inscripciones@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("root@prueba.example", RolesPersonal.AdministradorDelSistema);
        await CrearUsuarioAsync("administrativo@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("directivo@prueba.example", RolesPersonal.Directivo);
        await CrearUsuarioAsync("ajeno@prueba.example", RolesPersonal.Directivo);
        await CrearUsuarioAsync("propuestas@prueba.example", RolesPersonal.Administrativo);
        await CrearUsuarioAsync("propuestas-ajeno@prueba.example", RolesPersonal.Directivo);
    }

    private async Task CrearUsuarioAsync(string correo, string rol)
    {
        using var alcance = Services.CreateScope();
        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var cuenta = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = correo, Email = correo };
        var resultado = await administrador.CreateAsync(cuenta, "Prueba123!");
        if (!resultado.Succeeded) throw new InvalidOperationException("No se pudo preparar la cuenta ficticia.");
        await administrador.AddToRoleAsync(cuenta, rol);
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
        contexto.UsuariosPersonal.Add(new UsuarioPersonal(cuenta.Id, "Personal de prueba", correo));
        await contexto.SaveChangesAsync();
    }

    public async Task AsignarAccesoAsync(
        string correo,
        Guid institucionId,
        string rol)
    {
        using var alcance = Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
        var usuario = await contexto.UsuariosPersonal.SingleAsync(u => u.CorreoElectronico == correo);
        usuario.AsignarAcceso(institucionId, rol);
        await contexto.SaveChangesAsync();
    }
}
