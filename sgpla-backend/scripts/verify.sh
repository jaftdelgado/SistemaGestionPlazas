#!/usr/bin/env bash
# Verificación completa del backend, solo con Docker: restore, build, format y las tres suites de pruebas.
# Al final avisa de los atajos nuevos respecto a la rama base (pruebas deshabilitadas, supresión de advertencias,
# catch vacíos, esperas en pruebas y versiones fuera de Directory.Packages.props).
#
# Uso (desde cualquier carpeta del repositorio, en Git Bash, Linux o macOS):
#   sgpla-backend/scripts/verify.sh
#
# Variables opcionales:
#   SGPLA_BASE   referencia contra la que se buscan atajos nuevos (por omisión, origin/develop)
#   SGPLA_LOG    archivo del log completo (por omisión, sgpla-backend/TestResults/verify.log)
#
# Sale con el código de la verificación: 0 solo si build, format y las tres suites pasan.
set -uo pipefail

readonly IMAGEN_SDK="mcr.microsoft.com/dotnet/sdk:10.0.401"

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BACKEND="$RAIZ/sgpla-backend"
LOG="${SGPLA_LOG:-$BACKEND/TestResults/verify.log}"
BASE="${SGPLA_BASE:-origin/develop}"
mkdir -p "$(dirname "$LOG")"

# En Git Bash, Docker necesita la ruta de Windows del host y que MSYS no reescriba las rutas del contenedor.
# MSYS_NO_PATHCONV se aplica solo a la llamada de docker: exportado, git dejaría de entender las rutas /c/...
host_extra=()
sin_conversion=()
case "$(uname -s)" in
  MINGW* | MSYS* | CYGWIN*)
    sin_conversion=(env MSYS_NO_PATHCONV=1)
    RAIZ_HOST="$(cd "$RAIZ" && pwd -W)"
    ;;
  Linux)
    RAIZ_HOST="$RAIZ"
    # Testcontainers publica SQL Server en el host; en Linux host.docker.internal no existe por omisión.
    host_extra=(--add-host=host.docker.internal:host-gateway)
    ;;
  *)
    RAIZ_HOST="$RAIZ"
    ;;
esac

echo "Verificando $BACKEND con $IMAGEN_SDK (log: $LOG)..."

# La copia dentro del contenedor aísla bin/ y obj/ de los del host; la normalización a LF es una defensa por si
# la copia de trabajo tiene CRLF (.editorconfig exige LF y dotnet format fallaría).
${sin_conversion[@]+"${sin_conversion[@]}"} docker run --rm ${host_extra[@]+"${host_extra[@]}"} \
  -v "$RAIZ_HOST/sgpla-backend:/src:ro" \
  -v "$RAIZ_HOST/.editorconfig:/.editorconfig:ro" \
  -v sgpla-nuget:/root/.nuget/packages \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  "$IMAGEN_SDK" sh -c "
    mkdir -p /w && cp /.editorconfig /w/ && cd /src &&
    tar --exclude='./**/bin' --exclude='./**/obj' --exclude=./TestResults -cf - . | (mkdir -p /w/b && cd /w/b && tar xf -) &&
    cd /w/b && find . -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.props' -o -name '*.json' -o -name '*.slnx' \) -exec sed -i 's/\r\$//' {} + &&
    dotnet restore Sgpla.slnx && dotnet build Sgpla.slnx -c Release --no-restore &&
    dotnet format Sgpla.slnx --verify-no-changes --no-restore &&
    dotnet test -c Release --no-build --project tests/Sgpla.ArchitectureTests &&
    dotnet test -c Release --no-build --project tests/Sgpla.UnitTests &&
    dotnet test -c Release --no-build --project tests/Sgpla.IntegrationTests" > "$LOG" 2>&1
estado=$?

echo
echo "== Resumen =="
grep -E "Warning\(s\)|Error\(s\)|Test run summary|total:|failed:|succeeded:|skipped:" "$LOG" || true
if grep -q "Formatted code file\|error WHITESPACE\|error IDE" "$LOG"; then
  echo "dotnet format encontró cambios pendientes (ver el log)."
fi

echo
echo "== Atajos nuevos respecto a $BASE =="
if git -C "$RAIZ" rev-parse --verify --quiet "$BASE" > /dev/null; then
  base_comun="$(git -C "$RAIZ" merge-base "$BASE" HEAD)"
  # Líneas agregadas (confirmadas o no) desde la base común, con archivo y línea. Solo archivos de código y de
  # proyecto: este script contiene los patrones que busca y no debe avisarse a sí mismo.
  atajos="$(git -C "$RAIZ" diff -U0 "$base_comun" -- 'sgpla-backend/*.cs' 'sgpla-backend/*.csproj' \
    'sgpla-backend/*.props' 'sgpla-backend/*.targets' | awk '
    /^\+\+\+ b\// { archivo = substr($0, 7); next }
    /^@@/ { split($3, r, ","); linea = substr(r[1], 2) + 0; next }
    /^\+/ {
      texto = substr($0, 2)
      motivo = ""
      if (archivo ~ /\.cs$/ && texto ~ /Skip[ \t]*=/) motivo = "prueba deshabilitada (Skip)"
      else if (texto ~ /#pragma[ \t]+warning[ \t]+disable/) motivo = "#pragma warning disable"
      else if (texto ~ /SuppressMessage/) motivo = "SuppressMessage (exige Justification)"
      else if (texto ~ /<NoWarn>/) motivo = "<NoWarn>"
      else if (texto ~ /catch[ \t]*(\([^)]*\))?[ \t]*\{[ \t]*\}/) motivo = "catch vacío"
      else if (archivo ~ /^sgpla-backend\/tests\// && texto ~ /(Task\.Delay|Thread\.Sleep)/) motivo = "espera en una prueba"
      else if (archivo ~ /\.csproj$/ && texto ~ /PackageReference/ && texto ~ /(Version|VersionOverride)=/) motivo = "versión fuera de Directory.Packages.props"
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
