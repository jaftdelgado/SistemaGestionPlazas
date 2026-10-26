import * as React from "react";

// Devuelve el valor tras `retardoMs` sin cambios, para no consultar el API en cada pulsación.
export function useValorConRetardo<T>(valor: T, retardoMs: number): T {
  const [conRetardo, setConRetardo] = React.useState(valor);

  React.useEffect(() => {
    const temporizador = setTimeout(() => setConRetardo(valor), retardoMs);
    return () => clearTimeout(temporizador);
  }, [valor, retardoMs]);

  return conRetardo;
}
