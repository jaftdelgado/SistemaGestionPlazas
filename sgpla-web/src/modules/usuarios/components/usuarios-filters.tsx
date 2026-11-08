import {
  Building03Icon,
  GraduationScrollIcon,
  Shield01Icon,
} from "@hugeicons/core-free-icons";
import type { IconSvgElement } from "@hugeicons/react";
import * as React from "react";
import {
  FilterToolbar,
  type FilterChip,
  type FilterField,
  type FilterFieldOption,
} from "@/shared/ui/filter-toolbar";
import { SearchInput } from "@/shared/ui/search-input";

type FilterKey = "rolId" | "areaAcademicaId" | "entidadAcademicaId";

type UsuariosFilterValues = Partial<Record<FilterKey, number>>;

const FIELDS: { key: FilterKey; label: string; icon: IconSvgElement }[] = [
  { key: "rolId", label: "Rol", icon: Shield01Icon },
  {
    key: "areaAcademicaId",
    label: "Área académica",
    icon: GraduationScrollIcon,
  },
  {
    key: "entidadAcademicaId",
    label: "Entidad académica",
    icon: Building03Icon,
  },
];

function isFilterKey(id: string): id is FilterKey {
  return FIELDS.some((field) => field.key === id);
}

type UsuariosFiltersProps = {
  searchText: string;
  onSearchChange: (text: string) => void;
  values: UsuariosFilterValues;
  rolOptions: FilterFieldOption[];
  areaOptions: FilterFieldOption[];
  entidadOptions: FilterFieldOption[];
  onFiltersChange: (values: UsuariosFilterValues) => void;
};

export function UsuariosFilters({
  searchText,
  onSearchChange,
  values,
  rolOptions,
  areaOptions,
  entidadOptions,
  onFiltersChange,
}: UsuariosFiltersProps) {
  const fields = React.useMemo<FilterField[]>(() => {
    const options: Record<FilterKey, FilterFieldOption[]> = {
      rolId: rolOptions,
      areaAcademicaId: areaOptions,
      entidadAcademicaId: entidadOptions,
    };
    return FIELDS.map((field) => ({
      id: field.key,
      label: field.label,
      icon: field.icon,
      options: options[field.key],
    }));
  }, [rolOptions, areaOptions, entidadOptions]);

  const chips: FilterChip[] = FIELDS.flatMap((field) => {
    const value = values[field.key];
    return value === undefined
      ? []
      : [{ id: field.key, label: field.label, value: String(value) }];
  });

  return (
    <div className="flex flex-wrap items-center justify-between gap-4">
      <SearchInput
        aria-label="Buscar usuarios"
        placeholder="Buscar usuarios"
        className="max-w-xs"
        value={searchText}
        onChange={(event) => onSearchChange(event.target.value)}
      />
      <FilterToolbar
        filters={chips}
        fields={fields}
        onAdd={(chip) => {
          if (isFilterKey(chip.id)) {
            onFiltersChange({ ...values, [chip.id]: Number(chip.value) });
          }
        }}
        onRemove={(id) => {
          if (isFilterKey(id)) {
            onFiltersChange({ ...values, [id]: undefined });
          }
        }}
        onClearAll={() => onFiltersChange({})}
      />
    </div>
  );
}
