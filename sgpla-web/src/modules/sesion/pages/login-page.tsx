import { useSearch } from "@tanstack/react-router";
import { useState, type SubmitEvent } from "react";
import { Button } from "@/components/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { getFieldErrors, getGeneralMessage } from "@/lib/api/problem";
import { useLogin } from "../application/use-login";

export function LoginPage() {
  const { redirect } = useSearch({ from: "/login" });
  const { mutate, isPending, error } = useLogin(redirect);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const errors = getFieldErrors(error);
  const usernameError = errors["correo"]?.join(" ");
  const passwordError = errors["contrasena"]?.join(" ");
  const generalError = getGeneralMessage(error);

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    mutate({ body: { correo: username, contrasena: password } });
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <form className="w-full max-w-sm" onSubmit={handleSubmit}>
        <FieldGroup>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
            <p className="text-sm text-muted-foreground">
              Ingresa con tu cuenta institucional.
            </p>
          </div>
          <Field data-invalid={usernameError ? "true" : undefined}>
            <FieldLabel htmlFor="usuario">Usuario</FieldLabel>
            <Input
              id="usuario"
              name="usuario"
              autoComplete="username"
              required
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              aria-invalid={usernameError ? true : undefined}
            />
            <FieldError>{usernameError}</FieldError>
          </Field>
          <Field data-invalid={passwordError ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena">Contraseña</FieldLabel>
            <PasswordInput
              id="contrasena"
              name="contrasena"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              aria-invalid={passwordError ? true : undefined}
            />
            <FieldDescription>
              Es la misma contraseña de tu correo institucional.
            </FieldDescription>
            <FieldError>{passwordError}</FieldError>
          </Field>
          {generalError && (
            <p role="alert" className="text-sm text-destructive">
              {generalError}
            </p>
          )}
          <Button type="submit" disabled={isPending}>
            {isPending ? "Entrando…" : "Entrar"}
          </Button>
        </FieldGroup>
      </form>
    </main>
  );
}
