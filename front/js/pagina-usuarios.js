import { escapar as e, etiqueta } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  tabla,
  boton,
  resumen,
  vistaPreviaOrganizacion,
} from "./organizacion-comun.js";
export async function usuarios(ctx) {
  const cabecera = encabezado({
    titulo: "Usuarios y permisos",
    descripcion:
      "Administrá cuentas y accesos por institución. Solo disponible para Root.",
  });
  if (ctx.estado.previa) return cabecera + vistaPreviaOrganizacion();
  if (!ctx.estado.usuario.roles.includes("AdministradorDelSistema"))
    throw new Error("Esta pantalla requiere Administrador del sistema.");
  const usuarios = await ctx.api("/usuarios");
  ctx.organizacion = { usuarios };
  return (
    cabecera +
    resumen(
      "Cuentas del sistema",
      tabla(
        ["Nombre", "Correo", "Estado", "Accesos", "Acciones"],
        usuarios.map((x) => [
          e(x.nombre),
          e(x.correoElectronico),
          etiqueta(x.activo ? "Activo" : "Inactivo"),
          x.accesos
            .map((a) =>
              e(
                `${ctx.estado.instituciones.find((i) => i.id === a.institucionId)?.nombre ?? a.institucionId}: ${a.rol}`,
              ),
            )
            .join("<br>"),
          boton(
            "org-estado-usuario",
            x.activo ? "Deshabilitar" : "Habilitar",
            x.id,
          ) +
            boton("org-acceso-usuario", "Asignar acceso", x.id) +
            (x.accesos.length
              ? boton("org-revocar-usuario", "Revocar acceso", x.id)
              : ""),
        ]),
      ),
      boton("org-crear-usuario", "Crear usuario"),
    )
  );
}
