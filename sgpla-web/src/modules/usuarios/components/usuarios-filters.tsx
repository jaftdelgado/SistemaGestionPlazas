import {
  Building03Icon,
  GraduationScrollIcon,
  Shield01Icon,
} from "@hugeicons/core-free-icons";
import * as React from "react";
import {
  FilterToolbar,
  type FilterChip,
  type FilterField,
} from "@/components/ui/filter-toolbar";
import { SearchInput } from "@/components/ui/search-input";
import {
  useAreasAcademicas,
  useEntidadesAcademicas,
} from "../application/usuarios-queries";
import { abbreviateFacultad } from "../application/usuario-row";
import { getFilterValue } from "../application/use-usuarios-listing";
import { ROLES } from "../domain/roles";

const ROL_FIELD: FilterField = {
  id: "rol",
  label: "Rol",
  icon: Shield01Icon,
  options: ROLES.map((rol) => ({
    value: String(rol.id),
    label: rol.name,
  })),
};

type UsuariosFiltersProps = {
  searchText: string;
  onSearchChange: (text: string) => void;
  filters: FilterChip[];
  onFiltersChange: (filters: FilterChip[]) => void;
};

export function UsuariosFilters({
  searchText,
  onSearchChange,
  filters,
  onFiltersChange,
}: UsuariosFiltersProps) {
  const { data: areas } = useAreasAcademicas();
  const { data: entidades } = useEntidadesAcademicas(
    getFilterValue(filters, "area"),
  );

  // Con un área elegida, el filtro de entidad solo ofrece las de esa área.
  const fields = React.useMemo<FilterField[]>(
    () => [
      ROL_FIELD,
      {
        id: "area",
        label: "Área académica",
        icon: GraduationScrollIcon,
        options: (areas ?? []).map((area) => ({
          value: String(area.id),
          label: area.nombre,
        })),
      },
      {
        id: "entidad",
        label: "Entidad académica",
        icon: Building03Icon,
        options: (entidades?.elementos ?? []).map((entidad) => ({
          value: String(entidad.id),
          label: abbreviateFacultad(entidad.nombre),
        })),
      },
    ],
    [areas, entidades],
  );

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
        filters={filters}
        fields={fields}
        onAdd={(filter) =>
          onFiltersChange([
            // Cambiar el área invalida la entidad elegida, que podría ser de otra área.
            ...filters.filter(
              (f) =>
                f.id !== filter.id &&
                !(filter.id === "area" && f.id === "entidad"),
            ),
            filter,
          ])
        }
        onRemove={(id) => onFiltersChange(filters.filter((f) => f.id !== id))}
        onClearAll={() => onFiltersChange([])}
      />
    </div>
  );
}
