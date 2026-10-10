import { clsx, type ClassValue } from "clsx";
import { extendTailwindMerge } from "tailwind-merge";

// Registra la utilidad shadow-border para que se resuelva contra otras sombras (p. ej. shadow-none).
const twMerge = extendTailwindMerge({
  extend: { theme: { shadow: ["border"] } },
});

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
