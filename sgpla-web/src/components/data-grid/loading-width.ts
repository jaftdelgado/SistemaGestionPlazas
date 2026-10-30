const MIN_WIDTH = 45;
const MAX_WIDTH = 90;

// Hash FNV-1a: determinista, sin estado y suficiente para repartir anchos.
function hash(text: string) {
  let value = 0x811c9dc5;
  for (let i = 0; i < text.length; i++) {
    value ^= text.charCodeAt(i);
    value = Math.imul(value, 0x01000193) >>> 0;
  }
  return value;
}

// Ancho porcentual del esqueleto de una celda: igual en cada carga, distinto por columna y fila, y según la semilla del grid.
export function skeletonWidth(
  seed: string,
  columnId: string,
  rowIndex: number,
) {
  const range = MAX_WIDTH - MIN_WIDTH + 1;
  return MIN_WIDTH + (hash(`${seed}:${columnId}:${rowIndex}`) % range);
}
