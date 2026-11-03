using Jardin.Configuracion.Domain.ObjetosDeValor;

namespace Jardin.Configuracion.Domain.Entidades;

// Representa el jardín y controla los cambios de sus datos.
public class Institucion
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string? Codigo { get; private set; }
    public Domicilio Domicilio { get; private set; } = null!;
    public string TelefonoContacto { get; private set; } = "";
    public string? CorreoElectronicoContacto { get; private set; }
    public Coordenadas? Coordenadas { get; private set; }
    public bool Activa { get; private set; }
    public int RevisionUbicacion { get; private set; }
    public int RevisionDatos { get; private set; }

    // EF reconstruye los datos guardados; el alta pública utiliza el constructor validado.
    private Institucion() { }

    public Institucion(
        string nombre,
        Domicilio domicilio,
        string telefonoContacto,
        string? codigo = null,
        string? correoElectronicoContacto = null,
        Coordenadas? coordenadas = null)
    {
        ValidarNombre(nombre);
        ValidarDomicilio(domicilio);
        ValidarTelefonoContacto(telefonoContacto);
        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Domicilio = domicilio;
        TelefonoContacto = telefonoContacto.Trim();
        Codigo = NormalizarOpcional(codigo);
        CorreoElectronicoContacto = NormalizarOpcional(correoElectronicoContacto);
        Coordenadas = coordenadas;
        Activa = true;
        RevisionUbicacion = 1;
        RevisionDatos = 1;
    }

    public void ActualizarDatos(
        string nombre,
        string? codigo,
        Domicilio domicilio)
    {
        ValidarNombre(nombre);
        ValidarDomicilio(domicilio);
        Nombre = nombre.Trim();
        Codigo = NormalizarOpcional(codigo);
        if (Domicilio != domicilio)
        {
            Domicilio = domicilio;
            Coordenadas = null;
            RevisionUbicacion++;
        }
        RevisionDatos++;
    }

    public void ActualizarContacto(string telefonoContacto, string? correoElectronicoContacto)
    {
        ValidarTelefonoContacto(telefonoContacto);
        TelefonoContacto = telefonoContacto.Trim();
        CorreoElectronicoContacto = NormalizarOpcional(correoElectronicoContacto);
        RevisionDatos++;
    }

    public void ActualizarUbicacion(Coordenadas coordenadas)
    {
        ArgumentNullException.ThrowIfNull(coordenadas);
        if (Coordenadas != coordenadas)
        {
            Coordenadas = coordenadas;
            RevisionUbicacion++;
            RevisionDatos++;
        }
    }

    // El PUT entrega el estado completo. Validamos todo antes de modificar el objeto.
    public void ActualizarInformacion(
        string nombre,
        Domicilio domicilio,
        string telefonoContacto,
        string? codigo,
        string? correoElectronicoContacto,
        Coordenadas? coordenadas)
    {
        ValidarNombre(nombre);
        ValidarDomicilio(domicilio);
        ValidarTelefonoContacto(telefonoContacto);
        if (Domicilio != domicilio || Coordenadas != coordenadas)
            RevisionUbicacion++;
        Nombre = nombre.Trim();
        Domicilio = domicilio;
        TelefonoContacto = telefonoContacto.Trim();
        Codigo = NormalizarOpcional(codigo);
        CorreoElectronicoContacto = NormalizarOpcional(correoElectronicoContacto);
        Coordenadas = coordenadas;
        RevisionDatos++;
    }

    public void Desactivar()
    {
        if (Activa)
        {
            Activa = false;
            RevisionDatos++;
        }
    }

    private static void ValidarNombre(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
    }

    private static void ValidarDomicilio(Domicilio domicilio)
    {
        ArgumentNullException.ThrowIfNull(domicilio);
        ArgumentException.ThrowIfNullOrWhiteSpace(domicilio.Calle);
        ArgumentException.ThrowIfNullOrWhiteSpace(domicilio.Numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(domicilio.Localidad);
        ArgumentException.ThrowIfNullOrWhiteSpace(domicilio.Provincia);
    }

    private static void ValidarTelefonoContacto(string telefonoContacto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(telefonoContacto);
    }

    private static string? NormalizarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
