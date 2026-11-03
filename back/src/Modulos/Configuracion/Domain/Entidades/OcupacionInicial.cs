namespace Jardin.Configuracion.Domain.Entidades;

// Cantidad inicial por grupo etario. La identificación individual se resolverá con P-18.
public sealed class OcupacionInicial
{
    public int Sala { get; private set; }
    public int Cantidad { get; private set; }

    private OcupacionInicial() { }

    public OcupacionInicial(int sala, int cantidad)
    {
        if (sala is not (3 or 4 or 5)) throw new ArgumentOutOfRangeException(nameof(sala), "Las salas del prototipo son 3, 4 y 5.");
        if (cantidad < 0) throw new ArgumentOutOfRangeException(nameof(cantidad), "La ocupación no puede ser negativa.");
        Sala = sala;
        Cantidad = cantidad;
    }

    internal void CambiarCantidad(int cantidad)
    {
        if (cantidad < 0) throw new ArgumentOutOfRangeException(nameof(cantidad));
        Cantidad = cantidad;
    }
}
