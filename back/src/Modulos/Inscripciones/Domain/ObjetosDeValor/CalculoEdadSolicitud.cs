using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Domain.ObjetosDeValor;

// Conserva los datos realmente usados; una corrección futura del aspirante no reescribe este cálculo.
public sealed record CalculoEdadSolicitud
{
    public DateOnly FechaNacimientoUtilizada { get; private set; }
    public DateOnly FechaCorteUtilizada { get; private set; }
    public int EdadCalculada { get; private set; }
    public int? Sala { get; private set; }
    private CalculoEdadSolicitud() { }

    public CalculoEdadSolicitud(Aspirante aspirante, DateOnly fechaCorte)
    {
        ArgumentNullException.ThrowIfNull(aspirante);
        if (fechaCorte == default) throw new ArgumentException("La fecha de corte es obligatoria.");
        FechaNacimientoUtilizada = aspirante.FechaNacimiento;
        FechaCorteUtilizada = fechaCorte;
        EdadCalculada = aspirante.CalcularEdad(fechaCorte);
        // Una edad excepcional requiere resolución; no se la convierte automáticamente en otra sala.
        Sala = EdadCalculada is >= 3 and <= 5 ? EdadCalculada : null;
    }
}
