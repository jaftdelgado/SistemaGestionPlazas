import type { components } from "@/lib/api/schema";

type CuentaListada = components["schemas"]["CuentaResponse"];

export type UsuarioFila = {
  id: string;
  nombre: string;
  correo: string;
  rol: string;
  // Entidad o área de la cuenta; el Superusuario no tiene ámbito.
  ambito: string;
  tipoAmbito: "Entidad" | "Área" | null;
};

// "Facultad de Estadística e Informática" se muestra como "Fac. de Estadística e Informática".
export function abreviarFacultad(nombre: string) {
  return nombre.replace(/\bFacultad\b/g, "Fac.");
}

// Las cuentas DGAA traen área y las de Entidad Académica, entidad; el Superusuario, ninguna.
function ambitoDeCuenta(
  cuenta: CuentaListada,
): Pick<UsuarioFila, "ambito" | "tipoAmbito"> {
  if (cuenta.entidadAcademica) {
    return {
      ambito: abreviarFacultad(cuenta.entidadAcademica.nombre),
      tipoAmbito: "Entidad",
    };
  }
  if (cuenta.areaAcademica) {
    return { ambito: cuenta.areaAcademica.nombre, tipoAmbito: "Área" };
  }
  return { ambito: "", tipoAmbito: null };
}

export function aFilaUsuario(cuenta: CuentaListada): UsuarioFila {
  return {
    id: String(cuenta.id),
    nombre: cuenta.nombre,
    correo: cuenta.correo,
    rol: cuenta.rol.nombre,
    ...ambitoDeCuenta(cuenta),
  };
}
