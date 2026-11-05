export { atenderFormularioOrganizacion } from "./operaciones-formularios.js";
import { atenderAdmision } from "./acciones-admision.js";
import { atenderEspera } from "./acciones-espera.js";
import { atenderSecciones } from "./acciones-secciones.js";
import { atenderAdministracion } from "./acciones-administracion.js";

const operaciones = new Map();
for (const nombre of [
  "org-aceptacion",
  "org-caso-historial",
  "org-confirmar-propuesta",
  "org-generar-propuesta",
  "org-modificar-propuesta",
  "org-orden",
  "org-reabrir",
  "org-resolver",
  "org-sorteo",
])
  operaciones.set(nombre, atenderAdmision);
for (const nombre of [
  "org-baja",
  "org-baja-historial",
  "org-candidatos",
  "org-confirmar-ofrecimiento",
  "org-ofrecer",
  "org-ofrecimiento-historial",
  "org-respuesta",
])
  operaciones.set(nombre, atenderEspera);
for (const nombre of [
  "org-asignar",
  "org-composicion",
  "org-composicion-historial",
  "org-confirmar-distribucion",
  "org-definir-seccion",
  "org-generar-distribucion",
])
  operaciones.set(nombre, atenderSecciones);
for (const nombre of [
  "org-acceso-usuario",
  "org-crear-usuario",
  "org-estado-usuario",
  "org-exportar-nomina",
  "org-exportar-reportes",
  "org-nomina",
  "org-revocar-usuario",
])
  operaciones.set(nombre, atenderAdministracion);

export async function atenderAccionOrganizacion(elemento, ctx) {
  const accion = elemento.dataset.accion;
  if (!accion?.startsWith("org-")) return false;
  if (ctx.estado.previa) {
    ctx.notificar("Iniciá sesión para operar con datos reales.");
    return true;
  }
  const atender = operaciones.get(accion);
  if (!atender) throw new Error("Operación no disponible.");
  return atender(elemento, ctx);
}
