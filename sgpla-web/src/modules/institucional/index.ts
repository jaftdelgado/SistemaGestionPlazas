// Contrato del módulo: lo único que las rutas y los demás módulos pueden importar.
export type { AreaAcademica } from "./domain/area-academica";
export {
  abbreviateFacultad,
  type EntidadAcademica,
} from "./domain/entidad-academica";
export { areasAcademicasQuery } from "./api/areas-academicas";
export { entidadesAcademicasQuery } from "./api/entidades-academicas";
