namespace Jardin.Vacantes.Domain.Entidades;

public sealed record EntradaEvaluacionPrioridad(
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    bool FueraDeTermino,
    IReadOnlyDictionary<string, bool> CondicionesVerificadas,
    decimal? DistanciaMetros)
{
    public bool TieneCondicion(string tipo)
        => CondicionesVerificadas.TryGetValue(tipo, out var verificada) && verificada;
}
