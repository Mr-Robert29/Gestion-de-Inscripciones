using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasOrdenSorteo
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var primero = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        var tercero = Guid.NewGuid();
        var sinDni = Guid.NewGuid();
        var resultados = OrdenadorSorteoCircular.Ordenar(
            347,
            [
                new ParticipanteSorteo(primero, Guid.NewGuid(), 3, "12345678"),
                new ParticipanteSorteo(segundo, Guid.NewGuid(), 3, "12345347"),
                new ParticipanteSorteo(tercero, Guid.NewGuid(), 3, "12345348"),
                new ParticipanteSorteo(sinDni, Guid.NewGuid(), 3, null)
            ]);

        var ordenado = resultados.Where(x => x.Posicion is not null).OrderBy(x => x.Posicion).ToArray();
        comprobar(ordenado[0].SolicitudId == segundo && ordenado[0].ClaveCircular == 0,
            "El orden circular comienza en la terminación del sorteo.");
        comprobar(ordenado[1].SolicitudId == tercero && ordenado[1].ClaveCircular == 1,
            "La clave circular conserva el orden ascendente posterior al inicio.");
        comprobar(ordenado[2].SolicitudId == primero && ordenado[2].ClaveCircular == 331,
            "La clave circular recorre el intervalo hasta completar el ciclo.");
        comprobar(resultados.Single(x => x.SolicitudId == sinDni).RequiereRevision,
            "Un participante sin DNI queda pendiente de revisión manual.");

        var empate = OrdenadorSorteoCircular.Ordenar(
            347,
            [
                new ParticipanteSorteo(Guid.NewGuid(), Guid.NewGuid(), 4, "12345347"),
                new ParticipanteSorteo(Guid.NewGuid(), Guid.NewGuid(), 4, "22345347")
            ]);
        comprobar(empate.All(x => x.RequiereRevision && x.Posicion is null),
            "Las terminaciones repetidas no se resuelven automáticamente.");
    }
}
