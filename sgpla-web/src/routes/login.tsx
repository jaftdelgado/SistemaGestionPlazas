import { createFileRoute, redirect } from "@tanstack/react-router";
import { leerToken } from "@/lib/api/sesion-token";
import { destinoSeguro } from "@/modules/sesion/lib/sesion-queries";
import { LoginPage } from "@/modules/sesion/pages/login-page";

export const Route = createFileRoute("/login")({
  validateSearch: (search: Record<string, unknown>): { redirect?: string } => ({
    redirect: typeof search.redirect === "string" ? search.redirect : undefined,
  }),
  beforeLoad: ({ search }) => {
    if (leerToken() !== null) {
      throw redirect({ to: destinoSeguro(search.redirect) });
    }
  },
  component: LoginPage,
});
