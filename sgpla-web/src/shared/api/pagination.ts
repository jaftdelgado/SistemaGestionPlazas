export type Pagina<T> = {
  elementos: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
};

type PaginaResponse<Dto> = {
  elementos: Dto[];
  pagina: number | string;
  tamanoPagina: number | string;
  total: number | string;
};

// El contrato publica los enteros como number | string; aquí se convierten una sola vez.
export function toPagina<Dto, T>(
  page: PaginaResponse<Dto>,
  map: (dto: Dto) => T,
): Pagina<T> {
  return {
    elementos: page.elementos.map(map),
    pagina: Number(page.pagina),
    tamanoPagina: Number(page.tamanoPagina),
    total: Number(page.total),
  };
}
