import { QueryClientProvider } from "@tanstack/react-query";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { crearQueryClient } from "@/lib/query-client";
import { registrarSesion } from "@/modules/sesion";
import { routeTree } from "@/routeTree.gen";
import "./index.css";

const queryClient = crearQueryClient();
const router = createRouter({ routeTree, context: { queryClient } });

registrarSesion(queryClient, () => {
  void router.navigate({
    to: "/login",
    search: { redirect: router.state.location.href },
  });
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

const raiz = document.getElementById("root");
if (!raiz) {
  throw new Error("No existe el elemento #root");
}

createRoot(raiz).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
);
