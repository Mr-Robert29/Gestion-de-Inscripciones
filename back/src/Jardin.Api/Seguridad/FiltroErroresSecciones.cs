using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jardin.Api.Seguridad;

public sealed class FiltroErroresSecciones : IExceptionFilter
{
    // Implementa IExceptionFilter.OnException: traduce errores de operaciones de Secciones.
    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is UnauthorizedAccessException)
            contexto.Result = new ForbidResult();
        else
        {
            var estado = contexto.Exception switch
            {
                KeyNotFoundException => 404,
                ArgumentException => 400,
                InvalidOperationException => 409,
                _ => 0
            };
            if (estado == 0) return;
            contexto.Result = new ObjectResult(new ProblemDetails
            {
                Status = estado,
                Detail = contexto.Exception.Message
            }) { StatusCode = estado };
        }
        contexto.ExceptionHandled = true;
    }
}
