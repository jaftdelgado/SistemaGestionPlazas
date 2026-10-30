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
import { abreviarFacultad } from "../application/usuario-fila";
import { valorDeFiltro } from "../application/use-usuarios-listado";
import { ROLES } from "../domain/roles";

const CAMPO_ROL: FilterField = {
  id: "rol",
  label: "Rol",
  icon: Shield01Icon,
  options: ROLES.map((rol) => ({
    value: String(rol.id),
    label: rol.nombre,
  })),
};

type UsuariosFiltrosProps = {
  textoBusqueda: string;
  onBusquedaChange: (texto: string) => void;
  filtros: FilterChip[];
  onFiltrosChange: (filtros: FilterChip[]) => void;
};

export function UsuariosFiltros({
  textoBusqueda,
  onBusquedaChange,
  filtros,
  onFiltrosChange,
}: UsuariosFiltrosProps) {
  const { data: areas } = useAreasAcademicas();
  const { data: entidades } = useEntidadesAcademicas(
    valorDeFiltro(filtros, "area"),
  );

  // Con un área elegida, el filtro de entidad solo ofrece las de esa área.
  const campos = React.useMemo<FilterField[]>(
    () => [
      CAMPO_ROL,
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
          label: abreviarFacultad(entidad.nombre),
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
        value={textoBusqueda}
        onChange={(evento) => onBusquedaChange(evento.target.value)}
      />
      <FilterToolbar
        filters={filtros}
        fields={campos}
        onAdd={(filtro) =>
          onFiltrosChange([
            // Cambiar el área invalida la entidad elegida, que podría ser de otra área.
            ...filtros.filter(
              (f) =>
                f.id !== filtro.id &&
                !(filtro.id === "area" && f.id === "entidad"),
            ),
            filtro,
          ])
        }
        onRemove={(id) => onFiltrosChange(filtros.filter((f) => f.id !== id))}
        onClearAll={() => onFiltrosChange([])}
      />
    </div>
  );
}
