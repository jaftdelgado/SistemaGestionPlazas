import { QueryClientProvider } from "@tanstack/react-query";
import {
  createMemoryHistory,
  createRouter,
  RouterProvider,
} from "@tanstack/react-router";
import { render, screen } from "@testing-library/react";
import { expect, it } from "vitest";
import { createQueryClient } from "@/lib/query-client";
import { routeTree } from "@/routeTree.gen";

// router.load() precarga los componentes que el plugin separa en fragmentos,
// así que el render no depende del tiempo que tarde su importación.
async function renderAt(path: string) {
  const queryClient = createQueryClient();
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [path] }),
    context: { queryClient },
  });
  await router.load();

  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

it("redirige al inicio de sesión cuando no hay sesión en la ruta raíz", async () => {
  await renderAt("/");

  expect(
    await screen.findByRole("heading", { name: "Iniciar sesión" }),
  ).toBeInTheDocument();
});

it("muestra la página 404 en una ruta inexistente", async () => {
  await renderAt("/ruta-inexistente");

  expect(await screen.findByText("Página no encontrada")).toBeInTheDocument();
});
