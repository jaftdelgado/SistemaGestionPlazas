import { ViewIcon, ViewOffIcon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { useState, type ComponentPropsWithRef } from "react";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
} from "@/components/ui/input-group";
import { cn } from "@/lib/utils";

export type PasswordInputProps = Omit<
  ComponentPropsWithRef<"input">,
  "size" | "type"
> & {
  size?: "default" | "sm";
};

export function PasswordInput({
  className,
  size,
  disabled,
  ...props
}: PasswordInputProps) {
  const [visible, setVisible] = useState(false);
  const icon = visible ? ViewOffIcon : ViewIcon;

  return (
    <InputGroup
      data-slot="password-input"
      size={size}
      // El recuadro del autocompletado es rectangular; se recorta al contorno redondeado.
      className={cn("overflow-hidden", className)}
    >
      <InputGroupInput
        // Edge agrega un botón de revelar al campo nativo; se oculta.
        // El autocompletado del navegador pinta un fondo propio que se sale
        // de los bordes redondeados del grupo; se cubre con el color del campo.
        className="[&:-webkit-autofill]:shadow-[inset_0_0_0_1000px_var(--input)] [&:-webkit-autofill]:[-webkit-text-fill-color:var(--foreground)] [&::-ms-reveal]:hidden"
        type={visible ? "text" : "password"}
        disabled={disabled}
        {...props}
      />
      <InputGroupAddon align="inline-end">
        <InputGroupButton
          size="icon-xs"
          aria-label={visible ? "Ocultar contraseña" : "Mostrar contraseña"}
          aria-pressed={visible}
          disabled={disabled}
          onClick={() => setVisible((actual) => !actual)}
        >
          <HugeiconsIcon icon={icon} aria-hidden="true" />
        </InputGroupButton>
      </InputGroupAddon>
    </InputGroup>
  );
}
