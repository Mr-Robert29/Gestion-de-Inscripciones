using System.Text;
using Jardin.Api.Middlewares;
using Jardin.Usuarios.Infrastructure;
using Jardin.Usuarios.Infrastructure.Inicializacion;
using Jardin.Usuarios.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores
builder.Services.AddControllers();

// 2. Documentacion OpenAPI y Swagger
builder.Services.AddOpenApi();

// 3. Modulo Usuarios (Infrastructure)
builder.Services.AgregarModuloUsuarios(builder.Configuration);

// 4. Autenticacion JWT Bearer
var jwtConfig = builder.Configuration.GetSection(ConfiguracionJwt.Seccion).Get<ConfiguracionJwt>() ?? new ConfiguracionJwt();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Emisor,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.ClaveSecreta)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 5. CORS
builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(politica =>
    {
        politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// 6. Pipeline HTTP
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
using (var scope = app.Services.CreateScope())
{
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
