using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasReglasDuplicados
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var a = new IdentificacionSolicitud(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "José",
            "Álvarez",
            "10000123",
            new DateOnly(
                2022,
                6,
                30));
        var b = a with { SolicitudId = Guid.NewGuid(), AspiranteId = Guid.NewGuid(), Nombre = "Otra", Apellido = "Persona", Dni = "90000123" };
        comprobar(DetectorPosiblesDuplicados.Evaluar(a, b) == CriterioCoincidenciaSolicitud.Ninguno,
            "Compartir la terminación del DNI no genera una alerta de duplicación.");
        comprobar(DetectorPosiblesDuplicados.Evaluar(a, b with { Dni = a.Dni }) == CriterioCoincidenciaSolicitud.DniCompleto,
            "La coincidencia del DNI completo alerta incluso con nombres diferentes.");
        b = b with { Nombre = "  josé ", Apellido = " áLVAREZ ", Dni = null };
        comprobar(DetectorPosiblesDuplicados.Evaluar(a, b) == CriterioCoincidenciaSolicitud.NombreApellidoNacimiento,
            "Nombre, apellido y nacimiento coinciden ignorando mayúsculas y espacios exteriores, con tildes.");
        comprobar(DetectorPosiblesDuplicados.Evaluar(a, b with { Nombre = "Jose" }) == CriterioCoincidenciaSolicitud.Ninguno &&
            DetectorPosiblesDuplicados.Evaluar(a, b with { FechaNacimiento = a.FechaNacimiento.AddDays(1) }) == CriterioCoincidenciaSolicitud.Ninguno,
            "No se eliminan tildes ni se omite la fecha de nacimiento para igualar personas.");
        comprobar(DetectorPosiblesDuplicados.Evaluar(a with { Dni = " " }, b with { Nombre = "Otra" }) == CriterioCoincidenciaSolicitud.Ninguno,
            "Dos documentos ausentes no identifican a una misma persona.");
        comprobar(DetectorPosiblesDuplicados.Evaluar(a, a) == CriterioCoincidenciaSolicitud.Ninguno &&
            DetectorPosiblesDuplicados.Evaluar(a, b with { InstitucionId = Guid.NewGuid() }) == CriterioCoincidenciaSolicitud.Ninguno &&
            DetectorPosiblesDuplicados.Evaluar(a, b with { PeriodoId = Guid.NewGuid() }) == CriterioCoincidenciaSolicitud.Ninguno,
            "La detección excluye la propia solicitud, otros jardines y otros períodos.");
        var mismaPersona = a with { SolicitudId = Guid.NewGuid() };
        comprobar(DetectorPosiblesDuplicados.BuscarParejas([a, mismaPersona]).Count == 1,
            "Varias coincidencias entre dos solicitudes producen una sola alerta.");
        var alerta = new PosibleDuplicado(
            mismaPersona,
            a,
            Guid.NewGuid());
        alerta.Resolver(
            EstadoPosibleDuplicado.DuplicacionConfirmada,
            Guid.NewGuid(),
            "Revisadas las dos cargas");
        comprobar(!alerta.ActualizarDeteccion(
            a,
            mismaPersona,
            Guid.NewGuid()) && alerta.Estado == EstadoPosibleDuplicado.DuplicacionConfirmada,
            "Invertir el orden de comparación no reabre una decisión vigente.");
    }
}

