using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPrioridades
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var salaCinco = Guid.NewGuid();
        var salaCuatro = Guid.NewGuid();
        var articulacion = Guid.NewGuid();
        var sinDistancia = Guid.NewGuid();
        var entradas = new[]
        {
            new EntradaEvaluacionPrioridad(
                salaCuatro,
                Guid.NewGuid(),
                4,
                false,
                new Dictionary<string, bool>(),
                100m),
            new EntradaEvaluacionPrioridad(
                salaCinco,
                Guid.NewGuid(),
                5,
                false,
                new Dictionary<string, bool>(),
                500m),
            new EntradaEvaluacionPrioridad(
                articulacion,
                Guid.NewGuid(),
                5,
                false,
                new Dictionary<string, bool> { ["ArticulacionPedagogica"] = true },
                900m),
            new EntradaEvaluacionPrioridad(
                sinDistancia,
                Guid.NewGuid(),
                5,
                false,
                new Dictionary<string, bool>(),
                null)
        };

        var resultados = EvaluadorPrioridades.Evaluar(
            entradas,
            [5, 4, 3],
            ["ArticulacionPedagogica", "Cercania", "FueraDeTermino"]);
        var ordenados = resultados.Where(x => x.Posicion is not null).OrderBy(x => x.Posicion).ToArray();
        comprobar(ordenados[0].SolicitudId == articulacion && ordenados[0].Sala == 5,
            "La condición verificada prevalece dentro de la sala correspondiente.");
        comprobar(ordenados[1].SolicitudId == salaCinco && ordenados[1].Sala == 5,
            "La Sala 5 se procesa antes que la Sala 4.");
        comprobar(ordenados[2].SolicitudId == salaCuatro && ordenados[2].Sala == 4,
            "La Sala 4 se procesa después de la Sala 5.");
        comprobar(resultados.Single(x => x.SolicitudId == sinDistancia).RequiereRevision &&
            resultados.Single(x => x.SolicitudId == sinDistancia).Posicion is null,
            "Una distancia ausente se deriva a revisión sin interpretarla como cero.");

        var empate = EvaluadorPrioridades.Evaluar(
            [
                new EntradaEvaluacionPrioridad(Guid.NewGuid(), Guid.NewGuid(), 3, false, new Dictionary<string, bool>(), 10m),
                new EntradaEvaluacionPrioridad(Guid.NewGuid(), Guid.NewGuid(), 3, false, new Dictionary<string, bool>(), 10m)
            ],
            [5, 4, 3],
            ["Cercania"]);
        comprobar(empate.All(x => x.RequiereRevision && x.Posicion is null),
            "Una igualdad completa de criterios no se desempata por identificador ni fecha.");
    }
}
