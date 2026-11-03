namespace Jardin.Configuracion.Contracts;

// Los resultados futuros deben conservar ambos Id para consultar exactamente sus entradas.
public sealed record DatosVersionesConfiguracion(Guid VersionReglasId, Guid VersionOfertaId);
