import {
  escapar as e,
  campo,
  seleccion,
  errorFormulario,
  fecha,
} from "./interfaz.js";
import {
  rutaPeriodo,
  rutaInstitucional,
  nombreTurno,
  nombreAula,
  nombreSolicitud,
  tabla,
  historial,
} from "./organizacion-comun.js";

export const motivo = () =>
  campo({
    nombre: "motivo",
    etiqueta: "Motivo de la operación",
    obligatorio: true,
  });
export const referencia = () =>
  campo({
    nombre: "referencia",
    etiqueta: "Referencia o constancia",
    obligatorio: true,
  });
export const selector = (
  nombre,
  etiqueta,
  opciones,
  valor = "",
  obligatorio = true,
) => seleccion({ nombre, etiqueta, opciones, valor, obligatorio });
export const opcionesEdades = [3, 4, 5].map((x) => ({
  id: x,
  nombre: `${x} años`,
}));
export const enviar = (ctx, ruta, datos, metodo = "POST") =>
  ctx.api(ruta, { metodo, datos });
export function formulario(ctx, titulo, contenido, guardar) {
  ctx.operacionOrganizacion = {
    guardar,
    institucionId: ctx.estado.institucion?.id,
    periodoId: ctx.estado.periodo?.id,
    usuario: ctx.estado.usuario,
  };
  ctx.abrirDialogo({
    titulo,
    contenido: `<form class="formulario" data-formulario="organizacion">${contenido}${errorFormulario()}<div class="formulario__acciones"><button type="button" class="boton boton--secundario" data-accion="cerrar-dialogo">Cancelar</button><button type="submit" class="boton" ${ctx.estado.previa ? "disabled" : ""}>Confirmar operación</button></div></form>`,
  });
}
export function ver(ctx, titulo, contenido) {
  ctx.abrirDialogo({
    titulo,
    contenido: `${contenido}<div class="formulario__acciones"><button class="boton" data-accion="cerrar-dialogo">Cerrar</button></div>`,
  });
}
export function exigirRegistro(lista, id) {
  const registro = lista?.find((x) => x.id === id);
  if (!registro)
    throw new Error("Volvé a consultar los datos antes de continuar.");
  return registro;
}
export async function atenderFormularioOrganizacion(form, ctx) {
  if (form.dataset.formulario !== "organizacion") return false;
  if (form.dataset.guardando === "true") return true;
  const operacion = ctx.operacionOrganizacion;
  const boton = form.querySelector('[type="submit"]');
  const error = form.querySelector("[data-error]");
  form.dataset.guardando = "true";
  boton.disabled = true;
  error.hidden = true;
  try {
    if (ctx.estado.previa || !ctx.estado.usuario)
      throw new Error("Iniciá sesión para guardar.");
    if (
      !operacion ||
      operacion.usuario !== ctx.estado.usuario ||
      operacion.institucionId !== ctx.estado.institucion?.id ||
      operacion.periodoId !== ctx.estado.periodo?.id
    )
      throw new Error(
        "Cambió el contexto. Cerrá este formulario y volvé a consultarlo.",
      );
    const datos = Object.fromEntries(new FormData(form));
    const resultado = await operacion.guardar(datos, new FormData(form));
    if (operacion.usuario !== ctx.estado.usuario) return true;
    ctx.cerrarDialogo();
    if (resultado?.destino) ctx.navegar(resultado.destino);
    else await ctx.actualizar();
    ctx.notificar(
      resultado?.mensaje ?? "La operación se registró correctamente.",
    );
  } catch (problema) {
    if (form.isConnected) {
      error.textContent =
        problema.message +
        (problema.estado === 409
          ? " Cerrá y volvé a consultar para usar la revisión actual."
          : "");
      error.hidden = false;
    }
  } finally {
    form.dataset.guardando = "false";
    if (boton.isConnected) boton.disabled = false;
  }
  return true;
}
