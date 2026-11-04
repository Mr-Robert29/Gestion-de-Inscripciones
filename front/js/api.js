import { configuracion } from "./configuracion.js";

let token = null;
let direccion = configuracion.direccionApi;

export class ErrorApi extends Error {
  constructor(mensaje, estado = 0) {
    super(mensaje);
    this.estado = estado;
  }
}

export function configurarDireccion(valor) {
  const url = new URL(valor);
  if (
    !["http:", "https:"].includes(url.protocol) ||
    url.username ||
    url.password ||
    url.search ||
    url.hash
  ) {
    throw new Error(
      "Ingresá una dirección HTTP o HTTPS válida para el servidor.",
    );
  }
  direccion = url.href.replace(/\/$/, "");
}

// La credencial permanece en memoria; no se escribe en localStorage ni sessionStorage.
export function establecerToken(valor) {
  token = valor;
}

// Los metros usan decimal en C#: preservamos su literal y evitamos convertirlo a Number.
export function serializarDatos(datos, decimales = {}) {
  if (!Object.keys(decimales).length) return JSON.stringify(datos);
  return `{${Object.entries(datos)
    .filter(([, valor]) => valor !== undefined)
    .map(([clave, valor]) => {
      if (!Object.hasOwn(decimales, clave))
        return `${JSON.stringify(clave)}:${JSON.stringify(valor)}`;
      const literal = String(decimales[clave])
        .trim()
        .replace(/^\+/, "")
        .replace(/^\./, "0.")
        .replace(/\.$/, ".0");
      if (!/^(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/.test(literal))
        throw new Error("Ingresá una distancia no negativa válida en metros.");
      return `${JSON.stringify(clave)}:${literal}`;
    })
    .join(",")}}`;
}

export async function solicitar(ruta, opciones = {}) {
  const tokenSolicitud = token;
  const cabeceras = { Accept: "application/json" };
  if (token) cabeceras.Authorization = `Bearer ${token}`;
  if (opciones.datos !== undefined)
    cabeceras["Content-Type"] = "application/json";
  const cuerpo =
    opciones.datos === undefined
      ? undefined
      : serializarDatos(opciones.datos, opciones.decimales);
  let respuesta;
  try {
    respuesta = await fetch(`${direccion}/api/v1${ruta}`, {
      method: opciones.metodo ?? "GET",
      headers: cabeceras,
      body: cuerpo,
      signal: AbortSignal.timeout(configuracion.tiempoEspera),
      credentials: "omit",
      cache: "no-store",
      redirect: "error",
    });
  } catch (error) {
    throw new ErrorApi(
      error.name === "TimeoutError"
        ? "El servidor tardó demasiado en responder. Intentá nuevamente."
        : "No pudimos conectar con el servidor. Verificá que la API esté encendida y revisá la conexión.",
    );
  }
  const texto = await respuesta.text();
  if (tokenSolicitud !== token)
    throw new ErrorApi(
      "La sesión cambió durante la operación. Volvé a consultar los datos.",
    );
  let datos = null;
  if (texto) {
    try {
      datos = JSON.parse(texto);
    } catch {
      /* También se admiten respuestas sin JSON. */
    }
  }
  if (!respuesta.ok) {
    const validaciones = datos?.errors
      ? Object.values(datos.errors).flat().join(" ")
      : "";
    const mensaje =
      datos?.detail ||
      datos?.mensaje ||
      validaciones ||
      {
        401: "Tu sesión venció. Iniciá sesión nuevamente.",
        403: "Tu cuenta no tiene permiso para realizar esta operación.",
        404: "No encontramos la información solicitada.",
        409: "Los datos cambiaron. Actualizá la página antes de volver a guardar.",
      }[respuesta.status] ||
      "No pudimos completar la operación. Intentá nuevamente.";
    if (respuesta.status === 401 && token) {
      establecerToken(null);
      window.dispatchEvent(new CustomEvent("sesion-vencida"));
    }
    throw new ErrorApi(mensaje, respuesta.status);
  }
  return datos;
}
