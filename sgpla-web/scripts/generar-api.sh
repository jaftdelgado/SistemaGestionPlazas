#!/usr/bin/env bash
# Regenera sgpla-web/src/lib/api/schema.d.ts desde el documento OpenAPI de la API, solo con Docker.
# Requiere la API levantada en Development (docker compose up -d). El generador vive aislado en
# sgpla-web/tools/openapi con su propio TypeScript, porque TypeScript 7 no expone la API que usa openapi-typescript.
#
# Uso (desde cualquier carpeta del repositorio, en Git Bash, Linux o macOS):
#   sgpla-web/scripts/generar-api.sh
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$RAIZ"

# MSYS_NO_PATHCONV evita que Git Bash reescriba las rutas del contenedor.
MSYS_NO_PATHCONV=1 docker compose run --rm web \
  sh -c "corepack enable && cd tools/openapi && pnpm install --frozen-lockfile && pnpm generar"
