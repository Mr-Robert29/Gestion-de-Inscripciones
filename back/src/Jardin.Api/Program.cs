using Jardin.Api.Integraciones;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Infrastructure;
using Jardin.Inscripciones.Application.Seguridad;
using System.Text;
using Jardin.Configuracion.Infrastructure;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Api.Seguridad;
using Jardin.Api.Middlewares;
using Jardin.Secciones.Infrastructure;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Infrastructure;
using Jardin.Usuarios.Infrastructure;
using Jardin.Usuarios.Infrastructure.Inicializacion;
using Jardin.Usuarios.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// Cargar variables de entorno desde archivo .env si existe en desarrollo local
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Pruebas")
    CargarArchivoEnv();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();


builder.Services.AddOpenApi();


builder.Services.AgregarModuloUsuarios(builder.Configuration);
builder.Services.AgregarModuloConfiguracion(builder.Configuration);
builder.Services.AgregarModuloInscripciones(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoUsuarioConfiguracion, ContextoUsuarioConfiguracion>();
builder.Services.AddScoped<IAccesoInstitucionesInscripciones, AccesoInstitucionesInscripciones>();
builder.Services.AddScoped<IEjecucionPeriodoSolicitud, EjecucionPeriodoSolicitud>();

builder.Services.AgregarModuloSecciones(builder.Configuration);
builder.Services.AgregarModuloVacantes(builder.Configuration);
builder.Services.AddScoped<IAutorizacionVacantes, AccesoVacantes>();

var jwtConfig = builder.Configuration.GetSection(ConfiguracionJwt.Seccion).Get<ConfiguracionJwt>() ?? new ConfiguracionJwt();
var claveJwt = !string.IsNullOrWhiteSpace(jwtConfig.ClaveSecreta)
    ? jwtConfig.ClaveSecreta
    : builder.Configuration["Jwt:ClaveSecreta"]
    ?? Environment.GetEnvironmentVariable("Jwt__ClaveSecreta");

if (string.IsNullOrWhiteSpace(claveJwt) || claveJwt.Length < 32)
{
    throw new InvalidOperationException(
        "La clave secreta para la firma JWT no está configurada o tiene menos de 32 caracteres. " +
        "Asegúrese de definir 'Jwt__ClaveSecreta' en su archivo .env o en las variables de entorno.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = !string.IsNullOrWhiteSpace(jwtConfig.Emisor) ? jwtConfig.Emisor : "Jardin.Api",
            ValidateAudience = true,
            ValidAudience = !string.IsNullOrWhiteSpace(jwtConfig.Audiencia) ? jwtConfig.Audiencia : "Jardin.Frontend",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(politica =>
    {
        politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Jardin.Api v1");
        options.RoutePrefix = "swagger";
    });
    app.MapScalarApiReference();
}

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<MiddlewareCuentaActiva>();
app.UseAuthorization();

app.MapControllers();

// 7. Inicializacion de datos iniciales / Seed
if (!app.Environment.IsEnvironment("Pruebas"))
{
    using var scope = app.Services.CreateScope();
    var inicializador = scope.ServiceProvider.GetService<InicializadorUsuarios>();
    if (inicializador is not null)
    {
        try
        {
            await inicializador.InicializarAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning("Inicialización diferida: La base de datos no está disponible actualmente ({Mensaje}).", ex.Message);
        }
    }
}

app.Run();

static void CargarArchivoEnv()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var envPath = Path.Combine(dir.FullName, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var clean = line.Trim();
                if (string.IsNullOrWhiteSpace(clean) || clean.StartsWith('#'))
                    continue;

                var parts = clean.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
            }
            break;
        }
        dir = dir.Parent;
    }
}

namespace Jardin.Api
{
    // Identifica el ensamblado de la API al crear el servidor HTTP de pruebas.
    public sealed class PuntoEntradaApi { }
}
