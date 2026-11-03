namespace Jardin.Configuracion.Domain.Entidades;

// El estado de recepción no representa la finalización de toda la admisión.
public enum EstadoPeriodo
{
    Preparado,
    RecepcionAbierta,
    RecepcionCerrada
}

public sealed class PeriodoInscripcion
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public int Ciclo { get; private set; }
    public DateOnly Inicio { get; private set; }
    public DateOnly Fin { get; private set; }
    public EstadoPeriodo Estado { get; private set; }
    public int Revision { get; private set; }

    private PeriodoInscripcion() { }

    public PeriodoInscripcion(
        Guid institucionId,
        int ciclo,
        DateOnly inicio,
        DateOnly fin)
    {
        if (institucionId == Guid.Empty) throw new ArgumentException("La institución es obligatoria.", nameof(institucionId));
        if (ciclo < 1 || ciclo > 9999) throw new ArgumentOutOfRangeException(nameof(ciclo), "El ciclo debe ser un año válido.");
        ValidarFechas(inicio, fin);
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        Ciclo = ciclo;
        Inicio = inicio;
        Fin = fin;
        Estado = EstadoPeriodo.Preparado;
        Revision = 1;
    }

    public void Abrir()
    {
        if (Estado != EstadoPeriodo.Preparado)
            throw new InvalidOperationException("Solo se puede abrir un período preparado.");
        ValidarFechas(Inicio, Fin);
        // Prototipo: el control de oferta y versiones se pospuso expresamente.
        Estado = EstadoPeriodo.RecepcionAbierta;
        Revision++;
    }

    public void CerrarRecepcion()
    {
        if (Estado != EstadoPeriodo.RecepcionAbierta)
            throw new InvalidOperationException("Solo se puede cerrar una recepción abierta.");
        Estado = EstadoPeriodo.RecepcionCerrada;
        Revision++;
    }

    private static void ValidarFechas(DateOnly inicio, DateOnly fin)
    {
        if (inicio == default || fin == default)
            throw new ArgumentException("Las fechas de inicio y fin son obligatorias.");
        if (fin < inicio)
            throw new ArgumentException("La fecha de fin no puede ser anterior al inicio.");
    }
}
