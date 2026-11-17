using System.Text.RegularExpressions;

namespace Jardin.Usuarios.Domain.Entidades;

// Agrupa las cuentas locales de una misma persona sin compartir sus credenciales.
public sealed class IdentidadPersonal
{
    public Guid Id { get; private set; }
    public string? DniNormalizado { get; private set; }

    private IdentidadPersonal() { }

    public IdentidadPersonal(Guid id, string? dni)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador no puede ser vacío.", nameof(id));
        Id = id;
        DniNormalizado = NormalizarDni(dni);
    }

    public void AsignarDni(string dni)
    {
        DniNormalizado = NormalizarDni(dni)
            ?? throw new ArgumentException("Ingresá un DNI válido.", nameof(dni));
    }

    public static string? NormalizarDni(string? dni)
    {
        if (string.IsNullOrWhiteSpace(dni)) return null;
        var normalizado = Regex.Replace(dni.Trim(), @"[.\s-]", "");
        if (!Regex.IsMatch(normalizado, @"^\d{7,8}$"))
            throw new ArgumentException("El DNI debe contener 7 u 8 números.", nameof(dni));
        return normalizado;
    }
}
