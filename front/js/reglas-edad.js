function leerFecha(fecha) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(fecha ?? "")) return null;
  const [anio, mes, dia] = fecha.split("-").map(Number);
  const valor = new Date(Date.UTC(anio, mes - 1, dia));
  return valor.getUTCFullYear() === anio && valor.getUTCMonth() === mes - 1 && valor.getUTCDate() === dia
    ? valor
    : null;
}

function fechaIso(valor) {
  return valor.toISOString().slice(0, 10);
}

function moverAnios(fecha, anios) {
  const anio = fecha.getUTCFullYear() + anios;
  const mes = fecha.getUTCMonth();
  const dia = Math.min(fecha.getUTCDate(), new Date(Date.UTC(anio, mes + 1, 0)).getUTCDate());
  return new Date(Date.UTC(anio, mes, dia));
}

export function edadesDeReglas(reglas) {
  const combinaciones = reglas?.CombinacionesEdades ?? reglas?.combinacionesEdades;
  if (!Array.isArray(combinaciones)) return null;
  const edades = [...new Set(combinaciones.flat().filter((edad) => Number.isInteger(edad) && edad >= 0))].sort((a, b) => a - b);
  return edades.length ? edades : null;
}

export function rangoFechasNacimiento(fechaCorte, edades) {
  const corte = leerFecha(fechaCorte);
  if (!corte || !edades?.length) return null;
  const edadMinima = Math.min(...edades);
  const edadMaxima = Math.max(...edades);
  const nacimientoMasReciente = moverAnios(corte, -edadMinima);
  const nacimientoMasAntiguo = moverAnios(corte, -(edadMaxima + 1));
  nacimientoMasAntiguo.setUTCDate(nacimientoMasAntiguo.getUTCDate() + 1);
  return { minimo: fechaIso(nacimientoMasAntiguo), maximo: fechaIso(nacimientoMasReciente) };
}

export function edadEnFecha(fechaNacimiento, fechaCorte) {
  const nacimiento = leerFecha(fechaNacimiento);
  const corte = leerFecha(fechaCorte);
  if (!nacimiento || !corte || nacimiento > corte) return null;
  let edad = corte.getUTCFullYear() - nacimiento.getUTCFullYear();
  if (
    corte.getUTCMonth() < nacimiento.getUTCMonth() ||
    (corte.getUTCMonth() === nacimiento.getUTCMonth() && corte.getUTCDate() < nacimiento.getUTCDate())
  ) edad--;
  return edad;
}

export function fechaNacimientoAdmitida(fechaNacimiento, fechaCorte, edades) {
  const edad = edadEnFecha(fechaNacimiento, fechaCorte);
  return edad !== null && edades?.includes(edad);
}
