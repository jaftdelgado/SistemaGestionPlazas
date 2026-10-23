// Primera letra de las dos primeras palabras del nombre; con una sola palabra, sus dos primeras letras.
export function iniciales(nombre: string): string {
  const [primera, segunda] = nombre.trim().split(/\s+/).filter(Boolean);
  if (primera === undefined) return "";
  const letras =
    segunda === undefined
      ? Array.from(primera).slice(0, 2)
      : [Array.from(primera)[0], Array.from(segunda)[0]];
  return letras.join("").toLocaleUpperCase("es");
}
