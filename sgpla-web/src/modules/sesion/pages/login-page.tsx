import { useState, type SubmitEvent } from "react";
import { Button } from "@/shared/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/shared/ui/field";
import { Input } from "@/shared/ui/input";
import { PasswordInput } from "@/shared/ui/password-input";
import { useLoginPage } from "../../hooks/use-login-page";

export function LoginPage() {
  const { submit, isPending, usernameError, passwordError, generalError } =
    useLoginPage();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    submit({ correo: username, contrasena: password });
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
