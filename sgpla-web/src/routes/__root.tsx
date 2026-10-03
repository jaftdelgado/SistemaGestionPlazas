import type { QueryClient } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { createRootRouteWithContext, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()(
  {
    component: RaizLayout,
    notFoundComponent: NoEncontrada,
  },
);

function RaizLayout() {
  return (
    <>
      <Outlet />
      {import.meta.env.DEV && (
        <>
          <TanStackRouterDevtools />
          <ReactQueryDevtools />
        </>
      )}
    </>
  );
}

function NoEncontrada() {
  return (
    <main>
      <h1>Página no encontrada</h1>
      <p>La dirección que buscas no existe.</p>
    </main>
  );
}
