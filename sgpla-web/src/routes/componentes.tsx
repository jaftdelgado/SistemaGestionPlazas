import { createFileRoute, Link } from "@tanstack/react-router";
import {
  Add01Icon,
  ArrowRight01Icon,
  ArrowUpRight01Icon,
  Copy01Icon,
  Delete01Icon,
  Folder01Icon,
  Home01Icon,
  Mail01Icon,
  Search01Icon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
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
// Cada componente nuevo agrega una <Section> con sus variantes y tamaños.
export const Route = createFileRoute("/componentes")({
  component: Componentes,
});

const variants = ["default", "secondary", "ghost", "destructive"] as const;

const sizes = ["sm", "default"] as const;
const iconSizes = ["icon-sm", "icon"] as const;

const exampleRoute = [
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

      <Section title="Button">
        <Subsection title="Variantes">
          {variants.map((variant) => (
            <Button key={variant} variant={variant}>
              {variant}
            </Button>
          ))}
        </Subsection>

        <Subsection title="Tamaños">
          {sizes.map((size) => (
            <Button key={size} size={size}>
              {size}
            </Button>
          ))}
        </Subsection>

        <Subsection title="Tamaños de ícono">
          {iconSizes.map((size) => (
            <Button
              key={size}
              size={size}
              variant="secondary"
              aria-label={size}
            >
              <HugeiconsIcon icon={Add01Icon} />
            </Button>
          ))}
        </Subsection>

        <Subsection title="Con ícono">
          <Button>
            <HugeiconsIcon icon={Add01Icon} data-icon="inline-start" />
            Agregar
          </Button>
          <Button variant="secondary">
            Siguiente
            <HugeiconsIcon icon={ArrowRight01Icon} data-icon="inline-end" />
          </Button>
        </Subsection>

        <Subsection title="Deshabilitado">
          {variants.map((variant) => (
            <Button key={variant} variant={variant} disabled>
              {variant}
            </Button>
          ))}
        </Subsection>

        <Subsection title="Inválido (aria-invalid)">
          {variants.map((variant) => (
            <Button key={variant} variant={variant} aria-invalid>
              {variant}
            </Button>
          ))}
        </Subsection>
      </Section>

      <Section title="Input">
        <Subsection title="Básico">
          <Input className="max-w-xs" placeholder="Escribe aquí" />
          <Input className="max-w-xs" defaultValue="Con valor" />
        </Subsection>

        <Subsection title="Tipos">
          <Input className="max-w-xs" type="email" placeholder="Correo" />
          <Input
            className="max-w-xs"
            type="password"
            placeholder="Contraseña"
          />
          <Input className="max-w-xs" type="file" />
        </Subsection>

        <Subsection title="Estados">
          <Input className="max-w-xs" placeholder="Deshabilitado" disabled />
          <Input className="max-w-xs" placeholder="Inválido" aria-invalid />
        </Subsection>
      </Section>

      <Section title="Input Group">
        <Subsection title="Align inline-start (predeterminado)">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Buscar..." />
            <InputGroupAddon>
              <HugeiconsIcon icon={Search01Icon} />
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Align inline-end">
          <InputGroup className="max-w-xs">
            <InputGroupInput type="email" placeholder="Correo" />
            <InputGroupAddon align="inline-end">
              <HugeiconsIcon icon={Mail01Icon} />
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Align block-start">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Usuario" />
            <InputGroupAddon align="block-start">
              <InputGroupText>Nombre de usuario</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Align block-end">
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Monto" />
            <InputGroupAddon align="block-end">
              <InputGroupText>Cantidad en pesos</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Texto">
          <InputGroup className="max-w-xs">
            <InputGroupAddon>
              <InputGroupText>https://</InputGroupText>
            </InputGroupAddon>
            <InputGroupInput placeholder="uv.mx" />
            <InputGroupAddon align="inline-end">
              <InputGroupText>.mx</InputGroupText>
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Botón">
          <InputGroup className="max-w-xs">
            <InputGroupInput defaultValue="https://uv.mx" readOnly />
            <InputGroupAddon align="inline-end">
              <InputGroupButton size="icon-xs" aria-label="Copiar">
                <HugeiconsIcon icon={Copy01Icon} />
              </InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Buscar..." />
            <InputGroupAddon align="inline-end">
              <InputGroupButton variant="secondary">Buscar</InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Textarea">
          <InputGroup className="max-w-xs">
            <InputGroupTextarea placeholder="Escribe un mensaje..." />
            <InputGroupAddon align="block-end">
              <InputGroupText>0 / 280</InputGroupText>
              <InputGroupButton variant="default" className="ml-auto">
                Enviar
              </InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
        </Subsection>

        <Subsection title="Estados">
          <InputGroup className="max-w-xs" data-disabled="true">
            <InputGroupInput placeholder="Deshabilitado" disabled />
            <InputGroupAddon>
              <HugeiconsIcon icon={Search01Icon} />
            </InputGroupAddon>
          </InputGroup>
          <InputGroup className="max-w-xs">
            <InputGroupInput placeholder="Inválido" aria-invalid />
            <InputGroupAddon>
              <HugeiconsIcon icon={Search01Icon} />
            </InputGroupAddon>
          </InputGroup>
        </Subsection>
      </Section>

      <Section title="Breadcrumb">
        <Subsection title="Básico">
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
        </Subsection>

        <Subsection title="Con ícono">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink href="#inicio">
                  <HugeiconsIcon icon={Home01Icon} aria-hidden="true" />
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
        </Subsection>

        <Subsection title="Separador personalizado">
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
        </Subsection>

        <Subsection title="Colapsado (maxItems = 3)">
          <Breadcrumb className="w-full max-w-lg">
            <BreadcrumbList maxItems={3}>
              {exampleRoute.map((labelText, index) => (
                <BreadcrumbItem key={labelText}>
                  {index > 0 && <BreadcrumbSeparator />}
                  {index === exampleRoute.length - 1 ? (
                    <BreadcrumbPage>{labelText}</BreadcrumbPage>
                  ) : (
                    <BreadcrumbLink href={`#${labelText.toLowerCase()}`}>
                      {labelText}
                    </BreadcrumbLink>
                  )}
                </BreadcrumbItem>
              ))}
            </BreadcrumbList>
          </Breadcrumb>
        </Subsection>

        <Subsection title="Con enrutador">
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
        </Subsection>

        <Subsection title="Interactivo">
          <InteractiveBreadcrumb />
        </Subsection>
      </Section>

      <Section title="Tooltip">
        <Subsection title="Lados">
          {(["top", "right", "bottom", "left"] as const).map((side) => (
            <Tooltip key={side}>
              <TooltipTrigger render={<Button variant="secondary" />}>
                {side}
              </TooltipTrigger>
              <TooltipContent side={side}>Tooltip hacia {side}</TooltipContent>
            </Tooltip>
          ))}
        </Subsection>

        <Subsection title="Con ícono">
          <Tooltip>
            <TooltipTrigger
              render={
                <Button variant="ghost" size="icon" aria-label="Agregar" />
              }
            >
              <HugeiconsIcon icon={Add01Icon} />
            </TooltipTrigger>
            <TooltipContent>Agregar elemento</TooltipContent>
          </Tooltip>
        </Subsection>
      </Section>

      <Section title="Alert Dialog">
        <Subsection title="Predeterminado">
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
        </Subsection>

        <Subsection title="Tamaño sm con ícono">
          <AlertDialog>
            <AlertDialogTrigger render={<Button variant="secondary" />}>
              Descartar cambios
            </AlertDialogTrigger>
            <AlertDialogContent size="sm">
              <AlertDialogHeader>
                <AlertDialogMedia>
                  <HugeiconsIcon icon={Delete01Icon} />
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
        </Subsection>
      </Section>

      <Section title="Sidebar">
        <Subsection title="Interactivo">
          <SidebarDemo />
        </Subsection>
      </Section>
    </main>
  );
}

function InteractiveBreadcrumb() {
  const [depth, setDepth] = useState(5);
  const currentPage = useRef<HTMLSpanElement>(null);
  const focusOn = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (focusOn.current === depth) {
      currentPage.current?.focus();
      focusOn.current = null;
    }
  }, [depth]);

  return (
    <div className="w-full max-w-lg space-y-8">
      <Breadcrumb className="min-h-[4.25rem] sm:min-h-8">
        <BreadcrumbList maxItems={3}>
          {exampleRoute.slice(0, depth + 1).map((labelText, index) => (
            <BreadcrumbItem key={labelText}>
              {index > 0 && <BreadcrumbSeparator />}
              {index === depth ? (
                <BreadcrumbPage ref={currentPage} tabIndex={-1}>
                  {index === 0 && (
                    <HugeiconsIcon icon={Home01Icon} aria-hidden="true" />
                  )}
                  {labelText}
                </BreadcrumbPage>
              ) : (
                <BreadcrumbLink
                  href={`#${labelText.toLowerCase()}`}
                  onClick={(event) => {
                    if (
                      event.metaKey ||
                      event.ctrlKey ||
                      event.shiftKey ||
                      event.altKey ||
                      event.button !== 0
                    )
                      return;
                    event.preventDefault();
                    focusOn.current = index;
                    setDepth(index);
                  }}
                >
                  {index === 0 && (
                    <HugeiconsIcon icon={Home01Icon} aria-hidden="true" />
                  )}
                  {labelText}
                </BreadcrumbLink>
              )}
            </BreadcrumbItem>
          ))}
        </BreadcrumbList>
      </Breadcrumb>
      <div className="min-h-20 border-t pt-5">
        {depth < exampleRoute.length - 1 ? (
          <Button
            variant="ghost"
            className="h-auto w-full justify-start gap-3 border-border px-4 py-3 text-start"
            onClick={() => {
              focusOn.current = depth + 1;
              setDepth((value) =>
                Math.min(value + 1, exampleRoute.length - 1),
              );
            }}
          >
            <HugeiconsIcon
              icon={Folder01Icon}
              aria-hidden="true"
              className="text-muted-foreground"
            />
            <span className="flex-1">{exampleRoute[depth + 1]}</span>
            <HugeiconsIcon
              icon={ArrowUpRight01Icon}
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

function Section({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-6">
      <h2 className="border-b pb-2 text-xl font-semibold">{title}</h2>
      {children}
    </section>
  );
}

function Subsection({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3">
      <h3 className="text-sm font-medium text-muted-foreground">{title}</h3>
      <div className="flex flex-wrap items-center gap-3">{children}</div>
    </div>
  );
}
