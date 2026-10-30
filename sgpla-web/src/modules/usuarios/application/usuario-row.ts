import type { components } from "@/lib/api/schema";

type ListedCuenta = components["schemas"]["CuentaResponse"];

export type UsuarioRow = {
  id: string;
  name: string;
  email: string;
  rol: string;
  // Entidad o área de la cuenta; el Superusuario no tiene ámbito.
  ambito: string;
  ambitoType: "Entidad" | "Área" | null;
};

// "Facultad de Estadística e Informática" se muestra como "Fac. de Estadística e Informática".
export function abbreviateFacultad(name: string) {
  return name.replace(/\bFacultad\b/g, "Fac.");
}

// Las cuentas DGAA traen área y las de Entidad Académica, entidad; el Superusuario, ninguna.
function getCuentaAmbito(
  cuenta: ListedCuenta,
): Pick<UsuarioRow, "ambito" | "ambitoType"> {
  if (cuenta.entidadAcademica) {
    return {
      ambito: abbreviateFacultad(cuenta.entidadAcademica.nombre),
      ambitoType: "Entidad",
    };
  }
  if (cuenta.areaAcademica) {
    return { ambito: cuenta.areaAcademica.nombre, ambitoType: "Área" };
  }
  return { ambito: "", ambitoType: null };
}

export function toUsuarioRow(cuenta: ListedCuenta): UsuarioRow {
  return {
    id: String(cuenta.id),
    name: cuenta.nombre,
    email: cuenta.correo,
    rol: cuenta.rol.nombre,
    ...getCuentaAmbito(cuenta),
  };
}
