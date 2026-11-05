import { escapar as e, campo, fecha } from "./interfaz.js";
import {
  rutaPeriodo,
  rutaInstitucional,
  nombreTurno,
  nombreAula,
  nombreSolicitud,
  tabla,
  historial,
} from "./organizacion-comun.js";
import {
  motivo,
  referencia,
  selector,
  opcionesEdades,
  enviar,
  formulario,
  ver,
  exigirRegistro,
} from "./operaciones-formularios.js";

export async function atenderAdministracion(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (["org-exportar-reportes", "org-exportar-nomina"].includes(accion)) {
    const tipo =
      accion === "org-exportar-reportes" ? "reportes" : "nomina-distrital";
    const archivo = await ctx.api(
      `${rutaInstitucional(ctx)}/${tipo}/exportar`,
      { descarga: true },
    );
    const url = URL.createObjectURL(
      new Blob(["\uFEFF", archivo.contenido], { type: archivo.tipo }),
    );
    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = `${tipo}-${ctx.estado.periodo.ciclo}.csv`;
    enlace.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  } else if (accion === "org-nomina") {
    const x = await ctx.api(`${rutaInstitucional(ctx)}/nomina-distrital`);
    ver(
      ctx,
      "Nómina de aspirantes sin vacante",
      `<p>${e(ctx.estado.institucion.nombre)} · ${e(ctx.estado.periodo.ciclo)} · ${e(fecha(x.fechaGeneracion))}</p>` +
        tabla(
          ["Aspirante", "Sala", "Estado", "Motivo"],
          [...x.sinVacante, ...x.pendientes].map((s) => [
            e(`${s.nombre} ${s.apellido}`),
            e(s.sala ?? "Pendiente"),
            e(s.estado),
            e(s.motivo),
          ]),
        ),
    );
  } else if (accion === "org-crear-usuario")
    formulario(
      ctx,
      "Crear cuenta de personal",
      campo({ nombre: "nombre", etiqueta: "Nombre", obligatorio: true }) +
        campo({
          nombre: "correoElectronico",
          etiqueta: "Correo",
          tipo: "email",
          obligatorio: true,
        }) +
        campo({
          nombre: "claveInicial",
          etiqueta: "Contraseña inicial",
          tipo: "password",
          obligatorio: true,
          atributos: 'autocomplete="new-password" minlength="8"',
        }) +
        selector(
          "rolGlobal",
          "Rol",
          ["Administrativo", "Directivo", "AdministradorDelSistema"].map(
            (x) => ({ id: x, nombre: x }),
          ),
        ) +
        selector(
          "institucionInicialId",
          "Institución inicial",
          [
            { id: "", nombre: "Sin acceso inicial" },
            ...ctx.estado.instituciones,
          ],
          "",
          false,
        ),
      (d) =>
        enviar(ctx, "/usuarios", {
          ...d,
          institucionInicialId: d.institucionInicialId || null,
        }),
    );
  else if (
    [
      "org-estado-usuario",
      "org-acceso-usuario",
      "org-revocar-usuario",
    ].includes(accion)
  ) {
    const x = exigirRegistro(datos.usuarios, id);
    if (accion === "org-estado-usuario")
      formulario(
        ctx,
        x.activo ? "Deshabilitar cuenta" : "Habilitar cuenta",
        `<p>${e(x.nombre)} · ${e(x.correoElectronico)}</p><label class="casilla"><input type="checkbox" required>Confirmo el cambio de estado de esta cuenta.</label>`,
        () =>
          enviar(ctx, `/usuarios/${id}/estado`, { activo: !x.activo }, "PATCH"),
      );
    else if (accion === "org-acceso-usuario")
      formulario(
        ctx,
        "Asignar acceso institucional",
        selector("institucionId", "Institución", ctx.estado.instituciones) +
          selector(
            "rol",
            "Rol en la institución",
            ["Administrativo", "Directivo"].map((x) => ({ id: x, nombre: x })),
          ),
        (d) => enviar(ctx, `/usuarios/${id}/accesos`, d),
      );
    else
      formulario(
        ctx,
        "Revocar acceso institucional",
        selector(
          "institucionId",
          "Acceso a revocar",
          x.accesos.map((a) => ({
            id: a.institucionId,
            nombre: `${ctx.estado.instituciones.find((i) => i.id === a.institucionId)?.nombre ?? a.institucionId} · ${a.rol}`,
          })),
        ),
        (d) =>
          enviar(
            ctx,
            `/usuarios/${id}/accesos/${d.institucionId}`,
            undefined,
            "DELETE",
          ),
      );
  }
  return true;
}
