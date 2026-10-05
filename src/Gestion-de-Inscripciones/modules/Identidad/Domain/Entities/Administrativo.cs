namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public class Administrativo : Usuario
    {
        public Administrativo(string nombre, string email) : base(nombre, email)
        {
            Rol = "Administrativo";
        }

        public override bool TienePermiso(string operacion)
        {
            var permisosAdmin = new List<string> { "RegistrarSolicitud", "ModificarAsignacion", "ResolverCasoManual" };
            return permisosAdmin.Contains(operacion);
        }
    }
}
