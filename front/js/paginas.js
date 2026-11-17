import { vacantes } from "./pagina-vacantes.js?v=20261117-ingresos-filtros-v1";
import { secciones } from "./pagina-secciones.js?v=20261117-distribucion-filtros-v1";
import { reportes } from "./pagina-reportes.js";
import { usuarios } from "./pagina-usuarios.js";
import { escapar as e, icono, etiqueta, vacio } from "./interfaz.js";
import { encabezado, tarjeta, base } from "./compartidos.js";
import { inicio } from "./pagina-inicio.js";
import { solicitudes, nuevaSolicitud } from "./pagina-solicitudes.js";
import { claveBorradores, recuperarBorradores } from "./borradores-solicitud.js";
import {
  sinInstitucion,
  institucion,
  periodos,
  oferta,
} from "./pagina-configuracion.js";
import { ficha } from "./pagina-ficha.js";
import { tutorial } from "./pagina-tutorial.js";
import { personalInstitucion } from "./pagina-personal.js";
export const titulos = {
  inicio: "Inicio",
  tutorial: "Tutorial de uso",
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
  personal: "Personal del jardín",
};
async function duplicados(ctx) {
  const lista = ctx.estado.previa
    ? { resultados: [], hayMas: false }
    : await ctx.api(
        `${base(ctx.estado)}/periodos/${encodeURIComponent(ctx.estado.periodo.id)}/duplicados?tamanoPagina=100`,
      );
  ctx.duplicados = lista.resultados;
  const valor = (objeto, propiedad) => objeto?.[propiedad] ?? objeto?.[propiedad[0].toUpperCase() + propiedad.slice(1)];
  const aspiranteEnAlerta = (datos, letra, id) => {
    const solicitud = valor(datos, letra);
    const nombre = [valor(solicitud, "nombre"), valor(solicitud, "apellido")].filter(Boolean).join(" ");
    return `<a class="duplicado__persona" href="#solicitud/${encodeURIComponent(id)}"><span>${e(nombre || "Aspirante sin nombre")}</span><small>Solicitud <span class="duplicado__id">${e(id.slice(0, 8))}</span></small></a>`;
  };
  const criterioLegible = {
    MismoAspirante: "Mismo aspirante registrado",
    DniCompleto: "Mismo DNI completo",
    NombreApellidoNacimiento: "Mismo nombre, apellido y fecha de nacimiento",
  };
  return (
    encabezado({
      titulo: "Posibles duplicados",
      descripcion: "Encontrá solicitudes que podrían corresponder al mismo aspirante y revisalas antes de avanzar con la admisión.",
      accion:
        '<button class="boton boton--secundario" data-accion="detectar-duplicados">Revisar coincidencias</button>',
    }) +
    `<div class="aviso"><strong>¿Para qué sirve?</strong> Señala solicitudes que podrían ser de la misma persona. Compará los datos de ambas fichas y registrá si es una duplicación o si son personas diferentes. Esta alerta no cambia ni elimina solicitudes.</div><section class="tarjeta"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Alertas del período</h2><span class="formulario__nota">${lista.resultados.length} ${lista.resultados.length === 1 ? "caso para revisar" : "casos para revisar"}</span></div>${lista.resultados.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Solicitudes comparadas</th><th>Por qué se marcó</th><th>Estado</th><th>Acción</th></tr></thead><tbody>${lista.resultados.map((d) => `<tr><td><div class="duplicado__pareja">${aspiranteEnAlerta(d.datosEvaluados, "a", d.solicitudAId)}<span class="duplicado__separador" aria-hidden="true">↔</span>${aspiranteEnAlerta(d.datosEvaluados, "b", d.solicitudBId)}</div></td><td><div class="duplicado__criterios">${d.criterios.map((criterio) => `<span class="etiqueta etiqueta--neutra">${e(criterioLegible[criterio] ?? criterio)}</span>`).join("")}</div></td><td>${etiqueta(d.estado)}</td><td>${ctx.puedeResolver() ? `<button class="boton boton--secundario boton--pequeno" data-accion="resolver-duplicado" data-id="${e(d.id)}">Revisar y decidir</button>` : "Requiere Directivo"}</td></tr>`).join("")}</tbody></table></div>` : vacio({ titulo: "No hay alertas para revisar", descripcion: "Podés ejecutar la detección para comparar las solicitudes existentes. Las nuevas solicitudes también se revisan automáticamente.", dibujo: "listo" })}${lista.hayMas ? '<div class="tarjeta__pie">Se muestran las primeras 100 alertas. Hay más resultados; la paginación de esta pantalla se incorporará posteriormente.</div>' : ""}</section>`
  );
}
export async function presentarPagina(ruta, ctx) {
  const pagina = ruta.split("/")[0];
  if (pagina === "tutorial") return tutorial(ctx);
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
  if (pagina === "personal") return personalInstitucion(ctx);
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
  if (pagina === "nueva-solicitud") {
    const aspiranteId = ruta.split("/")[1];
    if (aspiranteId === "nuevo") {
      ctx.estado.borrador = null;
    } else if (aspiranteId) {
      const clave = claveBorradores(ctx.estado);
      const borradores = await recuperarBorradores(ctx);
      if (clave !== claveBorradores(ctx.estado)) return "";
      ctx.estado.borrador = borradores.find(
        (item) => item.id === decodeURIComponent(aspiranteId),
      ) ?? null;
    }
    return nuevaSolicitud(ctx);
  }
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
