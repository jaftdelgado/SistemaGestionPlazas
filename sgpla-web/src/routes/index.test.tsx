import { QueryClientProvider } from "@tanstack/react-query";
import {
  createMemoryHistory,
  createRouter,
  RouterProvider,
} from "@tanstack/react-router";
import { render, screen } from "@testing-library/react";
import { expect, it } from "vitest";
import { crearQueryClient } from "@/lib/query-client";
import { routeTree } from "@/routeTree.gen";

// router.load() precarga los componentes que el plugin separa en fragmentos,
// así que el render no depende del tiempo que tarde su importación.
async function renderizarEn(ruta: string) {
  const queryClient = crearQueryClient();
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [ruta] }),
    context: { queryClient },
  });
  await router.load();

  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

it("muestra el encabezado en la ruta raíz", async () => {
  await renderizarEn("/");

  expect(
    await screen.findByRole("heading", { name: "SGPLa" }),
  ).toBeInTheDocument();
});

it("muestra la página 404 en una ruta inexistente", async () => {
  await renderizarEn("/ruta-inexistente");

  expect(await screen.findByText("Página no encontrada")).toBeInTheDocument();
});
