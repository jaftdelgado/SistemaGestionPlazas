# Modelo de datos de SGPLa

## 1. Propósito y estado

Este documento es la fuente de verdad del diseño de datos actual de SGPLa. Su objetivo es conservar las decisiones necesarias para implementar posteriormente el esquema físico, las migraciones y las reglas de aplicación sin tener que reconstruir el análisis funcional.

> El diagrama entidad-relación Mermaid completo se mantiene exclusivamente en `DATABASE_DIAGRAM.md`. Este archivo contiene la definición normativa de entidades, atributos y reglas; ambos documentos deben leerse conjuntamente.

Estado del diseño: **definido e implementado en SQL** en `sgpla-backend/src/Sgpla.Database/Baseline/baseline.sql` (esquemas, tablas, restricciones e índices). Los datos iniciales de los catálogos están en `Baseline/seed.sql`.

| Propiedad | Decisión |
|---|---|
| Motor | SQL Server |
| Base de datos | `sgpla-bd` |
| Esquemas | `academico` para oferta académica, `plazas` para ofertas vacantes, avisos y Consejo Técnico, `usuarios` para identidad y acceso, e `integracion` para servicios externos |
| Nomenclatura | `snake_case`, tablas en singular |
| Llaves primarias | `int IDENTITY(1,1)` |
| Baja lógica | `fecha_eliminacion datetime2(0) NULL`, expresada en UTC; snapshots, bitácoras, borradores y maestros del esquema `plazas` aplican las excepciones documentadas |
| Auditoría general | No habrá campos transversales de creación o modificación; integraciones, documentos, revisiones y transiciones del proceso conservan únicamente las marcas temporales que exige su trazabilidad |

> El guion en `sgpla-bd` obliga a delimitar el nombre al escribir SQL, por ejemplo: `USE [sgpla-bd]`.

## 2. Alcance

El modelo cubre:

- catálogos institucionales;
- ubicación física y datos de contacto por región, campus, entidad académica y domicilio postal;
- clasificación por área académica;
- programas educativos y planes de estudio;
- experiencias educativas y áreas de formación;
- periodos escolares;
- oferta académica identificada por NRC;
- sesiones de horario vigentes por programación académica;
- sincronización de horarios desde PLANEA;
- usuarios, roles y ámbitos de autorización;
- docentes y asignaciones docentes vinculadas con la programación académica;
- ofertas vacantes, Avisos, Aspirantes, Solicitudes y sesiones del Consejo Técnico;
- solicitudes de apertura de grupos académicos y su vinculación posterior con una Programación Académica;
- autenticación institucional mediante LDAP y autenticación local de Superusuarios.

Quedan fuera de este modelo:

- catálogos administrables de edificios y aulas;
- estudiantes y la identidad individual de estudiantes interesados en una apertura;
- inscripciones;
- sesiones, refresh tokens y MFA;
- recuperación de contraseña por correo;
- notificación de resultados, solicitud de movimiento y Movimiento de Alta de DGRH;
- autenticación propia para Aspirantes, votos individuales y reglas configurables de quórum.

## 3. Estructura general

La ubicación y propiedad académica siguen esta jerarquía:

```text
REGION
  -> CAMPUS
    -> ENTIDAD_ACADEMICA
      -> PROGRAMA_EDUCATIVO
        -> PLAN_ESTUDIOS
          -> EXPERIENCIA_EDUCATIVA
            -> PROGRAMACION_ACADEMICA
              -> HORARIO_PROGRAMACION
```

Las clasificaciones y el periodo se conectan así:

```text
AREA_ACADEMICA -> ENTIDAD_ACADEMICA
MUNICIPIO -> ENTIDAD_ACADEMICA
AREA_FORMACION -> EXPERIENCIA_EDUCATIVA
SISTEMA_EDUCATIVO -> PROGRAMA_EDUCATIVO
NIVEL_FORMACION -> PROGRAMA_EDUCATIVO
PERIODO_ESCOLAR -> PROGRAMACION_ACADEMICA
PERIODO_ESCOLAR -> SINCRONIZACION_PLANEA
SINCRONIZACION_PLANEA -> HORARIO_PROGRAMACION
```

La región y el área académica de un programa educativo se derivan desde su entidad académica:

```text
PROGRAMA_EDUCATIVO
  -> ENTIDAD_ACADEMICA
    -> CAMPUS
      -> REGION
    -> AREA_ACADEMICA
```

`programa_educativo` no almacenará `region_id` ni `area_academica_id`. `programacion_academica` tampoco almacenará región, campus o entidad académica. Los horarios almacenarán edificio y aula como texto proveniente de PLANEA, no como catálogos internos.

Las ofertas vacantes y sus intentos de publicación siguen esta estructura:

```text
PROGRAMACION_ACADEMICA -> OFERTA -> AVISO_OFERTA <- AVISO
AVISO_OFERTA -> SOLICITUD -> ASPIRANTE
AVISO -> ACTA_CONSEJO_TECNICO -> ACTA_OFERTA -> AVISO_OFERTA
```

La identidad y autorización siguen esta estructura:

```text
ROL -> USUARIO

USUARIO SUPERUSUARIO -> CREDENCIAL_SUPERUSUARIO
USUARIO DGAA -> USUARIO_DGAA -> AREA_ACADEMICA
USUARIO ENTIDAD_ACADEMICA -> USUARIO_ENTIDAD_ACADEMICA -> ENTIDAD_ACADEMICA
```

## 4. Diagrama entidad-relación

La representación visual completa se encuentra exclusivamente en `DATABASE_DIAGRAM.md`. El archivo Mermaid mantiene las relaciones entre los esquemas `academico`, `integracion`, `usuarios` y `plazas`, incluidas las Solicitudes de Apertura.


## 5. Convenciones comunes

Las entidades persistentes principales de los esquemas `academico`, `plazas` y `usuarios`, salvo las excepciones documentadas, tendrán:

| Columna | Tipo SQL Server | Nulabilidad | Regla |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | Llave primaria agrupada |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | `NULL` significa activo; cualquier valor representa baja lógica y debe estar en UTC |

Reglas generales:

- Las claves y códigos ASCII se almacenan en `varchar`, recortados y en mayúsculas.
- Los nombres y textos en español se almacenan en `nvarchar`.
- Todo nombre, clave y código obligatorio debe contener al menos un carácter útil después de recortar espacios.
- Las claves institucionales, códigos y relaciones de propiedad son inmutables según las reglas de la sección 10.
- Las llaves foráneas usan `ON DELETE NO ACTION` y `ON UPDATE NO ACTION`.
- No se permitirán eliminaciones físicas mediante los flujos normales salvo las excepciones de borradores, snapshots y maestros nunca referenciados documentadas en esta sección y en la sección 15.
- Por defecto, las restricciones únicas incluyen filas dadas de baja. Solo se usarán índices únicos filtrados cuando una regla histórica lo requiera expresamente, como el folio reutilizable de un Acta eliminada.
- No se reutilizarán claves ni combinaciones de negocio pertenecientes a registros dados de baja.

Excepciones deliberadas:

- `academico.horario_programacion` representa únicamente el snapshot vigente de PLANEA, no tiene `fecha_eliminacion` y admite reemplazo físico transaccional.
- `integracion.sincronizacion_planea` es una bitácora append-only, usa marcas temporales propias y no tiene `fecha_eliminacion`.
- `academico.municipio` es un catálogo fijo de los municipios de Veracruz, no administrable y sin baja lógica.
- `academico.region` y `academico.campus` son catálogos fijos de la semilla, no administrables y sin baja lógica (Modulo_Institucional.md, decisión D1).
- `academico.sistema_educativo`, `academico.nivel_formacion` y `academico.area_formacion` son catálogos fijos cargados por la semilla, no administrables y sin baja lógica (Modulo_OfertaEducativa.md, decisión D1).
- `usuarios.rol` es un catálogo fijo, no administrable y sin baja lógica.
- `academico.grado_academico`, `academico.tipo_documento_expediente`, `plazas.tratamiento_academico`, `plazas.modalidad_recepcion`, `plazas.tipo_plaza` y `plazas.tipo_contratacion` son catálogos fijos cargados por la semilla, no administrables y sin baja lógica.
- Los perfiles `usuarios.usuario_dgaa` y `usuarios.usuario_entidad_academica` dependen del ciclo de vida de `usuarios.usuario` y no tienen `fecha_eliminacion` propia.
- `usuarios.credencial_superusuario` tiene `fecha_eliminacion` propia para que la autenticación local exija que tanto la cuenta como la credencial estén activas; ambas fechas se coordinan transaccionalmente.
- `plazas.articulo` es el único catálogo administrable del proceso de plazas: no usa baja lógica, su número se vuelve inmutable después de la primera referencia y su descripción siempre es editable (§15.2).
- `plazas.aviso` en estado `CREADO` y sus hijos de borrador admiten eliminación física bajo las reglas de la sección 15.
- `plazas.acta_consejo_tecnico` y `plazas.acta_oferta` son las únicas entidades nuevas con baja lógica; no se restauran.
- Ofertas, Aspirantes, Docentes e integrantes del Consejo solo admiten eliminación física cuando nunca fueron referenciados.
- Los binarios documentales permanecen en almacenamiento externo; SQL Server conserva metadatos, SHA-256 y versiones.

Convención de nombres para objetos SQL:

| Objeto | Patrón |
|---|---|
| Llave primaria | `pk_<tabla>` |
| Llave foránea | `fk_<tabla_hija>__<tabla_padre>` |
| Restricción única | `uq_<tabla>__<columnas>` |
| Restricción de validación | `ck_<tabla>__<regla>` |
| Índice no único | `ix_<tabla>__<columnas>` |

## 6. Diccionario de datos

### 6.1 `academico.region`

Solo lectura; valores de la semilla (Modulo_Institucional.md, decisión D1).

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `int` | `NOT NULL` | Clave institucional numérica, positiva, única e inmutable |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Nombre editable; no necesita ser único |

Restricciones:

- `uq_region__clave (clave)`.
- `ck_region__clave_positiva`: `clave > 0`.
- `ck_region__nombre_no_vacio`.

### 6.2 `academico.campus`

Solo lectura; valores de la semilla (Modulo_Institucional.md, decisión D1).

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `varchar(50)` | `NOT NULL` | Alfanumérica, mayúsculas, única e inmutable |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Editable; no necesita ser único |
| `region_id` | `int` | `NOT NULL` | FK inmutable a `academico.region` |

Restricciones e índices:

- `uq_campus__clave (clave)`.
- `fk_campus__region (region_id)`.
- `ck_campus__clave_formato` y `ck_campus__nombre_no_vacio`.
- `ix_campus__region_id (region_id)`.

Un campus pertenece exactamente a una región y no puede cambiar de región después de su creación.

### 6.3 `academico.area_academica`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `int` | `NOT NULL` | Clave institucional numérica, positiva, única e inmutable |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Editable; puede repetirse |
| `telefono` | `char(10)` | `NOT NULL` | Diez dígitos ASCII, editable |
| `extension` | `varchar(10)` | `NULL` | Extensión opcional de uno a diez dígitos; conserva ceros iniciales |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones:

- `uq_area_academica__clave (clave)`.
- `ck_area_academica__clave_positiva`: `clave > 0`.
- `ck_area_academica__nombre_no_vacio`.
- `ck_area_academica__telefono_formato`: exactamente diez dígitos ASCII.
- `ck_area_academica__extension_formato`: `NULL` o de uno a diez dígitos ASCII; una entrada vacía se normaliza a `NULL`.

Es un catálogo global compartido por todas las regiones y campus.

### 6.4 `academico.municipio`

Catálogo fijo de los municipios de Veracruz. Usa identificadores internos y no admite altas, modificaciones, bajas lógicas ni eliminaciones durante la operación normal.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK interna asignada al cargar los datos semilla |
| `nombre` | `nvarchar(150) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Nombre único e inmutable |

Restricciones:

- `pk_municipio (id)`.
- `uq_municipio__nombre (nombre)`.
- `ck_municipio__nombre_no_vacio`.

El catálogo no almacena una clave INEGI. Sus identificadores son propios de SGPLa y deben permanecer estables después de cargar las semillas.

### 6.5 `academico.entidad_academica`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `varchar(50)` | `NOT NULL` | Alfanumérica, mayúsculas, única e inmutable |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Editable; no necesita ser único |
| `calle` | `nvarchar(200) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Nombre de la vialidad, obligatorio y editable |
| `numero_exterior` | `nvarchar(20) COLLATE Modern_Spanish_100_CI_AI` | `NULL` | Identificador exterior alfanumérico y editable; `NULL` si el inmueble no tiene número |
| `colonia` | `nvarchar(150) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Colonia obligatoria y editable |
| `codigo_postal` | `char(5)` | `NOT NULL` | Exactamente cinco dígitos |
| `telefono` | `char(10)` | `NOT NULL` | Número nacional mexicano, exactamente diez dígitos y editable |
| `extension` | `varchar(10)` | `NULL` | Extensión telefónica opcional de uno a diez dígitos; conserva ceros iniciales |
| `campus_id` | `int` | `NOT NULL` | FK inmutable a `academico.campus` |
| `area_academica_id` | `int` | `NOT NULL` | FK a `academico.area_academica` |
| `municipio_id` | `int` | `NOT NULL` | FK editable a `academico.municipio` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones e índices:

- `uq_entidad_academica__clave (clave)`.
- `fk_entidad_academica__campus (campus_id)`.
- `fk_entidad_academica__area_academica (area_academica_id)`.
- `fk_entidad_academica__municipio (municipio_id)`.
- `ck_entidad_academica__clave_formato`, `ck_entidad_academica__nombre_no_vacio` y `ck_entidad_academica__direccion_no_vacia`.
- `ck_entidad_academica__numero_exterior`: si está presente, debe contener al menos un carácter útil después de recortar espacios.
- `ck_entidad_academica__codigo_postal_formato`: exactamente cinco caracteres ASCII entre `0` y `9`.
- `ck_entidad_academica__telefono_formato`: exactamente diez caracteres ASCII entre `0` y `9`, sin prefijo internacional ni separadores.
- `ck_entidad_academica__extension_formato`: `NULL` o entre uno y diez caracteres ASCII entre `0` y `9`; una entrada vacía o compuesta solo por espacios se normaliza a `NULL` antes de persistir.
- `ix_entidad_academica__campus_id (campus_id)`.
- `ix_entidad_academica__area_academica_id (area_academica_id)`.
- `ix_entidad_academica__municipio_id (municipio_id)`.
- `ix_entidad_academica__telefono (telefono)`, no único.
- Índices de búsqueda no únicos para `calle`, `numero_exterior`, `colonia` y `codigo_postal`; el índice de `numero_exterior` se filtra a valores no nulos.

La entidad pertenece a un solo campus y tiene exactamente un municipio, un domicilio postal y un teléfono vigentes. Su área académica puede corregirse únicamente mientras no tenga programas educativos; después queda congelada. El domicilio y los datos de contacto son editables, no conservan historial y el domicilio no se valida contra el campus. Varias entidades pueden compartir exactamente la misma dirección o teléfono.

### 6.6 `academico.sistema_educativo`

Catálogo fijo de solo lectura; valores de la semilla. Sin clave institucional: su `id` es la identidad técnica y su nombre es la identidad de negocio.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `nombre` | `nvarchar(200) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Nombre globalmente único |

Restricciones:

- `uq_sistema_educativo__nombre (nombre)`.
- `ck_sistema_educativo__nombre_no_vacio`.

Nombres equivalentes por mayúsculas o acentos representan el mismo sistema educativo. Ejemplos: Escolarizado, Virtual y Abierta.

### 6.7 `academico.nivel_formacion`

Catálogo fijo de solo lectura; valores de la semilla. Niveles a los que pueden pertenecer los programas educativos.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `varchar(50)` | `NOT NULL` | Alfanumérica, mayúsculas, globalmente única |
| `nombre` | `nvarchar(200) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Globalmente único |

Restricciones:

- `uq_nivel_formacion__clave (clave)`.
- `uq_nivel_formacion__nombre (nombre)`.
- `ck_nivel_formacion__clave_formato` y `ck_nivel_formacion__nombre_no_vacio`.

Ejemplos: TSU, Licenciatura, Maestría y Doctorado.

### 6.8 `academico.programa_educativo`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `nombre` | `nvarchar(200) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Editable; la comparación ignora mayúsculas y acentos |
| `entidad_academica_id` | `int` | `NOT NULL` | FK inmutable a `academico.entidad_academica` |
| `sistema_educativo_id` | `int` | `NOT NULL` | FK obligatoria a `academico.sistema_educativo` |
| `nivel_formacion_id` | `int` | `NOT NULL` | FK obligatoria a `academico.nivel_formacion` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones:

- `uq_programa_educativo__entidad_nombre_sistema (entidad_academica_id, nombre, sistema_educativo_id)`.
- `fk_programa_educativo__entidad_academica (entidad_academica_id)`.
- `fk_programa_educativo__sistema_educativo (sistema_educativo_id)`.
- `fk_programa_educativo__nivel_formacion (nivel_formacion_id)`.
- `ck_programa_educativo__nombre_no_vacio`.
- `ix_programa_educativo__sistema_educativo_id (sistema_educativo_id)`.
- `ix_programa_educativo__nivel_formacion_id (nivel_formacion_id)`.

Dos entidades académicas pueden ofrecer programas con el mismo nombre, incluso si pertenecen a la misma región. Una entidad puede ofrecer el mismo nombre en sistemas educativos diferentes, pero no puede repetir la combinación nombre y sistema educativo aunque cambie el nivel de formación.

La región y el área académica del programa siempre se derivan desde `entidad_academica`. Cada programa tiene exactamente un sistema educativo y un nivel de formación.

### 6.9 `academico.plan_estudios`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `codigo` | `varchar(50)` | `NOT NULL` | Código opaco, mayúsculas, inmutable; puede contener guiones |
| `programa_educativo_id` | `int` | `NOT NULL` | FK inmutable a `academico.programa_educativo` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones:

- `uq_plan_estudios__programa_educativo_id_codigo (programa_educativo_id, codigo)`.
- `fk_plan_estudios__programa_educativo (programa_educativo_id)`.
- `ck_plan_estudios__codigo_no_vacio`.

Un código como `ISOF-18-ECR` puede repetirse en programas diferentes, pero no dentro del mismo programa. El plan no tiene archivo: la base es la única fuente de verdad y sus EE se importan desde el Excel de la UV interpretado por el front (Modulo_OfertaEducativa.md, decisiones D5 y D6). El plan no tiene estado ni fechas de vigencia; deja de utilizarse cuando sus EE ya no son programadas.

### 6.10 `academico.area_formacion`

Catálogo fijo de solo lectura; valores de la semilla: claves `111` (Área de Formación Básica), `112` (Área de Formación Disciplinaria) y `113` (Área de Formación Terminal), las que usa la UV en sus planes (`CODE_AREA_F`).

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `varchar(50)` | `NOT NULL` | Alfanumérica, mayúsculas, única |
| `nombre` | `nvarchar(200)` | `NOT NULL` | No necesita ser único |

Restricciones:

- `uq_area_formacion__clave (clave)`.
- `ck_area_formacion__clave_formato` y `ck_area_formacion__nombre_no_vacio`.

Es un catálogo global que clasifica experiencias educativas y es distinto de `area_academica`.

### 6.11 `academico.experiencia_educativa`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Editable |
| `materia_ee` | `varchar(50)` | `NOT NULL` | Código alfanumérico opaco, mayúsculas e inmutable |
| `curso_ee` | `varchar(50)` | `NOT NULL` | Código alfanumérico opaco, mayúsculas e inmutable |
| `horas_teoricas` | `int` | `NOT NULL` | Entero mayor o igual a cero |
| `horas_practicas` | `int` | `NOT NULL` | Entero mayor o igual a cero |
| `creditos` | `int` | `NOT NULL` | Entero mayor que cero |
| `cupo_minimo` | `int` | `NULL` | Cupo mínimo no negativo |
| `cupo_maximo` | `int` | `NULL` | Cupo máximo no negativo |
| `perfil_docente` | `nvarchar(max)` | `NULL` | Texto libre editable |
| `area_formacion_id` | `int` | `NOT NULL` | FK a `academico.area_formacion` |
| `plan_estudios_id` | `int` | `NOT NULL` | FK inmutable a `academico.plan_estudios` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones e índices:

- `uq_experiencia_educativa__plan_materia_curso (plan_estudios_id, materia_ee, curso_ee)`.
- `fk_experiencia_educativa__area_formacion (area_formacion_id)`.
- `fk_experiencia_educativa__plan_estudios (plan_estudios_id)`.
- Checks de nombre, formato de códigos, horas no negativas y créditos positivos.
- `ck_experiencia_educativa__cupos`: cada cupo informado es no negativo y, si ambos existen, `cupo_minimo <= cupo_maximo`.
- `ix_experiencia_educativa__area_formacion_id (area_formacion_id)`.

Cada EE es exclusiva de un plan. Una EE con el mismo nombre o los mismos códigos en otro plan es una entidad independiente.

### 6.12 `academico.periodo_escolar`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `clave` | `char(6)` | `NOT NULL` | Exactamente seis dígitos, única e inmutable |
| `fecha_inicio` | `date` | `NOT NULL` | Límite inclusivo |
| `fecha_fin` | `date` | `NOT NULL` | Límite inclusivo |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones:

- `uq_periodo_escolar__clave (clave)`.
- `ck_periodo_escolar__clave_formato`: exactamente seis caracteres entre `0` y `9`.
- `ck_periodo_escolar__rango_fechas`: `fecha_inicio <= fecha_fin`.

Los periodos pueden traslaparse y no tienen estado operativo.

### 6.13 `academico.programacion_academica`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `nrc` | `varchar(20)` | `NOT NULL` | Código alfanumérico opaco, mayúsculas e inmutable |
| `periodo_escolar_id` | `int` | `NOT NULL` | FK a `academico.periodo_escolar` |
| `experiencia_educativa_id` | `int` | `NOT NULL` | FK a `academico.experiencia_educativa` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones e índices:

- `uq_programacion_academica__periodo_escolar_id_nrc (periodo_escolar_id, nrc)`.
- `fk_programacion_academica__periodo_escolar (periodo_escolar_id)`.
- `fk_programacion_academica__experiencia_educativa (experiencia_educativa_id)`.
- `ck_programacion_academica__nrc_formato`.
- `ix_programacion_academica__experiencia_educativa_id (experiencia_educativa_id)`.

El NRC puede reutilizarse en periodos diferentes. No se almacenará `sec_campus`: campus, región, entidad académica y área académica se obtienen navegando las relaciones de la EE.

### 6.14 `integracion.sincronizacion_planea`

Registra cada intento de obtener y aplicar el snapshot universitario de horarios de un periodo. Es una bitácora append-only: no usa baja lógica ni almacena el JSON recibido.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `periodo_escolar_id` | `int` | `NOT NULL` | FK a `academico.periodo_escolar` |
| `estado` | `varchar(20)` | `NOT NULL` | `EN_PROCESO`, `EXITOSA` o `FALLIDA` |
| `iniciada_en` | `datetime2(0)` | `NOT NULL` | Instante UTC de inicio |
| `finalizada_en` | `datetime2(0)` | `NULL` | Instante UTC de finalización |
| `registros_recibidos` | `int` | `NOT NULL` | Objetos recibidos; inicia en cero |
| `registros_ignorados` | `int` | `NOT NULL` | Objetos sin ninguna sesión; inicia en cero |
| `sesiones_generadas` | `int` | `NOT NULL` | Sesiones posteriores a expandir los días; inicia en cero |
| `duplicados_descartados` | `int` | `NOT NULL` | Duplicados exactos eliminados; inicia en cero |
| `advertencias` | `int` | `NOT NULL` | Rangos o traslapes observados; inicia en cero |
| `mensaje_error` | `nvarchar(4000)` | `NULL` | Resumen del fallo; el detalle completo vive en la telemetría |

Restricciones e índices:

- `fk_sincronizacion_planea__periodo_escolar (periodo_escolar_id)`.
- `ck_sincronizacion_planea__estado` limita los tres estados permitidos.
- Checks para que todos los conteos sean mayores o iguales a cero.
- `ck_sincronizacion_planea__fechas_estado`: `EN_PROCESO` no tiene fecha final; `EXITOSA` o `FALLIDA` sí la tienen y esta no precede al inicio.
- `ux_sincronizacion_planea__periodo_en_proceso`, índice único filtrado por `estado = 'EN_PROCESO'`, impide dos ejecuciones simultáneas del mismo periodo.
- `ix_sincronizacion_planea__periodo_inicio (periodo_escolar_id, iniciada_en)` permite consultar el historial de intentos.

Una sincronización pasa una sola vez de `EN_PROCESO` a `EXITOSA` o `FALLIDA`; un estado final no puede modificarse.

### 6.15 `academico.horario_programacion`

Contiene exclusivamente las sesiones vigentes importadas desde PLANEA. Una fila representa una sesión semanal para un día, intervalo de horas, rango de fechas y espacio concretos.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `programacion_academica_id` | `int` | `NOT NULL` | FK a `academico.programacion_academica` |
| `sincronizacion_planea_id` | `int` | `NOT NULL` | FK a la sincronización exitosa que produjo la sesión |
| `dia_semana` | `tinyint` | `NOT NULL` | `1=Lunes`, `2=Martes`, ..., `6=Sábado` |
| `hora_inicio` | `time(0)` | `NOT NULL` | Hora local de inicio |
| `hora_fin` | `time(0)` | `NOT NULL` | Hora local final inclusiva |
| `fecha_inicio` | `date` | `NOT NULL` | Primer día de aplicación, inclusivo |
| `fecha_fin` | `date` | `NOT NULL` | Último día de aplicación, inclusivo |
| `edificio` | `nvarchar(100)` | `NULL` | Texto de PLANEA, recortado; vacío se convierte en `NULL` |
| `aula` | `nvarchar(100)` | `NULL` | Texto de PLANEA, recortado; vacío se convierte en `NULL` |

No tiene `fecha_eliminacion`, ya que es una proyección reemplazable y no un registro histórico.

Restricciones e índices:

- `fk_horario_programacion__programacion_academica (programacion_academica_id)`.
- `fk_horario_programacion__sincronizacion_planea (sincronizacion_planea_id)`.
- `ck_horario_programacion__dia_semana`: valor entre 1 y 6.
- `ck_horario_programacion__rango_horas`: `hora_inicio < hora_fin`; no se admiten cruces de medianoche.
- `ck_horario_programacion__rango_fechas`: `fecha_inicio <= fecha_fin`.
- `uq_horario_programacion__sesion` sobre programación, día, horas, fechas, edificio y aula evita duplicados exactos.
- `ix_horario_programacion__sincronizacion_planea_id (sincronizacion_planea_id)`.

No se impiden traslapes entre sesiones no idénticas. PLANEA es la fuente de verdad: los traslapes se conservan y se contabilizan como advertencias.

### 6.16 `usuarios.rol`

Catálogo fijo que no admite altas, modificaciones, bajas lógicas ni eliminaciones durante la operación normal.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `tinyint` | `NOT NULL` | PK estable, asignada manualmente y sin autogeneración |
| `nombre` | `nvarchar(100)` | `NOT NULL` | Único e inmutable |

Datos semilla obligatorios:

| `id` | `nombre` |
|---:|---|
| 1 | Superusuario |
| 2 | DGAA |
| 3 | Entidad Académica |

Restricciones:

- `pk_rol (id)`.
- `uq_rol__nombre (nombre)`.
- `ck_rol__catalogo_fijo`: solo admite los pares `(1, N'Superusuario')`, `(2, N'DGAA')` y `(3, N'Entidad Académica')`.

La aplicación representa estos identificadores mediante constantes tipadas. No debe resolver permisos mediante cadenas ni permitir que los IDs o nombres sean configurables.

### 6.17 `usuarios.usuario`

Identidad común de acceso. Cada usuario tiene exactamente un rol y su mecanismo de autenticación se deriva de este.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `correo` | `varchar(254) COLLATE Latin1_General_100_CI_AI` | `NOT NULL` | Identificador de acceso, minúsculas e inmutable |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Nombre completo editable |
| `rol_id` | `tinyint` | `NOT NULL` | FK inmutable a `usuarios.rol` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Baja lógica UTC |

Restricciones e índices:

- `fk_usuario__rol (rol_id)`.
- `ck_usuario__correo_formato` y `ck_usuario__nombre_no_vacio`.
- `ck_usuario__correo_institucional`: los roles DGAA y Entidad Académica requieren dominio `@uv.mx`; Superusuario puede usar cualquier dominio válido.
- `ux_usuario__correo_activo (correo) WHERE fecha_eliminacion IS NULL`, índice único filtrado.
- `ix_usuario__rol_id (rol_id)`.

El correo solo es único entre cuentas activas. Después de desactivar una cuenta podrá utilizarse en una nueva identidad con otro rol o ámbito. Las cuentas no se restauran (`Modulo_Usuarios.md`, decisión D3).

### 6.18 `usuarios.usuario_dgaa`

Perfil 1:1 exclusivo de usuarios con rol DGAA.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `usuario_id` | `int` | `NOT NULL` | PK y FK a `usuarios.usuario` |
| `area_academica_id` | `int` | `NOT NULL` | FK inmutable a `academico.area_academica` |

Restricciones e índices:

- `fk_usuario_dgaa__usuario (usuario_id)`.
- `fk_usuario_dgaa__area_academica (area_academica_id)`.
- `ix_usuario_dgaa__area_academica_id (area_academica_id)`.

Varios usuarios DGAA pueden compartir la misma área académica.

### 6.19 `usuarios.usuario_entidad_academica`

Perfil 1:1 exclusivo de usuarios con rol Entidad Académica.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `usuario_id` | `int` | `NOT NULL` | PK y FK a `usuarios.usuario` |
| `entidad_academica_id` | `int` | `NOT NULL` | FK inmutable a `academico.entidad_academica` |

Restricciones e índices:

- `fk_usuario_entidad_academica__usuario (usuario_id)`.
- `fk_usuario_entidad_academica__entidad_academica (entidad_academica_id)`.
- `ix_usuario_entidad_academica__entidad_academica_id (entidad_academica_id)`.

Varios usuarios pueden compartir la misma entidad académica.

### 6.20 `usuarios.credencial_superusuario`

Credencial 1:1 exclusiva de usuarios con `rol_id = 1` (Superusuario). La relación desde `usuario` es uno a cero o uno; DGAA y Entidad Académica no pueden tener una fila en esta tabla. Esta exclusividad se valida en la capa de aplicación dentro de la misma transacción que crea o modifica la cuenta.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `usuario_id` | `int` | `NOT NULL` | PK y FK a `usuarios.usuario` |
| `contrasena` | `varchar(500)` | `NOT NULL` | Verificador Argon2id completo en formato PHC; nunca texto plano ni un valor reversible |
| `fecha_actualizacion` | `datetime2(0)` | `NULL` | `NULL` identifica una contraseña temporal pendiente de cambio; de lo contrario, instante UTC del cambio realizado por el usuario |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | `NULL` significa credencial activa; cualquier valor representa baja lógica UTC |

Restricciones:

- `fk_credencial_superusuario__usuario (usuario_id)`.
- `ck_credencial_superusuario__contrasena_no_vacia`.

Aunque la columna se llame `contrasena`, almacena exclusivamente una cadena PHC con algoritmo, versión, parámetros, sal y hash. No se guardan la contraseña original, una versión cifrada reversible, intentos fallidos, fecha de expiración ni historial de contraseñas.

### 6.21 `academico.grado_academico`

Catálogo fijo de grados académicos. Usa identificadores internos y no admite altas, modificaciones, bajas lógicas ni eliminaciones durante la operación normal.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK interna asignada al cargar los datos semilla |
| `nombre` | `nvarchar(150) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Nombre único e inmutable |

Valores cargados por `Baseline/seed.sql`, en orden de jerarquía académica: `1` Licenciatura, `2` Especialidad, `3` Maestría y `4` Doctorado. Los identificadores deben permanecer estables. Se relaciona con tratamientos académicos, formaciones de Aspirantes y formaciones de Docentes.

### 6.22 `academico.tipo_documento_expediente`

Catálogo fijo de tipos de documento de expediente. Usa identificadores internos y no admite altas, modificaciones, bajas lógicas ni eliminaciones durante la operación normal.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK interna asignada al cargar los datos semilla |
| `nombre` | `nvarchar(150) COLLATE Modern_Spanish_100_CI_AI` | `NOT NULL` | Nombre único e inmutable |

Sirve para los documentos lógicos de Aspirantes y Docentes. Sus valores se cargarán en `Baseline/seed.sql` con ids estables; aún no están definidos.

## 7. Cardinalidades

Las cardinalidades del proceso de plazas vacantes se detallan en el diagrama y los diccionarios de la sección 15.

| Padre | Hijo | Cardinalidad |
|---|---|---|
| `region` | `campus` | Una región tiene cero o muchos campus; cada campus tiene exactamente una región |
| `campus` | `entidad_academica` | Un campus tiene cero o muchas entidades; cada entidad tiene exactamente un campus |
| `area_academica` | `entidad_academica` | Un área clasifica cero o muchas entidades; cada entidad tiene exactamente un área |
| `municipio` | `entidad_academica` | Un municipio localiza cero o muchas entidades; cada entidad tiene exactamente un municipio |
| `entidad_academica` | `programa_educativo` | Una entidad ofrece cero o muchos programas; cada programa tiene exactamente una entidad |
| `sistema_educativo` | `programa_educativo` | Un sistema educativo clasifica cero o muchos programas; cada programa tiene exactamente un sistema educativo |
| `nivel_formacion` | `programa_educativo` | Un nivel clasifica cero o muchos programas; cada programa tiene exactamente un nivel |
| `programa_educativo` | `plan_estudios` | Un programa contiene cero o muchos planes; cada plan tiene exactamente un programa |
| `plan_estudios` | `experiencia_educativa` | Un plan contiene cero o muchas EE; cada EE tiene exactamente un plan |
| `area_formacion` | `experiencia_educativa` | Un área clasifica cero o muchas EE; cada EE tiene exactamente un área |
| `periodo_escolar` | `programacion_academica` | Un periodo agrupa cero o muchas programaciones; cada programación tiene exactamente un periodo |
| `experiencia_educativa` | `programacion_academica` | Una EE tiene cero o muchas programaciones; cada programación tiene exactamente una EE |
| `programacion_academica` | `horario_programacion` | Una programación tiene cero o muchas sesiones vigentes; cada sesión tiene exactamente una programación |
| `periodo_escolar` | `sincronizacion_planea` | Un periodo tiene cero o muchos intentos; cada intento corresponde exactamente a un periodo |
| `sincronizacion_planea` | `horario_programacion` | Una sincronización exitosa produce cero o muchas sesiones; cada sesión identifica exactamente su sincronización |
| `rol` | `usuario` | Un rol tiene cero o muchos usuarios; cada usuario tiene exactamente un rol |
| `usuario` | `usuario_dgaa` | Un usuario tiene cero o un perfil DGAA; cada perfil pertenece exactamente a un usuario |
| `area_academica` | `usuario_dgaa` | Un área tiene cero o muchos usuarios DGAA; cada perfil DGAA tiene exactamente un área |
| `usuario` | `usuario_entidad_academica` | Un usuario tiene cero o un perfil de entidad; cada perfil pertenece exactamente a un usuario |
| `entidad_academica` | `usuario_entidad_academica` | Una entidad tiene cero o muchos usuarios; cada perfil tiene exactamente una entidad |
| `usuario` | `credencial_superusuario` | Un usuario tiene cero o una credencial; cada credencial pertenece exactamente a un usuario Superusuario |
| `grado_academico` | `tratamiento_academico` | Un grado tiene cero o muchos tratamientos; cada tratamiento pertenece a un grado |
| `grado_academico` | `formacion_aspirante` | Un grado clasifica cero o muchas formaciones de Aspirantes |
| `grado_academico` | `formacion_docente` | Un grado clasifica cero o muchas formaciones de Docentes |
| `tipo_documento_expediente` | `documento_aspirante` | Un tipo clasifica cero o muchos documentos de Aspirantes |
| `tipo_documento_expediente` | `documento_docente` | Un tipo clasifica cero o muchos documentos de Docentes |
| `docente` | `formacion_docente` | Un Docente tiene cero o muchas formaciones |
| `docente` | `documento_docente` | Un Docente tiene cero o muchas cabeceras documentales |
| `documento_docente` | `version_documento_docente` | Una cabecera tiene una o muchas versiones |

## 8. Índices previstos

Esta sección enumera los índices del modelo académico y de usuarios. Los índices adicionales del esquema `plazas` se especifican en la sección 15.11.

Las PK serán agrupadas. Las restricciones únicas crearán índices únicos y cubrirán el prefijo de varias FKs. Además se crearán estos índices no únicos:

- `ix_campus__region_id`;
- `ix_entidad_academica__campus_id`;
- `ix_entidad_academica__area_academica_id`;
- `ix_entidad_academica__municipio_id`;
- `ix_entidad_academica__calle`;
- `ix_entidad_academica__numero_exterior`, filtrado por `numero_exterior IS NOT NULL`;
- `ix_entidad_academica__colonia`;
- `ix_entidad_academica__codigo_postal`;
- `ix_entidad_academica__telefono`;
- `ix_area_academica__telefono (telefono)`;
- `ix_docente__num_personal (num_personal)`;
- `ix_formacion_docente__docente_id (docente_id)`;
- `ix_documento_docente__docente_id (docente_id)`;
- `ix_version_documento_docente__documento_docente_id (documento_docente_id)`;
- `ix_programa_educativo__sistema_educativo_id`;
- `ix_programa_educativo__nivel_formacion_id`;
- `ix_experiencia_educativa__area_formacion_id`;
- `ix_programacion_academica__experiencia_educativa_id`;
- `ix_sincronizacion_planea__periodo_inicio`;
- `ux_sincronizacion_planea__periodo_en_proceso`, único y filtrado;
- `ix_horario_programacion__sincronizacion_planea_id`;
- `ux_usuario__correo_activo`, único y filtrado por cuentas activas;
- `ix_usuario__rol_id`;
- `ix_usuario_dgaa__area_academica_id`;
- `ix_usuario_entidad_academica__entidad_academica_id`.

No se define por ahora un índice aislado para `fecha_eliminacion`; deberá agregarse únicamente si los patrones y planes de consulta demuestran que aporta valor.

## 9. Bajas lógicas y restauración

Estas son las reglas generales del modelo académico y de usuarios. El esquema `plazas` aplica las excepciones de eliminación, conservación histórica y archivado definidas en la sección 15.10.

La base protege la estructura con PK, FK, UNIQUE y CHECK. La aplicación implementa las reglas dinámicas dentro de una transacción.

### 9.1 Baja bloqueada por hijos activos

Región y campus son de solo lectura y no se dan de baja (Modulo_Institucional.md, decisión D1). En el resto de la jerarquía de propiedad no hay cascada: la baja de un padre se bloquea mientras tenga hijos activos (Modulo_OfertaEducativa.md, decisiones D10 y D11):

```text
entidad_academica
  -> programa_educativo
    -> plan_estudios
      -> experiencia_educativa
        -> programacion_academica
```

- una entidad académica no se da de baja con programas activos;
- un programa no se da de baja con planes activos;
- una EE no se da de baja con programaciones activas;
- excepción: la baja de un plan da de baja el plan y todas sus EE activas con el mismo instante UTC, y se bloquea si alguna EE tiene programaciones activas;
- un periodo escolar no se da de baja si tiene cualquier programación, incluidas las dadas de baja, ni si otro módulo lo referencia.

`horario_programacion` es una excepción de ciclo de vida: sus sesiones se eliminan físicamente cuando Integracion da de baja una programación. Las bitácoras de `integracion.sincronizacion_planea` se conservan.

Toda baja debe:

1. ejecutarse en una sola transacción;
2. usar el mismo instante UTC para todos los registros afectados;
3. ser idempotente y no reemplazar la fecha de registros ya eliminados;
4. bloquear nuevas referencias a cualquier padre desactivado.

### 9.2 Catálogos de clasificación

`area_academica`, `area_formacion`, `sistema_educativo`, `nivel_formacion`, `grado_academico` y `tipo_documento_expediente` no son relaciones de propiedad:

- la baja de un área académica se bloquea mientras tenga entidades académicas activas;
- `area_formacion`, `sistema_educativo` y `nivel_formacion` son catálogos fijos (§6.6, §6.7 y §6.10), sin baja lógica;
- `grado_academico` y `tipo_documento_expediente` son catálogos fijos (§6.21 y §6.22), sin baja lógica;
- la baja de un área académica se bloquea también mientras tenga usuarios DGAA activos (`Modulo_Usuarios.md`, sección 9);
- la baja de una entidad académica se bloquea mientras tenga usuarios de entidad activos (`Modulo_Usuarios.md`, sección 9);
- ninguna baja de estos catálogos se propaga a las entidades clasificadas.

### 9.3 Restauración

Área y entidad académica no se restauran (Modulo_Institucional.md, decisión D3): un registro dado de baja no existe para la API de Institucional y `DELETE` sobre él responde 404, no 204. OfertaEducativa tampoco restaura (Modulo_OfertaEducativa.md, decisión D4): un programa, plan, EE, programación o periodo dado de baja no existe para la API. La restauración deja de aplicar a todo el modelo académico.

Si en el futuro se agregara una restauración, sería selectiva, nunca en cascada:

- se reactiva un registro estableciendo `fecha_eliminacion = NULL`;
- todos sus padres obligatorios deben estar activos;
- restaurar un padre no restaura descendientes automáticamente;
- cada descendiente debe restaurarse mediante una acción explícita y en orden jerárquico.

Los horarios no se restauran. Después de restaurar una programación o cualquiera de sus padres debe ejecutarse una sincronización exitosa de PLANEA para volver a materializarlos.

La baja de `usuarios.usuario` conserva su perfil de ámbito. Al dar de baja un Superusuario, la aplicación asigna el mismo instante UTC a `usuario.fecha_eliminacion` y `credencial_superusuario.fecha_eliminacion` dentro de una sola transacción. Las cuentas no se restauran (`Modulo_Usuarios.md`, decisión D3): una cuenta dada de baja no existe para la API, y el correo queda libre para una cuenta nueva.

## 10. Inmutabilidad y edición

Son inmutables desde la creación:

- todas las claves institucionales;
- `plan_estudios.codigo`;
- `experiencia_educativa.materia_ee` y `curso_ee`;
- `periodo_escolar.clave`;
- `programacion_academica.nrc`;
- `campus.region_id`;
- `entidad_academica.campus_id`;
- `programa_educativo.entidad_academica_id`;
- `plan_estudios.programa_educativo_id`;
- `experiencia_educativa.plan_estudios_id`.

Reglas adicionales:

- `entidad_academica.area_academica_id` puede cambiar únicamente mientras la entidad no tenga programas educativos.
- `programa_educativo.sistema_educativo_id` y `nivel_formacion_id` pueden cambiar únicamente si el programa nunca ha tenido un plan de estudios, incluyendo planes dados de baja.
- Desde la creación del primer plan, sistema educativo y nivel quedan inmutables. Cambiar el sistema educativo antes de ese momento debe volver a validar la unicidad del programa.
- Crear un programa requiere que su sistema educativo y nivel de formación existan.
- Si la EE nunca tuvo una programación, incluidas las dadas de baja, pueden cambiar sus horas, créditos y área de formación (Modulo_OfertaEducativa.md, decisión D13).
- Si la EE tuvo alguna programación, incluidas las dadas de baja, solo pueden cambiar `experiencia_educativa.nombre`, `perfil_docente` y los cupos; cualquier cambio de cupo se valida con las reglas de no negatividad y orden.
- Los nombres descriptivos de los catálogos administrables pueden corregirse respetando sus restricciones; `municipio`, `rol`, `sistema_educativo`, `nivel_formacion`, `area_formacion`, `grado_academico`, `tipo_documento_expediente`, `tratamiento_academico`, `modalidad_recepcion`, `tipo_plaza` y `tipo_contratacion` son catálogos fijos.
- `entidad_academica.calle`, `numero_exterior`, `colonia`, `codigo_postal` y `municipio_id` son editables y representan únicamente el domicilio vigente; los valores anteriores no se conservan.
- `entidad_academica.telefono` y `extension` son editables y representan únicamente los datos de contacto vigentes; los valores anteriores no se conservan.
- El domicilio es independiente de `campus_id`: no se valida que el municipio o código postal correspondan con el campus y una modificación de domicilio no cambia esa relación inmutable.
- `horario_programacion` es de solo lectura para los usuarios y procesos internos; solo la sincronización de PLANEA puede reemplazar sus filas.
- `sincronizacion_planea` es append-only, salvo la transición única de `EN_PROCESO` a un estado final.
- `usuarios.usuario.correo`, `rol_id` y las relaciones de ámbito son inmutables.
- Cambiar de rol, correo, área o entidad requiere desactivar la cuenta anterior y crear otra.
- `usuarios.usuario.nombre` es editable localmente y no se sobrescribe desde LDAP.
- No puede desactivarse el último Superusuario activo.

Estas reglas se aplican en la capa de aplicación; el futuro DDL no necesita triggers para implementarlas.

## 11. Consultas derivadas importantes

Para obtener la ubicación y clasificación de un programa:

```text
programa_educativo
  -> entidad_academica
    -> campus
      -> region
    -> area_academica
    -> municipio
  -> sistema_educativo
  -> nivel_formacion
```

Para obtenerlas desde una programación:

```text
programacion_academica
  -> experiencia_educativa
    -> plan_estudios
      -> programa_educativo
        -> entidad_academica
          -> campus
            -> region
          -> area_academica
          -> municipio
        -> sistema_educativo
        -> nivel_formacion
```

Para obtener el domicilio postal vigente de una entidad se consultan directamente `calle`, `numero_exterior`, `colonia` y `codigo_postal`, y se obtiene el nombre del municipio mediante `municipio_id`. Estos componentes permiten filtrar entidades por calle, número exterior, colonia, código postal o municipio sin construir ni persistir una dirección concatenada.

Los datos de contacto se consultan directamente desde `entidad_academica.telefono` y `extension`. La búsqueda por teléfono usa la coincidencia exacta de los diez dígitos normalizados; no se construye ni persiste una representación con `+52`, espacios, guiones o paréntesis.

Para localizar una programación a partir de PLANEA se usa obligatoriamente la combinación:

```text
PERIODO -> periodo_escolar.clave
NRC     -> programacion_academica.nrc
```

El NRC por sí solo no identifica una programación porque puede reutilizarse en periodos diferentes.

Para obtener el horario y su procedencia:

```text
programacion_academica
  -> horario_programacion
    -> sincronizacion_planea
```

Para resolver el ámbito de autorización:

```text
usuario DGAA
  -> usuario_dgaa
    -> area_academica
      -> entidad_academica
        -> programa_educativo

usuario Entidad Académica
  -> usuario_entidad_academica
    -> entidad_academica
      -> programa_educativo
```

Superusuario no tiene perfil de ámbito y su autorización es global.

Para reconstruir el expediente de una plaza y sus intentos de publicación:

```text
oferta
  -> programacion_academica
    -> experiencia_educativa
      -> plan_estudios
        -> programa_educativo
    -> periodo_escolar
    -> horario_programacion
  -> aviso_oferta
    -> aviso
    -> solicitud
      -> aspirante
      -> perfil_aspirante
    -> acta_oferta
      -> acta_consejo_tecnico
```

Los metadatos de documentos y revisiones se consultan por el Aviso o Acta correspondiente; el binario se recupera del almacenamiento externo mediante `clave_almacenamiento`.

Para Docentes se consultan directamente sus formaciones y cabeceras documentales versionadas. No se deben agregar columnas redundantes para acortar estas rutas sin un análisis posterior de rendimiento y consistencia.

## 12. Integración de horarios con PLANEA

> Pendiente de revisión contra el payload real de PLANEA (`pendientes.md`, P9).

### 12.1 Fuente y alcance

El endpoint tiene la forma:

```text
https://planea.uv.mx/planea/index.php/apiroladoovr/periodo/{PERIODO}
```

Cada llamada representa el snapshot completo de todas las programaciones académicas de la universidad para un solo periodo. Antes de importar horarios, `sgpla-bd` debe contener todas esas programaciones.

La respuesta operativa esperada es un arreglo JSON directo y no vacío. La integración debe exigir una respuesta HTTP exitosa y contenido JSON. Una página HTML, una respuesta de mantenimiento o un arreglo vacío se consideran fallos y nunca reemplazan el snapshot vigente.

### 12.2 Campos consumidos

Solo se consumen:

- `PERIODO` y `NRC`, para localizar `programacion_academica`;
- `ID_DOCENTE` y `NOMBRE`, para materializar el docente y la asignación inicial cuando la programación todavía no está bajo gestión local;
- `EDIFICIO` y `AULA`, como texto opcional de la sesión;
- `FECHA_INICIO` y `FECHA_FIN`;
- los pares `LUN_INI/LUN_FIN` hasta `SAB_INI/SAB_FIN`.

Se ignoran y no se persisten:

- `IND_DOCENTE`;
- `IND_PRINCIPAL`;
- `RESPONSABILIDAD`;
- `HRS_SEMANA`;
- `rhs_id`;
- `HR_ACTIVIDADDOCENTE`;
- `rhs_plaza`;
- `rhs_horasexc`;
- cualquier propiedad adicional desconocida.

### 12.3 Transformación y validación

- Cada objeto debe tener el mismo `PERIODO` solicitado.
- `ID_DOCENTE` se normaliza como `docente.num_personal`; `ID_DOCENTE` y `NOMBRE` deben aparecer juntos o ambos ser nulos.
- Todas las filas de un NRC deben identificar como máximo un docente; valores distintos invalidan el snapshot completo.
- Para una asignación PLANEA se usa la menor `FECHA_INICIO` y la mayor `FECHA_FIN` de sus filas.
- Si ya existe cualquier Oferta para la programación, el docente de PLANEA no sobrescribe la gestión local y la discrepancia se registra como advertencia.
- `(PERIODO, NRC)` debe encontrar exactamente una programación activa; cualquier ausencia o ambigüedad invalida el snapshot completo.
- Un par inicio/fin completamente nulo significa que no hay sesión ese día.
- Si solo uno de los dos valores del día está presente, el snapshot completo es inválido.
- Un objeto con todos los días nulos se ignora y aumenta `registros_ignorados`.
- Un objeto con varios días produce una sesión local por cada día informado.
- Las horas usan `HHmm`, deben representar horas válidas y cumplir inicio menor que fin.
- PLANEA expresa la hora final como inclusiva: `0800-0859` se almacena como `08:00-08:59`.
- No se permiten sesiones que crucen medianoche.
- Las fechas usan formato ISO y cumplen `fecha_inicio <= fecha_fin`.
- Una sesión fuera del rango configurado del periodo se conserva y genera advertencia.
- Edificio y aula se recortan; una cadena vacía se convierte en `NULL`.
- Sesiones exactas repetidas por diferentes docentes se deduplican.
- Sesiones con distinto edificio o aula permanecen separadas.
- Los traslapes no idénticos de una programación se conservan y generan advertencia.

### 12.4 Sincronización atómica

La unidad atómica es el periodo universitario completo:

1. Crear `integracion.sincronizacion_planea` en estado `EN_PROCESO` y adquirir exclusión para el periodo.
2. Consultar PLANEA y validar transporte, tipo de contenido, estructura y que el arreglo no esté vacío.
3. Validar todos los objetos, resolver programaciones, expandir días, normalizar espacios y deduplicar en memoria o staging.
4. Si existe cualquier error, marcar el intento `FALLIDA` con un resumen de hasta 4000 caracteres. El snapshot vigente queda intacto.
5. Si todo es válido, iniciar una transacción de base de datos.
6. Eliminar físicamente todos los horarios de las programaciones del periodo.
7. Insertar el nuevo conjunto, relacionando cada sesión con la sincronización actual.
8. Crear o actualizar los Docentes identificados por número de personal e inicializar las asignaciones PLANEA que todavía no estén bajo gestión local.
9. Marcar la bitácora `EXITOSA`, completar conteos y `finalizada_en`.
10. Confirmar la transacción. Ante cualquier fallo, revertir el reemplazo y finalizar el intento como `FALLIDA`.

Solo puede existir una sincronización `EN_PROCESO` por periodo. PLANEA es la única fuente de los horarios. También inicializa Docentes y asignaciones antes de que exista una Oferta; desde la creación de esta, SGPLa se vuelve la fuente de verdad de la asignación docente.

## 13. Usuarios, autorización y autenticación

### 13.1 Invariantes de rol

La aplicación debe garantizar en una sola transacción:

- Superusuario (`rol_id = 1`) tiene exactamente una `credencial_superusuario` y no tiene perfiles DGAA o Entidad Académica.
- DGAA (`rol_id = 2`) tiene exactamente un `usuario_dgaa` y no tiene credencial de Superusuario ni perfil de entidad.
- Entidad Académica (`rol_id = 3`) tiene exactamente un `usuario_entidad_academica` y no tiene credencial de Superusuario ni perfil DGAA.
- Un usuario no puede cambiar de rol, correo ni ámbito después de crearse.
- La base aplica PK, FK, checks e índices; la completitud y exclusividad entre tablas 1:1 se validan en la capa de aplicación.

Los tres IDs y nombres del catálogo son fijos e inmutables. La aplicación usa constantes tipadas para referirse a ellos.

Las políticas de autorización son fijas y no se modelan tablas configurables de permisos:

- Superusuario tiene acceso administrativo global.
- DGAA accede al área asignada y a sus entidades y programas.
- Entidad Académica accede exclusivamente a la entidad asignada y su descendencia.

### 13.2 Alta y ciclo de vida

- El primer Superusuario se crea mediante un comando de bootstrap seguro que recibe el secreto fuera del código fuente.
- Un Superusuario activo registra cuentas DGAA, Entidad Académica y Superusuarios adicionales.
- Toda alta requiere correo, nombre, rol y, cuando corresponda, ámbito activo.
- DGAA y Entidad Académica requieren correo `@uv.mx` y nunca reciben contraseña local.
- Al crear un Superusuario se genera una contraseña temporal, se almacena únicamente su verificador Argon2id en `contrasena` y se mantiene `fecha_actualizacion = NULL`.
- Cuando el Superusuario cambia la contraseña, se reemplaza `contrasena` por el nuevo verificador y se asigna a `fecha_actualizacion` el instante UTC actual.
- Cuando otro Superusuario restablece la contraseña, se genera una temporal nueva, se reemplaza `contrasena` y se regresa `fecha_actualizacion` a `NULL`.
- No se implementa recuperación por correo.
- El correo es único entre usuarios activos. Una baja permite crear una nueva cuenta con el mismo correo y otro rol o ámbito.
- Al desactivar un Superusuario se asigna la misma `fecha_eliminacion` UTC a la cuenta y a su credencial dentro de una transacción; no se permite desactivar el último Superusuario activo.
- Las cuentas no se restauran: una vez dada de baja, no se reactiva (`Modulo_Usuarios.md`, decisión D3).

### 13.3 Autenticación LDAP

Para DGAA y Entidad Académica:

1. Normalizar el correo a minúsculas y localizar una cuenta activa única.
2. Validar rol, perfil de ámbito, correo `@uv.mx` y que el área o entidad permanezca activa.
3. Autenticar el correo y contraseña recibida contra LDAP mediante TLS.
4. Descartar la contraseña inmediatamente; nunca se persiste, cifra, cachea ni registra.
5. Si LDAP no está disponible o rechaza las credenciales, denegar el nuevo acceso sin fallback local.

El nombre almacenado es obligatorio y administrado localmente; no se sobrescribe con atributos de LDAP.

La seguridad del canal es configurable (`Ldaps`, `StartTls` o `SinTls`); `SinTls` solo se acepta en el entorno Development, que es como opera hoy el directorio de la UV (`Modulo_Usuarios.md`, decisión D2).

### 13.4 Autenticación local

Para Superusuario:

- Exigir que tanto `usuario.fecha_eliminacion` como `credencial_superusuario.fecha_eliminacion` sean `NULL`; si cualquiera contiene una fecha, rechazar el acceso.
- Verificar con Argon2id la cadena PHC almacenada en `credencial_superusuario.contrasena` y comparar de forma segura.
- La cadena almacena versión, parámetros, sal y hash; una sal separada no es necesaria.
- Los parámetros deben poder incrementarse y provocar rehash después de un acceso exitoso.
- Si la UV exige FIPS antes de implementar, sustituir Argon2id por PBKDF2-HMAC-SHA-256 manteniendo el verificador versionado.
- Un pepper, si se adopta, vive fuera de SQL Server en un almacén de secretos.
- Una credencial con `fecha_actualizacion IS NULL` solo permite completar el flujo obligatorio de cambio de contraseña; no otorga acceso administrativo normal.

Política elegida:

- mínimo 8 y máximo 128 caracteres;
- al menos una mayúscula, una minúscula, un número y un símbolo;
- sin expiración periódica;
- sin historial de contraseñas;
- sin contador de intentos, bloqueo ni rate limiting.

La ausencia de rate limiting es un riesgo aceptado: Argon2id mitiga ataques fuera de línea, pero no evita intentos repetidos contra el endpoint. NIST recomienda limitar intentos y almacenar verificadores con sal; OWASP recomienda Argon2id cuando no se requiere FIPS. Véanse [NIST SP 800-63B](https://pages.nist.gov/800-63-4/sp800-63b.html) y [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

### 13.5 Auditoría y elementos externos

- Accesos exitosos, fallidos, restablecimientos y cambios administrativos se envían como eventos estructurados a telemetría externa.
- No se crea una tabla SQL de auditoría de autenticación.
- Sesiones, refresh tokens, MFA y recuperación por correo quedan fuera del modelo actual.

Estos eventos se emiten hoy como logs estructurados con `[LoggerMessage]` y `EventId` fijos; se exportarán a telemetría externa cuando el host la configure (`Modulo_Usuarios.md`, decisión D12).

## 14. Casos de aceptación

### Identidad y unicidad

- Aceptar la misma clave de plan en programas diferentes.
- Rechazar la misma clave de plan dos veces en un programa.
- Aceptar la misma combinación `materia_ee + curso_ee` en planes diferentes.
- Rechazar esa combinación dos veces dentro del mismo plan.
- Aceptar el mismo NRC en periodos diferentes.
- Rechazar el mismo NRC dos veces dentro de un periodo.
- Aceptar programas con el mismo nombre en entidades distintas.
- Aceptar programas con el mismo nombre dentro de una entidad cuando cambia el sistema educativo.
- Rechazar nombres equivalentes por mayúsculas o acentos dentro de una misma entidad y sistema educativo, aunque cambie el nivel de formación.
- Rechazar sistemas educativos con nombres globales equivalentes por mayúsculas o acentos.
- Rechazar claves o nombres duplicados de nivel de formación.
- Rechazar nombres de municipio equivalentes por mayúsculas o acentos.
- Permitir que varias entidades académicas compartan exactamente el mismo domicilio.
- Permitir que varias entidades académicas compartan el mismo teléfono.
- Rechazar la reutilización de claves y combinaciones conservadas en filas dadas de baja.

### Validaciones

- Rechazar claves numéricas menores o iguales a cero.
- Rechazar claves alfanuméricas vacías, con espacios exteriores o sin normalizar.
- Rechazar entidades académicas sin calle, colonia, código postal o municipio.
- Aceptar `numero_exterior = NULL` y rechazar un número exterior que solo contenga espacios.
- Rechazar códigos postales que no contengan exactamente cinco dígitos ASCII.
- Rechazar una entidad académica sin teléfono o con un teléfono que no contenga exactamente diez dígitos ASCII.
- Aceptar `extension = NULL`, normalizar una extensión vacía a `NULL` y rechazar extensiones con caracteres no numéricos o más de diez dígitos.
- Conservar los ceros iniciales de teléfono y extensión.
- Rechazar programas sin sistema educativo o nivel de formación.
- Rechazar horas negativas y créditos menores que uno.
- Aceptar cupos nulos o parciales, rechazar cupos negativos y rechazar `cupo_minimo > cupo_maximo` cuando ambos existan.
- Rechazar teléfonos de Área Académica que no contengan diez dígitos y normalizar extensiones vacías a `NULL`.
- Rechazar claves de periodo que no contengan exactamente seis dígitos.
- Rechazar periodos con `fecha_fin < fecha_inicio`.
- Permitir periodos con fechas traslapadas.

### Relaciones y derivaciones

- Rechazar toda entidad académica sin campus, área académica o un `municipio_id` existente.
- Derivar correctamente región, campus, entidad, área académica y municipio desde un programa y desde una programación.
- Confirmar que programa y programación no almacenan ubicación o área académica redundante.
- Obtener sistema educativo y nivel directamente desde el programa educativo.
- Filtrar entidades académicas por calle, número exterior, colonia, código postal y municipio.
- Localizar entidades mediante coincidencia exacta del teléfono normalizado.

### Mutabilidad

- Impedir el cambio de región de un campus.
- Impedir el cambio de campus de una entidad.
- Permitir modificar todos los componentes del domicilio de una entidad sin cambiar su identidad ni conservar historial.
- Permitir modificar teléfono y extensión sin cambiar la identidad ni conservar historial.
- Permitir que el domicilio editado no corresponda con la ubicación del campus, sin modificar `campus_id`.
- Impedir altas, modificaciones y bajas del catálogo fijo de municipios mediante los flujos normales.
- Impedir altas, modificaciones y bajas de los catálogos fijos de grados académicos, tipos de documento de expediente, tratamientos académicos, modalidades de recepción, tipos de plaza y tipos de contratación mediante los flujos normales.
- Permitir corregir el número de un artículo solo mientras ningún Aviso lo use, y su descripción en cualquier momento.
- Impedir reasignar programas, planes o EE.
- Permitir cambiar el área académica de una entidad sin programas.
- Rechazar el cambio de área académica después de crear su primer programa.
- Permitir cambiar sistema educativo o nivel de un programa que nunca ha tenido planes.
- Rechazar ese cambio después de crear cualquier plan, incluso si todos los planes están dados de baja.
- Congelar los atributos curriculares de una EE después de su primera programación, incluidas las dadas de baja.
- Verificar que el plan de estudios no tiene archivo: sus EE se importan y la descarga se genera desde la base.
- Crear Docentes desde PLANEA con puesto nulo y conservar sin historial sus datos y formaciones.
- Versionar únicamente los archivos de Docentes.
- Copiar perfil y grados del Aspirante al crear un Docente designado, sin copiar sus archivos.

### Baja y restauración

Los casos de restauración no aplican a ninguna entidad del modelo académico: ni área, entidad, programa, plan, EE, programación o periodo se restauran (Modulo_Institucional.md, decisión D3; Modulo_OfertaEducativa.md, decisión D4). Tampoco aplican a las cuentas de usuario, que siguen la misma regla (Modulo_Usuarios.md, decisión D3).

- Bloquear la baja de un padre con hijos activos (entidad, programa y EE), sin cascada.
- Dar de baja un plan junto con todas sus EE activas con el mismo instante UTC, y bloquearlo si alguna EE tiene programaciones activas.
- Bloquear la baja de un periodo con cualquier programación, incluidas las dadas de baja.
- Bloquear la baja de áreas académicas con entidades activas.
- Verificar que sistema educativo, nivel de formación y área de formación no tienen baja lógica.
- Impedir nuevas referencias a registros desactivados.

### Horarios y sincronización

- Resolver cada programación usando conjuntamente periodo y NRC.
- Dividir un objeto con varios días en varias sesiones.
- Ignorar y contabilizar objetos con todos los días nulos.
- Rechazar pares de horas incompletos, formatos inválidos, rangos inversos y cruces de medianoche.
- Deduplicar la misma sesión repetida por distintos docentes.
- Conservar sesiones con la misma hora pero edificio o aula diferente.
- Conservar y advertir sesiones no idénticas que se traslapen.
- Importar con advertencia una sesión fuera de las fechas configuradas del periodo.
- Rechazar un periodo inconsistente, NRC desconocido, arreglo vacío, contenido no JSON o respuesta HTML.
- Conservar íntegro el snapshot anterior ante cualquier fallo.
- Reemplazar físicamente todos los horarios del periodo dentro de una sola transacción exitosa.
- Impedir dos sincronizaciones simultáneas del mismo periodo.
- Impedir modificaciones manuales de horarios.
- Importar `ID_DOCENTE` y `NOMBRE` como Docente y asignación inicial, rechazando datos incompletos o varios docentes para el mismo NRC; el puesto puede quedar nulo.
- Verificar que no se persistan `IND_DOCENTE`, `IND_PRINCIPAL`, `RESPONSABILIDAD`, `HRS_SEMANA`, `rhs_id`, horas de excepción ni el payload completo.
- Confirmar que una sincronización posterior no sobrescriba al docente de una programación que ya tenga Oferta.
- Eliminar físicamente horarios al dar de baja su programación y no restaurarlos automáticamente.

### Usuarios y autenticación

- Verificar que `usuarios.rol` contenga únicamente los pares fijos `1 / Superusuario`, `2 / DGAA` y `3 / Entidad Académica`, con IDs sin autogeneración y nombres inmutables.
- Crear Superusuario, DGAA y Entidad Académica con exactamente su perfil correspondiente.
- Rechazar usuarios sin perfil, con un perfil incompatible o con más de un perfil.
- Rechazar una `credencial_superusuario` para DGAA o Entidad Académica y un Superusuario sin su credencial activa.
- Permitir varios usuarios para una misma área o entidad académica.
- Rechazar correos activos duplicados ignorando mayúsculas.
- Permitir reutilizar el correo de una cuenta desactivada y bloquear su restauración si ya existe otra activa.
- Rechazar correos no `@uv.mx` para DGAA y Entidad Académica.
- Impedir cambios de correo, rol y ámbito.
- Autenticar DGAA y Entidad Académica únicamente contra LDAP sobre TLS.
- Denegar nuevos accesos LDAP cuando el directorio no esté disponible.
- Verificar que contraseñas LDAP y locales nunca aparezcan en texto en la base, logs o telemetría.
- Verificar Argon2id con una sal distinta por credencial y parámetros incluidos en la cadena PHC.
- Al crear un Superusuario, verificar que se genere una contraseña temporal, se almacene solo su verificador PHC en `contrasena` y `fecha_actualizacion` permanezca en `NULL`.
- Exigir que una credencial con `fecha_actualizacion IS NULL` solo permita el cambio obligatorio de contraseña, sin otorgar acceso administrativo normal.
- Al cambiar la contraseña, reemplazar el verificador y asignar a `fecha_actualizacion` el instante UTC actual.
- Al restablecerla otro Superusuario, generar una contraseña temporal nueva, reemplazar el verificador y regresar `fecha_actualizacion` a `NULL`.
- Al desactivar un Superusuario, asignar la misma `fecha_eliminacion` UTC al usuario y a su credencial dentro de una transacción.
- Rechazar la autenticación local si la credencial está eliminada o si el usuario asociado está eliminado.
- Al reactivar un Superusuario, limpiar ambas fechas de eliminación, generar una contraseña temporal distinta de la anterior y establecer `fecha_actualizacion = NULL` en una transacción.
- Impedir la baja del último Superusuario activo.
- Bloquear la baja de un área o entidad académica con usuarios activos.
- Confirmar que DGAA solo acceda a su área y que Entidad Académica solo acceda a su entidad.
- Verificar que los eventos de autenticación se emitan a telemetría externa.


## 15. Ofertas vacantes, Avisos y Consejo Técnico

### 15.1 Límites y estructura del dominio

El esquema `plazas` modela desde la identificación de una vacante hasta la resolución y firma del Acta del Consejo Técnico. La Notificación de Resultados, los Dictámenes de Categoría, la Solicitud de Movimiento y el Movimiento de Alta de DGRH permanecen fuera de alcance.

`programacion_academica` conserva su significado actual: una fila representa una EE identificada por NRC dentro de un periodo. `oferta` representa un episodio independiente de vacancia sobre esa programación. La misma programación puede originar Ofertas históricas diferentes, pero solo una puede permanecer abierta a la vez.

`aviso` representa un intento documental de publicación y `aviso_oferta` la participación concreta de una Oferta en ese intento. Una Oferta puede publicarse secuencialmente en varios Avisos, nunca en dos intentos abiertos simultáneamente.

La vista Mermaid de este dominio también se mantiene exclusivamente en `DATABASE_DIAGRAM.md`; aquí se conserva la especificación textual y normativa.


### 15.2 Catálogos permanentes

Los siguientes catálogos usan `int IDENTITY(1,1)`, no tienen `fecha_eliminacion` y no admiten eliminación física durante la operación. Los nombres se comparan con `Modern_Spanish_100_CI_AI`.

| Tabla | Columnas adicionales | Unicidad | Tipo |
|---|---|---|---|
| `academico.grado_academico` | `nombre nvarchar(150)` | `nombre` | Fijo (§6.21) |
| `academico.tipo_documento_expediente` | `nombre nvarchar(150)` | `nombre` | Fijo (§6.22) |
| `plazas.tratamiento_academico` | `nombre nvarchar(30)`, `grado_academico_id int` | `nombre` global | Fijo |
| `plazas.modalidad_recepcion` | `nombre nvarchar(100)`, `requiere_lugar bit` | `nombre` | Fijo |
| `plazas.tipo_plaza` | `nombre nvarchar(150)` | `nombre` | Fijo |
| `plazas.tipo_contratacion` | `nombre nvarchar(150)` | `nombre` | Fijo |
| `plazas.articulo` | `numero varchar(50)`, `descripcion nvarchar(1000)` | `numero` normalizado | Administrable |

Los catálogos fijos se cargan en `Baseline/seed.sql` con ids estables y no admiten altas, modificaciones ni bajas durante la operación normal. Salvo `grado_academico` y `tratamiento_academico`, sus valores aún no están definidos.

`tratamiento_academico` carga `1` Lic (Licenciatura), `2` Mtro y `3` Mtra (Maestría), y `4` Dr y `5` Dra (Doctorado). Especialidad no tiene tratamiento.

`plazas.articulo` es el único administrable y solo lo registra y corrige el Superusuario:

- `numero` es una referencia opaca que permite valores como `42` o `42 BIS`. Se normaliza recortando los espacios exteriores, colapsando los internos a uno solo y pasándolo a mayúsculas, y solo admite caracteres ASCII imprimibles. Puede corregirse mientras ningún Aviso use el artículo; después se vuelve inmutable.
- `descripcion` es obligatoria, no admite texto vacío y puede corregirse en cualquier momento. Una corrección que no la incluye conserva la actual; una vez registrada, el artículo nunca queda sin descripción.
- Cada Aviso elige exactamente un artículo; el artículo no se duplica en Oferta ni Programación Académica.

### 15.3 `plazas.integrante_consejo_tecnico`

No existe una tabla `consejo_tecnico`: cada Entidad Académica posee implícitamente un solo Consejo y administra directamente sus integrantes.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `entidad_academica_id` | `int` | `NOT NULL` | FK a `academico.entidad_academica` |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Nombre completo oficial |
| `cargo` | `nvarchar(200)` | `NOT NULL` | Texto libre |
| `tratamiento_academico_id` | `int` | `NOT NULL` | FK a `plazas.tratamiento_academico` |
| `fecha_inicio` | `date` | `NOT NULL` | Inicio inclusivo |
| `fecha_fin` | `date` | `NULL` | Fin inclusivo; `NULL` significa vigencia actual |

Reglas:

- `fecha_fin >= fecha_inicio` cuando exista.
- No se permiten vigencias traslapadas para el mismo nombre normalizado dentro de una entidad.
- Un cambio de cargo o tratamiento cierra la vigencia anterior y crea otra fila.
- Solo puede eliminarse físicamente un registro capturado por error que nunca haya sido referenciado por `acta_asistencia`.

### 15.4 Docentes y asignaciones

#### `academico.docente`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `nombre` | `nvarchar(200)` | `NOT NULL` | Nombre completo |
| `num_personal` | `varchar(50)` | `NULL` | Mayúsculas; único cuando exista |
| `puesto` | `nvarchar(200)` | `NULL` | Puesto vigente; PLANEA puede dejarlo nulo |
| `descripcion_perfil` | `nvarchar(max)` | `NULL` | Descripción vigente del perfil |

`num_personal` podrá completarse después de crear al Docente. Nombre, puesto y descripción representan únicamente el estado vigente y no se versionan. PLANEA puede crear o actualizar el nombre por número de personal, pero no modifica puesto, descripción, formaciones ni documentos. El registro solo puede eliminarse físicamente si nunca recibió una asignación ni tiene formaciones o documentos.

#### `academico.asignacion_docente`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `programacion_academica_id` | `int` | `NOT NULL` | FK |
| `docente_id` | `int` | `NOT NULL` | FK |
| `origen` | `varchar(10)` | `NOT NULL` | `PLANEA` o `SGPLA` |
| `fecha_inicio` | `date` | `NOT NULL` | Inicio inclusivo |
| `fecha_fin` | `date` | `NULL` | Fin inclusivo |
| `sincronizacion_planea_id` | `int` | `NULL` | Obligatoria para origen `PLANEA` |
| `acta_oferta_id` | `int` | `NULL` | Obligatoria para origen `SGPLA` |

Reglas:

- `fecha_fin >= fecha_inicio` cuando exista.
- Una programación no puede tener dos asignaciones traslapadas.
- Origen `PLANEA` exige `sincronizacion_planea_id` y prohíbe `acta_oferta_id`.
- Origen `SGPLA` exige `acta_oferta_id` y prohíbe `sincronizacion_planea_id`.
- Una `acta_oferta` produce como máximo una asignación.
- Al avalar una designación, la asignación anterior termina el día previo a `aviso.fecha_vacantes` y la nueva inicia en esa fecha con `fecha_fin = NULL`. Si el rango anterior no puede cerrarse válidamente, la operación completa se rechaza.
 
#### `academico.formacion_docente`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `docente_id` | `int` | `NOT NULL` | FK |
| `grado_academico_id` | `int` | `NOT NULL` | FK a `academico.grado_academico` |
| `descripcion` | `nvarchar(500)` | `NOT NULL` | Detalle de la formación |

Un Docente puede tener cero o muchas formaciones y puede registrar varias del mismo grado. Las formaciones no se versionan; al ser designado un Aspirante se copian sus grados pertinentes como nuevos registros.

#### `academico.documento_docente`

Es la cabecera lógica del documento y admite varias cabeceras del mismo tipo.

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `docente_id` | `int` | `NOT NULL` |
| `tipo_documento_expediente_id` | `int` | `NOT NULL` |

#### `academico.version_documento_docente`

Cada fila es una versión documental; el binario permanece en almacenamiento externo.

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `documento_docente_id` | `int` | `NOT NULL` |
| `nombre` | `nvarchar(260)` | `NOT NULL` |
| `mime` | `varchar(255)` | `NOT NULL` |
| `tamano` | `bigint` | `NOT NULL` |
| `checksum_sha256` | `binary(32)` | `NOT NULL` |
| `clave_almacenamiento` | `nvarchar(500)` | `NOT NULL` |
| `numero_version` | `int` | `NOT NULL` |
| `es_vigente` | `bit` | `NOT NULL` |
| `cargado_en` | `datetime2(0)` | `NOT NULL` |
| `cargado_por_usuario_id` | `int` | `NOT NULL` |

Se exige una sola versión vigente por documento lógico, unicidad (documento_docente_id, numero_version), tamaño positivo y clave de almacenamiento globalmente única. Solo los documentos conservan historial; los datos y formaciones del Docente se actualizan directamente.
### 15.5 Ofertas e intentos de publicación

#### `plazas.oferta`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `programacion_academica_id` | `int` | `NOT NULL` | FK inmutable |
| `clave_plaza` | `varchar(100)` | `NOT NULL` | Identificador opaco de la plaza; recortado y en mayúsculas |
| `tipo_plaza_id` | `int` | `NOT NULL` | FK |
| `tipo_contratacion_id` | `int` | `NOT NULL` | FK |
| `perfil_solicitado` | `nvarchar(max)` | `NOT NULL` | Texto libre |
| `justificacion` | `nvarchar(1000)` | `NULL` | Obligatoria si existe docente vigente al crear |
| `estado` | `varchar(20)` | `NOT NULL` | `DISPONIBLE`, `EN_PUBLICACION` o `CUBIERTA` |
| `cerrada_en` | `datetime2(0)` | `NULL` | UTC; se asigna al cubrir definitivamente la Oferta |

NRC, EE, programa, plan, periodo, entidad, sistema educativo y horarios se derivan navegando desde `programacion_academica`. HSM también es derivada: por cada sesión distinta se calcula `CEILING((DATEDIFF(minute, hora_inicio, hora_fin) + 1) / 60.0)` y se suman los bloques. El horario es fijo dentro del periodo.

Solo puede existir una Oferta no cubierta por (`programacion_academica_id`, `clave_plaza`). Un índice único filtrado por `cerrada_en IS NULL` protege esa regla y permite varias plazas independientes para el mismo NRC. Crear la primera Oferta de una programación bloquea futuras actualizaciones de docente desde PLANEA. La Entidad Académica propietaria crea y configura la Oferta. Al agregar la primera relación `aviso_oferta`, se congelan entidad, periodo, sistema educativo y artículo del Aviso. La Oferta no almacena `articulo_id`: hereda el artículo único del Aviso y puede participar después en otro Aviso con un artículo distinto.

`estado = CUBIERTA` exige `cerrada_en IS NOT NULL`; `DISPONIBLE` y `EN_PUBLICACION` exigen `cerrada_en IS NULL`. Una Oferta cubierta nunca se reabre y conserva su historia. Si posteriormente termina la asignación docente, se crea otra Oferta para la misma programación y clave de plaza.

La Oferta no tiene `articulo_id`: el artículo pertenece al intento de publicación representado por el Aviso. La misma Oferta puede republicarse en otro Aviso con un artículo diferente.

#### `plazas.aviso`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `entidad_academica_id` | `int` | `NOT NULL` | Propietaria; inmutable |
| `periodo_escolar_id` | `int` | `NOT NULL` | Ámbito del Aviso |
| `sistema_educativo_id` | `int` | `NOT NULL` | Ámbito del Aviso |
| `articulo_id` | `int` | `NOT NULL` | Fundamento de este intento |
| `tipo_comunicado` | `varchar(15)` | `NOT NULL` | `AVISO` o `CONVOCATORIA` |
| `modalidad_recepcion_id` | `int` | `NULL` | Obligatoria antes de enviar |
| `requisitos` | `nvarchar(max)` | `NULL` | Texto plano; obligatorio antes de enviar |
| `lugar_recepcion` | `nvarchar(500)` | `NULL` | Obligatorio si la modalidad requiere lugar; en otro caso debe ser `NULL` |
| `correo_contacto` | `varchar(254)` | `NULL` | Obligatorio antes de enviar |
| `nombre_titular` | `nvarchar(200)` | `NULL` | Nombre que se presenta en el documento; obligatorio antes de enviar |
| `creado_en` | `datetime2(0)` | `NOT NULL` | UTC |
| `fecha_publicacion` | `date` | `NULL` | Obligatoria al publicar |
| `fecha_consejo_tecnico` | `date` | `NULL` | Obligatoria antes de enviar |
| `fecha_vacantes` | `date` | `NULL` | Obligatoria antes de enviar |
| `url_publicacion` | `varchar(2048)` | `NULL` | URL HTTP/HTTPS obligatoria al publicar |
| `estado` | `varchar(30)` | `NOT NULL` | Ciclo documentado abajo |
| `cancelado_en` | `datetime2(0)` | `NULL` | UTC |
| `cancelado_por_usuario_id` | `int` | `NULL` | Usuario DGAA |
| `motivo_cancelacion` | `nvarchar(1000)` | `NULL` | Obligatorio si está cancelado |
| `archivado_en` | `datetime2(0)` | `NULL` | UTC; marca de archivado operativo |
| `archivado_por_usuario_id` | `int` | `NULL` | Usuario que archivó |

Estados:

```text
CREADO
  -> EN_REVISION_DGAA
  -> DEVUELTO_DGAA -> EN_REVISION_DGAA
  -> AVALADO_DGAA
  -> FIRMADO
  -> PUBLICADO

DEVUELTO_DGAA | AVALADO_DGAA | FIRMADO
  -> CANCELADO
```

La cancelación solo la ejecuta DGAA, exige que no exista revisión abierta y queda prohibida después de publicar. Cierra los intentos abiertos con causa `CANCELADO` y regresa sus Ofertas a `DISPONIBLE`.

`archivado_en` y `archivado_por_usuario_id` son ambos nulos o ambos informados. Solo se archiva un Aviso `PUBLICADO` o `CANCELADO`; archivar no modifica su estado ni elimina historia.

Un Aviso puede contener Ofertas de varios Programas Educativos, pero todas deben derivar la misma entidad, periodo y sistema educativo declarados en el Aviso. La entidad es inmutable desde el alta. Periodo, sistema, artículo, tipo de comunicado y nombre del titular se congelan al primer envío.

#### `plazas.horario_recepcion_requisito`

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `aviso_id` | `int` | `NOT NULL` |
| `fecha` | `date` | `NOT NULL` |
| `hora_inicio` | `time(0)` | `NOT NULL` |
| `hora_fin` | `time(0)` | `NOT NULL` |

Debe cumplirse `hora_inicio < hora_fin`. Un Aviso admite varios intervalos por día, pero no traslapes ni duplicados. Antes de enviar se exige `recepción <= fecha_consejo_tecnico <= fecha_vacantes`. Al publicar se exige además `fecha_publicacion < recepción`.

#### `plazas.aviso_oferta`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `aviso_id` | `int` | `NOT NULL` | FK |
| `oferta_id` | `int` | `NOT NULL` | FK |
| `incorporado_en` | `datetime2(0)` | `NOT NULL` | UTC |
| `cerrado_en` | `datetime2(0)` | `NULL` | `NULL` identifica el intento abierto |
| `causa_cierre` | `varchar(20)` | `NULL` | `DESIGNADA`, `DESIERTA`, `SIN_ASPIRANTES` o `CANCELADO` |

Restricciones:

- Unicidad `(aviso_id, oferta_id)`.
- Índice único filtrado sobre `oferta_id WHERE cerrado_en IS NULL`.
- `cerrado_en` y `causa_cierre` deben ser ambos nulos o ambos no nulos.
- Agregar la relación cambia la Oferta a `EN_PUBLICACION`.
- La aplicación bloquea la Oferta y valida que su entidad, periodo y sistema educativo derivados coincidan con el Aviso; el artículo se hereda del Aviso único.
- El primer `aviso_oferta` congela el ámbito del Aviso. No se permite cambiar entidad, periodo, sistema o artículo después de agregar la primera Oferta.
- El índice único filtrado sobre `oferta_id WHERE cerrado_en IS NULL` impide que una Oferta participe simultáneamente en dos Avisos activos.
- Cerrar por `DESIERTA` o `SIN_ASPIRANTES` regresa la Oferta a `DISPONIBLE`.
- Cerrar por `DESIGNADA` la cambia a `CUBIERTA` y asigna `oferta.cerrada_en` en la misma transacción.
- En `CREADO` o `DEVUELTO_DGAA` se puede retirar físicamente la relación si aún no existe Solicitud ni ActaOferta. Reincorporarla crea otra fila.


### 15.6 Documentos y revisiones del Aviso

#### `plazas.documento_aviso`

Cada fila es una versión documental; el binario vive fuera de SQL Server.

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `aviso_id` | `int` | `NOT NULL` |
| `tipo` | `varchar(10)` | `NOT NULL` |
| `nombre` | `nvarchar(260)` | `NOT NULL` |
| `mime` | `varchar(255)` | `NOT NULL` |
| `tamano` | `bigint` | `NOT NULL` |
| `checksum_sha256` | `binary(32)` | `NOT NULL` |
| `clave_almacenamiento` | `nvarchar(500)` | `NOT NULL` |
| `numero_version` | `int` | `NOT NULL` |
| `es_vigente` | `bit` | `NOT NULL` |
| `cargado_en` | `datetime2(0)` | `NOT NULL` |
| `cargado_por_usuario_id` | `int` | `NOT NULL` |

`tipo` admite `ORIGINAL` y `FIRMADO`. Se exige tamaño positivo, clave de almacenamiento globalmente única, unicidad `(aviso_id, tipo, numero_version)` y una sola versión vigente por Aviso y tipo. Reemplazar un documento crea otra fila y desmarca la vigencia anterior.

#### `plazas.revision_aviso`

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `aviso_id` | `int` | `NOT NULL` |
| `numero_revision` | `int` | `NOT NULL` |
| `documento_original_id` | `int` | `NOT NULL` |
| `enviado_por_usuario_id` | `int` | `NOT NULL` |
| `enviado_en` | `datetime2(0)` | `NOT NULL` |
| `resuelto_por_usuario_id` | `int` | `NULL` |
| `resuelto_en` | `datetime2(0)` | `NULL` |
| `resultado` | `varchar(10)` | `NULL` |
| `comentarios` | `nvarchar(max)` | `NULL` |

`resultado` admite `AVALADA` y `DEVUELTA`. Existe una sola revisión abierta por Aviso y unicidad `(aviso_id, numero_revision)`. Enviar exige una versión `ORIGINAL` vigente y la revisión referencia exactamente esa versión. `DEVUELTA` exige comentarios no vacíos. Pasar a `FIRMADO` exige un `documento_aviso` vigente de tipo `FIRMADO`.

### 15.7 Aspirantes, perfiles y Solicitudes

#### `plazas.aspirante` y perfil versionado

`aspirante` contiene únicamente `id int IDENTITY(1,1)` como identidad estable. Sus datos mutables se versionan en `perfil_aspirante`:

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `aspirante_id` | `int` | `NOT NULL` |
| `numero_version` | `int` | `NOT NULL` |
| `nombre` | `nvarchar(200)` | `NOT NULL` |
| `correo` | `varchar(254)` | `NOT NULL` |
| `puesto_actual` | `nvarchar(200)` | `NULL` |
| `descripcion_perfil` | `nvarchar(max)` | `NOT NULL` |
| `es_vigente` | `bit` | `NOT NULL` |
| `creado_en` | `datetime2(0)` | `NOT NULL` |
| `creado_por_usuario_id` | `int` | `NOT NULL` |

Se exige unicidad `(aspirante_id, numero_version)`, una sola versión vigente por Aspirante y correo único entre perfiles vigentes ignorando mayúsculas. Actualizar nombre, correo, puesto o descripción crea otra versión; no modifica la anterior.

`plazas.formacion_aspirante` contiene `id`, `perfil_aspirante_id`, `grado_academico_id` y `descripcion nvarchar(500)`. Una versión puede incluir varias formaciones del mismo grado.

#### Documentos del Aspirante

`plazas.documento_aspirante` es la cabecera lógica:

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `aspirante_id` | `int` | `NOT NULL` |
| `tipo_documento_expediente_id` | `int` | `NOT NULL` |

Se permiten varias cabeceras del mismo tipo, por ejemplo dos títulos académicos.

`plazas.version_documento_aspirante` contiene `documento_aspirante_id` y los mismos metadatos, SHA-256, versión, vigencia, fecha y usuario de carga definidos para `documento_aviso`. Se exige una sola versión vigente por documento lógico y unicidad `(documento_aspirante_id, numero_version)`.

#### `plazas.solicitud`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `aviso_oferta_id` | `int` | `NOT NULL` | Intento concreto |
| `aspirante_id` | `int` | `NOT NULL` | FK |
| `perfil_aspirante_id` | `int` | `NOT NULL` | Versión evaluada del mismo Aspirante |
| `registrada_en` | `datetime2(0)` | `NOT NULL` | UTC |
| `estado` | `varchar(20)` | `NOT NULL` | `REGISTRADA`, `ADMITIDA`, `NO_ADMITIDA` o `RETIRADA` |
| `observaciones` | `nvarchar(max)` | `NULL` | Propias de este intento |
| `admitida_en` | `datetime2(0)` | `NULL` | UTC |
| `admitida_por_usuario_id` | `int` | `NULL` | Usuario EA |
| `no_admitida_en` | `datetime2(0)` | `NULL` | UTC |
| `no_admitida_por_usuario_id` | `int` | `NULL` | Usuario EA |
| `motivo_no_admision` | `nvarchar(1000)` | `NULL` | Obligatorio en `NO_ADMITIDA` |
| `retirada_en` | `datetime2(0)` | `NULL` | Obligatorio en `RETIRADA` |
| `motivo_retiro` | `nvarchar(1000)` | `NULL` | Opcional |

Restricciones:

- Unicidad `(aviso_oferta_id, aspirante_id)`.
- El perfil debe pertenecer al mismo Aspirante.
- Solo se registra cuando el Aviso está `PUBLICADO` y el instante convertido a la zona institucional cae dentro de un `horario_recepcion_requisito`.
- Mientras está `REGISTRADA` puede cambiar la versión de perfil y sus documentos.
- Al pasar a `ADMITIDA`, perfil y documentos quedan inmutables.
- `NO_ADMITIDA` exige motivo, fecha y actor.
- Una Solicitud `ADMITIDA` puede pasar a `RETIRADA` antes de la sesión y deja de participar en la votación.

`plazas.solicitud_documento` usa PK compuesta `(solicitud_id, version_documento_aspirante_id)`. La versión debe pertenecer al mismo Aspirante y queda fijada aunque después exista otra versión vigente.

Los Aspirantes no se autentican en SGPLa. La Entidad Académica captura los datos recibidos. Otra entidad solo puede consultar un Aspirante cuando exista una Solicitud vinculada a uno de sus Avisos; el alta de la persona y su primera Solicitud es atómica. Una búsqueda exacta por correo puede resolver una identidad existente sin revelar previamente el expediente completo.

### 15.8 Actas, resultados, asistencia y votación

#### `plazas.acta_consejo_tecnico`

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` | PK |
| `aviso_id` | `int` | `NOT NULL` | FK |
| `entidad_academica_id` | `int` | `NOT NULL` | Debe coincidir con la del Aviso |
| `folio` | `varchar(100)` | `NOT NULL` | Opaco, mayúsculas; admite letras, números, guiones y diagonales |
| `fecha` | `date` | `NOT NULL` | Fecha real de sesión |
| `lugar` | `nvarchar(300)` | `NULL` | Espacio o aula; obligatorio antes de enviar |
| `hora_inicio` | `time(0)` | `NULL` | Obligatoria antes de enviar |
| `hora_fin` | `time(0)` | `NULL` | Obligatoria antes de enviar |
| `asuntos_generales` | `nvarchar(max)` | `NULL` | Opcional |
| `estado` | `varchar(30)` | `NOT NULL` | Ciclo documentado abajo |
| `archivado_en` | `datetime2(0)` | `NULL` | UTC; solo después de firmar |
| `archivado_por_usuario_id` | `int` | `NULL` | Usuario que archivó |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` | Solo en `CREADA` |
| `eliminado_por_usuario_id` | `int` | `NULL` | Obligatorio al eliminar |

Estados:

```text
CREADA
  -> EN_REVISION_DGAA
  -> DEVUELTA_DGAA -> EN_REVISION_DGAA
  -> AVALADA_DGAA
  -> FIRMADA
```

El Acta solo se crea para un Aviso `PUBLICADO` en o después de `aviso.fecha_consejo_tecnico`. Su fecha se inicializa con la programada, pero puede corregirse para reflejar la sesión real. La sesión no cruza medianoche y cumple `hora_inicio < hora_fin`.

Existe unicidad filtrada `(entidad_academica_id, folio) WHERE fecha_eliminacion IS NULL`. El folio puede reutilizarse después de eliminar un Acta. La baja lógica solo se permite en `CREADA`, no se restaura y elimina físicamente sus hijos y objetos externos de borrador. El archivado exige estado `FIRMADA`, conserva el Acta completa y requiere conjuntamente fecha y usuario.

#### `plazas.acta_oferta`

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `acta_consejo_tecnico_id` | `int` | `NOT NULL` |
| `aviso_oferta_id` | `int` | `NOT NULL` |
| `resultado` | `varchar(20)` | `NOT NULL` |
| `observaciones` | `nvarchar(max)` | `NULL` |
| `solicitud_designada_id` | `int` | `NULL` |
| `docente_asignado_id` | `int` | `NULL` |
| `fecha_eliminacion` | `datetime2(0)` | `NULL` |
| `eliminado_por_usuario_id` | `int` | `NULL` |

`resultado` admite `PENDIENTE`, `DESIGNADA`, `DESIERTA` y `SIN_ASPIRANTES`. El Acta y AvisoOferta deben pertenecer al mismo Aviso. Existe como máximo un `acta_oferta` activo por `aviso_oferta`.

Reglas:

- `PENDIENTE` solo existe durante edición; ningún Acta se envía con resultados pendientes.
- `DESIGNADA` exige una Solicitud `ADMITIDA` del mismo AvisoOferta y la referencia a Docente se completa al avalar.
- `DESIERTA` exige que exista al menos una Solicitud, prohíbe designada y exige observaciones.
- `SIN_ASPIRANTES` exige cero Solicitudes, prohíbe designada y exige observaciones.
- Al avalar `DESIGNADA`, la Entidad elige un Docente existente o la operación crea uno con el nombre del perfil evaluado y `num_personal = NULL`. En la misma transacción se guarda `docente_asignado_id`, se crea la asignación, se cierra AvisoOferta y se cubre la Oferta.
- Al avalar `DESIERTA` o `SIN_ASPIRANTES`, se cierra el intento y la Oferta vuelve a `DISPONIBLE` aunque la firma del Acta se complete después.
- Un Acta `DEVUELTA_DGAA` puede retirar una ActaOferta mediante baja lógica. Sus votos permanecen como historia inactiva y AvisoOferta puede incorporarse a otra Acta.

#### `plazas.votacion_solicitud`

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `acta_oferta_id` | `int` | `NOT NULL` |
| `solicitud_id` | `int` | `NOT NULL` |
| `votos` | `int` | `NOT NULL` |

PK `(acta_oferta_id, solicitud_id)`. La Solicitud debe estar `ADMITIDA` y pertenecer al mismo AvisoOferta. Antes de enviar existe una fila para cada Solicitud admitida, incluso con cero votos. Los votos son no negativos, su suma no supera integrantes asistentes y la Solicitud designada debe tener más de cero votos y ser el máximo único, sin empate.

#### `plazas.acta_asistencia`

| Columna | Tipo | Nulabilidad |
|---|---|---|
| `id` | `int IDENTITY(1,1)` | `NOT NULL` |
| `acta_consejo_tecnico_id` | `int` | `NOT NULL` |
| `integrante_consejo_tecnico_id` | `int` | `NOT NULL` |
| `nombre` | `nvarchar(200)` | `NOT NULL` |
| `tratamiento` | `nvarchar(30)` | `NOT NULL` |
| `cargo` | `nvarchar(200)` | `NOT NULL` |
| `asistio` | `bit` | `NOT NULL` |
| `firmo` | `bit` | `NOT NULL` |

Unicidad por Acta e integrante. Se copian todos los integrantes vigentes en la fecha de sesión. Si la fecha cambia mientras el Acta está `CREADA` o `DEVUELTA_DGAA`, la lista se regenera antes del envío. Los textos son snapshots inmutables después de enviar. `firmo = 1` exige `asistio = 1`. Toda Acta enviada requiere al menos un asistente; no se calcula quórum porcentual.

### 15.9 Documentos y revisiones del Acta

`plazas.documento_acta` tiene la misma estructura y reglas de versión que `documento_aviso`, sustituyendo `aviso_id` por `acta_consejo_tecnico_id`. Conserva versiones `ORIGINAL` y `FIRMADO`, una vigente por tipo, metadatos, SHA-256, clave externa, usuario y fecha de carga.

`plazas.revision_acta` tiene la misma estructura de ciclo que `revision_aviso`:

- Acta y número de revisión único.
- Versión exacta del documento `ORIGINAL` enviado.
- Usuario y fecha de envío.
- Usuario y fecha de resolución.
- Resultado `AVALADA` o `DEVUELTA`.
- Comentarios obligatorios para devolución.
- Una sola revisión abierta por Acta.

Enviar exige documento original vigente, resultados finales, asistencia y votaciones consistentes. El aval es irreversible y aplica atómicamente todos los cierres y asignaciones de sus ActaOferta; cualquier fallo revierte el Acta completa. Pasar a `FIRMADA` exige documento firmado vigente.

### 15.10 Mutabilidad, eliminación y autorización

- `aviso` en `CREADO` se elimina físicamente con `aviso_oferta`, horarios y documentos de borrador. La operación se bloquea si existe cualquier Solicitud, revisión o Acta. Ofertas y Aspirantes sobreviven.
- Un Aviso enviado nunca se elimina. Antes de `PUBLICADO` DGAA puede cancelarlo con motivo, fecha y actor, siempre que no exista revisión abierta.
- `acta_consejo_tecnico` solo admite baja lógica en `CREADA`. No se restaura.
- Avisos y Actas archivados conservan todas sus relaciones; el archivado es una condición operativa y no equivale a eliminación.
- `acta_oferta` retirada de un Acta devuelta conserva baja lógica e historial; su padre sigue operativo.
- Oferta, Aspirante, Docente e integrante del Consejo solo se eliminan físicamente si nunca fueron referenciados.
- Los documentos y perfiles históricos referidos por Solicitudes o revisiones nunca se eliminan ni se sobrescriben.
- Entidad Académica crea Ofertas, Avisos, Aspirantes, Solicitudes y Actas dentro de su entidad; envía documentos y firma/publica.
- DGAA solo revisa o cancela Avisos y revisa Actas de entidades pertenecientes a su Área Académica.
- Superusuario conserva acceso global.
- Aspirante, Docente e integrante del Consejo no son identidades de autenticación.
- Las reglas entre estados, perfiles exclusivos, ámbitos, traslapes, snapshots y operaciones atómicas se aplican en la capa de aplicación; PK, FK, CHECK, unicidad e índices filtrados protegen los invariantes expresables en SQL Server.

### 15.11 Índices y restricciones principales

Además de PK y FK:

- `uq_docente__num_personal`, filtrado por `num_personal IS NOT NULL`.
- Índices de `asignacion_docente` por programación, docente, sincronización y ActaOferta.
- Índice único filtrado de Oferta abierta por (`programacion_academica_id`, `clave_plaza`) donde `cerrada_en IS NULL`.
- `uq_aviso_oferta__aviso_oferta` y unicidad filtrada de intento abierto por Oferta.
- Índices de Aviso por entidad, periodo, sistema, artículo y estado.
- Índices de Docente por sus claves de relación.
- CHECK de `aviso.tipo_comunicado` y consistencia conjunta de sus datos de cancelación y archivado.
- Índices de horarios de recepción por Aviso y fecha.
- Unicidad de versiones y una versión vigente por padre y tipo documental.
- Una revisión abierta por Aviso o Acta.
- Correo único entre perfiles vigentes y una versión de perfil vigente por Aspirante.
- `uq_solicitud__aviso_oferta_aspirante`.
- Unicidad filtrada de folio de Acta activa por entidad.
- Unicidad filtrada de ActaOferta activa por AvisoOferta.
- Índices de Acta por Aviso, entidad, fecha y estado.
- CHECK de consistencia conjunta para archivado y baja lógica de Acta.
- Índices de Solicitud por Aspirante, AvisoOferta y estado.

### 15.12 Casos de aceptación del proceso

- Crear una Oferta desde una programación sin docente y también con docente cuando existe justificación.
- Impedir dos Ofertas abiertas para la misma clave de plaza y NRC/periodo, permitiendo plazas distintas en el mismo NRC.
- Agrupar en un Aviso Ofertas de programas diferentes con la misma entidad, periodo y sistema educativo.
- Rechazar una Oferta cuyo ámbito derivado no coincida con el Aviso.
- Impedir cambiar entidad, periodo, sistema o artículo de un Aviso después de agregar su primera Oferta.
- Republicar una Oferta cerrada por `DESIERTA` o `SIN_ASPIRANTES` en otro Aviso con artículo diferente.
- Impedir dos Avisos activos para una misma Oferta.
- Rechazar una Oferta incompatible y dos intentos abiertos de la misma Oferta.
- Eliminar físicamente un Aviso `CREADO` sin eliminar sus Ofertas ni Aspirantes.
- Bloquear su eliminación después de una Solicitud, revisión o Acta.
- Cancelar antes de publicar únicamente por DGAA, sin revisión abierta, y liberar las Ofertas.
- Rechazar cancelación después de `PUBLICADO`.
- Aceptar únicamente `AVISO` o `CONVOCATORIA` como tipo de comunicado y exigir titular antes del primer envío.
- Archivar únicamente Avisos terminales y Actas firmadas sin eliminar ni modificar sus relaciones.
- Validar intervalos de recepción sin traslapes y rechazar Solicitudes fuera de ellos.
- Reutilizar un Aspirante por correo en Avisos y entidades diferentes sin alterar perfiles históricos.
- Admitir múltiples formaciones del mismo grado y múltiples documentos del mismo tipo.
- Congelar versión de perfil y documentos al admitir la Solicitud.
- Rechazar una segunda Solicitud del mismo Aspirante para el mismo AvisoOferta.
- Crear un Acta que resuelva varias Ofertas del mismo Aviso.
- Rechazar folio activo duplicado por entidad y permitir reutilizarlo tras eliminar un Acta creada.
- Verificar snapshots de nombre, tratamiento y cargo aunque cambie después el padrón.
- Rechazar firma de un integrante ausente, Actas sin asistentes y votaciones mayores que la asistencia.
- Exigir una votación por Solicitud admitida y un máximo único positivo para `DESIGNADA`.
- Exigir cero Solicitudes para `SIN_ASPIRANTES` y observaciones para resultados sin designación.
- Avalar `DESIERTA` o `SIN_ASPIRANTES`, cerrar el intento y republicar la Oferta en otro Aviso.
- Avalar `DESIGNADA` y comprobar creación/reutilización de Docente, cierre de la asignación anterior, nueva asignación y Oferta `CUBIERTA` dentro de una transacción.
- Conservar una nueva Oferta separada si la programación vuelve a quedar vacante.
- Conservar todas las versiones de documentos y vincular cada revisión con el original evaluado.
- Rechazar devolución sin comentarios y una segunda revisión abierta.
- Verificar que los binarios no se almacenen en SQL Server y que SHA-256 detecte alteraciones.
- Crear un Docente con puesto nulo desde PLANEA, agregar grados y versionar sus archivos.
- Copiar perfil y grados al designar un Aspirante sin copiar sus archivos.

## 16. Solicitudes de apertura académica

### 16.1 Propósito y alcance

academico.solicitud_apertura representa una petición formal de una Entidad Académica hacia su Área Académica para autorizar la apertura de una sección de una Experiencia Educativa en el periodo siguiente configurado institucionalmente.

No debe confundirse con plazas.solicitud, que representa la postulación de un Aspirante a una Oferta de plaza docente.

La solicitud almacena únicamente experiencia_educativa_id. El Plan de Estudios, Programa Educativo, Entidad Académica y Sistema Educativo se derivan de la cadena académica inmutable. La modalidad mostrada corresponde al Sistema Educativo del Programa. No se almacenan copias de nombres ni una lista de estudiantes: únicamente se registra la cantidad de estudiantes interesados.

La aplicación recibe desde configuración externa una pareja global de periodo actual y periodo siguiente. Al crear la solicitud debe comprobar que el periodo solicitado coincide con el siguiente configurado y que ambos periodos estén activos. El cambio posterior de esa configuración no invalida solicitudes existentes.

### 16.2 academico.solicitud_apertura

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| id | int IDENTITY(1,1) | NOT NULL | PK e identificador técnico |
| experiencia_educativa_id | int | NOT NULL | FK a academico.experiencia_educativa |
| periodo_escolar_id | int | NOT NULL | FK al periodo objetivo |
| seccion | varchar(20) | NOT NULL | Código de letras y dígitos ASCII, normalizado a mayúsculas |
| cantidad_estudiantes | int | NOT NULL | Entero positivo |
| justificacion | nvarchar(max) | NOT NULL | Texto no vacío |
| oficio_respaldo_id | int | NOT NULL | FK única a academico.archivo_solicitud_apertura |
| estado | varchar(20) | NOT NULL | PENDIENTE, ACEPTADA, RECHAZADA o CANCELADA |
| creada_en | datetime2(0) | NOT NULL | UTC |
| creada_por_usuario_id | int | NOT NULL | Usuario EA creador |
| actualizada_en | datetime2(0) | NULL | UTC de la última edición |
| actualizada_por_usuario_id | int | NULL | Usuario EA de la última edición |
| resuelta_en | datetime2(0) | NULL | UTC de aceptación o rechazo |
| resuelta_por_usuario_id | int | NULL | Usuario DGAA que resolvió |
| comentarios_resolucion | nvarchar(max) | NULL | Obligatorios para rechazo; opcionales para aceptación |
| cancelada_en | datetime2(0) | NULL | UTC de cancelación |
| cancelada_por_usuario_id | int | NULL | Usuario EA que canceló |
| motivo_cancelacion | nvarchar(1000) | NULL | Obligatorio al cancelar |
| programacion_academica_id | int | NULL | Vinculación posterior, FK única filtrada |
| vinculada_en | datetime2(0) | NULL | UTC del vínculo |
| vinculada_por_usuario_id | int | NULL | Usuario EA que vinculó |

No tiene fecha_eliminacion: los estados terminales conservan la petición y su oficio.

Restricciones:

- ck_solicitud_apertura__estado.
- ck_solicitud_apertura__seccion_formato: no vacía y solo A-Z o 0-9.
- ck_solicitud_apertura__cantidad_positiva.
- ck_solicitud_apertura__justificacion_no_vacia.
- Los campos de actor y fecha se informan conjuntamente.
- PENDIENTE no tiene resolución, cancelación ni programación vinculada.
- ACEPTADA tiene resolución y puede tener, opcionalmente, una vinculación completa.
- RECHAZADA tiene resolución y comentarios no vacíos, sin cancelación ni programación.
- CANCELADA tiene cancelación y motivo no vacío, sin resolución ni programación.
- Una vinculación siempre ocurre después de la resolución y sus tres campos se informan conjuntamente.

Índices y unicidad:

- Índice único filtrado (experiencia_educativa_id, periodo_escolar_id, seccion) WHERE estado = 'PENDIENTE'.
- Índice único filtrado programacion_academica_id WHERE programacion_academica_id IS NOT NULL.
- Índices por Experiencia/estado, periodo/estado y usuarios actores.

### 16.3 academico.archivo_solicitud_apertura

Es el archivo único del oficio de respaldo. El contenido binario vive en almacenamiento externo.

| Columna | Tipo | Nulabilidad | Notas |
|---|---|---|---|
| id | int IDENTITY(1,1) | NOT NULL | PK |
| nombre | nvarchar(260) | NOT NULL | Nombre no vacío |
| mime | varchar(255) | NOT NULL | Debe ser application/pdf |
| tamano | bigint | NOT NULL | Mayor que cero; sin máximo fijo |
| checksum_sha256 | binary(32) | NOT NULL | Integridad del binario |
| clave_almacenamiento | nvarchar(500) | NOT NULL | Única |
| cargado_en | datetime2(0) | NOT NULL | UTC |
| cargado_por_usuario_id | int | NOT NULL | FK al usuario que cargó |

Cada solicitud tiene exactamente un oficio. Al reemplazarlo en una solicitud pendiente se registra el nuevo archivo, se actualiza la FK y se elimina el archivo anterior cuando queda sin referencias. No se conservan versiones. Las solicitudes terminales conservan su oficio.

### 16.4 Autorización y ciclo de vida

Solo un usuario EA activo cuya entidad coincida con la Entidad derivada de la EE puede crear, editar, cancelar o vincular.

Cualquier usuario EA activo de la misma entidad puede editar mientras la solicitud esté pendiente. Solo pueden cambiar cantidad_estudiantes, justificacion y oficio_respaldo_id; cada edición actualiza actualizada_en y actualizada_por_usuario_id.

Una solicitud se crea en PENDIENTE y sigue este ciclo terminal:

PENDIENTE -> ACEPTADA
PENDIENTE -> RECHAZADA
PENDIENTE -> CANCELADA

Cualquier DGAA activa del Área Académica derivada puede aceptar o rechazar. Aceptar exige que ambos cupos vigentes de la EE existan y que la cantidad esté dentro del intervalo inclusivo. Crear, editar y aceptar usan los cupos vigentes; si un cambio de cupos deja una pendiente fuera de rango, la aceptación queda bloqueada hasta corregirla.

Cancelar solo está permitido desde PENDIENTE, requiere motivo y es ejecutado por un usuario EA del mismo ámbito. ACEPTADA, RECHAZADA y CANCELADA no se editan ni revierten. El Superusuario no tiene acceso a este proceso.

### 16.5 Vinculación posterior

Una solicitud ACEPTADA puede vincularse con una academico.programacion_academica activa. La programación debe corresponder a la misma EE y periodo; la existencia de otros NRC no bloquea la solicitud.

La relación es uno a uno en ambos sentidos, se registra con usuario y fecha, y es inmutable. No crea ni modifica la Programación Académica, no agrega seccion a esa tabla y no vuelve a validar cupos.

La baja de una Programación vinculada está bloqueada.

### 16.6 Bajas e integridad de padres

La baja de una EE, Plan, Programa, Entidad o Periodo se bloquea mientras exista una Solicitud de Apertura PENDIENTE o ACEPTADA. Para una solicitud aceptada la prohibición permanece incluso después de vincular la Programación.

Las solicitudes RECHAZADAS y CANCELADAS no impiden la baja. Las operaciones de creación, edición, resolución, cancelación, vinculación y baja se ejecutan transaccionalmente cuando afectan varias filas o validaciones cruzadas.

## 17. Decisiones pendientes

No existen decisiones funcionales pendientes para implementar este modelo base. Aún deben realizarse como trabajo posterior:

- datos iniciales de los catálogos no incluidos en `Baseline/seed.sql`: `area_academica`, `entidad_academica` y `periodo_escolar` los registra el Superusuario, y los catálogos fijos `tipo_documento_expediente`, `modalidad_recepcion`, `tipo_plaza` y `tipo_contratacion` siguen sin valores definidos;
- implementación transaccional de bajas, restauraciones e inmutabilidad;
- implementación del adaptador y la sincronización atómica con PLANEA, incluidos Docentes y asignaciones iniciales;
- implementación del bootstrap, autenticación LDAP y verificación local de Superusuarios;
- implementación transaccional de los ciclos de Aviso y Acta, revisiones, votación, designación y republicación;
- definición del almacenamiento externo de binarios (Avisos, Actas, Docentes y Solicitudes de Apertura; el plan de estudios no tiene archivo) y del proceso seguro para confirmar o compensar cargas;
- confirmación institucional de si se requiere criptografía FIPS antes de implementar el hash local;
- pruebas de integración contra SQL Server;
- revisión de índices con datos y consultas representativas.
