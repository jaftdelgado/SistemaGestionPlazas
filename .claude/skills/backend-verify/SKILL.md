---
name: backend-verify
description: Verificación del backend de SGPLa solo con Docker, mediante sgpla-backend/scripts/verify.sh y smoke.sh. USE FOR compilar, comprobar el formato y correr las tres suites antes de entregar un PR, leer el resumen y los avisos de atajos, hacer la prueba de humo con token de Superusuario y curl, consultar la base con sqlcmd, y diagnosticar fallas de build, format, Testcontainers o CRLF. DO NOT USE FOR escribir pruebas (backend-testing), implementar código (backend-resource) ni revisar un PR ajeno (merge-review, que usa esta verificación).
---

Esta skill vive en `.agents/skills/backend-verify/SKILL.md`. Lee ese archivo completo, junto con los archivos que cite, y sigue sus instrucciones.
