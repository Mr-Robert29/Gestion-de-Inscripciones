using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPropuestasIngresos
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var manana = Guid.NewGuid();
        var tarde = Guid.NewGuid();
        var oferta = Guid.NewGuid();
        var segunda = Guid.NewGuid();
        int[][] combinaciones = [[3], [4], [5], [3, 4], [4, 5]];
        CandidatoProvisional Candidato(int edad, Guid turno, params Guid[] alternativas)
            => new(Guid.NewGuid(), edad, turno, alternativas);
        var tres = Candidato(3, manana);
        var cinco = Candidato(5, manana);
        comprobar(ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 2, [])],
            [tres, cinco],
            combinaciones) is null, "HU18: no mezcla 3 y 5 aunque la suma de capacidad alcance.");
        var factible = ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 1, []), new RecursoProvisional(segunda, manana, 1, [])],
            [tres, cinco],
            combinaciones);
        comprobar(factible?.Count == 2 && factible.Select(x => x.OfertaId).Distinct().Count() == 2,
            "HU18: separa edades incompatibles en recursos distintos.");
        var flexible = Candidato(4, manana, tarde);
        var soloManana = Candidato(4, manana);
        var reasignada = ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 1, []), new RecursoProvisional(segunda, tarde, 1, [])],
            [flexible, soloManana],
            combinaciones)!;
        comprobar(reasignada.Single(x => x.SolicitudId == flexible.SolicitudId).TurnoId == tarde &&
            reasignada.Single(x => x.SolicitudId == soloManana.SolicitudId).TurnoId == manana,
            "HU18: reacomoda la propuesta completa utilizando únicamente una alternativa aceptada.");
        comprobar(ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, tarde, 2, [])],
            [soloManana],
            combinaciones) is null, "HU18: no asigna una alternativa que la familia no aceptó.");
        comprobar(ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 2, [4, 4])],
            [Candidato(4, manana)],
            combinaciones) is null, "HU18: las ocupaciones iniciales consumen capacidad.");
        comprobar(ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 2, [5])],
            [tres],
            combinaciones) is null, "HU18: respeta la edad de quienes ya ocupan el aula.");

        var periodo = Guid.NewGuid();
        var version = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow.AddMinutes(-1);
        SolicitudPropuesta Solicitud(int sala, decimal? distancia, string dni, Guid? turno = null)
            => new(Guid.NewGuid(), Guid.NewGuid(), sala, false, [], distancia, 1, 1,
                turno ?? manana, [], dni, null, ahora, false);
        var cercano = Solicitud(5, 10m, "99999999");
        var lejano = Solicitud(5, 20m, "00000347");
        EntradasPropuestaIngresos Entradas(params SolicitudPropuesta[] solicitudes)
            => new(Guid.NewGuid(), periodo, 1, "RecepcionCerrada", version, Guid.NewGuid(), [5, 4, 3],
                ["ArticulacionPedagogica", "HermanosYGruposConvivientes", "Cercania", "HijosDelPersonal", "SorteoUltimaInstancia", "FueraDeTermino"],
                combinaciones, [new OfertaPropuesta(oferta, Guid.NewGuid(), manana, 1, true, [])], solicitudes);
        var sorteo = new Sorteo(periodo, 5, 347, new DateOnly(2026, 11, 1), "Acta", 1, Guid.NewGuid(), ahora);
        var ordenada = PlanificadorPropuestaIngresos.Generar(Entradas(lejano, cercano), [sorteo], [], []);
        comprobar(ordenada.Single(x => x.SolicitudId == cercano.Id).Estado == "IngresoPropuesto" &&
            ordenada.Single(x => x.SolicitudId == lejano.Id).Estado == "EsperaPropuesta",
            "HU18: el sorteo no atraviesa prioridades diferentes.");
        var menor = Solicitud(4, 1m, "00000347");
        var edades = PlanificadorPropuestaIngresos.Generar(Entradas(menor, cercano), [], [], []);
        comprobar(edades.Single(x => x.SolicitudId == cercano.Id).Estado == "IngresoPropuesto",
            "HU18: respeta el orden de salas aunque una edad menor esté más cerca.");
        var empate = Solicitud(5, 10m, "00000347");
        var circular = PlanificadorPropuestaIngresos.Generar(Entradas(cercano, empate), [sorteo], [], []);
        comprobar(circular.Single(x => x.SolicitudId == empate.Id).Estado == "IngresoPropuesto",
            "HU18: aplica el recorrido circular dentro del grupo de condiciones iguales.");
        var sinDni = empate with { Dni = null };
        var pendientes = PlanificadorPropuestaIngresos.Generar(Entradas(cercano, sinDni, menor), [sorteo], [], []);
        comprobar(pendientes.All(x => x.Estado == "PendienteRevision"),
            "HU18: un orden pendiente no adelanta a candidatos posteriores que compiten por el turno.");
        var caso = new CasoRevision(periodo, 5, TipoCasoRevision.SinDni,
            [cercano.Id, sinDni.Id], "Prueba", sorteo.Id, sorteo.Version, version);
        caso.Resolver(Guid.NewGuid(), [sinDni.Id, cercano.Id], "Acta administrativa", 1);
        var resuelta = PlanificadorPropuestaIngresos.Generar(Entradas(cercano, sinDni), [sorteo], [caso], []);
        comprobar(resuelta.Single(x => x.SolicitudId == sinDni.Id).Estado == "IngresoPropuesto",
            "HU18: consume una resolución manual vigente del grupo exacto.");
        var obsoleta = PlanificadorPropuestaIngresos.Generar(
            Entradas(cercano, sinDni with { UltimoCambio = DateTimeOffset.UtcNow.AddMinutes(1) }), [sorteo], [caso], []);
        comprobar(obsoleta.All(x => x.Estado == "PendienteRevision"),
            "HU18: una resolución anterior a la corrección no decide el orden actual.");
        var reserva = new OcupacionPlaza(periodo, Guid.NewGuid(), oferta, 5, "Reserva");
        var ocupada = PlanificadorPropuestaIngresos.Generar(Entradas(cercano), [], [], [reserva]);
        comprobar(ocupada.Single().Estado == "EsperaPropuesta", "HU18: descuenta la reserva activa.");
        reserva.ConfirmarIngreso();
        comprobar(PlanificadorPropuestaIngresos.Generar(Entradas(cercano), [], [], [reserva]).Single().Estado == "EsperaPropuesta",
            "HU18: convertir reserva en ingreso conserva una sola ocupación.");
        var fuera = PlanificadorPropuestaIngresos.Generar(Entradas(cercano with { FueraDeTermino = true }), [], [], []);
        comprobar(fuera.Single().Estado == "PendienteRevision", "HU18: no decide automáticamente una recepción fuera de término.");
        var duplicado = PlanificadorPropuestaIngresos.Generar(Entradas(cercano with { DuplicacionPendiente = true }), [], [], []);
        comprobar(duplicado.Single().Estado == "PendienteRevision", "HU18: no elige arbitrariamente cuál duplicación continúa.");
        var sinDistancia = PlanificadorPropuestaIngresos.Generar(Entradas(cercano with { DistanciaMetros = null }), [], [], []);
        comprobar(sinDistancia.Single().Estado == "PendienteRevision", "HU18: distancia desconocida no significa cero.");
        var casiIguales = PlanificadorPropuestaIngresos.Generar(
            Entradas(cercano with { DistanciaMetros = 10.001m }, empate with { DistanciaMetros = 10.004m }),
            [sorteo],
            [],
            []);
        comprobar(casiIguales.Single(x => x.SolicitudId == empate.Id).Estado == "IngresoPropuesto",
            "HU18: valores iguales a dos decimales forman el mismo grupo; no se reescribe la medición.");
        var casoPendiente = new CasoRevision(
            periodo,
            5,
            TipoCasoRevision.EmpateDistancia,
            [cercano.Id, empate.Id],
            "Empate pendiente",
            sorteo.Id,
            sorteo.Version,
            version);
        comprobar(PlanificadorPropuestaIngresos.Generar(Entradas(cercano, empate), [sorteo], [casoPendiente], [])
            .All(x => x.Estado == "PendienteRevision"), "HU18: el sorteo no elude un caso administrativo abierto del grupo.");
        var evaluacionRedondeada = EvaluadorPrioridades.Evaluar(
            [new EntradaEvaluacionPrioridad(cercano.Id, cercano.AspiranteId, 5, false, new Dictionary<string, bool>(), 10.001m),
                new EntradaEvaluacionPrioridad(empate.Id, empate.AspiranteId, 5, false, new Dictionary<string, bool>(), 10.004m)],
            [5, 4, 3],
            ["Cercania"],
            2);
        comprobar(evaluacionRedondeada.All(x => x.RequiereRevision),
            "HU18/HU14: evaluación y propuesta utilizan la misma precisión configurada.");
        var otroTurno = Solicitud(4, 50m, "00000999", tarde);
        var otraOferta = new OfertaPropuesta(segunda, Guid.NewGuid(), tarde, 1, true, []);
        var contextoSeparado = Entradas(cercano, sinDni, otroTurno);
        var independiente = PlanificadorPropuestaIngresos.Generar(
            contextoSeparado with { Ofertas = contextoSeparado.Ofertas.Append(otraOferta).ToArray() },
            [sorteo],
            [],
            []);
        comprobar(independiente.Single(x => x.SolicitudId == otroTurno.Id).Estado == "IngresoPropuesto",
            "HU18: un pendiente no bloquea candidatos de un turno independiente.");
        var cancelacion = new CancellationToken(true);
        try
        {
            ComprobadorDistribucionProvisional.Buscar([], [], combinaciones, cancelacion);
            comprobar(false, "HU18: debía cancelar la comprobación.");
        }
        catch (OperationCanceledException) { comprobar(true, "HU18: la cancelación no produce una espera ficticia."); }
        var granOcupacion = ComprobadorDistribucionProvisional.Buscar(
            [new RecursoProvisional(oferta, manana, 100_000, [5], 99_999)],
            [Candidato(5, manana)],
            combinaciones);
        comprobar(granOcupacion?.Count == 1,
            "HU18: cuenta ocupaciones sin crear un objeto por cada plaza inicial.");
    }
}
