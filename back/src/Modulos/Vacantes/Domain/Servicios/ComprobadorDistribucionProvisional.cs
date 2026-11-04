namespace Jardin.Vacantes.Domain.Servicios;

public sealed record RecursoProvisional(
    Guid OfertaId,
    Guid TurnoId,
    int Capacidad,
    IReadOnlyList<int> EdadesOcupantes,
    int? CantidadOcupantes = null);

public sealed record CandidatoProvisional(
    Guid SolicitudId,
    int Sala,
    Guid TurnoPreferidoId,
    IReadOnlyList<Guid> AlternativasAceptadas);

public sealed record AsignacionProvisional(Guid SolicitudId, Guid OfertaId, Guid TurnoId);

// Busca una distribución conjunta: sumar capacidades no detecta mezclas de edades imposibles.
public static class ComprobadorDistribucionProvisional
{
    public static IReadOnlyList<AsignacionProvisional>? Buscar(
        IReadOnlyList<RecursoProvisional> recursos,
        IReadOnlyList<CandidatoProvisional> candidatos,
        IReadOnlyList<int[]> combinaciones,
        CancellationToken tokenCancelacion = default)
    {
        if (recursos.Select(x => x.OfertaId).Distinct().Count() != recursos.Count)
            throw new ArgumentException("La oferta no puede repetirse.");
        if (candidatos.Select(x => x.SolicitudId).Distinct().Count() != candidatos.Count)
            throw new ArgumentException("Un candidato no puede asignarse dos veces.");
        if (combinaciones.Count == 0 || combinaciones.Any(x => x.Length == 0 || x.Any(e => e is < 3 or > 5)))
            throw new ArgumentException("Las combinaciones de edades no son válidas.");
        var edades = recursos.Select(x => x.EdadesOcupantes.ToList()).ToArray();
        var cantidades = recursos.Select(x => x.CantidadOcupantes ?? x.EdadesOcupantes.Count).ToArray();
        bool Compatible(IEnumerable<int> valores) => combinaciones.Any(c => valores.All(c.Contains));
        for (var i = 0; i < recursos.Count; i++)
        {
            if (recursos[i].Capacidad < 0 || cantidades[i] < edades[i].Distinct().Count() ||
                cantidades[i] > recursos[i].Capacidad || !Compatible(edades[i]))
                throw new InvalidOperationException("La ocupación existente no es compatible con la oferta y sus combinaciones.");
        }
        var asignaciones = new List<AsignacionProvisional>();
        var visitas = 0;

        bool Asignar(int indice)
        {
            tokenCancelacion.ThrowIfCancellationRequested();
            // Agotar el límite produce un error; nunca se informa una espera falsa.
            if (++visitas > 200_000)
                throw new InvalidOperationException("La comprobación excedió su límite de búsqueda. No se guardó una propuesta parcial.");
            if (indice == candidatos.Count) return true;
            var candidato = candidatos[indice];
            if (candidato.Sala is < 3 or > 5) throw new ArgumentException("La sala no es válida.");
            var turnos = new[] { candidato.TurnoPreferidoId }.Concat(candidato.AlternativasAceptadas).Distinct();
            foreach (var turno in turnos)
            {
                var estadosProbados = new HashSet<string>();
                for (var r = 0; r < recursos.Count; r++)
                {
                    if (recursos[r].TurnoId != turno || cantidades[r] >= recursos[r].Capacidad) continue;
                    if (!Compatible(edades[r].Append(candidato.Sala))) continue;
                    // Recursos equivalentes no necesitan explorarse de nuevo en esta rama.
                    var clave = $"{recursos[r].Capacidad - cantidades[r]}:{string.Join(',', edades[r].Distinct().Order())}";
                    if (!estadosProbados.Add(clave)) continue;
                    edades[r].Add(candidato.Sala);
                    cantidades[r]++;
                    asignaciones.Add(new AsignacionProvisional(candidato.SolicitudId, recursos[r].OfertaId, turno));
                    if (Asignar(indice + 1)) return true;
                    asignaciones.RemoveAt(asignaciones.Count - 1);
                    edades[r].RemoveAt(edades[r].Count - 1);
                    cantidades[r]--;
                }
            }
            return false;
        }

        return Asignar(0) ? asignaciones.ToArray() : null;
    }
}
