namespace Jardin.Inscripciones.Contracts;

// La lista informa lo registrado; no garantiza un catálogo obligatorio completo.
public sealed record ConsultarAntecedentesRespuesta(
    Guid SolicitudId,
    int RevisionSolicitud,
    IReadOnlyList<DatosDocumentoSolicitud> Documentos,
    IReadOnlyList<DatosCondicionSolicitud> Condiciones);
