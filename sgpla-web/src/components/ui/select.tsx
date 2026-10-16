import { CheckmarkRegular, ChevronDownRegular } from "@fluentui/react-icons";
import { Select as SelectPrimitive } from "@base-ui/react/select";
import { cn } from "@/lib/utils";

export type SelectOption = {
  value: string;
  label: string;
  disabled?: boolean;
};

export type SelectProps = Omit<
  SelectPrimitive.Root.Props<string>,
  "children" | "items"
> & {
  options: SelectOption[];
  placeholder?: string;
  size?: "default" | "sm";
  /** Nombre accesible del campo cuando no hay una etiqueta visible asociada. */
  "aria-label"?: string;
  id?: string;
  className?: string;
};

function Select({
  options,
  placeholder = "Selecciona una opción",
  size = "default",
  id,
  className,
  "aria-label": ariaLabel,
  ...props
}: SelectProps) {
  return (
    <SelectPrimitive.Root items={options} {...props}>
      <SelectPrimitive.Trigger
        id={id}
        aria-label={ariaLabel}
        data-slot="select-trigger"
        data-size={size}
        className={cn(
          "flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-full border border-transparent bg-input px-4 py-1 text-left text-base shadow-border transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 data-[size=sm]:h-8 data-[size=sm]:px-3 md:text-sm",
          className,
        )}
      >
        <SelectPrimitive.Value
          placeholder={placeholder}
          className="truncate data-placeholder:text-muted-foreground"
        />
        <SelectPrimitive.Icon className="shrink-0 text-muted-foreground transition-transform data-popup-open:rotate-180">
          <ChevronDownRegular aria-hidden="true" className="size-4" />
        </SelectPrimitive.Icon>
      </SelectPrimitive.Trigger>
      <SelectPrimitive.Portal>
        <SelectPrimitive.Positioner
          sideOffset={4}
          alignItemWithTrigger={false}
          className="isolate z-50"
        >
          <SelectPrimitive.Popup
            data-slot="select-content"
            className="max-h-(--available-height) w-(--anchor-width) min-w-(--anchor-width) origin-(--transform-origin) overflow-y-auto rounded-2xl bg-popover p-1 text-popover-foreground shadow-border data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95"
          >
            <SelectPrimitive.List>
              {options.map((option) => (
                <SelectPrimitive.Item
                  key={option.value}
                  value={option.value}
                  disabled={option.disabled}
                  className="relative flex min-h-8 cursor-default items-center gap-2 rounded-full py-1 pr-8 pl-3 text-base outline-none select-none data-disabled:pointer-events-none data-disabled:opacity-50 data-highlighted:bg-muted md:text-sm"
                >
                  <SelectPrimitive.ItemText className="truncate">
                    {option.label}
                  </SelectPrimitive.ItemText>
                  <SelectPrimitive.ItemIndicator className="absolute right-3 flex items-center">
                    <CheckmarkRegular aria-hidden="true" className="size-4" />
                  </SelectPrimitive.ItemIndicator>
                </SelectPrimitive.Item>
              ))}
            </SelectPrimitive.List>
          </SelectPrimitive.Popup>
        </SelectPrimitive.Positioner>
      </SelectPrimitive.Portal>
    </SelectPrimitive.Root>
  );
}

export { Select };
