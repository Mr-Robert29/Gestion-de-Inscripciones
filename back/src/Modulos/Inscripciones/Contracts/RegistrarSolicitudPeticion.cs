namespace Jardin.Inscripciones.Contracts;

// La sala, el estado de recepción y las versiones los determina el servidor.
public sealed record RegistrarSolicitudPeticion(
    Guid AspiranteId,
    DatosDomicilioSolicitud Domicilio,
    Guid TurnoPreferidoId,
    IReadOnlyList<Guid>? TurnosAlternativosAceptados = null,
    string? ReferenciaConsentimiento = null,
    string? ArticulacionPedagogica = "No",
    string? InstitucionProcedencia = null,
    string? HermanosGrupoConviviente = "No",
    string? DetalleHermanoGrupo = null,
    string? HijoPersonal = "No",
    string? DetalleHijoPersonal = null);
