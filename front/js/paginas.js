import { vacantes } from "./pagina-vacantes.js";
import { secciones } from "./pagina-secciones.js";
import { reportes } from "./pagina-reportes.js";
import { usuarios } from "./pagina-usuarios.js";
import { escapar as e, icono, etiqueta, vacio } from "./interfaz.js";
import { encabezado, tarjeta, base } from "./compartidos.js";
import { inicio } from "./pagina-inicio.js";
import { solicitudes, nuevaSolicitud } from "./pagina-solicitudes.js";
import {
  sinInstitucion,
  institucion,
  periodos,
  oferta,
} from "./pagina-configuracion.js";
import { ficha } from "./pagina-ficha.js";
export const titulos = {
  inicio: "Inicio",
  solicitudes: "Solicitudes",
  "nueva-solicitud": "Nueva solicitud",
  solicitud: "Ficha de solicitud",
  domicilios: "Domicilios y distancias",
  documentacion: "Documentación",
  duplicados: "Posibles duplicados",
  institucion: "Mi institución",
  periodos: "Períodos",
  oferta: "Aulas, turnos y oferta",
  vacantes: "Vacantes y admisión",
  secciones: "Secciones",
  ayuda: "Ayuda y estado del sistema",
  reportes: "Resultados e historial",
  usuarios: "Usuarios y permisos",
};
async function duplicados(ctx) {
  const lista = ctx.estado.previa
    ? { resultados: [], hayMas: false }
    : await ctx.api(
        `${base(ctx.estado)}/periodos/${encodeURIComponent(ctx.estado.periodo.id)}/duplicados?tamanoPagina=100`,
      );
  ctx.duplicados = lista.resultados;
  return (
    encabezado({
      titulo: "Posibles duplicados",
      descripcion: "Revisá las coincidencias antes de procesar los resultados.",
      accion:
        '<button class="boton boton--secundario" data-accion="detectar-duplicados">Revisar coincidencias</button>',
    }) +
    `<div class="aviso">Una coincidencia no elimina, fusiona ni excluye solicitudes. Las decisiones deben registrarse con su motivo.</div><section class="tarjeta"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Alertas del período</h2></div>${lista.resultados.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Solicitudes</th><th>Coincidencias</th><th>Estado</th><th>Acción</th></tr></thead><tbody>${lista.resultados.map((d) => `<tr><td><a href="#solicitud/${encodeURIComponent(d.solicitudAId)}">${e(d.solicitudAId.slice(0, 8))}</a> / <a href="#solicitud/${encodeURIComponent(d.solicitudBId)}">${e(d.solicitudBId.slice(0, 8))}</a></td><td>${e(d.criterios.join(", "))}</td><td>${etiqueta(d.estado)}</td><td>${ctx.puedeResolver() ? `<button class="boton boton--secundario boton--pequeno" data-accion="resolver-duplicado" data-id="${e(d.id)}">Registrar decisión</button>` : "Requiere Directivo"}</td></tr>`).join("")}</tbody></table></div>` : vacio({ titulo: "Sin alertas en esta consulta", descripcion: "Podés ejecutar la detección sobre las solicitudes existentes. Los nuevos registros también generan alertas cuando corresponde.", dibujo: "listo" })}${lista.hayMas ? '<div class="tarjeta__pie">Se muestran las primeras 100 alertas. Hay más resultados; la paginación de esta pantalla se incorporará posteriormente.</div>' : ""}</section>`
  );
}
export async function presentarPagina(ruta, ctx) {
  const pagina = ruta.split("/")[0];
  if (pagina === "ayuda")
    return (
      encabezado({
        titulo: "Ayuda y estado del sistema",
        descripcion: "Qué podés hacer en esta primera versión.",
      }) +
      tarjeta({
        titulo: "Tu espacio de trabajo",
        cuerpo:
          '<p>Seleccioná una institución y un período en el encabezado. Cada operación requiere los permisos de tu cuenta.</p><p>El login real necesita el backend y su base de datos disponibles. La vista previa utiliza únicamente datos ficticios.</p><p>Las distancias y la revisión de domicilios son manuales. Vacantes permite revisar propuestas, espera y ofrecimientos. Secciones permite definir grupos y confirmar distribuciones. Resultados e historial permite consultar y descargar reportes.</p><p>Si el sistema indica que los datos cambiaron, actualizá la consulta antes de volver a registrar una decisión. Se conserva el historial.</p><p class="formulario__nota">La sesión permanece en memoria: al recargar la página, deberás ingresar nuevamente.</p>',
      })
    );
  if (pagina === "usuarios") return usuarios(ctx);
  if (!ctx.estado.institucion) return sinInstitucion(ctx);
  if (pagina === "inicio") return inicio(ctx);
  if (pagina === "institucion") return institucion(ctx);
  if (pagina === "periodos") return periodos(ctx);
  if (pagina === "oferta") return oferta(ctx);
  if (!ctx.estado.periodo)
    return (
      encabezado({
        titulo: titulos[pagina] ?? "Inscripciones",
        descripcion: "Primero seleccioná o creá un período.",
      }) +
      tarjeta({
        titulo: "Sin período seleccionado",
        cuerpo:
          '<p>Las solicitudes pertenecen a una institución y un período.</p><a class="boton" href="#periodos">Ir a períodos</a>',
      })
    );
  if (pagina === "vacantes") return vacantes(ruta, ctx);
  if (pagina === "secciones") return secciones(ruta, ctx);
  if (pagina === "reportes") return reportes(ctx);
  if (pagina === "solicitud") return ficha(ruta, ctx);
  if (pagina === "nueva-solicitud") return nuevaSolicitud(ctx);
  if (pagina === "solicitudes") return solicitudes(ctx);
  if (pagina === "domicilios") return solicitudes(ctx, "domicilio");
  if (pagina === "documentacion") return solicitudes(ctx, "documentos");
  if (pagina === "duplicados") return duplicados(ctx);
  return vacio({
    titulo: "Esta página no existe",
    descripcion: "Volvé al inicio para continuar.",
    accion: '<a class="boton" href="#inicio">Ir al inicio</a>',
  });
}
