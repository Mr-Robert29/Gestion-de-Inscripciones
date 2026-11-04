using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasListaEspera
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var turno = Guid.NewGuid();
        SolicitudPropuesta Solicitud(int sala)
            => new(Guid.NewGuid(), Guid.NewGuid(), sala, false, [], 25.12345m, 1, 1,
                turno, [], "00000347", null, DateTimeOffset.UtcNow, false, sala);
        ResultadoPropuestaIngreso Resultado(SolicitudPropuesta solicitud, string estado)
            => new(solicitud.Id, solicitud.AspiranteId, solicitud.Sala, estado, null, null, false, "Resultado conservado");
        var admitido = Solicitud(5);
        var primero = Solicitud(5);
        var segundo = Solicitud(4);
        var pendiente = Solicitud(3) with { FueraDeTermino = true };
        var solicitudes = new[] { segundo, pendiente, primero, admitido };
        var resultados = new[] { Resultado(primero, "EsperaPropuesta"), Resultado(admitido, "IngresoPropuesto"),
            Resultado(segundo, "EsperaPropuesta"), Resultado(pendiente, "PendienteRevision") };
        var posiciones = new Dictionary<Guid, int> { [admitido.Id] = 1, [primero.Id] = 2, [segundo.Id] = 3 };
        var lista = ConstructorListaEspera.Construir(solicitudes, resultados, posiciones,
            new HashSet<Guid> { admitido.AspiranteId }, new HashSet<Guid>(), false, []);
        comprobar(lista.Count == 3 && lista.All(x => x.Solicitud.AspiranteId != admitido.AspiranteId),
            "HU20: excluye al aspirante ya admitido y separa la recepción pendiente.");
        comprobar(lista[0].Solicitud.Id == primero.Id && lista[0].PosicionGeneral == 2 && lista[0].PosicionEspera == 1 &&
            lista[1].Solicitud.Id == segundo.Id && lista[1].PosicionEspera == 2,
            "HU20: conserva el orden original aunque los arrays del resultado se hayan ordenado por otro campo.");
        comprobar(lista.Single(x => x.Solicitud.Id == pendiente.Id).PosicionEspera is null,
            "HU20: un caso pendiente no recibe una posición de espera elegible.");
        var reserva = ConstructorListaEspera.Construir([primero], [Resultado(primero, "EsperaPropuesta")], posiciones,
            new HashSet<Guid>(), new HashSet<Guid> { primero.AspiranteId }, false, []);
        comprobar(reserva.Single().Estado == "ConReserva", "HU20: una reserva no elimina al aspirante de la espera.");
        var revisada = ConstructorListaEspera.Construir([primero], [Resultado(primero, "EsperaPropuesta")], posiciones,
            new HashSet<Guid>(), new HashSet<Guid>(), true, []);
        comprobar(revisada.Single().Estado == "PendienteRevision" && revisada.Single().PosicionEspera is null,
            "HU20: no recalcula silenciosamente un orden afectado por correcciones.");
        var sinOrden = ConstructorListaEspera.Construir([primero], [Resultado(primero, "EsperaPropuesta")],
            new Dictionary<Guid, int>(), new HashSet<Guid>(), new HashSet<Guid>(), false, []);
        comprobar(sinOrden.Single().Estado == "PendienteRevision", "HU20: no inventa orden cuando falta su procedencia.");
        var nueva = Solicitud(4);
        var ampliada = ConstructorListaEspera.Construir([primero], [Resultado(primero, "EsperaPropuesta")], posiciones,
            new HashSet<Guid>(), new HashSet<Guid>(), false, [nueva]);
        comprobar(ampliada.Single(x => x.Solicitud.Id == nueva.Id).Estado == "PendienteRevision",
            "HU20: una solicitud nueva no se incorpora como espera confirmada automáticamente.");
        var repetida = primero with { Id = Guid.NewGuid() };
        var duplicadas = ConstructorListaEspera.Construir([primero, repetida],
            [Resultado(primero, "EsperaPropuesta"), Resultado(repetida, "EsperaPropuesta")], posiciones,
            new HashSet<Guid>(), new HashSet<Guid>(), false, []);
        comprobar(duplicadas.All(x => x.Estado == "PendienteRevision"),
            "HU20: no elige por su cuenta cuál de dos solicitudes del mismo aspirante continúa.");
    }
}
