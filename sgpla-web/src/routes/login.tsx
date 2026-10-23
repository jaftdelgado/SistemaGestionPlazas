import { createFileRoute } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";

export const Route = createFileRoute("/login")({
  component: Login,
});

// Maqueta conceptual: todavía no envía las credenciales ni valida el formulario.
function Login() {
  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <form
        className="w-full max-w-sm"
        onSubmit={(evento) => evento.preventDefault()}
      >
        <FieldGroup>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
            <p className="text-sm text-muted-foreground">
              Ingresa con tu cuenta institucional.
            </p>
          </div>
          <Field>
            <FieldLabel htmlFor="usuario">Usuario</FieldLabel>
            <Input id="usuario" name="usuario" autoComplete="username" />
          </Field>
          <Field>
            <FieldLabel htmlFor="contrasena">Contraseña</FieldLabel>
            <PasswordInput
              id="contrasena"
              name="contrasena"
              autoComplete="current-password"
            />
            <FieldDescription>
              Es la misma contraseña de tu correo institucional.
            </FieldDescription>
          </Field>
          <Button type="submit">Entrar</Button>
        </FieldGroup>
      </form>
    </main>
  );
}
