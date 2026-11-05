// Ejecutar contra --servidor-integracion: utiliza la API real con SQLite descartable.
import assert from "node:assert/strict";
import { configurarDireccion, establecerToken, solicitar } from "../js/api.js";
import { vacantes } from "../js/pagina-vacantes.js";
import { secciones } from "../js/pagina-secciones.js";
import { reportes } from "../js/pagina-reportes.js";
import { usuarios } from "../js/pagina-usuarios.js";
import { atenderAccionOrganizacion } from "../js/acciones-organizacion.js";
const fetchReal = globalThis.fetch;
globalThis.fetch = async (...args) => {
  const respuesta = await fetchReal(...args);
  if (respuesta.status >= 500)
    console.error((await respuesta.clone().text()).slice(0, 2500));
  return respuesta;
};
configurarDireccion("http://127.0.0.1:5189");
const acceso = await solicitar("/Autenticacion/iniciar-sesion", {
  metodo: "POST",
  datos: { correoElectronico: "root@prueba.example", clave: "Prueba123!" },
});
establecerToken(acceso.token);
const alta = (ruta, datos) => solicitar(ruta, { metodo: "POST", datos });
const poner = (ruta, datos) => solicitar(ruta, { metodo: "PUT", datos });
const institucion = await alta("/instituciones", {
  nombre: "Jardín integración frontend",
  telefonoContacto: "123456",
  domicilio: {
    calle: "Ficticia",
    numero: "123",
    localidad: "Ciudad",
    provincia: "Provincia",
  },
});
const base = `/instituciones/${institucion.id}`;
const periodo = await alta(`${base}/periodos`, {
  ciclo: 2027,
  inicio: "2026-11-01",
  fin: "2026-12-01",
});
const p = `/periodos/${periodo.id}`;
const aula = await alta(`${base}/aulas`, { nombre: "Sala de prueba" });
const turno = await alta(`${base}/turnos`, { nombre: "Mañana" });
const oferta = await alta(`${p}/ofertas`, {
  aulaId: aula.id,
  turnoId: turno.id,
  capacidad: 2,
  disponible: true,
  referenciaCapacidad: "Acta ficticia",
  origenOcupaciones: "Prueba aislada",
  ocupaciones: [],
  motivo: "Prueba de integración",
});
const reglas = await alta(`${p}/versiones-reglas`, {
  fechaCorte: "2027-06-30",
  referencia: "Prueba",
  motivo: "Publicar",
});
const versionOferta = await alta(`${p}/versiones-oferta`, {
  referencia: "Prueba",
  motivo: "Publicar",
});
await poner(`${p}/configuracion`, {
  versiones: { versionReglasId: reglas.id, versionOfertaId: versionOferta.id },
  revision: 1,
  motivo: "Asignar",
});
await alta(`${p}/abrir`, { revision: 2, motivo: "Abrir" });
const solicitudes = [];
for (const [nombre, edad, metros] of [
  ["Ana", 5, 10],
  ["Bruno", 5, 20],
  ["Carla", 4, 30],
]) {
  const persona = await alta(`${base}/aspirantes`, {
    nombre,
    apellido: "Ficticio",
    fechaNacimiento: `${2027 - edad}-06-30`,
  });
  const solicitud = await alta(`${base}/periodos/${periodo.id}/solicitudes`, {
    aspiranteId: persona.id,
    domicilio: {
      calle: "Ficticia",
      numero: "1",
      localidad: "Ciudad",
      provincia: "Provincia",
    },
    turnoPreferidoId: turno.id,
    turnosAlternativosAceptados: [],
  });
  await alta(`${base}/solicitudes/${solicitud.id}/distancias/manual`, {
    distanciaMetros: metros,
    referencia: "Prueba aislada",
    revisionDomicilio: 1,
    revisionUbicacionJardin: 1,
    ultimoNumeroRegistro: 0,
  });
  solicitudes.push(solicitud.id);
}
await alta(`${p}/cerrar-recepcion`, { revision: 3, motivo: "Procesar" });
const ctx = {
  estado: {
    instituciones: await solicitar("/instituciones"),
    usuario: { roles: ["AdministradorDelSistema"] },
    institucion: await solicitar(base),
    periodo: await solicitar(p),
    ofertas: await solicitar(`${p}/ofertas`),
    aulas: await solicitar(`${base}/aulas`),
    turnos: await solicitar(`${base}/turnos`),
    pagina: 1,
  },
  api: solicitar,
  puedeResolver: () => true,
  abrirDialogo: (d) => {
    ctx.dialogo = d;
  },
};
let pasos = 0;
async function pagina(funcion, ruta) {
  const html = await funcion(ruta, ctx);
  assert.ok(html.includes("tarjeta"));
  assert.ok(
    !html.includes("undefined"),
    `La página ${ruta} contiene valores ausentes`,
  );
  pasos++;
  return html;
}
async function accion(nombre, datos = {}, id = "") {
  ctx.operacionOrganizacion = null;
  await atenderAccionOrganizacion({ dataset: { accion: nombre, id } }, ctx);
  if (ctx.operacionOrganizacion)
    return ctx.operacionOrganizacion.guardar(datos, new FormData());
}
await pagina(vacantes, "vacantes/prioridades");
await accion("org-sorteo", {
  sala: "5",
  numero: "347",
  fecha: "2026-11-05",
  referencia: "Acta ficticia",
});
await accion("org-sorteo", {
  sala: "4",
  numero: "347",
  fecha: "2026-11-05",
  referencia: "Acta ficticia",
});
await accion("org-sorteo", {
  sala: "3",
  numero: "347",
  fecha: "2026-11-05",
  referencia: "Acta ficticia",
});
await pagina(vacantes, "vacantes/prioridades");
for (const caso of ctx.organizacion.casos.filter(
  (x) => x.estado !== "Resuelto",
)) {
  const entrada = { motivo: "Resolución manual ficticia" };
  caso.solicitudesAfectadas.forEach((id, i) => {
    entrada[`posicion${i}`] = String(i + 1);
  });
  await accion("org-resolver", entrada, caso.id);
}
await accion("org-orden", {}, "5");
await pagina(vacantes, "vacantes");
const propuesta = await accion("org-generar-propuesta", {
  motivo: "Propuesta desde frontend",
});
await pagina(vacantes, propuesta.destino);
await accion("org-confirmar-propuesta");
await pagina(vacantes, "vacantes/ingresos");
assert.equal(ctx.organizacion.ingresos.filter((x) => x.activo).length, 2);
await pagina(secciones, "secciones");
await accion("org-definir-seccion", {
  nombre: "Sección de cinco",
  aulaId: aula.id,
  turnoId: turno.id,
  edades: "5",
});
await pagina(secciones, "secciones");
const seccion = ctx.organizacion.secciones[0];
const distribucion = await accion("org-generar-distribucion");
await pagina(secciones, distribucion.destino);
assert.equal(ctx.organizacion.distribucion.asignaciones.length, 2);
await accion("org-confirmar-distribucion");
await pagina(vacantes, "vacantes/ingresos");
const ingreso = ctx.organizacion.ingresos[0];
await accion("org-baja", { motivo: "Baja de prueba aislada" }, ingreso.id);
await pagina(secciones, "secciones");
await accion(
  "org-composicion",
  { edades: "4,5", motivo: "Ampliar con plaza libre" },
  seccion.id,
);
await pagina(secciones, "secciones");
assert.deepEqual(ctx.organizacion.secciones[0].edadesAdmitidas, [4, 5]);
await accion("org-composicion-historial", {}, seccion.id);
await pagina(vacantes, "vacantes/espera");
assert.equal(ctx.organizacion.lista.totalEnEspera, 1);
await pagina(vacantes, `vacantes/candidatos/${oferta.id}`);
assert.equal(ctx.candidatosOferta.candidatos[0].solicitudId, solicitudes[2]);
await accion(
  "org-ofrecer",
  { motivo: "Ofrecer desde frontend" },
  solicitudes[2],
);
await pagina(vacantes, "vacantes/ofrecimientos");
const ofrecimiento = ctx.organizacion.ofrecimientos[0];
await accion(
  "org-respuesta",
  {
    respuesta: "Aceptado",
    motivo: "Familia acepta",
    referencia: "Acta ficticia",
  },
  ofrecimiento.id,
);
await pagina(vacantes, "vacantes/ofrecimientos");
await accion(
  "org-confirmar-ofrecimiento",
  { motivo: "Confirmar desde frontend" },
  ofrecimiento.id,
);
await pagina(secciones, "secciones/distribuciones");
const segunda = await accion("org-generar-distribucion");
await pagina(secciones, segunda.destino);
assert.equal(
  ctx.organizacion.distribucion.asignaciones.length,
  1,
  "No vuelve a asignar al ingreso que conserva su sección",
);
await accion("org-confirmar-distribucion");
const htmlReportes = await reportes(ctx);
assert.ok(htmlReportes.includes("Resultados e historial"));
const csv = await solicitar(
  `${base}/periodos/${periodo.id}/reportes/exportar`,
  { descarga: true },
);
assert.ok(csv.contenido.includes("Ingreso"));
const nomina = await solicitar(
  `${base}/periodos/${periodo.id}/nomina-distrital/exportar`,
  { descarga: true },
);
assert.ok(nomina.tipo.includes("text/csv"));
await usuarios(ctx);
await accion("org-crear-usuario", {
  nombre: "Personal ficticio",
  correoElectronico: `front-${Date.now()}@prueba.example`,
  claveInicial: "Prueba123!",
  rolGlobal: "Administrativo",
  institucionInicialId: institucion.id,
});
console.log(
  `Integración correcta: ${pasos} consultas de pantallas; propuestas, distribución, baja, multiedad, ofrecimiento, aceptación, ingreso, CSV y usuario mediante la API real aislada.`,
);
console.log(`Institución ficticia preparada: ${institucion.id}`);
