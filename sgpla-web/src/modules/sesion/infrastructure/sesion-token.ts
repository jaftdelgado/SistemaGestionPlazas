const CLAVE = "sgpla.sesion";

export type SesionGuardada = { token: string; expiraEn: string };

export function guardarSesion(sesion: SesionGuardada): void {
  try {
    localStorage.setItem(CLAVE, JSON.stringify(sesion));
  } catch {
    // Sin almacenamiento disponible (modo privado o cuota llena) la sesión no se conserva: el usuario deberá iniciar sesión de nuevo.
  }
}

export function leerToken(): string | null {
  try {
    const crudo = localStorage.getItem(CLAVE);
    if (crudo === null) {
      return null;
    }
    const sesion: unknown = JSON.parse(crudo);
    if (
      typeof sesion === "object" &&
      sesion !== null &&
      "token" in sesion &&
      "expiraEn" in sesion &&
      typeof sesion.token === "string" &&
      typeof sesion.expiraEn === "string" &&
      Date.parse(sesion.expiraEn) > Date.now()
    ) {
      return sesion.token;
    }
    borrarSesion();
    return null;
  } catch {
    // El almacenamiento no está disponible o el JSON es inválido: se trata como sin sesión.
    borrarSesion();
    return null;
  }
}

export function borrarSesion(): void {
  try {
    localStorage.removeItem(CLAVE);
  } catch {
    // Sin almacenamiento disponible no hay nada que borrar.
  }
}
