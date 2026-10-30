import createClient from "openapi-fetch";
import { expect, it } from "vitest";
import { createAuthorizationMiddleware } from "@/lib/api/client";
import type { paths } from "@/lib/api/schema";

async function authorizationSent(token: string | null) {
  let request: Request | undefined;
  const fakeFetch = (input: Request) => {
    request = input;
    return Promise.resolve(Response.json([]));
  };
  const client = createClient<paths>({
    baseUrl: "http://api.prueba",
    fetch: fakeFetch,
  });
  client.use(createAuthorizationMiddleware(() => token));

  await client.GET("/api/v1/institucional/regiones");

  return request?.headers.get("Authorization");
}

it("no envía Authorization cuando no hay token", async () => {
  expect(await authorizationSent(null)).toBeNull();
});

it("envía Authorization con el esquema Bearer cuando hay token", async () => {
  expect(await authorizationSent("abc123")).toBe("Bearer abc123");
});
