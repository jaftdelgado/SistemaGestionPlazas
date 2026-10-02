import { render, screen } from "@testing-library/react";
import { expect, it } from "vitest";
import { App } from "@/App";

it("muestra el encabezado de la aplicación", () => {
  render(<App />);

  expect(screen.getByRole("heading", { name: "SGPLa" })).toBeInTheDocument();
});
