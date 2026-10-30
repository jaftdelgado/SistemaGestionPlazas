// Solo se aceptan rutas internas: una dirección absoluta o que empieza con // o /\ llevaría a otro sitio.
export function safeDestination(redirect: string | undefined): string {
  return redirect?.startsWith("/") &&
    !redirect.startsWith("//") &&
    !redirect.startsWith("/\\")
    ? redirect
    : "/";
}
