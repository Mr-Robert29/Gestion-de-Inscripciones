import { escapar as e, etiqueta } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  tabla,
  boton,
  resumen,
  filtroCuentas,
  vistaPreviaOrganizacion,
} from "./organizacion-comun.js";

export async function usuarios(ctx) {
  const cabecera = encabezado({
    titulo: "Usuarios y permisos",
    descripcion: "Administrá las cuentas globales y los accesos de cada institución. Solo disponible para Root.",
  });
  if (ctx.estado.previa) return cabecera + vistaPreviaOrganizacion();
  if (!ctx.estado.usuario.roles.includes("AdministradorDelSistema"))
    throw new Error("Esta pantalla requiere Administrador del sistema.");

  const [usuarios, auditoria] = await Promise.all([
    ctx.api("/usuarios"),
    ctx.api("/usuarios/auditoria"),
  ]);
  const activas = usuarios.filter((x) => x.activo).length;
  const inactivas = usuarios.length - activas;
  ctx.organizacion = { usuarios };

  const filasCuentas = usuarios.map((x) => {
    return [
      `<div class="usuarios__cuenta"><strong>${e(x.nombre)}</strong><span>${e(x.correoElectronico)}</span></div>`,
      etiqueta(x.activo ? "Activo" : "Inactivo"),
      boton("ver-datos-usuario", "Ver datos", x.id),
      boton("org-administrar-usuario", "Administrar", x.id),
    ];
  });

  const filasAuditoria = auditoria.map((evento) => {
    const fecha = new Date(evento.fechaUtc).toLocaleString("es-AR");
    const actor = usuarios.find((x) => x.id === evento.actorUsuarioId)?.nombre ?? evento.actorUsuarioId.slice(0, 8);
    const afectado = usuarios.find((x) => x.id === evento.usuarioAfectadoId)?.nombre ?? evento.usuarioAfectadoId?.slice(0, 8) ?? "—";
    const institucion = ctx.estado.instituciones.find((x) => x.id === evento.institucionId)?.nombre ?? (evento.institucionId ? evento.institucionId.slice(0, 8) : "Global");
    return [e(fecha), e(evento.accion), e(actor), e(afectado), e(institucion), e(evento.detalle)];
  });

  return cabecera +
    `<nav class="pestanas usuarios__pestanas" role="tablist" aria-label="Secciones de usuarios"><button type="button" role="tab" aria-controls="panel-usuarios-cuentas" class="pestanas__enlace pestanas__enlace--activo" data-accion="usuarios-tab" data-panel="cuentas" aria-selected="true">Cuentas</button><button type="button" role="tab" aria-controls="panel-usuarios-historial" class="pestanas__enlace" data-accion="usuarios-tab" data-panel="historial" aria-selected="false">Historial de accesos</button></nav>` +
    `<section id="panel-usuarios-cuentas" role="tabpanel" data-usuarios-panel="cuentas">${filtroCuentas(activas, inactivas, "Nombre, correo, últimos 4 del DNI o institución")}${resumen(
      "Cuentas del sistema",
      tabla(
        ["Nombre y correo de la cuenta", "Estado", "Datos", "Acciones"],
        filasCuentas,
        { estadoFila: (_, indice) => usuarios[indice].activo ? "activo" : "inactivo", estadoInicial: "activo" },
      ),
      boton("org-crear-usuario", "Crear cuenta"),
    )}</section>` +
    `<section id="panel-usuarios-historial" role="tabpanel" data-usuarios-panel="historial" hidden>${resumen(
      "Historial de accesos",
      tabla(
        ["Fecha", "Acción", "Realizada por", "Cuenta afectada", "Institución", "Detalle"],
        filasAuditoria,
      ),
      "",
    )}</section>`;
}
