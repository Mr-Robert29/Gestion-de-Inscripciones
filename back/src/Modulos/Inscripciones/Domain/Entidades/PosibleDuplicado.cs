using System.Text.Json;
using Jardin.Inscripciones.Domain.Servicios;

namespace Jardin.Inscripciones.Domain.Entidades;

public enum EstadoPosibleDuplicado { PendienteRevision, DuplicacionConfirmada, PersonasDiferentes }

// La alerta y su resolución no borran, fusionan ni excluyen ninguna solicitud.
public sealed class PosibleDuplicado
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid SolicitudAId { get; private set; }
    public Guid SolicitudBId { get; private set; }
    public string PerfilDeteccion { get; private set; } = "";
    public CriterioCoincidenciaSolicitud Criterios { get; private set; }
    public string HuellaDatos { get; private set; } = "";
    public string DatosEvaluados { get; private set; } = "";
    public EstadoPosibleDuplicado Estado { get; private set; }
    public int Revision { get; private set; }
    public Guid UsuarioDeteccionId { get; private set; }
    public DateTimeOffset FechaDeteccion { get; private set; }
    public Guid UsuarioUltimaDeteccionId { get; private set; }
    public DateTimeOffset FechaUltimaDeteccion { get; private set; }
    public Guid? UsuarioResolucionId { get; private set; }
    public DateTimeOffset? FechaResolucion { get; private set; }
    public string? MotivoResolucion { get; private set; }
    private PosibleDuplicado() { }

    public PosibleDuplicado(
        IdentificacionSolicitud a,
        IdentificacionSolicitud b,
        Guid autor)
    {
        if (a.SolicitudId == Guid.Empty || b.SolicitudId == Guid.Empty || a.SolicitudId == b.SolicitudId ||
            a.InstitucionId == Guid.Empty || a.InstitucionId != b.InstitucionId || a.PeriodoId == Guid.Empty || a.PeriodoId != b.PeriodoId)
            throw new ArgumentException("Las solicitudes deben ser distintas y pertenecer a la misma institución y período.");
        if (DetectorPosiblesDuplicados.Evaluar(a, b) == CriterioCoincidenciaSolicitud.Ninguno)
            throw new ArgumentException("No se encontraron coincidencias para crear una alerta.");
        if (autor == Guid.Empty) throw new ArgumentException("El autor es obligatorio.");
        if (a.SolicitudId.CompareTo(b.SolicitudId) > 0) (a, b) = (b, a);
        Id = Guid.NewGuid();
        InstitucionId = a.InstitucionId;
        PeriodoId = a.PeriodoId;
        SolicitudAId = a.SolicitudId;
        SolicitudBId = b.SolicitudId;
        UsuarioDeteccionId = autor;
        FechaDeteccion = DateTimeOffset.UtcNow;
        ActualizarDeteccion(
            a,
            b,
            autor);
    }

    public bool ActualizarDeteccion(
        IdentificacionSolicitud a,
        IdentificacionSolicitud b,
        Guid autor)
    {
        if (a.SolicitudId.CompareTo(b.SolicitudId) > 0) (a, b) = (b, a);
        ValidarPareja(a, b);
        if (autor == Guid.Empty) throw new ArgumentException("El autor es obligatorio.");
        var huella = DetectorPosiblesDuplicados.CalcularHuella(a, b);
        if (HuellaDatos == huella && PerfilDeteccion == DetectorPosiblesDuplicados.Perfil) return false;
        HuellaDatos = huella;
        PerfilDeteccion = DetectorPosiblesDuplicados.Perfil;
        Criterios = DetectorPosiblesDuplicados.Evaluar(a, b);
        DatosEvaluados = JsonSerializer.Serialize(new { A = a, B = b });
        Estado = EstadoPosibleDuplicado.PendienteRevision;
        UsuarioUltimaDeteccionId = autor;
        FechaUltimaDeteccion = DateTimeOffset.UtcNow;
        UsuarioResolucionId = null;
        FechaResolucion = null;
        MotivoResolucion = null;
        Revision++;
        return true;
    }

    public bool CoincidenDatos(IdentificacionSolicitud a, IdentificacionSolicitud b)
    {
        if (a.SolicitudId.CompareTo(b.SolicitudId) > 0) (a, b) = (b, a);
        ValidarPareja(a, b);
        return PerfilDeteccion == DetectorPosiblesDuplicados.Perfil && HuellaDatos == DetectorPosiblesDuplicados.CalcularHuella(a, b);
    }

    public void Resolver(
        EstadoPosibleDuplicado decision,
        Guid autor,
        string motivo)
    {
        if (decision is not (EstadoPosibleDuplicado.DuplicacionConfirmada or EstadoPosibleDuplicado.PersonasDiferentes))
            throw new ArgumentException("Seleccioná duplicación confirmada o personas diferentes.");
        if (autor == Guid.Empty) throw new ArgumentException("El autor es obligatorio.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        Estado = decision;
        UsuarioResolucionId = autor;
        FechaResolucion = DateTimeOffset.UtcNow;
        MotivoResolucion = motivo.Trim();
        Revision++;
    }

    private void ValidarPareja(IdentificacionSolicitud a, IdentificacionSolicitud b)
    {
        if (a.SolicitudId != SolicitudAId || b.SolicitudId != SolicitudBId || a.InstitucionId != InstitucionId ||
            b.InstitucionId != InstitucionId || a.PeriodoId != PeriodoId || b.PeriodoId != PeriodoId)
            throw new ArgumentException("Los datos no pertenecen a la pareja de la alerta.");
    }
}
