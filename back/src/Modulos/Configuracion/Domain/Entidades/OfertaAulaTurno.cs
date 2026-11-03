namespace Jardin.Configuracion.Domain.Entidades;

// Recurso del período: no es una sección ni confirma ingresos.
public sealed class OfertaAulaTurno
{
    private readonly List<OcupacionInicial> _ocupaciones = [];
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid AulaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public int Capacidad { get; private set; }
    public bool Disponible { get; private set; }
    public string ReferenciaCapacidad { get; private set; } = "";
    public string OrigenOcupaciones { get; private set; } = "";
    public int Revision { get; private set; }
    public IReadOnlyCollection<OcupacionInicial> Ocupaciones => _ocupaciones.AsReadOnly();
    public int CapacidadNetaInicial => Disponible ? Capacidad - (int)_ocupaciones.Sum(o => (long)o.Cantidad) : 0;

    private OfertaAulaTurno() { }

    public OfertaAulaTurno(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        int capacidad,
        bool disponible,
        string referenciaCapacidad,
        string origenOcupaciones,
        IReadOnlyList<OcupacionInicial> ocupaciones)
    {
        if (periodoId == Guid.Empty || aulaId == Guid.Empty || turnoId == Guid.Empty)
            throw new ArgumentException("Período, aula y turno son obligatorios.");
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        AulaId = aulaId;
        TurnoId = turnoId;
        Actualizar(
            capacidad,
            disponible,
            referenciaCapacidad,
            origenOcupaciones,
            ocupaciones);
    }

    public void Actualizar(
        int capacidad,
        bool disponible,
        string referenciaCapacidad,
        string origenOcupaciones,
        IReadOnlyList<OcupacionInicial> ocupaciones)
    {
        ArgumentNullException.ThrowIfNull(ocupaciones);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenciaCapacidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(origenOcupaciones);
        if (capacidad < 0) throw new ArgumentOutOfRangeException(nameof(capacidad), "La capacidad no puede ser negativa.");
        if (ocupaciones.Any(o => o is null)) throw new ArgumentException("La lista contiene una ocupación nula.", nameof(ocupaciones));
        if (ocupaciones.Select(o => o.Sala).Distinct().Count() != ocupaciones.Count)
            throw new ArgumentException("No repitas una sala dentro de las ocupaciones iniciales.");
        if (ocupaciones.Sum(o => (long)o.Cantidad) > capacidad)
            throw new ArgumentException("La capacidad no puede ser menor que la ocupación inicial.");

        // Validamos antes de modificar. Conservamos los elementos existentes para el seguimiento de EF.
        Capacidad = capacidad;
        Disponible = disponible;
        ReferenciaCapacidad = referenciaCapacidad.Trim();
        OrigenOcupaciones = origenOcupaciones.Trim();
        var nuevas = ocupaciones.ToDictionary(o => o.Sala, o => o.Cantidad);
        _ocupaciones.RemoveAll(o => !nuevas.ContainsKey(o.Sala));
        foreach (var ocupacion in nuevas)
        {
            var existente = _ocupaciones.SingleOrDefault(o => o.Sala == ocupacion.Key);
            if (existente is null) _ocupaciones.Add(new OcupacionInicial(ocupacion.Key, ocupacion.Value));
            else existente.CambiarCantidad(ocupacion.Value);
        }
        Revision++;
    }
}
