import { useState, type SubmitEvent } from "react";
import { Button } from "@/shared/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/shared/ui/field";
import { PasswordInput } from "@/shared/ui/password-input";
import { useChangePasswordPage } from "../hooks/use-change-password-page";

export function ChangePasswordPage() {
  const { submit, isPending, mismatch, currentError, newError, generalError } =
    useChangePasswordPage();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    submit({ currentPassword, newPassword, confirmation });
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <form className="w-full max-w-sm" onSubmit={handleSubmit}>
        <FieldGroup>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Cambia tu contraseña</h1>
            <p className="text-sm text-muted-foreground">
              Tu contraseña es temporal. Elige una nueva para continuar.
            </p>
          </div>
          <Field data-invalid={currentError ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena-actual">
              Contraseña actual
            </FieldLabel>
            <PasswordInput
              id="contrasena-actual"
              name="contrasenaActual"
              autoComplete="current-password"
              required
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              aria-invalid={currentError ? true : undefined}
            />
            <FieldError>{currentError}</FieldError>
          </Field>
          <Field data-invalid={newError ? "true" : undefined}>
            <FieldLabel htmlFor="contrasena-nueva">Contraseña nueva</FieldLabel>
            <PasswordInput
              id="contrasena-nueva"
              name="contrasenaNueva"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              aria-invalid={newError ? true : undefined}
            />
            <FieldDescription>
              De 8 a 128 caracteres, con al menos una mayúscula, una minúscula,
              un número y un símbolo.
            </FieldDescription>
            <FieldError>{newError}</FieldError>
          </Field>
          <Field data-invalid={mismatch ? "true" : undefined}>
            <FieldLabel htmlFor="confirmacion">
              Confirma la contraseña nueva
            </FieldLabel>
            <PasswordInput
              id="confirmacion"
              name="confirmacion"
              autoComplete="new-password"
              required
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              aria-invalid={mismatch ? true : undefined}
            />
            {mismatch && <FieldError>Las contraseñas no coinciden.</FieldError>}
          </Field>
          {generalError && (
            <p role="alert" className="text-sm text-destructive">
              {generalError}
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
