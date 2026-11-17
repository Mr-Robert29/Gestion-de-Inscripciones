using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public sealed record CondicionArticulacionInterna(
    CondicionPrioridad Condicion,
    int RevisionSolicitud);
