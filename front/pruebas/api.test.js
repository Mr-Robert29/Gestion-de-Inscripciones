import prueba from "node:test";
import assert from "node:assert/strict";
import {
  serializarDatos,
  solicitar,
  establecerToken,
  configurarDireccion,
} from "../js/api.js";
import { escapar } from "../js/interfaz.js";

prueba(
  "conserva todos los dígitos de los metros al enviarlos como decimal JSON",
  () => {
    const metros = "123.12345678901234567890123456";
    const cuerpo = serializarDatos(
      { distanciaMetros: metros, referencia: 'Plano "A"', omitido: undefined },
      { distanciaMetros: metros },
    );
    assert.equal(
      cuerpo,
      '{"distanciaMetros":123.12345678901234567890123456,"referencia":"Plano \\"A\\""}'.replaceAll(
        "\\\\",
        "\\",
      ),
    );
    assert.equal(
      serializarDatos({ distanciaMetros: ".25" }, { distanciaMetros: ".25" }),
      '{"distanciaMetros":0.25}',
    );
  },
);

prueba(
  "rechaza valores negativos y texto que intentaría alterar el JSON",
  () => {
    for (const valor of ["-1", "NaN", "Infinity", '0,"otro":true', ""]) {
      assert.throws(() =>
        serializarDatos({ distanciaMetros: valor }, { distanciaMetros: valor }),
      );
    }
  },
);

prueba("escapa el texto externo antes de incorporarlo al HTML", () => {
  assert.equal(
    escapar('<img src="x" onerror="alert(1)">'),
    "&lt;img src=&quot;x&quot; onerror=&quot;alert(1)&quot;&gt;",
  );
});

prueba(
  "envía JWT y datos JSON sin cookies, e informa conflictos del servidor",
  async () => {
    const original = globalThis.fetch;
    configurarDireccion("http://localhost:5167");
    establecerToken("credencial-ficticia");
    globalThis.fetch = async (url, opciones) => {
      assert.equal(url, "http://localhost:5167/api/v1/prueba");
      assert.equal(
        opciones.headers.Authorization,
        "Bearer credencial-ficticia",
      );
      assert.equal(opciones.credentials, "omit");
      assert.deepEqual(JSON.parse(opciones.body), { nombre: "Jardín" });
      return new Response(JSON.stringify({ detail: "La revisión cambió." }), {
        status: 409,
      });
    };
    try {
      await assert.rejects(
        solicitar("/prueba", { metodo: "POST", datos: { nombre: "Jardín" } }),
        (error) =>
          error.estado === 409 && error.message === "La revisión cambió.",
      );
    } finally {
      globalThis.fetch = original;
      establecerToken(null);
    }
  },
);

prueba("descarta respuestas de una sesión que ya cambió", async () => {
  const original = globalThis.fetch;
  establecerToken("sesion-anterior");
  globalThis.fetch = async () => {
    establecerToken(null);
    return new Response("{}");
  };
  try {
    await assert.rejects(solicitar("/prueba"), /La sesión cambió/);
  } finally {
    globalThis.fetch = original;
    establecerToken(null);
  }
});
