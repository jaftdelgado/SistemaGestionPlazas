import { SearchRegular } from "@fluentui/react-icons";
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
        <SearchRegular aria-hidden="true" />
      </InputGroupAddon>
      <InputGroupInput type="search" {...props} />
    </InputGroup>
  );
}
