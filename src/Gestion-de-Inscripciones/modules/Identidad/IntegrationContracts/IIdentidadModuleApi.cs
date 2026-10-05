namespace SistemaAsignacionEscolar.Api.modules.Identidad.IntegrationContracts
{
    public interface IIdentidadModuleApi
    {
        // Devuelve un DTO simple
        Task<int> ObtenerEdadAlumnoAsync(int idAlumno);
    }
}
