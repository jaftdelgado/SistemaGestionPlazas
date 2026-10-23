export function validarBusquedaConRedirect(search: Record<string, unknown>): {
  redirect?: string;
} {
  return {
    redirect: typeof search.redirect === "string" ? search.redirect : undefined,
  };
}
