import { useSyncExternalStore } from "react";

/** Suscribe el componente a una media query. Sin `matchMedia` (pruebas, SSR) devuelve false. */
export function useMediaQuery(query: string) {
  return useSyncExternalStore(
    (notify) => {
      if (typeof window === "undefined" || !window.matchMedia) return () => {};
      const mq = window.matchMedia(query);
      mq.addEventListener("change", notify);
      return () => mq.removeEventListener("change", notify);
    },
    () =>
      typeof window !== "undefined" && !!window.matchMedia
        ? window.matchMedia(query).matches
        : false,
    () => false,
  );
}
