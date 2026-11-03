using System.Text.Json.Serialization;

namespace Jardin.Configuracion.Contracts;

// Cambia los valores de una combinación existente, manteniendo su identidad.
public sealed record ActualizarOfertaPeticion(
    [property: JsonRequired] int Capacidad,
    [property: JsonRequired] bool Disponible,
    string ReferenciaCapacidad,
    string OrigenOcupaciones,
    IReadOnlyList<DatosOcupacionInicial> Ocupaciones,
    int Revision,
    string Motivo);
