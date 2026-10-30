import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  safeDestination,
  hasSavedSesion,
  LoginPage,
  validateRedirectSearch,
} from "@/modules/sesion";

export const Route = createFileRoute("/login")({
  validateSearch: validateRedirectSearch,
  beforeLoad: ({ search }) => {
    if (hasSavedSesion()) {
      throw redirect({ to: safeDestination(search.redirect) });
    }
  },
  component: LoginPage,
});
