import { useEffect, useState } from "react";

/**
 * Devuelve true solo en dispositivos con hover real (ratón o trackpad).
 * Los dispositivos táctiles disparan un `:hover` fantasma al tocar que persiste hasta tocar en otro
 * lugar: los efectos exclusivos de hover se condicionan a este valor.
 */
export function useHoverCapable() {
  const [canHover, setCanHover] = useState(false);

  useEffect(() => {
    if (typeof window === "undefined" || !window.matchMedia) return;
    const mq = window.matchMedia("(hover: hover) and (pointer: fine)");
    const update = () => setCanHover(mq.matches);
    update();
    mq.addEventListener?.("change", update);
    return () => mq.removeEventListener?.("change", update);
  }, []);

  return canHover;
}
