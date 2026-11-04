export const institucionEjemplo = {
  id: "00000000-0000-4000-8000-000000000001",
  nombre: "Jardín Los Aromos",
  codigo: "JI-042",
  telefonoContacto: "221 555 0100",
  correoElectronicoContacto: "jardin@example.com",
  domicilio: {
    calle: "Los Aromos",
    numero: "125",
    localidad: "La Plata",
    provincia: "Buenos Aires",
  },
  coordenadas: null,
  revisionDatos: 1,
  revisionUbicacion: 1,
};
export const periodosEjemplo = [
  {
    id: "00000000-0000-4000-8000-000000000002",
    institucionId: institucionEjemplo.id,
    ciclo: 2027,
    inicio: "2026-11-01",
    fin: "2026-12-15",
    estado: "RecepcionAbierta",
    revision: 3,
    versionReglasId: "ejemplo",
    versionOfertaId: "ejemplo",
  },
];
export const aulasEjemplo = [
  { id: "aula-1", nombre: "Sala del sol" },
  { id: "aula-2", nombre: "Sala de las hojas" },
  { id: "aula-3", nombre: "Sala del bosque" },
];
export const turnosEjemplo = [
  { id: "turno-1", nombre: "Mañana" },
  { id: "turno-2", nombre: "Tarde" },
];
export const ofertasEjemplo = aulasEjemplo.map((aula, i) => ({
  id: `oferta-${i}`,
  aulaId: aula.id,
  turnoId: turnosEjemplo[i % 2].id,
  capacidad: 20,
  disponible: true,
  capacidadNetaInicial: 16 + i,
  ocupaciones: [{ sala: 5 - i, cantidad: 4 - i }],
  referenciaCapacidad: "Ejemplo ilustrativo",
  origenOcupaciones: "Relevamiento ficticio",
}));
export const solicitudesEjemplo = [
  {
    id: "solicitud-1",
    nombre: "Emma",
    apellido: "Fernández",
    dni: null,
    sala: 5,
    estadoRecepcion: "Ordinaria",
  },
  {
    id: "solicitud-2",
    nombre: "Mateo",
    apellido: "González",
    dni: "00000002",
    sala: 4,
    estadoRecepcion: "Ordinaria",
  },
  {
    id: "solicitud-3",
    nombre: "Sofía",
    apellido: "López",
    dni: "00000003",
    sala: 3,
    estadoRecepcion: "Ordinaria",
  },
  {
    id: "solicitud-4",
    nombre: "Benjamín",
    apellido: "Rodríguez",
    dni: null,
    sala: 5,
    estadoRecepcion: "FueraDeTerminoPendienteAutorizacion",
  },
  {
    id: "solicitud-5",
    nombre: "Olivia",
    apellido: "Martínez",
    dni: "00000005",
    sala: 4,
    estadoRecepcion: "Ordinaria",
  },
];
// Identidades y cifras de esta vista son ficticias; nunca se envían a la API.
export const detalleEjemplo = (id) => ({
  ...(solicitudesEjemplo.find((s) => s.id === id) ?? solicitudesEjemplo[0]),
  periodoId: periodosEjemplo[0].id,
  aspiranteId: "aspirante-ejemplo",
  fechaNacimientoUtilizada: "2022-05-10",
  fechaCorteUtilizada: "2027-06-30",
  domicilio: {
    calle: "Calle de ejemplo",
    numero: "120",
    localidad: "La Plata",
    provincia: "Buenos Aires",
  },
  turnoPreferidoId: "turno-1",
  turnosAlternativosAceptados: [],
  revisionDatos: 1,
  revisionDomicilio: 1,
  fechaRegistro: "2026-11-03T12:00:00Z",
});
