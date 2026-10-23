// Primera letra de las dos primeras palabras del nombre; con una sola palabra, sus dos primeras letras.
export function iniciales(nombre: string): string {
  const palabras = nombre.trim().split(/\s+/).filter(Boolean);
  if (palabras.length === 0) return "";
  const letras =
    palabras.length === 1
      ? Array.from(palabras[0]).slice(0, 2)
      : [Array.from(palabras[0])[0], Array.from(palabras[1])[0]];
  return letras.join("").toLocaleUpperCase("es");
}
