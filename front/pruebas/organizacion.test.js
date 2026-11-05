import test from "node:test";
import assert from "node:assert/strict";
import { vacantes } from "../js/pagina-vacantes.js";
import { secciones } from "../js/pagina-secciones.js";
import { reportes } from "../js/pagina-reportes.js";
import { usuarios } from "../js/pagina-usuarios.js";
import { atenderAccionOrganizacion } from "../js/acciones-organizacion.js";
import { estadoClave, validarClave } from "../js/validacion-clave.js";
import {
  configurarDireccion,
  establecerToken,
  solicitar,
  ErrorApi,
} from "../js/api.js";

const contexto = () => ({
  estado: {
    previa: false,
    usuario: { roles: ["AdministradorDelSistema"] },
    institucion: { id: "institucion" },
    periodo: { id: "periodo" },
    turnos: [],
    aulas: [],
    ofertas: [],
    instituciones: [],
  },
  puedeResolver: () => true,
  abrirDialogo(d) {
    this.dialogo = d;
  },
});
test("la contraseña del personal exige longitud y un número", () => {
  assert.deepEqual(estadoClave("abc123"), { longitud: true, digito: true });
  assert.throws(() => validarClave("abcdef", "abcdef"), /número/);
  assert.throws(() => validarClave("abc12", "abc12"), /6 caracteres/);
  assert.throws(() => validarClave("abc123", "abc124"), /no coinciden/);
  assert.doesNotThrow(() => validarClave("abc123", "abc123"));
});
test("las pantallas de vista previa no consultan ni guardan en la API", async () => {
  const ctx = contexto();
  ctx.estado.previa = true;
  ctx.api = () => assert.fail("La vista previa no debe enviar peticiones");
  for (const funcion of [
    () => vacantes("vacantes", ctx),
    () => secciones("secciones", ctx),
    () => reportes(ctx),
    () => usuarios(ctx),
  ])
    assert.match(await funcion(), /vista previa/);
});
test("la confirmación usa la huella de la propuesta mostrada y no confirma al abrir el formulario", async () => {
  const ctx = contexto();
  const llamadas = [];
  ctx.api = async (ruta, opciones) => {
    llamadas.push({ ruta, opciones });
    return [];
  };
  ctx.organizacion = {
    propuesta: { id: "propuesta", huellaEntradas: "huella-vigente" },
  };
  await atenderAccionOrganizacion(
    { dataset: { accion: "org-confirmar-propuesta" } },
    ctx,
  );
  assert.equal(llamadas.length, 0);
  await ctx.operacionOrganizacion.guardar({});
  assert.equal(llamadas[0].opciones.datos.huellaEsperada, "huella-vigente");
  assert.equal(llamadas[0].ruta, "/propuestas-ingresos/propuesta/confirmacion");
});
test("reintentar un ofrecimiento conserva su identificador de operación", async () => {
  const ctx = contexto();
  const cuerpos = [];
  ctx.candidatosOferta = {
    periodoId: "periodo",
    ofertaId: "oferta",
    propuestaListaId: "base",
    operacionId: "misma-operacion",
  };
  ctx.api = async (_, opciones) => {
    cuerpos.push(opciones.datos);
    return {};
  };
  await atenderAccionOrganizacion(
    { dataset: { accion: "org-ofrecer", id: "solicitud" } },
    ctx,
  );
  await ctx.operacionOrganizacion.guardar({ motivo: "Motivo" });
  await ctx.operacionOrganizacion.guardar({ motivo: "Motivo" });
  assert.equal(cuerpos[0].operacionId, cuerpos[1].operacionId);
  assert.equal(cuerpos[0].propuestaListaId, "base");
});
test("el orden manual exige posiciones únicas y conserva la revisión del caso", async () => {
  const ctx = contexto();
  ctx.api = async (_, opciones) => opciones.datos;
  ctx.organizacion = {
    casos: [{ id: "caso", revision: 7, solicitudesAfectadas: ["a", "b"] }],
  };
  await atenderAccionOrganizacion(
    { dataset: { accion: "org-resolver", id: "caso" } },
    ctx,
  );
  assert.throws(
    () =>
      ctx.operacionOrganizacion.guardar({
        posicion0: "1",
        posicion1: "1",
        motivo: "Decisión",
      }),
    /diferente/,
  );
  const datos = await ctx.operacionOrganizacion.guardar({
    posicion0: "2",
    posicion1: "1",
    motivo: "Decisión",
  });
  assert.deepEqual(datos.ordenDecidido, ["b", "a"]);
  assert.equal(datos.revision, 7);
});
test("la baja pendiente informa que la plaza todavía no está libre", async () => {
  const ctx = contexto();
  ctx.organizacion = {
    ingresos: [
      {
        id: "ingreso",
        revision: 4,
        bajaEnProceso: true,
        motivoBaja: "Motivo original",
      },
    ],
  };
  ctx.api = async (_, opciones) => {
    assert.equal(opciones.datos.revisionIngreso, 4);
    return { plazaLiberada: false };
  };
  await atenderAccionOrganizacion(
    { dataset: { accion: "org-baja", id: "ingreso" } },
    ctx,
  );
  assert.match(
    (await ctx.operacionOrganizacion.guardar({ motivo: "Motivo original" }))
      .mensaje,
    /permanece ocupada/,
  );
});
test("la pantalla escapa nombres y motivos externos de los ofrecimientos", async () => {
  const ctx = contexto();
  ctx.api = async (ruta) =>
    ruta.includes("buscar")
      ? {
          resultados: [
            { id: "s", nombre: "<img src=x onerror=alert(1)>", apellido: "A" },
          ],
          hayMas: false,
        }
      : [
          {
            id: "o",
            solicitudId: "s",
            estado: "Rechazado",
            fecha: "2026-11-05",
            historial: [],
          },
        ];
  const html = await vacantes("vacantes/ofrecimientos", ctx);
  assert.ok(!html.includes("<img"));
  assert.match(html, /&lt;img/);
});
test("las descargas CSV usan JWT, conservan el contenido y rechazan respuestas JSON", async () => {
  const anterior = globalThis.fetch;
  configurarDireccion("http://127.0.0.1:5189");
  establecerToken("token-ficticio");
  try {
    globalThis.fetch = async (_, opciones) => {
      assert.equal(opciones.headers.Authorization, "Bearer token-ficticio");
      return new Response("Nombre;Estado\nAna;Activo", {
        headers: { "Content-Type": "text/csv; charset=utf-8" },
      });
    };
    assert.match(
      (await solicitar("/archivo", { descarga: true })).contenido,
      /Ana;Activo/,
    );
    globalThis.fetch = async () =>
      new Response("{}", { headers: { "Content-Type": "application/json" } });
    await assert.rejects(
      solicitar("/archivo", { descarga: true }),
      /CSV esperado/,
    );
  } finally {
    globalThis.fetch = anterior;
    establecerToken(null);
  }
});
