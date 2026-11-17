import { escapar as e, etiqueta, vacio, icono } from "./interfaz.js";
import { encabezado, base } from "./compartidos.js";
import { boton, tabla, resumen, filtroCuentas } from "./organizacion-comun.js";

export async function personalInstitucion(ctx) {
  const esRoot = ctx.estado.usuario.roles.includes("AdministradorDelSistema");
  const rolLocal = ctx.estado.usuario.accesos.find(
    (acceso) => acceso.institucionId === ctx.estado.institucion.id,
  )?.rol;
  if (!esRoot && rolLocal !== "Directivo")
    throw new Error("Esta pantalla requiere el rol Directivo en la institución seleccionada.");

  const ruta = `${base(ctx.estado)}/personal`;
  const personal = await ctx.api(ruta);
  const activas = personal.filter((cuenta) => cuenta.activo).length;
  const inactivas = personal.length - activas;
  ctx.organizacion = { personal };
  const filas = personal.map((cuenta) => [
    e(cuenta.nombre),
    e(cuenta.correoElectronico),
    etiqueta(cuenta.rol),
    etiqueta(cuenta.activo ? "Activo" : "Inactivo"),
    cuenta.rol === "Administrativo"
      ? boton("personal-revocar", "Revocar acceso", cuenta.usuarioId)
      : '<span class="tabla__detalle">Gestionado por Root</span>',
  ]);
  return encabezado({
    titulo: "Personal del jardín",
    descripcion: `Administrá las cuentas que tienen acceso a ${ctx.estado.institucion.nombre}. Esta lista solo muestra el personal de esta institución.`,
    accion: `<button class="boton" data-accion="personal-crear">${icono("mas")}Agregar personal</button>`,
  }) + '<div class="aviso"><strong>Accesos del jardín:</strong> podés revocar el acceso Administrativo a esta institución. La cuenta seguirá activa y sus accesos a otros jardines no se modifican. El DNI se usa para vincular identidades y no se muestra en esta pantalla.</div>' + (personal.length ? filtroCuentas(activas, inactivas) : "") +
    resumen("Personal con acceso a esta institución", personal.length
      ? tabla(["Nombre", "Correo de acceso", "Rol", "Estado", "Acción"], filas, {
          estadoFila: (_, indice) => personal[indice].activo ? "activo" : "inactivo",
          estadoInicial: "activo",
        })
      : vacio({ titulo: "Todavía no hay personal asignado", descripcion: "Agregá una cuenta Administrativa para que pueda trabajar en esta institución.", dibujo: "personas" }),
    `<span class="etiqueta etiqueta--neutra">${activas} activas · ${inactivas} inactivas</span>`);
}
