import * as React from "react";

// Devuelve una función de identidad fija que siempre invoca la última versión de `callback`.
// Permite pasar manejadores a filas memoizadas sin que su identidad invalide el memo.
function useStableCallback<Args extends unknown[], Result>(
  callback: (...args: Args) => Result,
) {
  const callbackRef = React.useRef(callback);

  React.useLayoutEffect(() => {
    callbackRef.current = callback;
  });

  return React.useCallback((...args: Args) => callbackRef.current(...args), []);
}

export { useStableCallback };
