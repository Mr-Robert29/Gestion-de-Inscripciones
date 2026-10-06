using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Queries;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Usuarios.Queries;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories;
using SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Contexts;
using SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Repositories;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Api.Extensions
{
    public static class IdentidadModuleExtensions
    {
        public static IServiceCollection AddIdentidadModule(this IServiceCollection services, IConfiguration configuration)
        {
            // configuracion con postgre
            services.AddDbContext<IdentidadDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("IdentidadDbConnection")));

            // Registro de Repositorios (Interfaces -> Implementaciones)
            services.AddScoped<IAlumnoRepository, AlumnoRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            
            services.AddScoped<RegistrarAlumnoCommandHandler>();
            services.AddScoped<ConsultarAlumnoQueryHandler>();
            services.AddScoped<AsociarResponsableCommandHandler>();
            services.AddScoped<ConsultarUsuarioQueryHandler>();

            return services;
        }
    }
}
