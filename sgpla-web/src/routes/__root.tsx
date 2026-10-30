import type { QueryClient } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { createRootRouteWithContext, Outlet } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { ToastStack } from "@/components/ui/toast-stack";

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()(
  {
    component: RootLayout,
    notFoundComponent: NotFound,
  },
);

function RootLayout() {
  return (
    <>
      <Outlet />
      <ToastStack />
      {import.meta.env.DEV && (
        <>
          <TanStackRouterDevtools />
          <ReactQueryDevtools />
        </>
      )}
    </>
  );
}

function NotFound() {
  return (
    <main>
      <h1>Página no encontrada</h1>
      <p>La dirección que buscas no existe.</p>
    </main>
  );
}
