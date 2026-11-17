using Google.OrTools.Sat;

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

public sealed record ResultadoDistribucionProvisional(
    IReadOnlyList<AsignacionProvisional> Asignaciones,
    IReadOnlySet<Guid> SolicitudesAdmitidas);

// Resuelve asignaciones conjuntas respetando edades, capacidades y turnos aceptados.
public static class ComprobadorDistribucionProvisional
{
    public static IReadOnlyList<AsignacionProvisional>? Buscar(
        IReadOnlyList<RecursoProvisional> recursos,
        IReadOnlyList<CandidatoProvisional> candidatos,
        IReadOnlyList<int[]> combinaciones,
        CancellationToken tokenCancelacion = default)
    {
        var modelo = CrearModelo(recursos, candidatos, combinaciones, exigirTodos: true);
        var solver = CrearSolver();
        using var cancelacion = tokenCancelacion.Register(solver.StopSearch);
        var estado = solver.Solve(modelo.Modelo);
        if (estado == CpSolverStatus.Infeasible) return null;
        ValidarEstado(estado, solver, tokenCancelacion);
        return LeerAsignaciones(modelo, solver);
    }

    // Maximiza primero la cantidad de ingresos y, entre resultados de igual tamaño,
    // favorece los candidatos que aparecen antes en el orden de prioridad.
    public static ResultadoDistribucionProvisional ResolverPrioritario(
        IReadOnlyList<RecursoProvisional> recursos,
        IReadOnlyList<CandidatoProvisional> candidatos,
        IReadOnlyList<int[]> combinaciones,
        CancellationToken tokenCancelacion = default)
    {
        if (candidatos.Count == 0) return new ResultadoDistribucionProvisional([], new HashSet<Guid>());

        var modelo = CrearModelo(recursos, candidatos, combinaciones, exigirTodos: false);
        var cantidad = (long)candidatos.Count;
        var pesoCantidad = checked(cantidad * (cantidad + 1) / 2 + 1);
        var objetivo = modelo.Admitidos.Select((variable, indice) =>
            LinearExpr.Term(variable, checked(pesoCantidad + cantidad - indice)));
        modelo.Modelo.Maximize(LinearExpr.Sum(objetivo));

        var solver = CrearSolver("num_search_workers:8");
        using var cancelacion = tokenCancelacion.Register(solver.StopSearch);
        var estado = solver.Solve(modelo.Modelo);
        tokenCancelacion.ThrowIfCancellationRequested();
        ValidarEstado(estado, solver, tokenCancelacion);
        var admitidos = candidatos
            .Where((_, indice) => solver.Value(modelo.Admitidos[indice]) == 1)
            .Select(x => x.SolicitudId)
            .ToHashSet();
        return new ResultadoDistribucionProvisional(LeerAsignaciones(modelo, solver), admitidos);
    }

    private static ModeloDistribucion CrearModelo(
        IReadOnlyList<RecursoProvisional> recursos,
        IReadOnlyList<CandidatoProvisional> candidatos,
        IReadOnlyList<int[]> combinaciones,
        bool exigirTodos)
    {
        if (recursos.Select(x => x.OfertaId).Distinct().Count() != recursos.Count)
            throw new ArgumentException("La oferta no puede repetirse.");
        if (candidatos.Select(x => x.SolicitudId).Distinct().Count() != candidatos.Count)
            throw new ArgumentException("Un candidato no puede asignarse dos veces.");
        if (combinaciones.Count == 0 || combinaciones.Any(x => x.Length == 0 || x.Any(e => e is < 3 or > 5)))
            throw new ArgumentException("Las combinaciones de edades no son válidas.");

        var cantidades = recursos.Select(x => x.CantidadOcupantes ?? x.EdadesOcupantes.Count).ToArray();
        var composicionesValidas = recursos.Select(recurso => combinaciones
            .Where(combinacion => recurso.EdadesOcupantes.Distinct().All(combinacion.Contains))
            .ToArray()).ToArray();
        for (var indice = 0; indice < recursos.Count; indice++)
        {
            if (recursos[indice].Capacidad < 0 || cantidades[indice] < recursos[indice].EdadesOcupantes.Distinct().Count() ||
                cantidades[indice] > recursos[indice].Capacidad || composicionesValidas[indice].Length == 0)
                throw new InvalidOperationException("La ocupación existente no es compatible con la oferta y sus combinaciones.");
        }
        if (candidatos.Any(x => x.Sala is < 3 or > 5))
            throw new ArgumentException("La sala del aspirante no es válida.");

        var modelo = new CpModel();
        var admite = candidatos.Select((candidato, indice) => modelo.NewBoolVar($"admite_{indice}")).ToArray();
        var asignaciones = new BoolVar?[candidatos.Count, recursos.Count];
        var composiciones = new BoolVar[recursos.Count][];

        for (var recurso = 0; recurso < recursos.Count; recurso++)
        {
            composiciones[recurso] = composicionesValidas[recurso]
                .Select((_, indice) => modelo.NewBoolVar($"composicion_{recurso}_{indice}"))
                .ToArray();
            modelo.Add(LinearExpr.Sum(composiciones[recurso].Select(x => (LinearExpr)x)) == 1);
        }

        for (var candidato = 0; candidato < candidatos.Count; candidato++)
        {
            var opciones = new List<LinearExpr>();
            for (var recurso = 0; recurso < recursos.Count; recurso++)
            {
                var oferta = recursos[recurso];
                var solicitud = candidatos[candidato];
                var aceptaTurno = solicitud.TurnoPreferidoId == oferta.TurnoId ||
                    solicitud.AlternativasAceptadas.Contains(oferta.TurnoId);
                if (!aceptaTurno || cantidades[recurso] >= oferta.Capacidad) continue;

                var indicesComposicion = Enumerable.Range(0, composicionesValidas[recurso].Length)
                    .Where(indice => composicionesValidas[recurso][indice].Contains(solicitud.Sala))
                    .ToArray();
                if (indicesComposicion.Length == 0) continue;

                var asignacion = modelo.NewBoolVar($"asignacion_{candidato}_{recurso}");
                asignaciones[candidato, recurso] = asignacion;
                opciones.Add(asignacion);
                modelo.Add(asignacion <= LinearExpr.Sum(indicesComposicion
                    .Select(indice => (LinearExpr)composiciones[recurso][indice])));
            }

            modelo.Add(LinearExpr.Sum(opciones) == admite[candidato]);
            if (exigirTodos) modelo.Add(admite[candidato] == 1);
        }

        for (var recurso = 0; recurso < recursos.Count; recurso++)
        {
            var capacidadDisponible = recursos[recurso].Capacidad - cantidades[recurso];
            var asignados = Enumerable.Range(0, candidatos.Count)
                .Select(indice => asignaciones[indice, recurso])
                .Where(x => x is not null)
                .Select(x => (LinearExpr)x!);
            modelo.Add(LinearExpr.Sum(asignados) <= capacidadDisponible);
        }

        return new ModeloDistribucion(modelo, admite, asignaciones, candidatos, recursos);
    }

    private static CpSolver CrearSolver(string parametros = "num_search_workers:1") => new() { StringParameters = parametros };

    private static IReadOnlyList<AsignacionProvisional> LeerAsignaciones(ModeloDistribucion modelo, CpSolver solver)
    {
        var resultado = new List<AsignacionProvisional>();
        for (var candidato = 0; candidato < modelo.Asignaciones.GetLength(0); candidato++)
        {
            for (var recurso = 0; recurso < modelo.Asignaciones.GetLength(1); recurso++)
            {
                var variable = modelo.Asignaciones[candidato, recurso];
                if (variable is null || solver.Value(variable) != 1) continue;
                resultado.Add(new AsignacionProvisional(
                    modelo.Candidatos[candidato].SolicitudId,
                    modelo.Recursos[recurso].OfertaId,
                    modelo.Recursos[recurso].TurnoId));
            }
        }

        return resultado;
    }

    private static void ValidarEstado(CpSolverStatus estado, CpSolver solver, CancellationToken tokenCancelacion)
    {
        tokenCancelacion.ThrowIfCancellationRequested();
        if (estado is CpSolverStatus.Optimal or CpSolverStatus.Feasible) return;
        throw new InvalidOperationException(estado == CpSolverStatus.Infeasible
            ? "No existe una distribución compatible con la capacidad, las edades y los turnos aceptados."
            : $"CP-SAT no pudo determinar una distribución. Estado del solver: {estado}.");
    }

    private sealed record ModeloDistribucion(
        CpModel Modelo,
        BoolVar[] Admitidos,
        BoolVar?[,] Asignaciones,
        IReadOnlyList<CandidatoProvisional> Candidatos,
        IReadOnlyList<RecursoProvisional> Recursos);
}
