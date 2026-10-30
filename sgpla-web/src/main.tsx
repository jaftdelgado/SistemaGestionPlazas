import { QueryClientProvider } from "@tanstack/react-query";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createQueryClient } from "@/lib/query-client";
import { registerSesion } from "@/modules/sesion";
import { routeTree } from "@/routeTree.gen";
import "./index.css";

const queryClient = createQueryClient();
const router = createRouter({ routeTree, context: { queryClient } });

registerSesion(queryClient, () => {
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

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("No existe el elemento #root");
}

createRoot(rootElement).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
);
