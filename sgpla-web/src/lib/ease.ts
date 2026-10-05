// Tokens de movimiento compartidos: curvas de easing y resortes de los componentes animados.
// Se usan curvas marcadas; las predeterminadas `ease-in` y `ease-out` se sienten débiles.

export const EASE_OUT = [0.16, 1, 0.3, 1] as const;
export const EASE_IN_OUT = [0.77, 0, 0.175, 1] as const;
export const EASE_DRAWER = [0.32, 0.72, 0, 1] as const;

/** Forma CSS de EASE_OUT para transiciones en línea. */
export const EASE_OUT_CSS = "cubic-bezier(0.16, 1, 0.3, 1)";

/** Respuesta al presionar botones y otras superficies táctiles. */
export const SPRING_PRESS = {
  type: "spring",
  stiffness: 500,
  damping: 30,
  mass: 0.6,
} as const;

/** Intercambio de contenido: etiquetas e íconos que cambian de lugar dentro de un control. */
export const SPRING_SWAP = {
  type: "spring",
  stiffness: 460,
  damping: 30,
  mass: 0.55,
} as const;

/** Entrada de paneles superpuestos (modales y hojas) invocados con el puntero. */
export const SPRING_PANEL = {
  type: "spring",
  stiffness: 420,
  damping: 40,
  mass: 0.5,
} as const;

/** Deslizamientos de layout compartido: píldoras, indicadores y paneles que cambian de posición. */
export const SPRING_LAYOUT = {
  type: "spring",
  stiffness: 360,
  damping: 32,
  mass: 0.6,
} as const;

/** Física de seguimiento del cursor para efectos decorativos (magnético, inclinación, dock). */
export const SPRING_MOUSE = {
  stiffness: 200,
  damping: 15,
  mass: 0.3,
} as const;

/** Controles arrastrados y rellenos (sliders): configuración de `useSpring` críticamente amortiguada,
 * para que el valor siga al puntero con suavidad y no rebote en los extremos. */
export const SPRING_GLIDE = {
  stiffness: 700,
  damping: 50,
  mass: 0.5,
} as const;
