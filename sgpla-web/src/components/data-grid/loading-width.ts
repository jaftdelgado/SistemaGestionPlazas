const ANCHO_MINIMO = 45;
const ANCHO_MAXIMO = 90;

// Hash FNV-1a: determinista, sin estado y suficiente para repartir anchos.
function hash(texto: string) {
  let valor = 0x811c9dc5;
  for (let i = 0; i < texto.length; i++) {
    valor ^= texto.charCodeAt(i);
    valor = Math.imul(valor, 0x01000193) >>> 0;
  }
  return valor;
}

// Ancho porcentual del esqueleto de una celda: igual en cada carga, distinto por columna y fila, y según la semilla del grid.
export function anchoDeEsqueleto(
  semilla: string,
  columnId: string,
  fila: number,
) {
  const rango = ANCHO_MAXIMO - ANCHO_MINIMO + 1;
  return ANCHO_MINIMO + (hash(`${semilla}:${columnId}:${fila}`) % rango);
}
