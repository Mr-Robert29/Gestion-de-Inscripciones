using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{ 
    public class Alumno
    {
        public int IdAlumno { get; private set; }
        public string Nombre { get; private set; }
        public string Apellido { get; private set; }
        public string TipoDocumento { get; private set; }
        public string NroDocumento { get; private set; }
        public DateTime FechaNacimiento { get; private set; }
        public string Sexo { get; private set; }//podria ser genero tambien
        public Domicilio Domicilio {  get; private set; }

        private readonly List<Responsable> _responsables = new List<Responsable>();
        public IReadOnlyCollection<Responsable> Responsables => _responsables.AsReadOnly();
        
        protected Alumno() { }

        //este constructor reemplaza al Registrar(datos) del UML
        public Alumno(string nombre, string apellido, string tipoDocumento, string nroDocumento, DateTime fechaNacimiento, string sexo)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio");

            Nombre = nombre;
            Apellido = apellido;
            TipoDocumento = tipoDocumento;
            NroDocumento = nroDocumento;
            FechaNacimiento = fechaNacimiento;
            Sexo = sexo;
        }
        public void AsociarResponsable(Responsable responsable)
        {
            if (responsable == null) throw new ArgumentNullException(nameof(responsable));

            // RF-03: Permitir asociar uno o más responsables a un alumno
            if (!_responsables.Contains(responsable))
            {
                _responsables.Add(responsable);
            }
        }
        public void RegistrarDomicilio(Domicilio domicilio)
        {
            Domicilio = domicilio ?? throw new ArgumentNullException(nameof(domicilio));
        }
        public int ObtenerGrupoEtario(DateTime fechaReferencia)
        {
            // fechaReferencia debe ser la fecha en la cual se esta cursando el ciclo lectivo actual
            var edad = fechaReferencia.Year - FechaNacimiento.Year;
            if (FechaNacimiento.Date > fechaReferencia.AddYears(-edad)) edad--;
            return edad;
        }

    }
}