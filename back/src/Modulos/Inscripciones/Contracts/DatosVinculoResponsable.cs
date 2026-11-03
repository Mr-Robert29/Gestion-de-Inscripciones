namespace Jardin.Inscripciones.Contracts;

// El responsable se referencia por Id; no se crea una copia por cada aspirante.
public sealed record DatosVinculoResponsable(
    ConsultarResponsableRespuesta Responsable,
    string TipoVinculo,
    bool ContactoPrincipal,
    bool EsTutor);
