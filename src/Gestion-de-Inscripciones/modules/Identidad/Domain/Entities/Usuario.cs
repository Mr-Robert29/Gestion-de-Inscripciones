namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public abstract class Usuario
    {
        public int IdUsuario { get; private set; }
        public string Nombre { get; private set; }
        public string Email { get; private set; }
        public string Rol { get; protected set; }
        private readonly List<string> _historialAcciones = new List<string>();

        protected Usuario(string nombre, string email)
        {
            Nombre = nombre;
            Email = email;
        }

        public void Identificar()
        {
            // Lógica de dominio para marcar el login/sesión[cite: 16]
        }

        public void RegistrarAccion(string accion)
        {
            _historialAcciones.Add($"{DateTime.Now}: {accion}");
        }
        public abstract bool TienePermiso(string operacion);
    }
}
