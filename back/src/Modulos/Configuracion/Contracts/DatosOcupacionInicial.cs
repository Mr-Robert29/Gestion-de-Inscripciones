using System.Text.Json.Serialization;

namespace Jardin.Configuracion.Contracts;

// La cantidad debe enviarse expresamente, incluido cero; no se deduce de un campo omitido.
public sealed record DatosOcupacionInicial([property: JsonRequired] int Sala, [property: JsonRequired] int Cantidad);
