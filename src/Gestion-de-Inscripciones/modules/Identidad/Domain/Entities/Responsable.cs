namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public class Responsable
    {
        public int IdResponsable { get; private set; }
        public string Nombre { get; private set; }
        public string Apellido { get; private set; }
        public string TipoDocumento { get; private set; }
        public string NroDocumento { get; private set; }
        public string Telefono { get; private set; }
        public string Email { get; private set; }
        public string Vinculo { get; private set; }
        public Boolean EsTutor { get; private set; }
        public Responsable() { }
        public Responsable
            (string nombre, string apellido,string tipoDocumento, string nroDocumento, 
            string telefono, string eMail, string vinculo, Boolean esTutor)
        {
            Nombre = nombre;
            Apellido = apellido;
            TipoDocumento = tipoDocumento;
            NroDocumento = nroDocumento;
            Telefono = telefono;
            Email = eMail;
            Vinculo = vinculo;
            EsTutor = esTutor;
        }
        public void ModificarDatos(string telefono, string email, string vinculo, bool esTutor)
        {
            Telefono = telefono;
            Email = email;
            Vinculo = vinculo;
            EsTutor = esTutor;
        }
    }
}
