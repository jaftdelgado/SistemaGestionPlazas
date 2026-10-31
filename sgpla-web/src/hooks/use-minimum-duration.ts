import * as React from "react";

// Mantiene en true un estado de carga al menos `minMs` desde que empieza,
// para evitar parpadeos cuando la respuesta llega casi de inmediato.
export function useMinimumDuration(active: boolean, minMs: number) {
  const [prevActive, setPrevActive] = React.useState(active);
  const [minPassed, setMinPassed] = React.useState(!active);

  if (active !== prevActive) {
    setPrevActive(active);
    if (active) setMinPassed(false);
  }

  React.useEffect(() => {
    // Depende de minPassed y no de active: si la respuesta llega antes, el temporizador sigue corriendo.
    if (minPassed) return;
    const timeout = setTimeout(() => setMinPassed(true), minMs);
    return () => clearTimeout(timeout);
  }, [minPassed, minMs]);

  return active || !minPassed;
}
