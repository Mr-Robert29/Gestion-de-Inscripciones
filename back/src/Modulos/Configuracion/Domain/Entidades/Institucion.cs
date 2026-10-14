using Jardin.Configuracion.Domain.ObjetosDeValor;
using System;
// Representa el JARDIN
namespace Jardin.Configuracion.Domain.Entidades
{
    public class Institucion
    {
        //atributos de la clase Institucion
        public Guid Id { get; } // Guid = codigo de 128  bits ejemplo : 123e4567-e89b-12d3-a456-426614174000
        public Domicilio Domicilio { get; private set; } // composición de la clase Institucion con Domicilio
        public Coordenadas? Coordenadas { get; private set; } // composición de la clase Institucion con Coordenadas
        public string Nombre { get; private set; }
        public string? Codigo { get; private set; }
        public string TelefonoContacto { get; private set; }
        public string? CorreoElectronicoContacto { get; private set; }
        public bool Activa { get; private set; }
        public int RevisionUbicacion { get; private set; }

        //constructor de la clase Institucion(CREAMOS el OBJETO)
        public Institucion(string nombre,
            Domicilio domicilio,
            string telefonoContacto,
            string? codigo = null,
            string? correoElectronicoContacto = null,
            Coordenadas? coordenadas = null)
        {
            
            ValidarNombre(nombre); // comprobamos que el nombre no sea nulo o vacio
            ValidarDomicilio(domicilio); // comprobamos que el domicilio no sea nulo
            ValidarTelefonoContacto(telefonoContacto); // comprobamos que el telefono de contacto no sea nulo

            Id = Guid.NewGuid(); // generamos un nuevo GUID para la institucion
            Nombre = nombre.Trim(); // eliminamos los espacios en blanco al inicio y al final del nombre
            Domicilio = domicilio;
            TelefonoContacto = telefonoContacto.Trim();
            Codigo = NormalizarOpcional(codigo);
            CorreoElectronicoContacto = NormalizarOpcional(correoElectronicoContacto); // normalizamos el correo electronico de contacto
            Coordenadas = coordenadas;
            Activa = true; // por defecto la institucion esta activa
            RevisionUbicacion = 1; // inicializamos la revision de ubicacion en 1
        }

        //metodos publicos para actualizar los datos de contacto y ubicacion
        public void ActualizarDatos(
            string nombre,
            string? codigo,
            Domicilio domicilio)
        {
            // cambia el nombre, el codigo y el domicilio de la institucion
            ValidarNombre(nombre);
            ValidarDomicilio(domicilio);

            Nombre = nombre.Trim();
            Codigo = NormalizarOpcional(codigo);

            if (Domicilio != domicilio)
            {
                Domicilio = domicilio;

                // La ubicación anterior debe revisarse ante otra dirección.
                Coordenadas = null;
                RevisionUbicacion++;
            }
        }

        
        public void ActualizarContacto(string telefonoContacto,string? correoElectronicoContacto)
        {
            ValidarTelefonoContacto(telefonoContacto);

            TelefonoContacto = telefonoContacto.Trim();
            CorreoElectronicoContacto =
                NormalizarOpcional(correoElectronicoContacto);
        }

        public void ActualizarUbicacion(Coordenadas coordenadas)
        {
            ValidarCoordenadas(coordenadas);

            if (Coordenadas != coordenadas)
            {
                Coordenadas = coordenadas;
                RevisionUbicacion++;
            }
        }

        // metodo para desactivar la institucion
        public void Desactivar()
        {
            Activa = false;
        }

        //metodos privados de validación y normalización
        private static void ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("El nombre de la institución no puede estar vacío.",nameof(nombre));
            }
        }

        private static void ValidarDomicilio(Domicilio domicilio)
        {
            ArgumentNullException.ThrowIfNull(domicilio);
        }
        private static void ValidarTelefonoContacto(string telefonoContacto)
        {
            ArgumentNullException.ThrowIfNull(telefonoContacto);
        }
        private static void ValidarCoordenadas(Coordenadas coordenadas)
        {
            ArgumentNullException.ThrowIfNull(coordenadas);
        }
        // Convierte texto vacío en null y elimina espacios exteriores si hay texto.
        private static string? NormalizarOpcional(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Trim();
        }
    }
}
