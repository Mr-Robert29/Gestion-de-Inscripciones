namespace Jardin.Configuracion.Contracts;

// Capacidad neta inicial no equivale a vacantes finales: no descuenta ingresos ni reservas.
public sealed record ConsultarOfertaRespuesta(
    Guid Id,
    Guid PeriodoId,
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    bool Disponible,
    string ReferenciaCapacidad,
    string OrigenOcupaciones,
    IReadOnlyList<DatosOcupacionInicial> Ocupaciones,
    int CapacidadNetaInicial,
    int Revision);
