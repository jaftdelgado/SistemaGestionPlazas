import { useState } from "react";
import { Checkbox } from "@/components/ui/checkbox";

interface SelectionCheckboxProps {
  checked: boolean;
  indeterminate?: boolean;
  tabIndex?: number;
  "aria-label": string;
  onToggle: (checked: boolean) => void;
}

// Todo el área de la celda activa el checkbox y su animación de presión.
function SelectionCheckbox({
  checked,
  indeterminate,
  tabIndex,
  "aria-label": ariaLabel,
  onToggle,
}: SelectionCheckboxProps) {
  const [pressed, setPressed] = useState(false);
  const release = () => setPressed(false);

  return (
    <div
      className="flex h-10 w-full cursor-pointer items-center justify-center"
      onPointerDown={() => setPressed(true)}
      onPointerUp={release}
      onPointerLeave={release}
      onPointerCancel={release}
      onClick={(event) => {
        if (!(event.target as HTMLElement).closest("[data-slot=checkbox]")) {
          onToggle(!checked);
        }
      }}
    >
      <Checkbox
        aria-label={ariaLabel}
        checked={checked}
        indeterminate={indeterminate}
        tabIndex={tabIndex}
        pressed={pressed}
        onCheckedChange={onToggle}
      />
    </div>
  );
}

export { SelectionCheckbox };
