import { Search01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import type { ComponentPropsWithRef } from "react";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
} from "@/components/ui/input-group";

export type SearchInputProps = Omit<ComponentPropsWithRef<"input">, "size"> & {
  size?: "default" | "sm";
};

export function SearchInput({ className, size, ...props }: SearchInputProps) {
  return (
    <InputGroup data-slot="search-input" size={size} className={className}>
      <InputGroupAddon>
        <HugeiconsIcon icon={Search01Icon} aria-hidden="true" />
      </InputGroupAddon>
      <InputGroupInput type="search" {...props} />
    </InputGroup>
  );
}
