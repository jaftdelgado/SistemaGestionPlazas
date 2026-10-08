#!/usr/bin/env bash
# Verificación completa del frontend, solo con Docker: instalación con lockfile congelado, formato, lint, tipos,
# pruebas y build. Al final avisa de los atajos nuevos respecto a la rama base (supresiones de lint y de tipos,
# pruebas deshabilitadas, esperas en pruebas y versiones con ^ o ~ en los package.json).
#
# Uso (desde cualquier carpeta del repositorio, en Git Bash, Linux o macOS):
#   sgpla-web/scripts/verify.sh
#
# Variables opcionales:
#   SGPLA_BASE   referencia contra la que se buscan atajos nuevos (por omisión, origin/develop)
#   SGPLA_LOG    archivo del log completo (por omisión, sgpla-web/TestResults/verify.log)
#
# Sale con el código de la verificación: 0 solo si todos los pasos pasan.
set -uo pipefail

readonly IMAGEN_NODE="node:24.21.0-alpine"

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WEB="$RAIZ/sgpla-web"
LOG="${SGPLA_LOG:-$WEB/TestResults/verify.log}"
BASE="${SGPLA_BASE:-origin/develop}"
mkdir -p "$(dirname "$LOG")"

# La versión de Node de .nvmrc debe ser la de la imagen: el CI usa .nvmrc y este script, la imagen.
version_imagen="${IMAGEN_NODE#node:}"
version_imagen="${version_imagen%-alpine}"
version_nvmrc="$(tr -d '[:space:]' < "$WEB/.nvmrc")"
if [ "$version_nvmrc" != "$version_imagen" ]; then
  echo ".nvmrc ($version_nvmrc) no coincide con la imagen $IMAGEN_NODE ($version_imagen)." >&2
  exit 1
fi

# En Git Bash, Docker necesita la ruta de Windows del host y que MSYS no reescriba las rutas del contenedor.
# MSYS_NO_PATHCONV se aplica solo a la llamada de docker: exportado, git dejaría de entender las rutas /c/...
sin_conversion=()
case "$(uname -s)" in
  MINGW* | MSYS* | CYGWIN*)
    sin_conversion=(env MSYS_NO_PATHCONV=1)
    RAIZ_HOST="$(cd "$RAIZ" && pwd -W)"
    ;;
  *)
    RAIZ_HOST="$RAIZ"
    ;;
esac

echo "Verificando $WEB con $IMAGEN_NODE (log: $LOG)..."

# La copia dentro del contenedor aísla node_modules y dist de los del host; la normalización a LF es una defensa por
# si la copia de trabajo tiene CRLF (.editorconfig exige LF y Prettier fallaría). Cada paso deja una línea
# "== paso" en el log para el resumen.
${sin_conversion[@]+"${sin_conversion[@]}"} docker run --rm \
  -v "$RAIZ_HOST/sgpla-web:/src:ro" \
  -v "$RAIZ_HOST/.editorconfig:/.editorconfig:ro" \
  -v sgpla-pnpm-store:/pnpm/store \
  -v sgpla-verify-node-modules:/w/app/node_modules \
  -e pnpm_config_store_dir=/pnpm/store \
  "$IMAGEN_NODE" sh -c "
    mkdir -p /w/app && cp /.editorconfig /w/ && cd /src &&
    tar --exclude=./node_modules --exclude=./dist --exclude=./TestResults --exclude=./tools/openapi/node_modules -cf - . | (cd /w/app && tar xf -) &&
    cd /w/app && find . -type f \( -name '*.ts' -o -name '*.tsx' -o -name '*.json' -o -name '*.css' -o -name '*.html' -o -name '*.yaml' \) -exec sed -i 's/\r\$//' {} + &&
    corepack enable &&
    paso() { echo \"== paso: \$1\"; shift; \"\$@\"; } &&
    paso 'install' pnpm install --frozen-lockfile &&
    paso 'install (tools/openapi)' sh -c 'cd tools/openapi && pnpm install --frozen-lockfile' &&
    paso 'format:check' pnpm format:check &&
    paso 'lint' pnpm lint &&
    paso 'typecheck' pnpm typecheck &&
    paso 'test' pnpm test &&
    paso 'build' pnpm build" > "$LOG" 2>&1
estado=$?

echo
echo "== Resumen =="
echo "Node de la imagen y de .nvmrc: $version_imagen"
grep -E "^== paso|Test Files|Tests |Found [0-9]+ warnings|error TS|ERR_PNPM" "$LOG" || true

echo
echo "== Atajos nuevos respecto a $BASE =="
if git -C "$RAIZ" rev-parse --verify --quiet "$BASE" > /dev/null; then
  base_comun="$(git -C "$RAIZ" merge-base "$BASE" HEAD)"
  # Líneas agregadas (confirmadas o no) desde la base común, con archivo y línea. Solo código y package.json: este
  # script contiene los patrones que busca y no debe avisarse a sí mismo. Los archivos generados quedan fuera.
  atajos="$(git -C "$RAIZ" diff -U0 "$base_comun" -- 'sgpla-web/*.ts' 'sgpla-web/*.tsx' 'sgpla-web/*.js' \
    'sgpla-web/*.mjs' 'sgpla-web/*.json' ':!sgpla-web/src/routeTree.gen.ts' ':!sgpla-web/src/lib/api/schema.d.ts' | awk '
    /^\+\+\+ b\// { archivo = substr($0, 7); next }
    /^@@/ { split($3, r, ","); linea = substr(r[1], 2) + 0; next }
    /^\+/ {
      texto = substr($0, 2)
      motivo = ""
      if (texto ~ /(oxlint|eslint)-disable/ && texto !~ /--/) motivo = "supresión de lint sin justificación (-- motivo)"
      else if (texto ~ /@ts-ignore/) motivo = "@ts-ignore"
      else if (texto ~ /@ts-nocheck/) motivo = "@ts-nocheck"
      else if (texto ~ /@ts-expect-error[ \t]*(\*\/)?[ \t]*$/) motivo = "@ts-expect-error sin descripción"
      else if (archivo ~ /\.test\.tsx?$/ && texto ~ /\.(skip|only)\(/) motivo = "prueba deshabilitada o aislada (.skip/.only)"
      else if (archivo ~ /\.test\.tsx?$/ && texto ~ /setTimeout/) motivo = "espera en una prueba (setTimeout)"
      else if (archivo ~ /^sgpla-web\/(tools\/openapi\/)?package\.json$/ && texto ~ /"[^"]+":[ \t]*"[\^~]/) motivo = "versión con ^ o ~"
      if (motivo != "") printf "  %s:%d  %s\n", archivo, linea, motivo
      linea++
    }')"
  if [ -n "$atajos" ]; then
    echo "$atajos"
    echo "Cada caso necesita una justificación explícita y aprobación en la revisión."
  else
    echo "  ninguno"
  fi
else
  echo "  omitido: no existe la referencia $BASE (ejecuta git fetch)"
fi

echo
if [ "$estado" -eq 0 ]; then
  echo "Verificación: OK"
else
  echo "Verificación: FALLÓ (código $estado). Revisa $LOG"
fi
exit "$estado"
