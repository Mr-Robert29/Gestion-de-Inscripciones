using System.Globalization;
using System.Text.Json;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Propuestas;

// Lee las entradas históricas sin reconstruir entidades ni ejecutar nuevamente el proceso de admisión.
internal sealed record FuenteListaEspera(
    EntradasPropuestaIngresos Entradas,
    IReadOnlyList<ResultadoPropuestaIngreso> Resultados,
    IReadOnlyList<JsonElement> Sorteos,
    IReadOnlyList<JsonElement> Casos)
{
    public static FuenteListaEspera Leer(PropuestaIngresos propuesta)
    {
        try
        {
            using var documento = JsonDocument.Parse(propuesta.Entradas);
            var entradas = documento.RootElement.GetProperty("Entradas").Deserialize<EntradasPropuestaIngresos>()
                ?? throw new InvalidOperationException("No se conservan las entradas de admisión.");
            if (entradas.PeriodoId != propuesta.PeriodoId || entradas.InstitucionId != propuesta.InstitucionId)
                throw new InvalidOperationException("La instantánea no coincide con su institución y período.");
            return new FuenteListaEspera(
                entradas,
                JsonSerializer.Deserialize<ResultadoPropuestaIngreso[]>(propuesta.Resultados) ?? [],
                documento.RootElement.GetProperty("Sorteos").EnumerateArray().Select(x => x.Clone()).ToArray(),
                documento.RootElement.GetProperty("Casos").EnumerateArray().Select(x => x.Clone()).ToArray());
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException)
        {
            throw new InvalidOperationException("La instantánea de admisión no tiene un formato compatible.", error);
        }
    }

    public DatosSorteoListaEspera? ConsultarSorteo(SolicitudPropuesta solicitud)
    {
        var sorteos = Sorteos.Where(x => x.GetProperty("Sala").GetInt32() == solicitud.Sala).ToArray();
        if (sorteos.Length != 1) return null;
        var sorteo = sorteos[0];
        var numero = sorteo.GetProperty("Numero").GetInt32();
        var participante = new ParticipanteSorteo(solicitud.Id, solicitud.AspiranteId, solicitud.Sala!.Value, solicitud.Dni);
        var terminacion = participante.TerminacionDni;
        var mismoGrupo = Entradas.Solicitudes.Count(x => !x.FueraDeTermino && x.Sala == solicitud.Sala && Clave(x) == Clave(solicitud)) > 1;
        return new DatosSorteoListaEspera(
            sorteo.GetProperty("Id").GetGuid(),
            sorteo.GetProperty("Version").GetInt32(),
            numero.ToString("D3", CultureInfo.InvariantCulture),
            terminacion,
            terminacion is int valor ? (valor - numero + 1000) % 1000 : null,
            mismoGrupo && Entradas.OrdenProcedimiento.Contains("SorteoUltimaInstancia"));
    }

    public IReadOnlyList<DatosResolucionListaEspera> ConsultarCasos(Guid solicitudId)
        => Casos.Where(x => LeerIdentificadores(x.GetProperty("SolicitudesAfectadas")).Contains(solicitudId))
            .Select(x =>
            {
                var orden = LeerIdentificadores(x.GetProperty("OrdenDecidido"));
                var indice = Array.IndexOf(orden, solicitudId);
                var tipo = x.GetProperty("Tipo");
                var estado = x.GetProperty("Estado");
                return new DatosResolucionListaEspera(
                    x.GetProperty("Id").GetGuid(),
                    tipo.ValueKind == JsonValueKind.Number ? ((TipoCasoRevision)tipo.GetInt32()).ToString() : tipo.GetString()!,
                    estado.ValueKind == JsonValueKind.Number ? ((EstadoCasoRevision)estado.GetInt32()).ToString() : estado.GetString()!,
                    indice >= 0 ? indice + 1 : null,
                    x.GetProperty("MotivoResolucion").GetString());
            }).ToArray();

    private string Clave(SolicitudPropuesta solicitud)
        => string.Join('|', Entradas.OrdenProcedimiento.Where(x => x is not ("SorteoUltimaInstancia" or "FueraDeTermino"))
            .Select(x => x == "Cercania"
                ? solicitud.DistanciaMetros is decimal distancia
                    ? Math.Round(distancia, Entradas.PrecisionDistanciaDecimales, MidpointRounding.AwayFromZero).ToString("G29", CultureInfo.InvariantCulture)
                    : "sin-distancia"
                : solicitud.Condiciones.Contains(x, StringComparer.Ordinal) ? "1" : "0"));

    private static Guid[] LeerIdentificadores(JsonElement valor)
        => valor.ValueKind == JsonValueKind.String
            ? JsonSerializer.Deserialize<Guid[]>(valor.GetString()!) ?? []
            : valor.Deserialize<Guid[]>() ?? [];
}
