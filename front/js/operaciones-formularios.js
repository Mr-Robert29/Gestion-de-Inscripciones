import {
  escapar as e,
  campo,
  campoMotivo,
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
import { validarClave } from "./validacion-clave.js";

export const motivo = (sugerencias = [], opciones = {}) =>
  campoMotivo({ sugerencias, ...opciones });
export const motivos = {
  publicacion: [
    "Configuración inicial del período",
    "Actualización de la fecha de corte",
    "Actualización de las reglas de inscripción",
    "Actualización de aulas, turnos o vacantes",
    "Corrección de una configuración publicada",
  ],
  aperturaRecepcion: [
    "Apertura inicial de la inscripción",
    "Apertura autorizada para el período",
    "Reapertura de la recepción",
    "Corrección del calendario institucional",
  ],
  cierreRecepcion: [
    "Finalización del plazo de inscripción",
    "Cierre según el calendario institucional",
    "Cierre excepcional autorizado",
    "Finalización de la recepción ordinaria",
  ],
  cambioInstitucion: [
    "Actualización de datos institucionales",
    "Corrección de datos registrados",
    "Cambio de domicilio o datos de contacto",
    "Actualización de la ubicación geográfica",
  ],
  oferta: [
    "Carga inicial de la oferta del período",
    "Actualización de la capacidad disponible",
    "Incorporación de un aula o turno",
    "Corrección de la oferta registrada",
    "Actualización de vacantes disponibles",
  ],
  ofertaInstitucional: [
    "Configuración inicial de ofertas reutilizables",
    "Incorporación de una combinación de aula y turno",
    "Actualización de la capacidad de referencia",
    "Corrección de la oferta institucional",
  ],
  propuestaAdmision: [
    "Revisión de las solicitudes recibidas",
    "Disponibilidad de vacantes del período",
    "Aplicación de los criterios de prioridad",
    "Preparación de una nueva propuesta de ingresos",
  ],
  revisarPropuesta: [
    "Cumple los criterios de admisión",
    "No hay una vacante compatible disponible",
    "Documentación pendiente de revisión",
    "Corrección de datos de la solicitud",
    "Se mantiene en lista de espera",
  ],
  reabrirCaso: [
    "Se recibió nueva documentación",
    "Se corrigieron datos de la solicitud",
    "Se requiere revisar nuevamente el caso",
    "Se actualizó la información disponible",
  ],
  ordenManual: [
    "Desempate definido mediante sorteo",
    "Criterio institucional aprobado",
    "Revisión manual de las prioridades",
    "Corrección del orden registrado",
  ],
  duplicado: [
    "Se confirmó que ambas solicitudes son de la misma persona",
    "Se verificó que son personas diferentes",
    "Se corrigieron los datos identificatorios",
    "Se revisó la documentación disponible",
  ],
  revisionDomicilio: [
    "Domicilio verificado con la información disponible",
    "La dirección está incompleta y requiere corrección",
    "El domicilio no coincide con la documentación presentada",
    "Se necesita una referencia adicional para verificarlo",
  ],
  correccionSolicitud: [
    "Corrección solicitada por la familia",
    "Error de carga detectado por la institución",
    "Actualización de domicilio o turno preferido",
    "Corrección de información respaldada por documentación",
  ],
  documentacion: [
    "Documentación presentada por la familia",
    "Documento pendiente de revisión",
    "Documento observado por estar incompleto o ilegible",
    "Actualización o reemplazo de un documento",
    "Verificación de la documentación recibida",
  ],
  condicion: [
    "Nueva condición informada por la familia",
    "Verificación de la información declarada",
    "Se recibió documentación respaldatoria",
    "Se necesita actualizar la información registrada",
  ],
  ofrecerVacante: [
    "Vacante liberada en el período",
    "Candidato compatible según los criterios vigentes",
    "Continuidad de la lista de espera",
    "Se ofrece la vacante siguiendo el orden registrado",
  ],
  respuestaOferta: [
    "Respuesta comunicada por la familia",
    "Aceptación de la vacante ofrecida",
    "Rechazo de la vacante ofrecida",
    "Cancelación solicitada por la familia",
    "Corrección de una respuesta registrada",
  ],
  confirmarIngreso: [
    "La familia aceptó la vacante",
    "Se recibió la constancia familiar requerida",
    "Se verificaron las condiciones para confirmar el ingreso",
  ],
  bajaIngreso: [
    "Baja solicitada por la familia",
    "Cambio de institución",
    "Mudanza o cambio de domicilio",
    "Desistimiento de la vacante",
    "Corrección de una baja previamente informada",
  ],
  composicionSeccion: [
    "Adecuación de la composición por edades",
    "Reorganización de las secciones",
    "Ajuste de la oferta del período",
    "Corrección de la composición registrada",
  ],
  asignarSeccion: [
    "Asignación compatible con la edad y el turno",
    "Revisión manual de la distribución propuesta",
    "No hay una sección compatible disponible",
    "Ajuste por capacidad de las secciones",
  ],
};
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
export function formulario(ctx, titulo, contenido, guardar, opciones = {}) {
  ctx.operacionOrganizacion = {
    guardar,
    institucionId: ctx.estado.institucion?.id,
    periodoId: ctx.estado.periodo?.id,
    usuario: ctx.estado.usuario,
    permiteSinContexto: opciones.permiteSinContexto ?? false,
  };
  ctx.abrirDialogo({
    titulo,
    contenido: `<form class="formulario" data-formulario="organizacion">${contenido}${errorFormulario()}<div class="formulario__acciones"><button type="button" class="boton boton--secundario" data-accion="cerrar-dialogo">Cancelar</button><button type="submit" class="boton" ${ctx.estado.previa ? "disabled" : ""}>${opciones.textoConfirmar ?? "Confirmar operación"}</button></div></form>`,
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
    if (form.querySelector("[data-clave-inicial]"))
      validarClave(datos.claveInicial, datos.confirmarClave);
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
