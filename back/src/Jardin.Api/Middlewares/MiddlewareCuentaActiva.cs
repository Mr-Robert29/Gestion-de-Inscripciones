using System.Security.Claims;
using Jardin.Usuarios.Application.Abstracciones;

namespace Jardin.Api.Middlewares;

public class MiddlewareCuentaActiva
{
    private readonly RequestDelegate _next;

    public MiddlewareCuentaActiva(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRepositorioUsuarios repositorioUsuarios)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var usuarioId))
            {
                var usuario = await repositorioUsuarios.ObtenerPorIdAsync(usuarioId);
                if (usuario is null || !usuario.Activo)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        mensaje = "La cuenta de usuario ha sido deshabilitada o ya no existe."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
