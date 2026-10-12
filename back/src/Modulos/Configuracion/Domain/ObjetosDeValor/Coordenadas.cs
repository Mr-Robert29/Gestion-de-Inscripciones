namespace Jardin.Configuracion.Domain.ObjetosDeValor;

public sealed record Coordenadas
{
    public decimal Latitud { get; }
    public decimal Longitud { get; }
    public Coordenadas(decimal latitud, decimal longitud)
    {
        if (latitud < -90 || latitud > 90) throw new ArgumentOutOfRangeException(nameof(latitud));
        if (longitud < -180 || longitud > 180) throw new ArgumentOutOfRangeException(nameof(longitud));
        Latitud = latitud; Longitud = longitud;
    }
}
