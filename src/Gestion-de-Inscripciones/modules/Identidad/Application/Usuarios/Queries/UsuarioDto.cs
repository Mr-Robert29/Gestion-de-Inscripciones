namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Usuarios.Queries
{
    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
    }
}
