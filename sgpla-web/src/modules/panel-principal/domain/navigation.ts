// Secciones del panel; los elementos sin `to` esperan a que exista su módulo.
export const NAVIGATION_ITEMS = [
  { id: "inicio", title: "Inicio", to: "/" },
  { id: "ofertas", title: "Ofertas educativas" },
  { id: "usuarios", title: "Usuarios", to: "/usuarios" },
  { id: "configuracion", title: "Configuración" },
] as const;

export type NavigationItemId = (typeof NAVIGATION_ITEMS)[number]["id"];

export function getActiveItemId(pathname: string): NavigationItemId {
  return (
    NAVIGATION_ITEMS.find(
      (item) =>
        "to" in item &&
        (item.to === "/" ? pathname === "/" : pathname.startsWith(item.to)),
    )?.id ?? "inicio"
  );
}

// El inicio se titula como el panel; las demás secciones, con su nombre.
export function getPageTitle(id: NavigationItemId): string {
  if (id === "inicio") return "Panel principal";
  return NAVIGATION_ITEMS.find((item) => item.id === id)?.title ?? "";
}
