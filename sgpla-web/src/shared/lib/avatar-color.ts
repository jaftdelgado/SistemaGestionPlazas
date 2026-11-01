// Clases completas y literales: Tailwind no detecta nombres armados por concatenación.
// Fondos 600 o 700 para que el texto blanco conserve contraste AA.
const COLORS = [
  "bg-blue-600",
  "bg-emerald-700",
  "bg-violet-600",
  "bg-rose-600",
  "bg-orange-700",
  "bg-cyan-700",
  "bg-lime-700",
  "bg-slate-600",
] as const;

// FNV-1a de 32 bits: estable entre sesiones y reparte bien cadenas cortas.
function hash(text: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < text.length; i++) {
    h ^= text.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return h >>> 0;
}

// El color depende solo de la parte local del correo (antes del @), sin distinguir mayúsculas.
export function getAvatarColor(email: string): string {
  const localPart = (email.split("@")[0] ?? "").trim().toLowerCase();
  return COLORS[hash(localPart) % COLORS.length] ?? COLORS[0];
}
