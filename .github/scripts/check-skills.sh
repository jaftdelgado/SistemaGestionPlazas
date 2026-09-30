#!/usr/bin/env bash
# Valida las skills del proyecto y sus envoltorios para Claude Code.
#   .agents/skills/<nombre>/SKILL.md   skill canónica
#   .claude/skills/<nombre>/SKILL.md   envoltorio con el mismo name y description
# Uso: bash .github/scripts/check-skills.sh (desde cualquier carpeta del repositorio)
set -uo pipefail
export LC_ALL=C.UTF-8

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$RAIZ"

errores=0
falla() { echo "ERROR: $*"; errores=$((errores + 1)); }

# Valor de un campo de una sola línea del frontmatter YAML (entre los dos primeros ---).
campo() {
  awk -v clave="$2" '
    NR == 1 { if ($0 != "---") exit; next }
    $0 == "---" { exit }
    index($0, clave ": ") == 1 { print substr($0, length(clave) + 3); exit }
  ' "$1" | tr -d '\r'
}

shopt -s nullglob
skills=(.agents/skills/*/)
[ ${#skills[@]} -gt 0 ] || falla "no hay skills en .agents/skills/"

for carpeta in "${skills[@]}"; do
  nombre="$(basename "$carpeta")"
  skill=".agents/skills/$nombre/SKILL.md"
  envoltorio=".claude/skills/$nombre/SKILL.md"

  if [ ! -f "$skill" ]; then
    falla "$skill no existe"
    continue
  fi

  name="$(campo "$skill" name)"
  description="$(campo "$skill" description)"

  [ "$name" = "$nombre" ] || falla "$skill: name '$name' no coincide con la carpeta '$nombre'"
  [[ "$nombre" =~ ^[a-z0-9]+(-[a-z0-9]+)*$ ]] || falla "$skill: el nombre debe ser kebab-case en minúsculas"
  if [ -z "$description" ]; then
    falla "$skill: falta description (una sola línea en el frontmatter)"
  else
    [ "${#description}" -le 1024 ] || falla "$skill: description tiene ${#description} caracteres (máximo 1024)"
    case "$description" in
      *": "* | *" #"*) falla "$skill: description no puede contener ': ' ni ' #' (rompen el YAML sin comillas)" ;;
    esac
    case "$description" in
      [\"\'\[\{\>\|\*\&\!%@\`-]*) falla "$skill: description empieza con un carácter reservado de YAML" ;;
    esac
  fi

  if [ ! -f "$envoltorio" ]; then
    falla "falta el envoltorio $envoltorio"
  else
    [ "$(campo "$envoltorio" name)" = "$name" ] || falla "$envoltorio: name distinto de la skill"
    [ "$(campo "$envoltorio" description)" = "$description" ] || falla "$envoltorio: description distinta de la skill"
    grep -qF ".agents/skills/$nombre/SKILL.md" "$envoltorio" || falla "$envoltorio: no remite a .agents/skills/$nombre/SKILL.md"
  fi

  grep -qF "| \`$nombre\` |" AGENTS.md || falla "AGENTS.md: la skill '$nombre' no aparece en la tabla de skills"
done

for carpeta in .claude/skills/*/; do
  nombre="$(basename "$carpeta")"
  [ -d ".agents/skills/$nombre" ] || falla ".claude/skills/$nombre no tiene skill en .agents/skills/"
done

if [ "$errores" -gt 0 ]; then
  echo "check-skills: $errores error(es)."
  exit 1
fi
echo "check-skills: ${#skills[@]} skills y sus envoltorios son consistentes."
