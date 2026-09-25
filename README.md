# SGPLa

Monorepo del Sistema de Gestión de Plazas Académicas (SGPLa).

| Carpeta | Contenido |
|---|---|
| `sgpla-backend/` | API REST en .NET 10 con SQL Server. Ver [sgpla-backend/README.md](sgpla-backend/README.md). |
| `sgpla-web/` | Frontend en React (reservado, aún sin código). |
| `DATABASE.md`, `DATABASE_DIAGRAM.md` | Modelo de datos normativo y diagrama entidad-relación. |
| `PLAN_INICIAL.md` | Arquitectura, módulos y plan del esqueleto. |
| `docker-compose.yml`, `.env.example` | Entorno local completo: SQL Server 2022, migraciones y API. |

## Levantar el entorno

```bash
docker compose build --pull
docker compose up -d
```

`--pull` descarga la versión vigente de las imágenes base en lugar de usar la caché local; el detalle está en el README del backend.

La API queda en `http://localhost:8180`: documentación en `/scalar/v1` y estado en `/health`.

## CI

Cada aplicación tiene su propio workflow en `.github/workflows/`, filtrado por ruta:

- `backend-ci.yml` se ejecuta con cambios en `sgpla-backend/**`.
- `frontend-ci.yml` se agregará junto con el esqueleto de `sgpla-web/`.
