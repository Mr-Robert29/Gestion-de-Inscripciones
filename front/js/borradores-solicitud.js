import { obtenerDireccionApi } from "./api.js";

export function claveBorradores(estado) {
  const ids = [estado.usuario?.usuarioId, estado.institucion?.id, estado.periodo?.id];
  if (estado.previa || ids.some((id) => !id)) return null;
  return `jardin:borradores:${JSON.stringify([obtenerDireccionApi(), ...ids])}`;
}

export function leerBorradores(clave) {
  if (!clave) return [];
  try {
    const ids = JSON.parse(localStorage.getItem(clave) ?? "[]");
    return Array.isArray(ids) ? ids.filter((id) => typeof id === "string" && id) : [];
  } catch {
    return [];
  }
}

export function actualizarBorrador(clave, id, pendiente) {
  if (!clave) return;
  const ids = new Set(leerBorradores(clave));
  if (pendiente) ids.add(id);
  else ids.delete(id);
  try {
    if (ids.size) localStorage.setItem(clave, JSON.stringify([...ids]));
    else localStorage.removeItem(clave);
  } catch {
    return false;
  }
}

export async function recuperarBorradores(ctx) {
  const clave = claveBorradores(ctx.estado);
  const institucion = encodeURIComponent(ctx.estado.institucion.id);
  const enMemoria = new Map(ctx.estado.borradores.map((item) => [item.id, item]));
  const ids = new Set([...leerBorradores(clave), ...enMemoria.keys()]);
  const resultados = await Promise.all([...ids].map(async (id) => {
    if (enMemoria.has(id)) return enMemoria.get(id);
    try {
      return await ctx.api(`/instituciones/${institucion}/aspirantes/${encodeURIComponent(id)}`);
    } catch (error) {
      if (error.estado !== 404) throw error;
      actualizarBorrador(clave, id, false);
      return null;
    }
  }));
  return resultados.filter(Boolean);
}
