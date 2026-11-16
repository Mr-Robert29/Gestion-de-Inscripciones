import { escapar as e, campo, campoClave, fecha } from "./interfaz.js";
import {
  rutaPeriodo,
  rutaInstitucional,
  nombreTurno,
  nombreAula,
  nombreSolicitud,
  tabla,
  historial,
} from "./organizacion-comun.js";
import {
  referencia,
  selector,
  opcionesEdades,
  enviar,
  formulario,
  ver,
  exigirRegistro,
} from "./operaciones-formularios.js";

function listaAccesosInstitucionales(usuario, ctx) {
  const accesos = new Map(usuario.accesos.map((acceso) => [acceso.institucionId, acceso]));
  const instituciones = ctx.estado.instituciones;
  if (!instituciones.length)
    return '<p class="formulario__nota">No hay instituciones disponibles para mostrar.</p>';
  return `<ul class="usuarios__accesos-resumen">${instituciones.map((institucion) => {
    const acceso = accesos.get(institucion.id);
    return `<li><strong>${e(institucion.nombre)}</strong><div><span class="usuarios__acceso-estado ${acceso ? "usuarios__acceso-estado--asignado" : "usuarios__acceso-estado--sin-asignar"}">${acceso ? "Tiene acceso" : "Sin acceso"}</span>${acceso ? `<span class="usuarios__acceso-rol">Rol: ${e(acceso.rol)}</span>` : ""}</div></li>`;
  }).join("")}</ul>`;
}

export async function atenderAdministracion(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (["org-exportar-reportes", "org-exportar-nomina"].includes(accion)) {
    const tipo =
      accion === "org-exportar-reportes" ? "reportes" : "nomina-distrital";
    const archivo = await ctx.api(
      `${rutaInstitucional(ctx)}/${tipo}/exportar`,
      { descarga: true },
    );
    const url = URL.createObjectURL(
      new Blob(["\uFEFF", archivo.contenido], { type: archivo.tipo }),
    );
    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = `${tipo}-${ctx.estado.periodo.ciclo}.csv`;
    enlace.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  } else if (accion === "org-nomina") {
    const x = await ctx.api(`${rutaInstitucional(ctx)}/nomina-distrital`);
    ver(
      ctx,
      "Nómina de aspirantes sin vacante",
      `<p>${e(ctx.estado.institucion.nombre)} · ${e(ctx.estado.periodo.ciclo)} · ${e(fecha(x.fechaGeneracion))}</p>` +
        tabla(
          ["Aspirante", "Sala", "Estado", "Motivo"],
          [...x.sinVacante, ...x.pendientes].map((s) => [
            e(`${s.nombre} ${s.apellido}`),
            e(s.sala ?? "Pendiente"),
            e(s.estado),
            e(s.motivo),
          ]),
        ),
    );
  } else if (accion === "org-crear-usuario")
    formulario(
      ctx,
      "Crear cuenta",
      `<label class="campo"><span class="campo__etiqueta">Tipo de cuenta *</span><select class="campo__control" name="tipoCuenta" data-tipo-cuenta><option value="institucional" ${ctx.estado.instituciones.length ? "selected" : "disabled"}>Personal de una institución</option><option value="root" ${ctx.estado.instituciones.length ? "" : "selected"}>Administrador del sistema (Root)</option></select></label><fieldset class="alta-cuenta__institucional" data-cuenta-institucional ${ctx.estado.instituciones.length ? "" : "disabled hidden"}><legend>Acceso institucional</legend>` +
        selector("institucionId", "Institución", [
          { id: "", nombre: "Seleccioná una institución" },
          ...ctx.estado.instituciones,
        ], ctx.estado.institucion?.id ?? "") +
        selector("rol", "Rol en esta institución", [
          { id: "", nombre: "Seleccioná un rol" },
          { id: "Directivo", nombre: "Directivo" },
          { id: "Administrativo", nombre: "Administrativo" },
        ]) +
        '<p class="formulario__nota">La cuenta tendrá acceso solo a la institución elegida. La contraseña será temporal y deberá cambiarse en el primer ingreso. Si reutilizás el mismo correo y DNI de una cuenta existente, conservará su contraseña actual.</p></fieldset><p class="formulario__nota" data-cuenta-root hidden>Esta cuenta tendrá permisos globales de Administrador del sistema y no estará asociada a un jardín.</p>' +
      campo({ nombre: "dni", etiqueta: "DNI", obligatorio: true, atributos: 'inputmode="numeric" autocomplete="off" maxlength="12" placeholder="Ej.: 12.345.678"', ayuda: "Ingresá 7 u 8 números, con o sin puntos. Es obligatorio para identificar a la persona y relacionar sus cuentas entre instituciones." }) +
      campo({ nombre: "nombre", etiqueta: "Nombre", obligatorio: true, atributos: 'placeholder="Ej.: Ana Pérez"' }) +
        campo({
          nombre: "correoElectronico",
          etiqueta: "Correo",
          tipo: "email",
          obligatorio: true,
          atributos: 'autocomplete="email" placeholder="nombre@institucion.edu.ar"',
        }) +
        campoClave({
          nombre: "claveInicial",
          etiqueta: "Contraseña inicial",
          obligatorio: true,
          placeholder: "Al menos 6 caracteres y un número",
        }) +
        campoClave({
          nombre: "confirmarClave",
          etiqueta: "Confirmar contraseña",
          obligatorio: true,
          confirmacion: true,
          placeholder: "Repetí la contraseña inicial",
        }) +
        "",
      ({ confirmarClave: _confirmarClave, ...d }) =>
        d.tipoCuenta === "institucional"
          ? enviar(ctx, "/usuarios/personal-institucion", {
              nombre: d.nombre,
              correoElectronico: d.correoElectronico,
              claveInicial: d.claveInicial,
              dni: d.dni,
              institucionId: d.institucionId,
              rol: d.rol,
            })
          : enviar(ctx, "/usuarios", {
              nombre: d.nombre,
              correoElectronico: d.correoElectronico,
              claveInicial: d.claveInicial,
              rolGlobal: "AdministradorDelSistema",
              dni: d.dni,
              institucionInicialId: null,
            }),
      { permiteSinContexto: true },
    );
  else if (accion === "personal-crear")
    formulario(
      ctx,
      "Agregar personal a este jardín",
      '<p class="formulario__nota">Se creará un acceso Administrativo solo para esta institución. El DNI es obligatorio para identificar a la persona y vincular sus cuentas; no se mostrará al personal de otros jardines. La contraseña inicial deberá cambiarse en el primer ingreso.</p>' +
        campo({ nombre: "nombre", etiqueta: "Nombre y apellido", obligatorio: true, atributos: 'placeholder="Ej.: Ana Pérez"' }) +
        campo({ nombre: "dni", etiqueta: "DNI", obligatorio: true, atributos: 'inputmode="numeric" autocomplete="off" maxlength="12" placeholder="Ej.: 12.345.678"', ayuda: "Ingresá 7 u 8 números; podés incluir puntos o espacios." }) +
        campo({ nombre: "correoElectronico", etiqueta: "Correo para este jardín", tipo: "email", obligatorio: true, atributos: 'autocomplete="email" placeholder="nombre@institucion.edu.ar"' }) +
        campoClave({ nombre: "claveInicial", etiqueta: "Contraseña temporal", obligatorio: true, placeholder: "Al menos 6 caracteres y un número" }) +
        campoClave({ nombre: "confirmarClave", etiqueta: "Confirmar contraseña temporal", obligatorio: true, confirmacion: true, placeholder: "Repetí la contraseña temporal" }),
      ({ confirmarClave: _confirmarClave, ...d }) =>
        enviar(ctx, `/instituciones/${encodeURIComponent(ctx.estado.institucion.id)}/personal`, {
          ...d,
          dni: d.dni,
        }),
    );
  else if (accion === "personal-revocar") {
    const cuenta = exigirRegistro(datos.personal, id);
    formulario(
      ctx,
      "Revocar acceso al jardín",
      `<div class="aviso"><strong>${e(ctx.estado.institucion.nombre)}</strong><p>Se quitará el acceso de <strong>${e(cuenta.nombre)}</strong> a este jardín. Su cuenta seguirá activa y conservará los demás accesos que tenga.</p></div><label class="casilla"><input type="checkbox" required>Confirmo que quiero revocar este acceso.</label>`,
      () => enviar(ctx, `/instituciones/${encodeURIComponent(ctx.estado.institucion.id)}/personal/${encodeURIComponent(id)}`, undefined, "DELETE"),
      { textoConfirmar: "Revocar acceso" },
    );
  }
  else if (
    [
      "org-administrar-usuario",
      "org-estado-usuario",
      "org-acceso-usuario",
      "org-revocar-usuario",
      "org-vincular-dni",
    ].includes(accion)
  ) {
    const x = exigirRegistro(datos.usuarios, id);
    if (accion === "org-administrar-usuario") {
      const esRoot = (x.rolesGlobales ?? []).includes("AdministradorDelSistema");
      const esLaCuentaActual = x.id === ctx.estado.usuario.usuarioId;
      const rootsActivas = datos.usuarios.filter((usuario) =>
        usuario.activo && (usuario.rolesGlobales ?? []).includes("AdministradorDelSistema"),
      ).length;
      const puedeDeshabilitar = !x.activo || !esRoot || (!esLaCuentaActual && rootsActivas > 1);
      const estadoAccion = puedeDeshabilitar
        ? `<button class="boton boton--secundario boton--pequeno" data-accion="org-estado-usuario" data-id="${e(x.id)}">${x.activo ? "Deshabilitar" : "Habilitar"} cuenta</button>`
        : `<span class="tabla__detalle">${esLaCuentaActual ? "No podés deshabilitar tu cuenta actual." : "No se puede deshabilitar la última cuenta Root activa."}</span>`;
      const descripcionEstado = x.activo
        ? esRoot
          ? "Bloquea el inicio de sesión global de esta cuenta Root."
          : "Revoca sus accesos institucionales y deshabilita esta cuenta."
        : "Permite que vuelva a iniciar sesión. Los accesos revocados no se recuperan automáticamente.";
      const opcion = (titulo, descripcion, control) => `<section class="usuarios__admin-opcion"><div><h3>${e(titulo)}</h3><p>${e(descripcion)}</p></div>${control}</section>`;
      const opciones = [opcion(x.activo ? "Deshabilitar cuenta" : "Habilitar cuenta", descripcionEstado, estadoAccion)];
      if (!esRoot) {
        opciones.push(opcion(
          "Asignar acceso",
          "Elegí una institución y el rol que tendrá esta cuenta en ella.",
          `<button class="boton boton--secundario boton--pequeno" data-accion="org-acceso-usuario" data-id="${e(x.id)}">Asignar acceso</button>`,
        ));
        if (x.accesos.length)
          opciones.push(opcion(
            "Revocar acceso",
            "Quitá el acceso a una institución sin deshabilitar la cuenta ni afectar los demás jardines.",
            `<button class="boton boton--secundario boton--pequeno" data-accion="org-revocar-usuario" data-id="${e(x.id)}">Revocar acceso</button>`,
          ));
      }
      if (!x.dniEnmascarado)
        opciones.push(opcion(
          "Vincular DNI",
          "Vinculá esta cuenta con la identidad de la persona para reconocer sus cuentas entre instituciones.",
          `<button class="boton boton--secundario boton--pequeno" data-accion="org-vincular-dni" data-id="${e(x.id)}">Vincular DNI</button>`,
        ));
      ctx.abrirDialogo({
        titulo: "Administrar cuenta",
        descripcion: `${x.nombre} · ${x.correoElectronico}`,
        contenido: `<div class="usuarios__admin-acciones">${opciones.join("")}</div><div class="formulario__acciones"><button class="boton" data-accion="cerrar-dialogo">Cerrar</button></div>`,
      });
    }
    else if (accion === "org-vincular-dni")
      formulario(
        ctx,
        "Vincular la cuenta con una identidad",
        `<p>${e(x.nombre)} · ${e(x.correoElectronico)}</p><p class="formulario__nota">Si ya existe una identidad con este DNI, sus cuentas locales quedarán agrupadas para la vista de Root. Sus correos y contraseñas se conservan.</p>${campo({ nombre: "dni", etiqueta: "DNI", obligatorio: true })}`,
        (d) => enviar(ctx, `/usuarios/${encodeURIComponent(id)}/vincular-dni`, { dni: d.dni }),
        { permiteSinContexto: true },
      );
    else if (accion === "org-estado-usuario") {
      const esRoot = (x.rolesGlobales ?? []).includes("AdministradorDelSistema");
      const accesos = x.accesos.map((acceso) => ({
        institucion: ctx.estado.instituciones.find((i) => i.id === acceso.institucionId)?.nombre ?? "Institución sin nombre",
        rol: acceso.rol,
      }));
      const listaAccesos = accesos.length
        ? `<ul class="usuarios__confirmacion-accesos">${accesos.map((acceso) => `<li><strong>${e(acceso.institucion)}</strong><span>Rol: ${e(acceso.rol)}</span></li>`).join("")}</ul>`
        : '<p class="formulario__nota">Esta cuenta no tiene accesos institucionales asignados.</p>';
      const contenido = x.activo
        ? `<p>Vas a deshabilitar la cuenta de <strong>${e(x.nombre)}</strong> (${e(x.correoElectronico)}).</p>${esRoot ? '<div class="aviso"><p>Esta es una cuenta Root. Se bloqueará su inicio de sesión global; no tiene accesos institucionales asociados.</p></div>' : `<div class="aviso"><p>Antes de deshabilitarla, se revocarán todos los accesos institucionales de esta cuenta:</p>${listaAccesos}</div>`}<p class="formulario__nota">${esRoot ? "No se modifican otras cuentas vinculadas a la misma identidad." : "Si esta persona tiene otras cuentas vinculadas por DNI, esta operación solo afecta la cuenta seleccionada."}</p><label class="casilla"><input type="checkbox" required>Confirmo ${esRoot ? "deshabilitar esta cuenta Root" : "revocar estos accesos y deshabilitar la cuenta"}.</label>`
        : `<p>Vas a habilitar la cuenta de <strong>${e(x.nombre)}</strong> (${e(x.correoElectronico)}).</p><div class="aviso"><p>Habilitar permite que la cuenta vuelva a iniciar sesión. Los accesos institucionales revocados durante la baja no se recuperan automáticamente.</p></div><label class="casilla"><input type="checkbox" required>Confirmo que quiero habilitar esta cuenta.</label>`;
      formulario(
        ctx,
        x.activo ? "Deshabilitar cuenta" : "Habilitar cuenta",
        contenido,
        () => enviar(ctx, `/usuarios/${id}/estado`, { activo: !x.activo }, "PATCH"),
        {
          permiteSinContexto: true,
          textoConfirmar: x.activo
            ? accesos.length ? "Revocar accesos y deshabilitar" : "Deshabilitar cuenta"
            : "Habilitar cuenta",
        },
      );
    }
    else if (accion === "org-acceso-usuario")
      formulario(
        ctx,
        "Asignar acceso institucional",
        `<section class="usuarios__accesos-panel"><h3>Accesos actuales de ${e(x.nombre)}</h3>${listaAccesosInstitucionales(x, ctx)}</section><p class="formulario__nota">Elegí una institución y asigná un rol. Si ya tiene acceso, se actualizará el rol existente.</p>` +
          selector("institucionId", "Institución", ctx.estado.instituciones) +
          selector(
            "rol",
            "Rol en la institución",
            ["Administrativo", "Directivo"].map((x) => ({ id: x, nombre: x })),
          ),
        (d) => enviar(ctx, `/usuarios/${id}/accesos`, d),
        { permiteSinContexto: true },
      );
    else
      formulario(
        ctx,
        "Revocar acceso institucional",
        `<section class="usuarios__accesos-panel"><h3>Accesos actuales de ${e(x.nombre)}</h3>${listaAccesosInstitucionales(x, ctx)}</section><p class="formulario__nota">Al confirmar, se quitará únicamente el acceso de la institución seleccionada.</p>` +
          selector(
          "institucionId",
          "Acceso a revocar",
          x.accesos.map((a) => ({
            id: a.institucionId,
            nombre: `${ctx.estado.instituciones.find((i) => i.id === a.institucionId)?.nombre ?? a.institucionId} · ${a.rol}`,
          })),
        ),
        (d) =>
          enviar(
            ctx,
            `/usuarios/${id}/accesos/${d.institucionId}`,
            undefined,
            "DELETE",
          ),
        { permiteSinContexto: true },
      );
  }
  return true;
}
