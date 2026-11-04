using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Domain.Servicios;

public sealed record ComposicionVacante(Guid SeccionId, IReadOnlyList<int> Edades, int Capacidad, int Revision);

public static class CompatibilidadVacante
{
    public static bool EsCompatible(
        EntradasPropuestaIngresos entradas,
        OfertaPropuesta oferta,
        SolicitudPropuesta solicitud,
        IReadOnlyList<OcupacionPlaza> ocupaciones,
        IReadOnlyList<ComposicionVacante> secciones,
        Guid? reservaPropia = null)
    {
        if (!oferta.Disponible || solicitud.Sala is null || solicitud.FueraDeTermino || solicitud.DuplicacionPendiente) return false;
        if (solicitud.TurnoPreferidoId != oferta.TurnoId && !solicitud.Alternativas.Contains(oferta.TurnoId)) return false;
        if (secciones.Count > 1) throw new InvalidOperationException("Hay varias secciones para la misma aula y turno; revisá su organización antes de ofrecer.");
        var activas = ocupaciones.Where(x => x.Activa && x.OfertaId == oferta.Id && x.Id != reservaPropia).ToArray();
        var capacidad = secciones.Count == 0 ? oferta.Capacidad : Math.Min(oferta.Capacidad, secciones[0].Capacidad);
        var inicial = oferta.CantidadOcupacionInicial ?? oferta.EdadesOcupacionesIniciales.Count;
        if ((long)inicial + activas.Length >= capacidad) return false;
        var edades = oferta.EdadesOcupacionesIniciales.Concat(activas.Select(x => x.Sala)).Append(solicitud.Sala.Value).Distinct().ToArray();
        return entradas.Combinaciones.Any(c => edades.All(c.Contains)) &&
            (secciones.Count == 0 || edades.All(secciones[0].Edades.Contains));
    }
}
