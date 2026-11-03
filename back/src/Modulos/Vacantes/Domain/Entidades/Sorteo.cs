namespace Jardin.Vacantes.Domain.Entidades;

public sealed class Sorteo
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public int Sala { get; private set; }
    public int Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string Referencia { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool Vigente { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }

    private Sorteo() { }

    public Sorteo(
        Guid periodoId,
        int sala,
        int numero,
        DateOnly fecha,
        string referencia,
        int version,
        Guid usuarioId,
        DateTimeOffset fechaRegistro,
        Guid? id = null)
    {
        ValidarPeriodo(periodoId);
        ValidarSala(sala);
        ValidarNumero(numero);
        if (fecha == default)
            throw new ArgumentException("La fecha del sorteo es obligatoria.", nameof(fecha));
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "La versión debe ser positiva.");
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario es obligatorio.", nameof(usuarioId));

        Id = id ?? Guid.NewGuid();
        PeriodoId = periodoId;
        Sala = sala;
        Numero = numero;
        Fecha = fecha;
        Referencia = referencia.Trim();
        Version = version;
        Vigente = true;
        UsuarioId = usuarioId;
        FechaRegistro = fechaRegistro;
    }

    public void Desactivar()
    {
        Vigente = false;
    }

    public static void ValidarSala(int sala)
    {
        if (sala is < 3 or > 5)
            throw new ArgumentOutOfRangeException(nameof(sala), "La sala debe ser 3, 4 o 5.");
    }

    public static void ValidarNumero(int numero)
    {
        if (numero is < 0 or > 999)
            throw new ArgumentOutOfRangeException(nameof(numero), "El número del sorteo debe estar entre 000 y 999.");
    }

    private static void ValidarPeriodo(Guid periodoId)
    {
        if (periodoId == Guid.Empty)
            throw new ArgumentException("El período es obligatorio.", nameof(periodoId));
    }
}
