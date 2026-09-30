#!/usr/bin/env bash
# Prueba de humo del backend con docker compose: entorno limpio, primer Superusuario y un token utilizable.
# Borra la base local (docker compose down -v). Las peticiones propias de cada PR se hacen después con el token.
#
# Uso (desde cualquier carpeta del repositorio, en Git Bash, Linux o macOS; requiere curl):
#   sgpla-backend/scripts/smoke.sh
#
# Deja el token en sgpla-backend/TestResults/smoke-token.txt y muestra cómo usarlo:
#   TOKEN=$(cat sgpla-backend/TestResults/smoke-token.txt)
#   curl -H "Authorization: Bearer $TOKEN" http://localhost:8180/api/v1/...
set -euo pipefail

readonly CORREO="admin@sgpla.mx"
readonly NOMBRE="Admin"
readonly CONTRASENA_NUEVA="Sgpla_Humo_2026!"

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SALIDA="$RAIZ/sgpla-backend/TestResults"
cd "$RAIZ"

if [ ! -f .env ]; then
  echo "Falta .env en la raíz del repositorio: cópialo de .env.example (docker compose exige SGPLA_JWT_CLAVE)." >&2
  exit 1
fi

puerto="$(grep -E '^SGPLA_API_PORT=' .env | cut -d= -f2 | tr -d '\r' || true)"
API="http://localhost:${puerto:-8180}"
mkdir -p "$SALIDA"

echo "== Entorno limpio =="
docker compose down -v
docker compose up -d --build

echo "== Esperando $API/health =="
for _ in $(seq 1 60); do
  if curl -fsS "$API/health" > /dev/null 2>&1; then
    break
  fi
  sleep 3
done
curl -fsS "$API/health" > /dev/null || { echo "La API no respondió en $API/health." >&2; exit 1; }

echo "== Bootstrap del primer Superusuario =="
bootstrap="$(docker compose run --rm -e SGPLA_BOOTSTRAP_CORREO="$CORREO" -e SGPLA_BOOTSTRAP_NOMBRE="$NOMBRE" \
  api bootstrap-superusuario | tr -d '\r')"
echo "$bootstrap"
temporal="$(echo "$bootstrap" | sed -n 's/^Contraseña temporal: \([^ ]*\) .*/\1/p')"
[ -n "$temporal" ] || { echo "No se pudo leer la contraseña temporal del bootstrap." >&2; exit 1; }

# Extrae "token" de una respuesta JSON sin depender de jq.
extraer_token() { sed -n 's/.*"token":"\([^"]*\)".*/\1/p'; }

echo "== Iniciar sesión =="
codigo="$(curl -sS -o "$SALIDA/smoke-login.json" -w '%{http_code}' -X POST "$API/api/v1/usuarios/iniciar-sesion" \
  -H 'Content-Type: application/json' -d "{\"correo\":\"$CORREO\",\"contrasena\":\"$temporal\"}")"
echo "HTTP $codigo"
[ "$codigo" = "200" ] || { cat "$SALIDA/smoke-login.json" >&2; exit 1; }
token_temporal="$(extraer_token < "$SALIDA/smoke-login.json")"

echo "== Cambiar la contraseña =="
codigo="$(curl -sS -o "$SALIDA/smoke-cambio.json" -w '%{http_code}' -X POST "$API/api/v1/usuarios/sesion/cambiar-contrasena" \
  -H 'Content-Type: application/json' -H "Authorization: Bearer $token_temporal" \
  -d "{\"contrasenaActual\":\"$temporal\",\"contrasenaNueva\":\"$CONTRASENA_NUEVA\"}")"
echo "HTTP $codigo"
[ "$codigo" = "200" ] || { cat "$SALIDA/smoke-cambio.json" >&2; exit 1; }
token="$(extraer_token < "$SALIDA/smoke-cambio.json")"
[ -n "$token" ] || { echo "La respuesta de cambiar-contrasena no trae token." >&2; exit 1; }

printf '%s' "$token" > "$SALIDA/smoke-token.txt"
rm -f "$SALIDA/smoke-login.json" "$SALIDA/smoke-cambio.json"

echo
echo "Token de Superusuario en sgpla-backend/TestResults/smoke-token.txt ($CORREO / $CONTRASENA_NUEVA)."
echo "  TOKEN=\$(cat sgpla-backend/TestResults/smoke-token.txt)"
echo "  curl -H \"Authorization: Bearer \$TOKEN\" $API/api/v1/..."
