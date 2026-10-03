import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  component: Inicio,
});

function Inicio() {
  return (
    <main>
      <h1>SGPLa</h1>
      <p>Sistema de Gestión de Plazas</p>
    </main>
  );
}
