import { createFileRoute } from "@tanstack/react-router";
import { ArrowRight, Copy, Mail, Plus, Search } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
  InputGroupText,
  InputGroupTextarea,
} from "@/components/ui/input-group";

// Página provisional de desarrollo: galería para probar y ajustar los componentes.
// Cada componente nuevo agrega una <Seccion> con sus variantes y tamaños.
export const Route = createFileRoute("/componentes")({
  component: Componentes,
});

const variantes = ["default", "secondary", "ghost", "destructive"] as const;

const tamanos = ["sm", "default"] as const;
const tamanosIcono = ["icon-xs", "icon-sm", "icon", "icon-lg"] as const;

function Componentes() {
  return (
    <main className="mx-auto flex max-w-4xl flex-col gap-10 p-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-3xl font-semibold">Componentes</h1>
        <p className="text-muted-foreground">
          Página provisional de desarrollo para probar variantes y tamaños.
        </p>
      </header>

      <Seccion titulo="Button">
        <Subseccion titulo="Variantes">
          {variantes.map((variante) => (
            <Button key={variante} variant={variante}>
              {variante}
            </Button>
          ))}
        </Subseccion>

        <Subseccion titulo="Tamaños">
          {tamanos.map((tamano) => (
            <Button key={tamano} size={tamano}>
              {tamano}
            </Button>
          ))}
        </Subseccion>

        <Subseccion titulo="Tamaños de ícono">
          {tamanosIcono.map((tamano) => (
            <Button
              key={tamano}
              size={tamano}
              variant="secondary"
              aria-label={tamano}
            >
              <Plus />
            </Button>
          ))}
        </Subseccion>

        <Subseccion titulo="Con ícono">
          <Button>
            <Plus data-icon="inline-start" />
            Agregar
          </Button>
          <Button variant="secondary">
            Siguiente
            <ArrowRight data-icon="inline-end" />
          </Button>
        </Subseccion>

        <Subseccion titulo="Deshabilitado">
          {variantes.map((variante) => (
            <Button key={variante} variant={variante} disabled>
              {variante}
            </Button>
          ))}
        </Subseccion>

        <Subseccion titulo="Inválido (aria-invalid)">
          {variantes.map((variante) => (
            <Button key={variante} variant={variante} aria-invalid>
              {variante}
            </Button>
          ))}
        </Subseccion>
      </Seccion>

      <Seccion titulo="Input">
        <Subseccion titulo="Básico">
          <Input className="max-w-xs" placeholder="Escribe aquí" />
          <Input className="max-w-xs" defaultValue="Con valor" />
        </Subseccion>

        <Subseccion titulo="Tipos">
          <Input className="max-w-xs" type="email" placeholder="Correo" />
          <Input
            className="max-w-xs"
            type="password"
            placeholder="Contraseña"
          />
          <Input className="max-w-xs" type="file" />
        </Subseccion>

        <Subseccion titulo="Estados">
          <Input className="max-w-xs" placeholder="Deshabilitado" disabled />
          <Input className="max-w-xs" placeholder="Inválido" aria-invalid />
        </Subseccion>
      </Seccion>

      <Seccion titulo="Input Group">
        <Subseccion titulo="Align inline-start (predeterminado)">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Buscar..." />
            <InputGroupAddon>
              <Search />
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Align inline-end">
          <InputGroup className="max-w-xs">
            <InputGroupInput type="email" placeholder="Correo" />
            <InputGroupAddon align="inline-end">
              <Mail />
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Align block-start">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Usuario" />
            <InputGroupAddon align="block-start">
              <InputGroupText>Nombre de usuario</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Align block-end">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Monto" />
            <InputGroupAddon align="block-end">
              <InputGroupText>Cantidad en pesos</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Texto">
          <InputGroup className="max-w-xs">
            <InputGroupAddon>
              <InputGroupText>https://</InputGroupText>
            </InputGroupAddon>
            <InputGroupInput placeholder="uv.mx" />
            <InputGroupAddon align="inline-end">
              <InputGroupText>.mx</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Botón">
          <InputGroup className="max-w-xs">
            <InputGroupInput defaultValue="https://uv.mx" readOnly />
            <InputGroupAddon align="inline-end">
              <InputGroupButton size="icon-xs" aria-label="Copiar">
                <Copy />
              </InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Buscar..." />
            <InputGroupAddon align="inline-end">
              <InputGroupButton variant="secondary">Buscar</InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Textarea">
          <InputGroup className="max-w-xs">
            <InputGroupTextarea placeholder="Escribe un mensaje..." />
            <InputGroupAddon align="block-end">
              <InputGroupText>0 / 280</InputGroupText>
              <InputGroupButton variant="default" className="ml-auto">
                Enviar
              </InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Estados">
          <InputGroup className="max-w-xs" data-disabled="true">
            <InputGroupInput placeholder="Deshabilitado" disabled />
            <InputGroupAddon>
              <Search />
            </InputGroupAddon>
          </InputGroup>
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Inválido" aria-invalid />
            <InputGroupAddon>
              <Search />
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>
      </Seccion>
    </main>
  );
}

function Seccion({
  titulo,
  children,
}: {
  titulo: string;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-6">
      <h2 className="border-b pb-2 text-xl font-semibold">{titulo}</h2>
      {children}
    </section>
  );
}

function Subseccion({
  titulo,
  children,
}: {
  titulo: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3">
      <h3 className="text-sm font-medium text-muted-foreground">{titulo}</h3>
      <div className="flex flex-wrap items-center gap-3">{children}</div>
    </div>
  );
}
