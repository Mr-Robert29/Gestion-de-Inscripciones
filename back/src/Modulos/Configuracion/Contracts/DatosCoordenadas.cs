using System.Text.Json.Serialization;

namespace Jardin.Configuracion.Contracts;

// Datos de ubicación para mensajes de entrada o salida; los rangos se validan en Domain.
public sealed record DatosCoordenadas([property: JsonRequired] decimal Latitud, [property: JsonRequired] decimal Longitud);
