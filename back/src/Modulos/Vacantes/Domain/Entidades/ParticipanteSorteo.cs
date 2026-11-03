namespace Jardin.Vacantes.Domain.Entidades;

public sealed record ParticipanteSorteo(
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    string? Dni)
{
    public bool TieneDni => !string.IsNullOrWhiteSpace(Dni);

    public int? TerminacionDni
    {
        get
        {
            if (!TieneDni)
                return null;

            var ultimosDigitos = Dni!.Where(char.IsDigit).TakeLast(3).ToArray();
            return ultimosDigitos.Length == 3 && int.TryParse(new string(ultimosDigitos), out var terminacion)
                ? terminacion
                : null;
        }
    }
}
