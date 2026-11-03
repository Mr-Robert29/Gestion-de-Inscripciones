namespace Jardin.Configuracion.Contracts;

// La revisión evita operar sobre un estado anterior; el motivo forma parte del historial.
public sealed record CambiarRecepcionPeticion(int Revision, string Motivo);
