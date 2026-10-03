import createClient from "openapi-fetch";
import { expect, it } from "vitest";
import { crearMiddlewareAutorizacion } from "@/lib/api/client";
import type { paths } from "@/lib/api/schema";

async function autorizacionEnviada(token: string | null) {
  let peticion: Request | undefined;
  const fetchFalso = (entrada: Request) => {
    peticion = entrada;
    return Promise.resolve(Response.json([]));
  };
  const cliente = createClient<paths>({
    baseUrl: "http://api.prueba",
    fetch: fetchFalso,
  });
  cliente.use(crearMiddlewareAutorizacion(() => token));

  await cliente.GET("/api/v1/institucional/regiones");

  return peticion?.headers.get("Authorization");
}

it("no envía Authorization cuando no hay token", async () => {
  expect(await autorizacionEnviada(null)).toBeNull();
});

it("envía Authorization con el esquema Bearer cuando hay token", async () => {
  expect(await autorizacionEnviada("abc123")).toBe("Bearer abc123");
});
