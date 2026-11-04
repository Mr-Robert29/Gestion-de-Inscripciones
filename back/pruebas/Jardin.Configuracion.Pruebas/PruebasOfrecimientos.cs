using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasOfrecimientos
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var autor = Guid.NewGuid();
        var ofrecimiento = new OfrecimientoVacante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), autor, "Ofrecer");
        void Rechaza(Action operacion, string mensaje)
        {
            try { operacion(); throw new Exception("No se rechazó la operación inválida."); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException) { comprobar(true, mensaje); }
        }
        Rechaza(() => ofrecimiento.Confirmar(Guid.NewGuid(), 1), "HU21: no confirma sin aceptación familiar.");
        Rechaza(() => ofrecimiento.Responder("Aceptado", 1, autor, "Aceptó", null), "HU21: exige referencia de aceptación.");
        ofrecimiento.Responder("Aceptado", 1, autor, "Aceptó", "Acta familiar");
        ofrecimiento.Responder("Aceptado", 1, autor, "Aceptó", "Acta familiar");
        comprobar(ofrecimiento.Revision == 2 && ofrecimiento.Estado == "Aceptado", "HU21: repetir aceptación no cambia la revisión.");
        Rechaza(() => ofrecimiento.Responder("Rechazado", 1, autor, "Otro", null), "HU21: detecta una pantalla desactualizada.");
        ofrecimiento.Responder("Cancelado", 2, autor, "Cancelar", null);
        Rechaza(() => ofrecimiento.Confirmar(Guid.NewGuid(), 3), "HU21: un ofrecimiento cancelado no se confirma.");
        var ingreso = new IngresoConfirmado(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, Guid.NewGuid(), Guid.NewGuid(), 5, Guid.NewGuid(), autor);
        ingreso.SolicitarBaja(autor, "Retiro");
        comprobar(ingreso.Activo && ingreso.BajaEnProceso, "HU21: una baja pendiente no libera todavía el lugar.");
        ingreso.CompletarBaja();
        ingreso.CompletarBaja();
        comprobar(!ingreso.Activo && ingreso.Revision == 3 && ingreso.FechaBaja.HasValue, "HU21: completar baja es idempotente y conserva motivo y fecha.");
        var turno = Guid.NewGuid();
        var oferta = new OfertaPropuesta(Guid.NewGuid(), Guid.NewGuid(), turno, 1, true, []);
        var solicitud = new SolicitudPropuesta(Guid.NewGuid(), Guid.NewGuid(), 4, false, [], 10m, 1, 1, turno, [], "123", null, DateTimeOffset.UtcNow, false, 4);
        var datos = new EntradasPropuestaIngresos(Guid.NewGuid(), Guid.NewGuid(), 1, "RecepcionCerrada", Guid.NewGuid(), Guid.NewGuid(),
            [5, 4, 3], ["Cercania"], [[3], [4], [5], [3, 4], [4, 5]], [oferta], [solicitud]);
        comprobar(CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, [], []), "HU21: admite composición provisional si todavía no hay sección definitiva.");
        comprobar(!CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, [], [new ComposicionVacante(Guid.NewGuid(), [5], 1, 1)]),
            "HU21: una sección pura de cinco no admite a un aspirante de cuatro automáticamente.");
        var reserva = new OcupacionPlaza(datos.PeriodoId, solicitud.AspiranteId, oferta.Id, 4, "Reserva");
        comprobar(!CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, [reserva], []) &&
            CompatibilidadVacante.EsCompatible(datos, oferta, solicitud, [reserva], [], reserva.Id),
            "HU21: la reserva consume el lugar y se cuenta una sola vez al convertirla.");
    }
}
