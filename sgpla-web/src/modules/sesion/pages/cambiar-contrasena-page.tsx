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
import { PasswordInput } from "@/components/ui/password-input";
import { erroresDeCampo, mensajeDeProblema } from "@/lib/api/problema";
import { useCambiarContrasena } from "@/modules/sesion/hooks/use-cambiar-contrasena";

export function CambiarContrasenaPage() {
  const { redirect } = useSearch({ from: "/cambiar-contrasena" });
  const { mutate, isPending, error } = useCambiarContrasena(redirect);
  const [contrasenaActual, setContrasenaActual] = useState("");
  const [contrasenaNueva, setContrasenaNueva] = useState("");
  const [confirmacion, setConfirmacion] = useState("");
  const [noCoincide, setNoCoincide] = useState(false);

  const errores = erroresDeCampo(error);
  const errorActual = errores["contrasenaActual"]?.join(" ");
  const errorNueva = errores["contrasenaNueva"]?.join(" ");

  function enviar(evento: SubmitEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (contrasenaNueva !== confirmacion) {
      setNoCoincide(true);
      return;
    }
    setNoCoincide(false);
    mutate({ body: { contrasenaActual, contrasenaNueva } });
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <form className="w-full max-w-sm" onSubmit={enviar}>
        <FieldGroup>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Cambia tu contraseña</h1>
            <p className="text-sm text-muted-foreground">
              Tu contraseña es temporal. Elige una nueva para continuar.
            </p>
          </div>
          <Field data-invalid={errorActual ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena-actual">
              Contraseña actual
            </FieldLabel>
            <PasswordInput
              id="contrasena-actual"
              name="contrasenaActual"
              autoComplete="current-password"
              required
              value={contrasenaActual}
              onChange={(evento) => setContrasenaActual(evento.target.value)}
              aria-invalid={errorActual ? true : undefined}
            />
            <FieldError>{errorActual}</FieldError>
          </Field>
          <Field data-invalid={errorNueva ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena-nueva">Contraseña nueva</FieldLabel>
            <PasswordInput
              id="contrasena-nueva"
              name="contrasenaNueva"
              autoComplete="new-password"
              required
              value={contrasenaNueva}
              onChange={(evento) => setContrasenaNueva(evento.target.value)}
              aria-invalid={errorNueva ? true : undefined}
            />
            <FieldDescription>
              De 8 a 128 caracteres, con al menos una mayúscula, una minúscula,
              un número y un símbolo.
            </FieldDescription>
            <FieldError>{errorNueva}</FieldError>
          </Field>
          <Field data-invalid={noCoincide ? "true" : undefined}>
            <FieldLabel htmlFor="confirmacion">
              Confirma la contraseña nueva
            </FieldLabel>
            <PasswordInput
              id="confirmacion"
              name="confirmacion"
              autoComplete="new-password"
              required
              value={confirmacion}
              onChange={(evento) => setConfirmacion(evento.target.value)}
              aria-invalid={noCoincide ? true : undefined}
            />
            {noCoincide && (
              <FieldError>Las contraseñas no coinciden.</FieldError>
            )}
          </Field>
          {error && (
            <p role="alert" className="text-sm text-destructive">
              {mensajeDeProblema(error)}
            </p>
          )}
          <Button type="submit" disabled={isPending}>
            {isPending ? "Guardando…" : "Cambiar contraseña"}
          </Button>
        </FieldGroup>
      </form>
    </main>
  );
}
