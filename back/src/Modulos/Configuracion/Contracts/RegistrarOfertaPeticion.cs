using System.Text.Json.Serialization;

namespace Jardin.Configuracion.Contracts;

// Entrada de una oferta. Ocupaciones=[] expresa que se confirmó que no hay ocupaciones iniciales.
public sealed record RegistrarOfertaPeticion(
    Guid AulaId,
    Guid TurnoId,
    [property: JsonRequired] int Capacidad,
    [property: JsonRequired] bool Disponible,
    string ReferenciaCapacidad,
    string OrigenOcupaciones,
    IReadOnlyList<DatosOcupacionInicial> Ocupaciones,
    string Motivo);
