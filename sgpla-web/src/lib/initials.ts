// Primera letra de las dos primeras palabras del nombre; con una sola palabra, sus dos primeras letras.
export function getInitials(name: string): string {
  const [first, second] = name.trim().split(/\s+/).filter(Boolean);
  if (first === undefined) return "";
  const letters =
    second === undefined
      ? Array.from(first).slice(0, 2)
      : [Array.from(first)[0], Array.from(second)[0]];
  return letters.join("").toLocaleUpperCase("es");
}
