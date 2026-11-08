export type Cuenta = {
  id: number;
  nombre: string;
  correo: string;
  rol: { id: number; nombre: string };
  areaAcademica: { id: number; nombre: string } | null;
  entidadAcademica: { id: number; nombre: string } | null;
};

export type CuentasFilters = {
  rolId?: number;
  areaAcademicaId?: number;
  entidadAcademicaId?: number;
};

export type CuentasCriteria = CuentasFilters & {
  pagina: number;
  tamanoPagina: number;
  busqueda?: string;
};

export type Ambito =
  { type: "Entidad"; nombre: string } | { type: "Área"; nombre: string };

// Las cuentas DGAA traen área y las de Entidad Académica, entidad; el Superusuario, ninguna.
export function getAmbito(cuenta: Cuenta): Ambito | null {
  if (cuenta.entidadAcademica) {
    return { type: "Entidad", nombre: cuenta.entidadAcademica.nombre };
  }
  if (cuenta.areaAcademica) {
    return { type: "Área", nombre: cuenta.areaAcademica.nombre };
  }
  return null;
}

// Cambiar el área invalida la entidad elegida, que podría ser de otra área.
export function applyFilters(
  current: CuentasFilters,
  next: CuentasFilters,
): CuentasFilters {
  const areaChanged =
    next.areaAcademicaId !== undefined &&
    next.areaAcademicaId !== current.areaAcademicaId;
  const entidadUnchanged =
    next.entidadAcademicaId === current.entidadAcademicaId;
  return areaChanged && entidadUnchanged
    ? { ...next, entidadAcademicaId: undefined }
    : next;
}
