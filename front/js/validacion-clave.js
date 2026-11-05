const longitudMinima = 6;

export function estadoClave(clave = "") {
  return {
    longitud: clave.length >= longitudMinima,
    digito: /\d/.test(clave),
  };
}

export function validarClave(clave, confirmacion) {
  const estado = estadoClave(clave);
  if (!estado.longitud)
    throw new Error(
      `La contraseña debe tener al menos ${longitudMinima} caracteres.`,
    );
  if (!estado.digito)
    throw new Error("La contraseña debe contener al menos un número.");
  if (clave !== confirmacion)
    throw new Error("Las contraseñas no coinciden.");
}

export function actualizarIndicadoresClave(form) {
  const clave = form.querySelector("[data-clave-inicial]");
  if (!clave) return;
  const confirmacion = form.querySelector("[data-clave-confirmacion]");
  const estado = estadoClave(clave.value);
  for (const requisito of form.querySelectorAll("[data-clave-requisito]")) {
    const valido = Boolean(estado[requisito.dataset.claveRequisito]);
    requisito.classList.toggle("clave-requisito--valido", valido);
    requisito.setAttribute("aria-label", valido ? "Cumplido" : "Pendiente");
  }
  if (!confirmacion) return;
  const mensaje = form.querySelector("[data-clave-coincidencia]");
  const tieneValor = confirmacion.value.length > 0;
  const coincide = tieneValor && clave.value === confirmacion.value;
  mensaje.hidden = !tieneValor;
  mensaje.textContent = coincide
    ? "Las contraseñas coinciden."
    : "Las contraseñas todavía no coinciden.";
  mensaje.classList.toggle("clave-coincidencia--valida", coincide);
  mensaje.classList.toggle("clave-coincidencia--invalida", tieneValor && !coincide);
  confirmacion.setAttribute("aria-invalid", String(tieneValor && !coincide));
}
