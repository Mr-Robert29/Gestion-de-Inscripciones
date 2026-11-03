namespace Jardin.Configuracion.Domain.Entidades;

public enum TipoVersionConfiguracion { Reglas, Oferta }

// Una publicación inmutable: editar la fuente requiere publicar otra versión con otro Id.
public sealed class VersionConfiguracion
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public TipoVersionConfiguracion Tipo { get; private set; }
    public int Formato { get; private set; } = 1;
    public string Datos { get; private set; } = "";
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Motivo { get; private set; } = "";
    public string Referencia { get; private set; } = "";

    private VersionConfiguracion() { }

    public VersionConfiguracion(
        Guid periodoId,
        TipoVersionConfiguracion tipo,
        string datos,
        Guid usuarioId,
        string motivo,
        string referencia)
    {
        if (periodoId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("El período y el autor son obligatorios.");
        if (!Enum.IsDefined(tipo)) throw new ArgumentException("El tipo de versión no es válido.");
        ArgumentException.ThrowIfNullOrWhiteSpace(datos);
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        Tipo = tipo;
        Datos = datos;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Motivo = motivo.Trim();
        Referencia = referencia.Trim();
    }
}
