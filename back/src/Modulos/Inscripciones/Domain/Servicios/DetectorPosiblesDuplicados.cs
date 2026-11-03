using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Jardin.Inscripciones.Domain.Servicios;

public sealed record IdentificacionSolicitud(
    Guid SolicitudId,
    Guid InstitucionId,
    Guid PeriodoId,
    Guid AspiranteId,
    string Nombre,
    string Apellido,
    string? Dni,
    DateOnly FechaNacimiento);

[Flags]
public enum CriterioCoincidenciaSolicitud
{
    Ninguno = 0,
    MismoAspirante = 1,
    DniCompleto = 2,
    NombreApellidoNacimiento = 4
}

public static class DetectorPosiblesDuplicados
{
    public const string Perfil = "DuplicadosIniciales-2026-1";

    public static CriterioCoincidenciaSolicitud Evaluar(IdentificacionSolicitud a, IdentificacionSolicitud b)
    {
        if (a.SolicitudId == b.SolicitudId || a.InstitucionId != b.InstitucionId || a.PeriodoId != b.PeriodoId)
            return CriterioCoincidenciaSolicitud.Ninguno;
        var criterios = CriterioCoincidenciaSolicitud.Ninguno;
        if (a.AspiranteId == b.AspiranteId) criterios |= CriterioCoincidenciaSolicitud.MismoAspirante;
        if (!string.IsNullOrWhiteSpace(a.Dni) && !string.IsNullOrWhiteSpace(b.Dni) && a.Dni.Trim() == b.Dni.Trim())
            criterios |= CriterioCoincidenciaSolicitud.DniCompleto;
        if (NormalizarNombre(a.Nombre) == NormalizarNombre(b.Nombre) && NormalizarNombre(a.Apellido) == NormalizarNombre(b.Apellido) &&
            a.FechaNacimiento == b.FechaNacimiento)
            criterios |= CriterioCoincidenciaSolicitud.NombreApellidoNacimiento;
        return criterios;
    }

    // El orden evita duplicar A-B y B-A; no selecciona qué solicitud continúa.
    public static (Guid A, Guid B) OrdenarIds(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a, b) : (b, a);

    public static HashSet<(Guid A, Guid B)> BuscarParejas(IReadOnlyList<IdentificacionSolicitud> solicitudes)
    {
        var parejas = new HashSet<(Guid A, Guid B)>();
        void AgregarGrupos(IEnumerable<IEnumerable<IdentificacionSolicitud>> grupos)
        {
            foreach (var grupo in grupos)
            {
                var elementos = grupo.ToArray();
                for (var i = 0; i < elementos.Length; i++)
                    for (var j = i + 1; j < elementos.Length; j++)
                        if (Evaluar(elementos[i], elementos[j]) != CriterioCoincidenciaSolicitud.Ninguno)
                            parejas.Add(OrdenarIds(elementos[i].SolicitudId, elementos[j].SolicitudId));
            }
        }
        AgregarGrupos(solicitudes.GroupBy(s => new { s.InstitucionId, s.PeriodoId, s.AspiranteId }));
        AgregarGrupos(solicitudes.Where(s => !string.IsNullOrWhiteSpace(s.Dni)).GroupBy(s => new { s.InstitucionId, s.PeriodoId, Dni = s.Dni!.Trim() }));
        AgregarGrupos(solicitudes.GroupBy(s => new
        {
            s.InstitucionId,
            s.PeriodoId,
            Nombre = NormalizarNombre(s.Nombre),
            Apellido = NormalizarNombre(s.Apellido),
            s.FechaNacimiento
        }));
        return parejas;
    }

    // La huella detecta si cambió la identidad comparada, sin usar revisiones de domicilio o documentación.
    public static string CalcularHuella(IdentificacionSolicitud a, IdentificacionSolicitud b)
    {
        object Normalizada(IdentificacionSolicitud s) => new
        {
            s.SolicitudId,
            s.InstitucionId,
            s.PeriodoId,
            s.AspiranteId,
            Nombre = NormalizarNombre(s.Nombre),
            Apellido = NormalizarNombre(s.Apellido),
            Dni = string.IsNullOrWhiteSpace(s.Dni) ? null : s.Dni.Trim(),
            s.FechaNacimiento
        };
        var datos = JsonSerializer.Serialize(new { Perfil, A = Normalizada(a), B = Normalizada(b) });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
    }

    private static string NormalizarNombre(string nombre) => nombre.Trim().ToUpperInvariant();
}
