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
import { erroresDeCampo, mensajeDeProblema } from "@/lib/api/problema";
import { useIniciarSesion } from "@/modules/sesion/hooks/use-iniciar-sesion";

export function LoginPage() {
  const { redirect } = useSearch({ from: "/login" });
  const { mutate, isPending, error } = useIniciarSesion(redirect);
  const [usuario, setUsuario] = useState("");
  const [contrasena, setContrasena] = useState("");

  const errores = erroresDeCampo(error);
  const errorUsuario = errores["correo"]?.join(" ");
  const errorContrasena = errores["contraseña"]?.join(" ");

  function enviar(evento: SubmitEvent<HTMLFormElement>) {
    evento.preventDefault();
    mutate({ body: { correo: usuario, contrasena } });
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <form className="w-full max-w-sm" onSubmit={enviar}>
        <FieldGroup>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
            <p className="text-sm text-muted-foreground">
              Ingresa con tu cuenta institucional.
            </p>
          </div>
          <Field data-invalid={errorUsuario ? "true" : undefined}>
            <FieldLabel htmlFor="usuario">Usuario</FieldLabel>
            <Input
              id="usuario"
              name="usuario"
              autoComplete="username"
              required
              value={usuario}
              onChange={(evento) => setUsuario(evento.target.value)}
              aria-invalid={errorUsuario ? true : undefined}
            />
            <FieldError>{errorUsuario}</FieldError>
          </Field>
          <Field data-invalid={errorContrasena ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena">Contraseña</FieldLabel>
            <PasswordInput
              id="contrasena"
              name="contrasena"
              autoComplete="current-password"
              required
              value={contrasena}
              onChange={(evento) => setContrasena(evento.target.value)}
              aria-invalid={errorContrasena ? true : undefined}
            />
            <FieldDescription>
              Es la misma contraseña de tu correo institucional.
            </FieldDescription>
            <FieldError>{errorContrasena}</FieldError>
          </Field>
          {error && (
            <p role="alert" className="text-sm text-destructive">
              {mensajeDeProblema(error)}
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
