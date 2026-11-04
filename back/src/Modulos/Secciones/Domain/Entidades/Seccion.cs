namespace Jardin.Secciones.Domain.Entidades;


public sealed class Seccion
{
    private readonly List<int> _edadesAdmitidas = [];

    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid AulaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public TipoSeccion Tipo { get; private set; }
    public IReadOnlyCollection<int> EdadesAdmitidas => _edadesAdmitidas.AsReadOnly();
    public int Capacidad { get; private set; }
    public int Revision { get; private set; }
    public bool Activa { get; private set; }

    private Seccion() { }

    public Seccion(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        string nombre,
        IEnumerable<int> edadesAdmitidas,
        int capacidad = ReglasSeccion.CapacidadInicialReferencia,
        IEnumerable<IEnumerable<int>>? combinacionesPermitidas = null,
        Guid? id = null)
    {
        if (periodoId == Guid.Empty)
            throw new ArgumentException("El identificador del período no puede estar vacío.", nameof(periodoId));
        if (aulaId == Guid.Empty)
            throw new ArgumentException("El identificador del aula no puede estar vacío.", nameof(aulaId));
        if (turnoId == Guid.Empty)
            throw new ArgumentException("El identificador del turno no puede estar vacío.", nameof(turnoId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (capacidad <= 0)
            throw new ArgumentException("La capacidad de la sección debe ser mayor a cero.", nameof(capacidad));

        Id = id ?? Guid.NewGuid();
        PeriodoId = periodoId;
        AulaId = aulaId;
        TurnoId = turnoId;
        Nombre = nombre.Trim();
        Capacidad = capacidad;
        Activa = true;
        Revision = 1;

        EstablecerComposicion(edadesAdmitidas, combinacionesPermitidas);
    }

    public void CambiarComposicion(
        IEnumerable<int> nuevasEdades,
        string motivo,
        IEnumerable<IEnumerable<int>>? combinacionesPermitidas = null,
        IEnumerable<int>? edadesAlumnosAsignados = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ArgumentNullException.ThrowIfNull(nuevasEdades);


        if (edadesAlumnosAsignados is not null)
        {
            ValidarCompatibilidadConAlumnosAsignados(nuevasEdades, edadesAlumnosAsignados);
        }

        var edadesOrdenadas = nuevasEdades.Distinct().OrderBy(e => e).ToList();
        if (_edadesAdmitidas.SequenceEqual(edadesOrdenadas))
        {
            return;
        }

        EstablecerComposicion(edadesOrdenadas, combinacionesPermitidas);
        Revision++;
    }


    public void Renombrar(string nuevoNombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nuevoNombre);
        Nombre = nuevoNombre.Trim();
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    public bool EsCompatibleCon(int edad, Guid turnoId)
    {
        return Activa && TurnoId == turnoId && _edadesAdmitidas.Contains(edad);
    }

    public void ValidarCompatibilidad(int edad, Guid turnoId)
    {
        if (!Activa)
            throw new InvalidOperationException($"La sección '{Nombre}' se encuentra desactivada.");

        if (TurnoId != turnoId)
            throw new InvalidOperationException($"El turno '{turnoId}' no coincide con el turno configurado para la sección '{Nombre}'.");

        if (!_edadesAdmitidas.Contains(edad))
            throw new InvalidOperationException($"La edad {edad} no está admitida en la sección '{Nombre}'. Edades admitidas: [{string.Join(", ", _edadesAdmitidas)}].");
    }



    public void ValidarCompatibilidadConAlumnosAsignados(IEnumerable<int> nuevasEdades, IEnumerable<int> edadesAlumnosAsignados)
    {
        var conjuntoNuevasEdades = nuevasEdades.ToHashSet();
        var edadesIncompatibles = edadesAlumnosAsignados.Where(e => !conjuntoNuevasEdades.Contains(e)).Distinct().ToList();

        if (edadesIncompatibles.Count > 0)
        {
            throw new InvalidOperationException(
                $"No se puede cambiar la composición de la sección porque dejaría excluidos a alumnos actualmente asignados de edad(es): {string.Join(", ", edadesIncompatibles)}.");
        }
    }

    private void EstablecerComposicion(
        IEnumerable<int> edades,
        IEnumerable<IEnumerable<int>>? combinacionesPermitidas = null)
    {
        ArgumentNullException.ThrowIfNull(edades);

        var lista = edades.Distinct().OrderBy(e => e).ToList();
        if (lista.Count == 0)
            throw new ArgumentException("Debe especificarse al menos una edad admitida.", nameof(edades));

        if (!ReglasSeccion.EsCombinacionPermitida(lista, combinacionesPermitidas))
        {
            throw new InvalidOperationException(
                $"La combinación de edades [{string.Join(", ", lista)}] no está permitida para las secciones del período.");
        }

        _edadesAdmitidas.Clear();
        _edadesAdmitidas.AddRange(lista);
        Tipo = _edadesAdmitidas.Count > 1 ? TipoSeccion.Multiedad : TipoSeccion.Pura;
    }
}
