import { createFileRoute } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";

export const Route = createFileRoute("/")({
  component: Inicio,
});

function Inicio() {
  return (
    <main className="mx-auto flex min-h-screen max-w-xl flex-col items-start justify-center gap-4 p-6">
      <h1 className="text-3xl font-semibold">SGPLa</h1>
      <p className="text-muted-foreground">Sistema de Gestión de Plazas</p>
      <Button>Comenzar</Button>
    </main>
  );
}
