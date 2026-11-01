const STORAGE_KEY = "sgpla.sesion";

export type SavedSesion = { token: string; expiraEn: string };

export function saveSesion(sesion: SavedSesion): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion));
  } catch {
    // Sin almacenamiento disponible (modo privado o cuota llena) la sesión no se conserva: el usuario deberá iniciar sesión de nuevo.
  }
}

export function readToken(): string | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === null) {
      return null;
    }
    const sesion: unknown = JSON.parse(raw);
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
    clearSesion();
    return null;
  } catch {
    // El almacenamiento no está disponible o el JSON es inválido: se trata como sin sesión.
    clearSesion();
    return null;
  }
}

export function clearSesion(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Sin almacenamiento disponible no hay nada que borrar.
  }
}
