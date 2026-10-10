import { createFileRoute, Link } from "@tanstack/react-router";
import {
  ArrowRightRegular,
  ArrowUpRightRegular,
  CopyRegular,
  FolderRegular,
  HomeRegular,
  MailRegular,
  AddRegular,
  SearchRegular,
  DeleteRegular,
} from "@fluentui/react-icons";
import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { SidebarDemo } from "./-sidebar-demo";

// Página provisional de desarrollo: galería para probar y ajustar los componentes.
// Cada componente nuevo agrega una <Seccion> con sus variantes y tamaños.
export const Route = createFileRoute("/componentes")({
  component: Componentes,
});

const variantes = ["default", "secondary", "ghost", "destructive"] as const;

const tamanos = ["sm", "default"] as const;
const tamanosIcono = ["icon-sm", "icon"] as const;

const rutaEjemplo = [
  "Inicio",
  "Ofertas educativas",
  "Licenciaturas",
  "Ingeniería de Software",
  "Plan 2024",
  "Programación",
  "Horarios",
] as const;

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
              <AddRegular />
            </Button>
          ))}
        </Subseccion>

        <Subseccion titulo="Con ícono">
          <Button>
            <AddRegular data-icon="inline-start" />
            Agregar
          </Button>
          <Button variant="secondary">
            Siguiente
            <ArrowRightRegular data-icon="inline-end" />
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
              <SearchRegular />
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>

        <Subseccion titulo="Align inline-end">
          <InputGroup className="max-w-xs">
            <InputGroupInput type="email" placeholder="Correo" />
            <InputGroupAddon align="inline-end">
              <MailRegular />
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
                <CopyRegular />
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
              <SearchRegular />
            </InputGroupAddon>
          </InputGroup>
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Inválido" aria-invalid />
            <InputGroupAddon>
              <SearchRegular />
            </InputGroupAddon>
          </InputGroup>
        </Subseccion>
      </Seccion>

      <Seccion titulo="Breadcrumb">
        <Subseccion titulo="Básico">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink href="#inicio">Inicio</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator />
                <BreadcrumbLink href="#ofertas">
                  Ofertas educativas
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator />
                <BreadcrumbPage>Programación</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </Subseccion>

        <Subseccion titulo="Con ícono">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink href="#inicio">
                  <HomeRegular aria-hidden="true" />
                  Inicio
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator />
                <BreadcrumbLink href="#ofertas">
                  Ofertas educativas
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator />
                <BreadcrumbPage>Programación</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </Subseccion>

        <Subseccion titulo="Separador personalizado">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink href="#inicio">Inicio</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator>/</BreadcrumbSeparator>
                <BreadcrumbLink href="#ofertas">
                  Ofertas educativas
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator>/</BreadcrumbSeparator>
                <BreadcrumbPage>Programación</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </Subseccion>

        <Subseccion titulo="Colapsado (maxItems = 3)">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList maxItems={3}>
              {rutaEjemplo.map((etiqueta, indice) => (
                <BreadcrumbItem key={etiqueta}>
                  {indice > 0 && <BreadcrumbSeparator />}
                  {indice === rutaEjemplo.length - 1 ? (
                    <BreadcrumbPage>{etiqueta}</BreadcrumbPage>
                  ) : (
                    <BreadcrumbLink href={`#${etiqueta.toLowerCase()}`}>
                      {etiqueta}
                    </BreadcrumbLink>
                  )}
                </BreadcrumbItem>
              ))}
            </BreadcrumbList>
          </Breadcrumb>
        </Subseccion>

        <Subseccion titulo="Con enrutador">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink render={(props) => <Link to="/" {...props} />}>
                  Inicio
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <BreadcrumbSeparator />
                <BreadcrumbPage>Componentes</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </Subseccion>

        <Subseccion titulo="Interactivo">
          <BreadcrumbInteractivo />
        </Subseccion>
      </Seccion>

      <Seccion titulo="Tooltip">
        <Subseccion titulo="Lados">
          {(["top", "right", "bottom", "left"] as const).map((lado) => (
            <Tooltip key={lado}>
              <TooltipTrigger render={<Button variant="secondary" />}>
                {lado}
              </TooltipTrigger>
              <TooltipContent side={lado}>Tooltip hacia {lado}</TooltipContent>
            </Tooltip>
          ))}
        </Subseccion>

        <Subseccion titulo="Con ícono">
          <Tooltip>
            <TooltipTrigger
              render={
                <Button variant="ghost" size="icon" aria-label="Agregar" />
              }
            >
              <AddRegular />
            </TooltipTrigger>
            <TooltipContent>Agregar elemento</TooltipContent>
          </Tooltip>
        </Subseccion>
      </Seccion>

      <Seccion titulo="Alert Dialog">
        <Subseccion titulo="Predeterminado">
          <AlertDialog>
            <AlertDialogTrigger render={<Button variant="secondary" />}>
              Eliminar plan
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>
                  ¿Eliminar el plan de estudios?
                </AlertDialogTitle>
                <AlertDialogDescription>
                  Esta acción no se puede deshacer.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancelar</AlertDialogCancel>
                <AlertDialogAction variant="destructive">
                  Eliminar
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </Subseccion>

        <Subseccion titulo="Tamaño sm con ícono">
          <AlertDialog>
            <AlertDialogTrigger render={<Button variant="secondary" />}>
              Descartar cambios
            </AlertDialogTrigger>
            <AlertDialogContent size="sm">
              <AlertDialogHeader>
                <AlertDialogMedia>
                  <DeleteRegular />
                </AlertDialogMedia>
                <AlertDialogTitle>¿Descartar cambios?</AlertDialogTitle>
                <AlertDialogDescription>
                  Se perderá lo que no hayas guardado.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Seguir editando</AlertDialogCancel>
                <AlertDialogAction>Descartar</AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </Subseccion>
      </Seccion>

      <Seccion titulo="Sidebar">
        <Subseccion titulo="Interactivo">
          <SidebarDemo />
        </Subseccion>
      </Seccion>
    </main>
  );
}

function BreadcrumbInteractivo() {
  const [profundidad, setProfundidad] = useState(5);
  const paginaActual = useRef<HTMLSpanElement>(null);
  const enfocarEn = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (enfocarEn.current === profundidad) {
      paginaActual.current?.focus();
      enfocarEn.current = null;
    }
  }, [profundidad]);

  return (
    <div className="w-full max-w-lg space-y-8">
      <Breadcrumb className="min-h-[4.25rem] sm:min-h-8">
        <BreadcrumbList maxItems={3}>
          {rutaEjemplo.slice(0, profundidad + 1).map((etiqueta, indice) => (
            <BreadcrumbItem key={etiqueta}>
              {indice > 0 && <BreadcrumbSeparator />}
              {indice === profundidad ? (
                <BreadcrumbPage ref={paginaActual} tabIndex={-1}>
                  {indice === 0 && <HomeRegular aria-hidden="true" />}
                  {etiqueta}
                </BreadcrumbPage>
              ) : (
                <BreadcrumbLink
                  href={`#${etiqueta.toLowerCase()}`}
                  onClick={(evento) => {
                    if (
                      evento.metaKey ||
                      evento.ctrlKey ||
                      evento.shiftKey ||
                      evento.altKey ||
                      evento.button !== 0
                    )
                      return;
                    evento.preventDefault();
                    enfocarEn.current = indice;
                    setProfundidad(indice);
                  }}
                >
                  {indice === 0 && <HomeRegular aria-hidden="true" />}
                  {etiqueta}
                </BreadcrumbLink>
              )}
            </BreadcrumbItem>
          ))}
        </BreadcrumbList>
      </Breadcrumb>
      <div className="min-h-20 border-t pt-5">
        {profundidad < rutaEjemplo.length - 1 ? (
          <Button
            variant="ghost"
            className="h-auto w-full justify-start gap-3 border-border px-4 py-3 text-start"
            onClick={() => {
              enfocarEn.current = profundidad + 1;
              setProfundidad((valor) =>
                Math.min(valor + 1, rutaEjemplo.length - 1),
              );
            }}
          >
            <FolderRegular
              aria-hidden="true"
              className="text-muted-foreground"
            />
            <span className="flex-1">{rutaEjemplo[profundidad + 1]}</span>
            <ArrowUpRightRegular
              aria-hidden="true"
              className="text-muted-foreground"
            />
          </Button>
        ) : (
          <p className="py-3 text-center text-sm text-muted-foreground">
            Elige una ruta superior para regresar.
          </p>
        )}
      </div>
    </div>
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
