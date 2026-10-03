# SGPLa

Monorepo del Sistema de Gestión de Plazas Académicas (SGPLa).

| Carpeta | Contenido |
|---|---|
| `sgpla-backend/` | API REST en .NET 10 con SQL Server. Ver [sgpla-backend/README.md](sgpla-backend/README.md). |
| `sgpla-web/` | Frontend en React con Vite y TypeScript. Ver [sgpla-web/README.md](sgpla-web/README.md). |
| `DATABASE.md`, `DATABASE_DIAGRAM.md` | Modelo de datos normativo y diagrama entidad-relación. |
| `PLAN_INICIAL.md` | Arquitectura, módulos y plan del esqueleto. |
| `ESTANDAR_MODULOS.md` | Estándar normativo de implementación de los módulos del backend. |
| `DECISIONES.md` | Decisiones de los módulos cerrados y sus desviaciones respecto al modelo y al estándar. |
| `pendientes.md` | Reglas del modelo que esperan a un módulo que aún no existe. |
| `AGENTS.md`, `.agents/skills/` | Reglas para los agentes de IA y skills del equipo (metodología, patrones y verificación). `CLAUDE.md` y `.claude/skills/` remiten a ellos. |
| `docker-compose.yml`, `.env.example` | Entorno local completo: SQL Server 2022, migraciones, API y frontend. |

## Levantar el entorno

```bash
docker compose build --pull
docker compose up -d
```

`--pull` descarga la versión vigente de las imágenes base en lugar de usar la caché local; el detalle está en el README del backend.

La API queda en `http://localhost:8180`: documentación en `/scalar/v1` y estado en `/health`.

El frontend queda en `http://localhost:5173`, con recarga en caliente.

## CI

Cada aplicación tiene su propio workflow en `.github/workflows/`, filtrado por ruta:

- `backend-ci.yml` se ejecuta con cambios en `sgpla-backend/**`.
- `frontend-ci.yml` se ejecuta con cambios en `sgpla-web/**`.
