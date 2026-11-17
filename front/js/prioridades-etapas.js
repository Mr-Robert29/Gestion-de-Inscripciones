export const cumpleCriterio = (resultado, clave) =>
  resultado.explicacion?.some((texto) =>
    texto === `Criterio verificado: ${clave}.` ||
    (clave === "Cercania" && texto.startsWith("Cercanía:")),
  ) ?? false;

export const cohortePrioridad = (resultado) =>
  cumpleCriterio(resultado, "ArticulacionPedagogica")
    ? "ArticulacionPedagogica"
    : cumpleCriterio(resultado, "HermanosYGruposConvivientes")
      ? "HermanosYGruposConvivientes"
      : "Cercania";

export function candidatosDeEtapa(resultados, clave) {
  if (clave === "FueraDeTermino")
    return resultados.filter((x) => x.explicacion?.includes("Solicitud fuera de término."));
  const ordinarios = resultados.filter((x) => !x.explicacion?.includes("Solicitud fuera de término."));
  const sinArticulacion = ordinarios.filter((x) => !cumpleCriterio(x, "ArticulacionPedagogica"));
  const sinArticulacionNiHermano = sinArticulacion.filter(
    (x) => !cumpleCriterio(x, "HermanosYGruposConvivientes"),
  );
  if (clave === "ArticulacionPedagogica") return ordinarios;
  if (clave === "HermanosYGruposConvivientes") return sinArticulacion;
  if (clave === "Cercania")
    return sinArticulacionNiHermano.sort((a, b) => (a.posicion ?? Number.MAX_SAFE_INTEGER) - (b.posicion ?? Number.MAX_SAFE_INTEGER));
  if (clave === "HijosDelPersonal")
    return sinArticulacionNiHermano.filter((x) => cumpleCriterio(x, "HijosDelPersonal"));
  if (clave === "SorteoUltimaInstancia") return ordinarios.filter((x) => x.requiereRevision);
  return [];
}

export const prioridadVerificadaEnEtapa = (resultado, clave) =>
  clave === "Cercania"
    ? resultado.explicacion?.some((texto) => texto.startsWith("Cercanía:")) ?? false
    : cumpleCriterio(resultado, clave);

export function cohortesIncluidasHasta(clave) {
  if (clave === "ArticulacionPedagogica") return ["ArticulacionPedagogica"];
  if (clave === "HermanosYGruposConvivientes")
    return ["ArticulacionPedagogica", "HermanosYGruposConvivientes"];
  // La cercanía ordena a quienes no entraron por las dos prioridades anteriores.
  // Hijos del personal actúa como criterio posterior dentro de ese grupo, no como otra tanda de cupos.
  return ["ArticulacionPedagogica", "HermanosYGruposConvivientes", "Cercania"];
}
