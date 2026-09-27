-- SGPLa: baseline de la base de datos.
-- Crea los esquemas y las 55 tablas de DATABASE.md, sin datos (los catálogos
-- se cargan en seed.sql). Solo se ejecuta sobre una base vacía; los cambios de
-- esquema posteriores van en Scripts/, no aquí.
--
-- Nombres (DATABASE.md §5): pk_, fk_<hija>__<padre>, uq_, ck_, ix_ y ux_ para
-- índices únicos filtrados. Varias FK a usuarios.usuario: fk_<hija>__usuario__<papel>.
-- En los CHECK, LEN(x) = DATALENGTH(x) impide espacios finales en un varchar.
-- Las reglas entre tablas y de estado se validan en la aplicación.

-- Requeridas por los índices filtrados (sqlcmd las desactiva por omisión).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Esquemas

IF SCHEMA_ID(N'academico') IS NULL EXEC (N'CREATE SCHEMA academico AUTHORIZATION dbo;');
IF SCHEMA_ID(N'plazas') IS NULL EXEC (N'CREATE SCHEMA plazas AUTHORIZATION dbo;');
IF SCHEMA_ID(N'usuarios') IS NULL EXEC (N'CREATE SCHEMA usuarios AUTHORIZATION dbo;');
IF SCHEMA_ID(N'integracion') IS NULL EXEC (N'CREATE SCHEMA integracion AUTHORIZATION dbo;');
GO

-- 2. Usuarios: rol y cuenta (§6.16-6.17). Van primero porque otras tablas los referencian.

CREATE TABLE usuarios.rol
(
    id     tinyint       NOT NULL,
    nombre nvarchar(100) NOT NULL,
    CONSTRAINT pk_rol PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_rol__nombre UNIQUE (nombre),
    CONSTRAINT ck_rol__catalogo_fijo CHECK (
           (id = 1 AND nombre COLLATE Latin1_General_100_BIN2 = N'Superusuario')
        OR (id = 2 AND nombre COLLATE Latin1_General_100_BIN2 = N'DGAA')
        OR (id = 3 AND nombre COLLATE Latin1_General_100_BIN2 = N'Entidad Académica'))
);

CREATE TABLE usuarios.usuario
(
    id                int IDENTITY(1,1) NOT NULL,
    correo            varchar(254) COLLATE Latin1_General_100_CI_AI NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    rol_id            tinyint           NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_usuario PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_usuario__rol FOREIGN KEY (rol_id) REFERENCES usuarios.rol (id),
    CONSTRAINT ck_usuario__correo_formato CHECK (
            DATALENGTH(correo) = LEN(correo)
        AND correo LIKE '_%@_%._%'
        AND correo NOT LIKE '%@%@%'
        AND CHARINDEX(' ', correo) = 0
        AND correo COLLATE Latin1_General_100_BIN2 = LOWER(correo)),
    CONSTRAINT ck_usuario__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_usuario__correo_institucional CHECK (rol_id = 1 OR correo LIKE '%_@uv.mx')
);

CREATE UNIQUE INDEX ux_usuario__correo_activo ON usuarios.usuario (correo) WHERE fecha_eliminacion IS NULL;
CREATE INDEX ix_usuario__rol_id ON usuarios.usuario (rol_id);

-- 3. Académico: estructura institucional (§6.1-6.5)

CREATE TABLE academico.region
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             int               NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_region PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_region__clave UNIQUE (clave),
    CONSTRAINT ck_region__clave_positiva CHECK (clave > 0),
    CONSTRAINT ck_region__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.campus
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             varchar(50)       NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    region_id         int               NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_campus PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_campus__clave UNIQUE (clave),
    CONSTRAINT fk_campus__region FOREIGN KEY (region_id) REFERENCES academico.region (id),
    CONSTRAINT ck_campus__clave_formato CHECK (
            LEN(clave) > 0
        AND DATALENGTH(clave) = LEN(clave)
        AND clave COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_campus__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE INDEX ix_campus__region_id ON academico.campus (region_id);

CREATE TABLE academico.area_academica
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             int               NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    telefono          char(10)          NOT NULL,
    extension         varchar(10)       NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_area_academica PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_area_academica__clave UNIQUE (clave),
    CONSTRAINT ck_area_academica__clave_positiva CHECK (clave > 0),
    CONSTRAINT ck_area_academica__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_area_academica__telefono_formato CHECK (
            LEN(telefono) = 10
        AND telefono COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_area_academica__extension_formato CHECK (
           extension IS NULL
        OR (    LEN(extension) > 0
            AND DATALENGTH(extension) = LEN(extension)
            AND extension COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'))
);

CREATE INDEX ix_area_academica__telefono ON academico.area_academica (telefono);

CREATE TABLE academico.municipio
(
    id     int IDENTITY(1,1) NOT NULL,
    nombre nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    CONSTRAINT pk_municipio PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_municipio__nombre UNIQUE (nombre),
    CONSTRAINT ck_municipio__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.entidad_academica
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             varchar(50)       NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    calle             nvarchar(200) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    numero_exterior   nvarchar(20)  COLLATE Modern_Spanish_100_CI_AI NULL,
    colonia           nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    codigo_postal     char(5)           NOT NULL,
    telefono          char(10)          NOT NULL,
    extension         varchar(10)       NULL,
    campus_id         int               NOT NULL,
    area_academica_id int               NOT NULL,
    municipio_id      int               NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_entidad_academica PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_entidad_academica__clave UNIQUE (clave),
    CONSTRAINT fk_entidad_academica__campus FOREIGN KEY (campus_id) REFERENCES academico.campus (id),
    CONSTRAINT fk_entidad_academica__area_academica FOREIGN KEY (area_academica_id) REFERENCES academico.area_academica (id),
    CONSTRAINT fk_entidad_academica__municipio FOREIGN KEY (municipio_id) REFERENCES academico.municipio (id),
    CONSTRAINT ck_entidad_academica__clave_formato CHECK (
            LEN(clave) > 0
        AND DATALENGTH(clave) = LEN(clave)
        AND clave COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_entidad_academica__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_entidad_academica__direccion_no_vacia CHECK (LEN(TRIM(calle)) > 0 AND LEN(TRIM(colonia)) > 0),
    CONSTRAINT ck_entidad_academica__numero_exterior CHECK (numero_exterior IS NULL OR LEN(TRIM(numero_exterior)) > 0),
    CONSTRAINT ck_entidad_academica__codigo_postal_formato CHECK (
            LEN(codigo_postal) = 5
        AND codigo_postal COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_entidad_academica__telefono_formato CHECK (
            LEN(telefono) = 10
        AND telefono COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_entidad_academica__extension_formato CHECK (
           extension IS NULL
        OR (    LEN(extension) > 0
            AND DATALENGTH(extension) = LEN(extension)
            AND extension COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'))
);

CREATE INDEX ix_entidad_academica__campus_id ON academico.entidad_academica (campus_id);
CREATE INDEX ix_entidad_academica__area_academica_id ON academico.entidad_academica (area_academica_id);
CREATE INDEX ix_entidad_academica__municipio_id ON academico.entidad_academica (municipio_id);
CREATE INDEX ix_entidad_academica__calle ON academico.entidad_academica (calle);
CREATE INDEX ix_entidad_academica__numero_exterior ON academico.entidad_academica (numero_exterior) WHERE numero_exterior IS NOT NULL;
CREATE INDEX ix_entidad_academica__colonia ON academico.entidad_academica (colonia);
CREATE INDEX ix_entidad_academica__codigo_postal ON academico.entidad_academica (codigo_postal);
CREATE INDEX ix_entidad_academica__telefono ON academico.entidad_academica (telefono);

-- 4. Académico: oferta educativa (§6.6-6.13)

CREATE TABLE academico.sistema_educativo
(
    id                int IDENTITY(1,1) NOT NULL,
    nombre            nvarchar(200) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_sistema_educativo PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_sistema_educativo__nombre UNIQUE (nombre),
    CONSTRAINT ck_sistema_educativo__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.nivel_formacion
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             varchar(50)       NOT NULL,
    nombre            nvarchar(200) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_nivel_formacion PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_nivel_formacion__clave UNIQUE (clave),
    CONSTRAINT uq_nivel_formacion__nombre UNIQUE (nombre),
    CONSTRAINT ck_nivel_formacion__clave_formato CHECK (
            LEN(clave) > 0
        AND DATALENGTH(clave) = LEN(clave)
        AND clave COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_nivel_formacion__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.programa_educativo
(
    id                   int IDENTITY(1,1) NOT NULL,
    nombre               nvarchar(200) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    entidad_academica_id int               NOT NULL,
    sistema_educativo_id int               NOT NULL,
    nivel_formacion_id   int               NOT NULL,
    fecha_eliminacion    datetime2(0)      NULL,
    CONSTRAINT pk_programa_educativo PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_programa_educativo__entidad_nombre_sistema UNIQUE (entidad_academica_id, nombre, sistema_educativo_id),
    CONSTRAINT fk_programa_educativo__entidad_academica FOREIGN KEY (entidad_academica_id) REFERENCES academico.entidad_academica (id),
    CONSTRAINT fk_programa_educativo__sistema_educativo FOREIGN KEY (sistema_educativo_id) REFERENCES academico.sistema_educativo (id),
    CONSTRAINT fk_programa_educativo__nivel_formacion FOREIGN KEY (nivel_formacion_id) REFERENCES academico.nivel_formacion (id),
    CONSTRAINT ck_programa_educativo__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE INDEX ix_programa_educativo__sistema_educativo_id ON academico.programa_educativo (sistema_educativo_id);
CREATE INDEX ix_programa_educativo__nivel_formacion_id ON academico.programa_educativo (nivel_formacion_id);

CREATE TABLE academico.archivo_plan_estudios
(
    id                     int IDENTITY(1,1) NOT NULL,
    nombre                 nvarchar(260)     NOT NULL,
    mime                   varchar(255)      NOT NULL,
    tamano                 bigint            NOT NULL,
    checksum_sha256        binary(32)        NOT NULL,
    clave_almacenamiento   nvarchar(500)     NOT NULL,
    cargado_en             datetime2(0)      NOT NULL,
    cargado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_archivo_plan_estudios PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_archivo_plan_estudios__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_archivo_plan_estudios__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_archivo_plan_estudios__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_archivo_plan_estudios__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_archivo_plan_estudios__mime_no_vacio CHECK (LEN(TRIM(mime)) > 0),
    CONSTRAINT ck_archivo_plan_estudios__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE TABLE academico.plan_estudios
(
    id                       int IDENTITY(1,1) NOT NULL,
    codigo                   varchar(50)       NOT NULL,
    programa_educativo_id    int               NOT NULL,
    archivo_plan_estudios_id int               NOT NULL,
    fecha_eliminacion        datetime2(0)      NULL,
    CONSTRAINT pk_plan_estudios PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_plan_estudios__programa_educativo_id_codigo UNIQUE (programa_educativo_id, codigo),
    -- Sirve también como ix_plan_estudios__archivo_plan_estudios_id (§8).
    CONSTRAINT uq_plan_estudios__archivo_plan_estudios UNIQUE (archivo_plan_estudios_id),
    CONSTRAINT fk_plan_estudios__programa_educativo FOREIGN KEY (programa_educativo_id) REFERENCES academico.programa_educativo (id),
    CONSTRAINT fk_plan_estudios__archivo_plan_estudios FOREIGN KEY (archivo_plan_estudios_id) REFERENCES academico.archivo_plan_estudios (id),
    CONSTRAINT ck_plan_estudios__codigo_no_vacio CHECK (LEN(codigo) > 0 AND DATALENGTH(codigo) = LEN(codigo)),
    -- Admite guiones, p. ej. ISOF-18-ECR.
    CONSTRAINT ck_plan_estudios__codigo_formato CHECK (codigo COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9-]%')
);

CREATE TABLE academico.area_formacion
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             varchar(50)       NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_area_formacion PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_area_formacion__clave UNIQUE (clave),
    CONSTRAINT ck_area_formacion__clave_formato CHECK (
            LEN(clave) > 0
        AND DATALENGTH(clave) = LEN(clave)
        AND clave COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_area_formacion__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.experiencia_educativa
(
    id                int IDENTITY(1,1) NOT NULL,
    nombre            nvarchar(200)     NOT NULL,
    materia_ee        varchar(50)       NOT NULL,
    curso_ee          varchar(50)       NOT NULL,
    horas_teoricas    int               NOT NULL,
    horas_practicas   int               NOT NULL,
    creditos          int               NOT NULL,
    cupo_minimo       int               NULL,
    cupo_maximo       int               NULL,
    perfil_docente    nvarchar(max)     NULL,
    area_formacion_id int               NOT NULL,
    plan_estudios_id  int               NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_experiencia_educativa PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_experiencia_educativa__plan_materia_curso UNIQUE (plan_estudios_id, materia_ee, curso_ee),
    CONSTRAINT fk_experiencia_educativa__area_formacion FOREIGN KEY (area_formacion_id) REFERENCES academico.area_formacion (id),
    CONSTRAINT fk_experiencia_educativa__plan_estudios FOREIGN KEY (plan_estudios_id) REFERENCES academico.plan_estudios (id),
    CONSTRAINT ck_experiencia_educativa__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_experiencia_educativa__materia_ee_formato CHECK (
            LEN(materia_ee) > 0
        AND DATALENGTH(materia_ee) = LEN(materia_ee)
        AND materia_ee COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_experiencia_educativa__curso_ee_formato CHECK (
            LEN(curso_ee) > 0
        AND DATALENGTH(curso_ee) = LEN(curso_ee)
        AND curso_ee COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_experiencia_educativa__horas_no_negativas CHECK (horas_teoricas >= 0 AND horas_practicas >= 0),
    CONSTRAINT ck_experiencia_educativa__creditos_positivos CHECK (creditos > 0),
    CONSTRAINT ck_experiencia_educativa__cupos CHECK (
            (cupo_minimo IS NULL OR cupo_minimo >= 0)
        AND (cupo_maximo IS NULL OR cupo_maximo >= 0)
        AND (cupo_minimo IS NULL OR cupo_maximo IS NULL OR cupo_minimo <= cupo_maximo))
);

CREATE INDEX ix_experiencia_educativa__area_formacion_id ON academico.experiencia_educativa (area_formacion_id);

CREATE TABLE academico.periodo_escolar
(
    id                int IDENTITY(1,1) NOT NULL,
    clave             char(6)           NOT NULL,
    fecha_inicio      date              NOT NULL,
    fecha_fin         date              NOT NULL,
    fecha_eliminacion datetime2(0)      NULL,
    CONSTRAINT pk_periodo_escolar PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_periodo_escolar__clave UNIQUE (clave),
    CONSTRAINT ck_periodo_escolar__clave_formato CHECK (
            LEN(clave) = 6
        AND clave COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
    CONSTRAINT ck_periodo_escolar__rango_fechas CHECK (fecha_inicio <= fecha_fin)
);

CREATE TABLE academico.programacion_academica
(
    id                       int IDENTITY(1,1) NOT NULL,
    nrc                      varchar(20)       NOT NULL,
    periodo_escolar_id       int               NOT NULL,
    experiencia_educativa_id int               NOT NULL,
    fecha_eliminacion        datetime2(0)      NULL,
    CONSTRAINT pk_programacion_academica PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_programacion_academica__periodo_escolar_id_nrc UNIQUE (periodo_escolar_id, nrc),
    CONSTRAINT fk_programacion_academica__periodo_escolar FOREIGN KEY (periodo_escolar_id) REFERENCES academico.periodo_escolar (id),
    CONSTRAINT fk_programacion_academica__experiencia_educativa FOREIGN KEY (experiencia_educativa_id) REFERENCES academico.experiencia_educativa (id),
    CONSTRAINT ck_programacion_academica__nrc_formato CHECK (
            LEN(nrc) > 0
        AND DATALENGTH(nrc) = LEN(nrc)
        AND nrc COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%')
);

CREATE INDEX ix_programacion_academica__experiencia_educativa_id ON academico.programacion_academica (experiencia_educativa_id);

-- 5. Integración con PLANEA y horarios (§6.14-6.15)

CREATE TABLE integracion.sincronizacion_planea
(
    id                     int IDENTITY(1,1) NOT NULL,
    periodo_escolar_id     int               NOT NULL,
    estado                 varchar(20)       NOT NULL,
    iniciada_en            datetime2(0)      NOT NULL,
    finalizada_en          datetime2(0)      NULL,
    registros_recibidos    int               NOT NULL CONSTRAINT df_sincronizacion_planea__registros_recibidos DEFAULT (0),
    registros_ignorados    int               NOT NULL CONSTRAINT df_sincronizacion_planea__registros_ignorados DEFAULT (0),
    sesiones_generadas     int               NOT NULL CONSTRAINT df_sincronizacion_planea__sesiones_generadas DEFAULT (0),
    duplicados_descartados int               NOT NULL CONSTRAINT df_sincronizacion_planea__duplicados_descartados DEFAULT (0),
    advertencias           int               NOT NULL CONSTRAINT df_sincronizacion_planea__advertencias DEFAULT (0),
    mensaje_error          nvarchar(4000)    NULL,
    CONSTRAINT pk_sincronizacion_planea PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_sincronizacion_planea__periodo_escolar FOREIGN KEY (periodo_escolar_id) REFERENCES academico.periodo_escolar (id),
    CONSTRAINT ck_sincronizacion_planea__estado CHECK (estado IN ('EN_PROCESO', 'EXITOSA', 'FALLIDA')),
    CONSTRAINT ck_sincronizacion_planea__conteos_no_negativos CHECK (
            registros_recibidos >= 0
        AND registros_ignorados >= 0
        AND sesiones_generadas >= 0
        AND duplicados_descartados >= 0
        AND advertencias >= 0),
    CONSTRAINT ck_sincronizacion_planea__fechas_estado CHECK (
           (estado = 'EN_PROCESO' AND finalizada_en IS NULL)
        OR (estado IN ('EXITOSA', 'FALLIDA') AND finalizada_en IS NOT NULL AND finalizada_en >= iniciada_en))
);

CREATE UNIQUE INDEX ux_sincronizacion_planea__periodo_en_proceso ON integracion.sincronizacion_planea (periodo_escolar_id) WHERE estado = 'EN_PROCESO';
CREATE INDEX ix_sincronizacion_planea__periodo_inicio ON integracion.sincronizacion_planea (periodo_escolar_id, iniciada_en);

CREATE TABLE academico.horario_programacion
(
    id                        int IDENTITY(1,1) NOT NULL,
    programacion_academica_id int               NOT NULL,
    sincronizacion_planea_id  int               NOT NULL,
    dia_semana                tinyint           NOT NULL,
    hora_inicio               time(0)           NOT NULL,
    hora_fin                  time(0)           NOT NULL,
    fecha_inicio              date              NOT NULL,
    fecha_fin                 date              NOT NULL,
    edificio                  nvarchar(100)     NULL,
    aula                      nvarchar(100)     NULL,
    CONSTRAINT pk_horario_programacion PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_horario_programacion__programacion_academica FOREIGN KEY (programacion_academica_id) REFERENCES academico.programacion_academica (id),
    CONSTRAINT fk_horario_programacion__sincronizacion_planea FOREIGN KEY (sincronizacion_planea_id) REFERENCES integracion.sincronizacion_planea (id),
    CONSTRAINT ck_horario_programacion__dia_semana CHECK (dia_semana BETWEEN 1 AND 6),
    CONSTRAINT ck_horario_programacion__rango_horas CHECK (hora_inicio < hora_fin),
    CONSTRAINT ck_horario_programacion__rango_fechas CHECK (fecha_inicio <= fecha_fin),
    CONSTRAINT ck_horario_programacion__espacio_no_vacio CHECK (
            (edificio IS NULL OR LEN(TRIM(edificio)) > 0)
        AND (aula IS NULL OR LEN(TRIM(aula)) > 0)),
    CONSTRAINT uq_horario_programacion__sesion UNIQUE (
        programacion_academica_id, dia_semana, hora_inicio, hora_fin, fecha_inicio, fecha_fin, edificio, aula)
);

CREATE INDEX ix_horario_programacion__sincronizacion_planea_id ON academico.horario_programacion (sincronizacion_planea_id);

-- 6. Usuarios: perfiles de ámbito y credencial local (§6.18-6.20)

CREATE TABLE usuarios.usuario_dgaa
(
    usuario_id        int NOT NULL,
    area_academica_id int NOT NULL,
    CONSTRAINT pk_usuario_dgaa PRIMARY KEY CLUSTERED (usuario_id),
    CONSTRAINT fk_usuario_dgaa__usuario FOREIGN KEY (usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_usuario_dgaa__area_academica FOREIGN KEY (area_academica_id) REFERENCES academico.area_academica (id)
);

CREATE INDEX ix_usuario_dgaa__area_academica_id ON usuarios.usuario_dgaa (area_academica_id);

CREATE TABLE usuarios.usuario_entidad_academica
(
    usuario_id           int NOT NULL,
    entidad_academica_id int NOT NULL,
    CONSTRAINT pk_usuario_entidad_academica PRIMARY KEY CLUSTERED (usuario_id),
    CONSTRAINT fk_usuario_entidad_academica__usuario FOREIGN KEY (usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_usuario_entidad_academica__entidad_academica FOREIGN KEY (entidad_academica_id) REFERENCES academico.entidad_academica (id)
);

CREATE INDEX ix_usuario_entidad_academica__entidad_academica_id ON usuarios.usuario_entidad_academica (entidad_academica_id);

CREATE TABLE usuarios.credencial_superusuario
(
    usuario_id          int          NOT NULL,
    contrasena          varchar(500) NOT NULL,
    fecha_actualizacion datetime2(0) NULL,
    fecha_eliminacion   datetime2(0) NULL,
    CONSTRAINT pk_credencial_superusuario PRIMARY KEY CLUSTERED (usuario_id),
    CONSTRAINT fk_credencial_superusuario__usuario FOREIGN KEY (usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_credencial_superusuario__contrasena_no_vacia CHECK (LEN(TRIM(contrasena)) > 0)
);

-- 7. Académico: solicitudes de apertura (§16.2-16.3)

CREATE TABLE academico.archivo_solicitud_apertura
(
    id                     int IDENTITY(1,1) NOT NULL,
    nombre                 nvarchar(260)     NOT NULL,
    mime                   varchar(255)      NOT NULL,
    tamano                 bigint            NOT NULL,
    checksum_sha256        binary(32)        NOT NULL,
    clave_almacenamiento   nvarchar(500)     NOT NULL,
    cargado_en             datetime2(0)      NOT NULL,
    cargado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_archivo_solicitud_apertura PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_archivo_solicitud_apertura__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_archivo_solicitud_apertura__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_archivo_solicitud_apertura__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_archivo_solicitud_apertura__mime_pdf CHECK (mime = 'application/pdf'),
    CONSTRAINT ck_archivo_solicitud_apertura__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_archivo_solicitud_apertura__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE TABLE academico.solicitud_apertura
(
    id                         int IDENTITY(1,1) NOT NULL,
    experiencia_educativa_id   int               NOT NULL,
    periodo_escolar_id         int               NOT NULL,
    seccion                    varchar(20)       NOT NULL,
    cantidad_estudiantes       int               NOT NULL,
    justificacion              nvarchar(max)     NOT NULL,
    oficio_respaldo_id         int               NOT NULL,
    estado                     varchar(20)       NOT NULL,
    creada_en                  datetime2(0)      NOT NULL,
    creada_por_usuario_id      int               NOT NULL,
    actualizada_en             datetime2(0)      NULL,
    actualizada_por_usuario_id int               NULL,
    resuelta_en                datetime2(0)      NULL,
    resuelta_por_usuario_id    int               NULL,
    comentarios_resolucion     nvarchar(max)     NULL,
    cancelada_en               datetime2(0)      NULL,
    cancelada_por_usuario_id   int               NULL,
    motivo_cancelacion         nvarchar(1000)    NULL,
    programacion_academica_id  int               NULL,
    vinculada_en               datetime2(0)      NULL,
    vinculada_por_usuario_id   int               NULL,
    CONSTRAINT pk_solicitud_apertura PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_solicitud_apertura__oficio_respaldo_id UNIQUE (oficio_respaldo_id),
    CONSTRAINT fk_solicitud_apertura__experiencia_educativa FOREIGN KEY (experiencia_educativa_id) REFERENCES academico.experiencia_educativa (id),
    CONSTRAINT fk_solicitud_apertura__periodo_escolar FOREIGN KEY (periodo_escolar_id) REFERENCES academico.periodo_escolar (id),
    CONSTRAINT fk_solicitud_apertura__archivo_solicitud_apertura FOREIGN KEY (oficio_respaldo_id) REFERENCES academico.archivo_solicitud_apertura (id),
    CONSTRAINT fk_solicitud_apertura__programacion_academica FOREIGN KEY (programacion_academica_id) REFERENCES academico.programacion_academica (id),
    CONSTRAINT fk_solicitud_apertura__usuario__creada_por FOREIGN KEY (creada_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_solicitud_apertura__usuario__actualizada_por FOREIGN KEY (actualizada_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_solicitud_apertura__usuario__resuelta_por FOREIGN KEY (resuelta_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_solicitud_apertura__usuario__cancelada_por FOREIGN KEY (cancelada_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_solicitud_apertura__usuario__vinculada_por FOREIGN KEY (vinculada_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_solicitud_apertura__estado CHECK (estado IN ('PENDIENTE', 'ACEPTADA', 'RECHAZADA', 'CANCELADA')),
    CONSTRAINT ck_solicitud_apertura__seccion_formato CHECK (
            LEN(seccion) > 0
        AND DATALENGTH(seccion) = LEN(seccion)
        AND seccion COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'),
    CONSTRAINT ck_solicitud_apertura__cantidad_positiva CHECK (cantidad_estudiantes > 0),
    CONSTRAINT ck_solicitud_apertura__justificacion_no_vacia CHECK (LEN(TRIM(justificacion)) > 0),
    -- Actor y fecha se informan conjuntamente.
    CONSTRAINT ck_solicitud_apertura__actualizacion_conjunta CHECK (
        (actualizada_en IS NULL AND actualizada_por_usuario_id IS NULL) OR (actualizada_en IS NOT NULL AND actualizada_por_usuario_id IS NOT NULL)),
    CONSTRAINT ck_solicitud_apertura__resolucion_conjunta CHECK (
        (resuelta_en IS NULL AND resuelta_por_usuario_id IS NULL) OR (resuelta_en IS NOT NULL AND resuelta_por_usuario_id IS NOT NULL)),
    CONSTRAINT ck_solicitud_apertura__cancelacion_conjunta CHECK (
           (cancelada_en IS NULL AND cancelada_por_usuario_id IS NULL AND motivo_cancelacion IS NULL)
        OR (cancelada_en IS NOT NULL AND cancelada_por_usuario_id IS NOT NULL AND motivo_cancelacion IS NOT NULL AND LEN(TRIM(motivo_cancelacion)) > 0)),
    CONSTRAINT ck_solicitud_apertura__vinculacion_conjunta CHECK (
           (programacion_academica_id IS NULL AND vinculada_en IS NULL AND vinculada_por_usuario_id IS NULL)
        OR (programacion_academica_id IS NOT NULL AND vinculada_en IS NOT NULL AND vinculada_por_usuario_id IS NOT NULL)),
    CONSTRAINT ck_solicitud_apertura__vinculacion_posterior CHECK (vinculada_en IS NULL OR (resuelta_en IS NOT NULL AND vinculada_en >= resuelta_en)),
    CONSTRAINT ck_solicitud_apertura__estado_consistente CHECK (
           (estado = 'PENDIENTE' AND resuelta_en IS NULL AND comentarios_resolucion IS NULL AND cancelada_en IS NULL AND programacion_academica_id IS NULL)
        OR (estado = 'ACEPTADA' AND resuelta_en IS NOT NULL AND cancelada_en IS NULL)
        OR (estado = 'RECHAZADA' AND resuelta_en IS NOT NULL AND comentarios_resolucion IS NOT NULL AND LEN(TRIM(comentarios_resolucion)) > 0 AND cancelada_en IS NULL AND programacion_academica_id IS NULL)
        OR (estado = 'CANCELADA' AND cancelada_en IS NOT NULL AND resuelta_en IS NULL AND comentarios_resolucion IS NULL AND programacion_academica_id IS NULL))
);

CREATE UNIQUE INDEX ux_solicitud_apertura__pendiente ON academico.solicitud_apertura (experiencia_educativa_id, periodo_escolar_id, seccion) WHERE estado = 'PENDIENTE';
CREATE UNIQUE INDEX ux_solicitud_apertura__programacion_academica_id ON academico.solicitud_apertura (programacion_academica_id) WHERE programacion_academica_id IS NOT NULL;
CREATE INDEX ix_solicitud_apertura__experiencia_educativa_id_estado ON academico.solicitud_apertura (experiencia_educativa_id, estado);
CREATE INDEX ix_solicitud_apertura__periodo_escolar_id_estado ON academico.solicitud_apertura (periodo_escolar_id, estado);
CREATE INDEX ix_solicitud_apertura__creada_por_usuario_id ON academico.solicitud_apertura (creada_por_usuario_id);
CREATE INDEX ix_solicitud_apertura__actualizada_por_usuario_id ON academico.solicitud_apertura (actualizada_por_usuario_id) WHERE actualizada_por_usuario_id IS NOT NULL;
CREATE INDEX ix_solicitud_apertura__resuelta_por_usuario_id ON academico.solicitud_apertura (resuelta_por_usuario_id) WHERE resuelta_por_usuario_id IS NOT NULL;
CREATE INDEX ix_solicitud_apertura__cancelada_por_usuario_id ON academico.solicitud_apertura (cancelada_por_usuario_id) WHERE cancelada_por_usuario_id IS NOT NULL;
CREATE INDEX ix_solicitud_apertura__vinculada_por_usuario_id ON academico.solicitud_apertura (vinculada_por_usuario_id) WHERE vinculada_por_usuario_id IS NOT NULL;

-- 8. Catálogos permanentes (§6.21, §6.22, §15.2). Sin baja lógica.

CREATE TABLE academico.grado_academico
(
    id     int IDENTITY(1,1) NOT NULL,
    nombre nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    CONSTRAINT pk_grado_academico PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_grado_academico__nombre UNIQUE (nombre),
    CONSTRAINT ck_grado_academico__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE academico.tipo_documento_expediente
(
    id     int IDENTITY(1,1) NOT NULL,
    nombre nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    CONSTRAINT pk_tipo_documento_expediente PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_tipo_documento_expediente__nombre UNIQUE (nombre),
    CONSTRAINT ck_tipo_documento_expediente__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE plazas.tratamiento_academico
(
    id                 int IDENTITY(1,1) NOT NULL,
    nombre             nvarchar(30) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    grado_academico_id int               NOT NULL,
    CONSTRAINT pk_tratamiento_academico PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_tratamiento_academico__nombre UNIQUE (nombre),
    CONSTRAINT fk_tratamiento_academico__grado_academico FOREIGN KEY (grado_academico_id) REFERENCES academico.grado_academico (id),
    CONSTRAINT ck_tratamiento_academico__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE INDEX ix_tratamiento_academico__grado_academico_id ON plazas.tratamiento_academico (grado_academico_id);

CREATE TABLE plazas.articulo
(
    id          int IDENTITY(1,1) NOT NULL,
    numero      varchar(50)       NOT NULL,
    descripcion nvarchar(1000)    NOT NULL,
    CONSTRAINT pk_articulo PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_articulo__numero UNIQUE (numero),
    -- Admite espacios internos, p. ej. 42 BIS.
    CONSTRAINT ck_articulo__numero_formato CHECK (
            LEN(numero) > 0
        AND DATALENGTH(numero) = LEN(numero)
        AND numero NOT LIKE ' %'
        AND numero COLLATE Latin1_General_100_BIN2 = UPPER(numero)),
    CONSTRAINT ck_articulo__descripcion_no_vacia CHECK (LEN(TRIM(descripcion)) > 0)
);

CREATE TABLE plazas.modalidad_recepcion
(
    id             int IDENTITY(1,1) NOT NULL,
    nombre         nvarchar(100) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    requiere_lugar bit               NOT NULL,
    CONSTRAINT pk_modalidad_recepcion PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_modalidad_recepcion__nombre UNIQUE (nombre),
    CONSTRAINT ck_modalidad_recepcion__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE plazas.tipo_plaza
(
    id     int IDENTITY(1,1) NOT NULL,
    nombre nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    CONSTRAINT pk_tipo_plaza PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_tipo_plaza__nombre UNIQUE (nombre),
    CONSTRAINT ck_tipo_plaza__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

CREATE TABLE plazas.tipo_contratacion
(
    id     int IDENTITY(1,1) NOT NULL,
    nombre nvarchar(150) COLLATE Modern_Spanish_100_CI_AI NOT NULL,
    CONSTRAINT pk_tipo_contratacion PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_tipo_contratacion__nombre UNIQUE (nombre),
    CONSTRAINT ck_tipo_contratacion__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0)
);

-- 9. Académico: docentes (§15.4). asignacion_docente va al final (referencia a plazas.acta_oferta).

CREATE TABLE academico.docente
(
    id                 int IDENTITY(1,1) NOT NULL,
    nombre             nvarchar(200)     NOT NULL,
    num_personal       varchar(50)       NULL,
    puesto             nvarchar(200)     NULL,
    descripcion_perfil nvarchar(max)     NULL,
    CONSTRAINT pk_docente PRIMARY KEY CLUSTERED (id),
    CONSTRAINT ck_docente__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_docente__num_personal_formato CHECK (
           num_personal IS NULL
        OR (    LEN(num_personal) > 0
            AND DATALENGTH(num_personal) = LEN(num_personal)
            AND num_personal NOT LIKE ' %'
            AND num_personal COLLATE Latin1_General_100_BIN2 = UPPER(num_personal)))
);

-- Sirve también como ix_docente__num_personal (§8).
CREATE UNIQUE INDEX uq_docente__num_personal ON academico.docente (num_personal) WHERE num_personal IS NOT NULL;

CREATE TABLE academico.formacion_docente
(
    id                 int IDENTITY(1,1) NOT NULL,
    docente_id         int               NOT NULL,
    grado_academico_id int               NOT NULL,
    descripcion        nvarchar(500)     NOT NULL,
    CONSTRAINT pk_formacion_docente PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_formacion_docente__docente FOREIGN KEY (docente_id) REFERENCES academico.docente (id),
    CONSTRAINT fk_formacion_docente__grado_academico FOREIGN KEY (grado_academico_id) REFERENCES academico.grado_academico (id),
    CONSTRAINT ck_formacion_docente__descripcion_no_vacia CHECK (LEN(TRIM(descripcion)) > 0)
);

CREATE INDEX ix_formacion_docente__docente_id ON academico.formacion_docente (docente_id);

CREATE TABLE academico.documento_docente
(
    id                           int IDENTITY(1,1) NOT NULL,
    docente_id                   int               NOT NULL,
    tipo_documento_expediente_id int               NOT NULL,
    CONSTRAINT pk_documento_docente PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_documento_docente__docente FOREIGN KEY (docente_id) REFERENCES academico.docente (id),
    CONSTRAINT fk_documento_docente__tipo_documento_expediente FOREIGN KEY (tipo_documento_expediente_id) REFERENCES academico.tipo_documento_expediente (id)
);

CREATE INDEX ix_documento_docente__docente_id ON academico.documento_docente (docente_id);

CREATE TABLE academico.version_documento_docente
(
    id                     int IDENTITY(1,1) NOT NULL,
    documento_docente_id   int               NOT NULL,
    nombre                 nvarchar(260)     NOT NULL,
    mime                   varchar(255)      NOT NULL,
    tamano                 bigint            NOT NULL,
    checksum_sha256        binary(32)        NOT NULL,
    clave_almacenamiento   nvarchar(500)     NOT NULL,
    numero_version         int               NOT NULL,
    es_vigente             bit               NOT NULL,
    cargado_en             datetime2(0)      NOT NULL,
    cargado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_version_documento_docente PRIMARY KEY CLUSTERED (id),
    -- Sirve también como ix_version_documento_docente__documento_docente_id (§8).
    CONSTRAINT uq_version_documento_docente__documento_version UNIQUE (documento_docente_id, numero_version),
    CONSTRAINT uq_version_documento_docente__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_version_documento_docente__documento_docente FOREIGN KEY (documento_docente_id) REFERENCES academico.documento_docente (id),
    CONSTRAINT fk_version_documento_docente__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_version_documento_docente__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_version_documento_docente__mime_no_vacio CHECK (LEN(TRIM(mime)) > 0),
    CONSTRAINT ck_version_documento_docente__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_version_documento_docente__numero_version_positivo CHECK (numero_version > 0),
    CONSTRAINT ck_version_documento_docente__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE UNIQUE INDEX ux_version_documento_docente__vigente ON academico.version_documento_docente (documento_docente_id) WHERE es_vigente = 1;

-- 10. Plazas: Consejo Técnico, ofertas y avisos (§15.3, §15.5, §15.6)

CREATE TABLE plazas.integrante_consejo_tecnico
(
    id                       int IDENTITY(1,1) NOT NULL,
    entidad_academica_id     int               NOT NULL,
    nombre                   nvarchar(200)     NOT NULL,
    cargo                    nvarchar(200)     NOT NULL,
    tratamiento_academico_id int               NOT NULL,
    fecha_inicio             date              NOT NULL,
    fecha_fin                date              NULL,
    CONSTRAINT pk_integrante_consejo_tecnico PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_integrante_consejo_tecnico__entidad_academica FOREIGN KEY (entidad_academica_id) REFERENCES academico.entidad_academica (id),
    CONSTRAINT fk_integrante_consejo_tecnico__tratamiento_academico FOREIGN KEY (tratamiento_academico_id) REFERENCES plazas.tratamiento_academico (id),
    CONSTRAINT ck_integrante_consejo_tecnico__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_integrante_consejo_tecnico__cargo_no_vacio CHECK (LEN(TRIM(cargo)) > 0),
    CONSTRAINT ck_integrante_consejo_tecnico__rango_fechas CHECK (fecha_fin IS NULL OR fecha_fin >= fecha_inicio)
);

CREATE INDEX ix_integrante_consejo_tecnico__entidad_academica_id ON plazas.integrante_consejo_tecnico (entidad_academica_id, fecha_inicio);
CREATE INDEX ix_integrante_consejo_tecnico__tratamiento_academico_id ON plazas.integrante_consejo_tecnico (tratamiento_academico_id);

CREATE TABLE plazas.oferta
(
    id                        int IDENTITY(1,1) NOT NULL,
    programacion_academica_id int               NOT NULL,
    clave_plaza               varchar(100)      NOT NULL,
    tipo_plaza_id             int               NOT NULL,
    tipo_contratacion_id      int               NOT NULL,
    perfil_solicitado         nvarchar(max)     NOT NULL,
    justificacion             nvarchar(1000)    NULL,
    estado                    varchar(20)       NOT NULL,
    cerrada_en                datetime2(0)      NULL,
    CONSTRAINT pk_oferta PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_oferta__programacion_academica FOREIGN KEY (programacion_academica_id) REFERENCES academico.programacion_academica (id),
    CONSTRAINT fk_oferta__tipo_plaza FOREIGN KEY (tipo_plaza_id) REFERENCES plazas.tipo_plaza (id),
    CONSTRAINT fk_oferta__tipo_contratacion FOREIGN KEY (tipo_contratacion_id) REFERENCES plazas.tipo_contratacion (id),
    CONSTRAINT ck_oferta__clave_plaza_formato CHECK (
            LEN(clave_plaza) > 0
        AND DATALENGTH(clave_plaza) = LEN(clave_plaza)
        AND clave_plaza NOT LIKE ' %'
        AND clave_plaza COLLATE Latin1_General_100_BIN2 = UPPER(clave_plaza)),
    CONSTRAINT ck_oferta__perfil_solicitado_no_vacio CHECK (LEN(TRIM(perfil_solicitado)) > 0),
    CONSTRAINT ck_oferta__justificacion_no_vacia CHECK (justificacion IS NULL OR LEN(TRIM(justificacion)) > 0),
    CONSTRAINT ck_oferta__estado CHECK (estado IN ('DISPONIBLE', 'EN_PUBLICACION', 'CUBIERTA')),
    CONSTRAINT ck_oferta__cierre_estado CHECK (
           (estado = 'CUBIERTA' AND cerrada_en IS NOT NULL)
        OR (estado IN ('DISPONIBLE', 'EN_PUBLICACION') AND cerrada_en IS NULL))
);

CREATE UNIQUE INDEX ux_oferta__abierta ON plazas.oferta (programacion_academica_id, clave_plaza) WHERE cerrada_en IS NULL;
CREATE INDEX ix_oferta__programacion_academica_id ON plazas.oferta (programacion_academica_id);

CREATE TABLE plazas.aviso
(
    id                       int IDENTITY(1,1) NOT NULL,
    entidad_academica_id     int               NOT NULL,
    periodo_escolar_id       int               NOT NULL,
    sistema_educativo_id     int               NOT NULL,
    articulo_id              int               NOT NULL,
    tipo_comunicado          varchar(15)       NOT NULL,
    modalidad_recepcion_id   int               NULL,
    requisitos               nvarchar(max)     NULL,
    lugar_recepcion          nvarchar(500)     NULL,
    correo_contacto          varchar(254) COLLATE Latin1_General_100_CI_AI NULL,
    nombre_titular           nvarchar(200)     NULL,
    creado_en                datetime2(0)      NOT NULL,
    fecha_publicacion        date              NULL,
    fecha_consejo_tecnico    date              NULL,
    fecha_vacantes           date              NULL,
    url_publicacion          varchar(2048)     NULL,
    estado                   varchar(30)       NOT NULL,
    cancelado_en             datetime2(0)      NULL,
    cancelado_por_usuario_id int               NULL,
    motivo_cancelacion       nvarchar(1000)    NULL,
    archivado_en             datetime2(0)      NULL,
    archivado_por_usuario_id int               NULL,
    CONSTRAINT pk_aviso PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_aviso__entidad_academica FOREIGN KEY (entidad_academica_id) REFERENCES academico.entidad_academica (id),
    CONSTRAINT fk_aviso__periodo_escolar FOREIGN KEY (periodo_escolar_id) REFERENCES academico.periodo_escolar (id),
    CONSTRAINT fk_aviso__sistema_educativo FOREIGN KEY (sistema_educativo_id) REFERENCES academico.sistema_educativo (id),
    CONSTRAINT fk_aviso__articulo FOREIGN KEY (articulo_id) REFERENCES plazas.articulo (id),
    CONSTRAINT fk_aviso__modalidad_recepcion FOREIGN KEY (modalidad_recepcion_id) REFERENCES plazas.modalidad_recepcion (id),
    CONSTRAINT fk_aviso__usuario__cancelado_por FOREIGN KEY (cancelado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_aviso__usuario__archivado_por FOREIGN KEY (archivado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_aviso__tipo_comunicado CHECK (tipo_comunicado IN ('AVISO', 'CONVOCATORIA')),
    CONSTRAINT ck_aviso__estado CHECK (estado IN (
        'CREADO', 'EN_REVISION_DGAA', 'DEVUELTO_DGAA', 'AVALADO_DGAA', 'FIRMADO', 'PUBLICADO', 'CANCELADO')),
    CONSTRAINT ck_aviso__requisitos_no_vacios CHECK (requisitos IS NULL OR LEN(TRIM(requisitos)) > 0),
    CONSTRAINT ck_aviso__lugar_recepcion_no_vacio CHECK (lugar_recepcion IS NULL OR LEN(TRIM(lugar_recepcion)) > 0),
    CONSTRAINT ck_aviso__nombre_titular_no_vacio CHECK (nombre_titular IS NULL OR LEN(TRIM(nombre_titular)) > 0),
    CONSTRAINT ck_aviso__correo_contacto_formato CHECK (
           correo_contacto IS NULL
        OR (    DATALENGTH(correo_contacto) = LEN(correo_contacto)
            AND correo_contacto LIKE '_%@_%._%'
            AND correo_contacto NOT LIKE '%@%@%'
            AND CHARINDEX(' ', correo_contacto) = 0)),
    CONSTRAINT ck_aviso__url_publicacion_formato CHECK (
           url_publicacion IS NULL
        OR (    CHARINDEX(' ', url_publicacion) = 0
            AND (url_publicacion LIKE 'http://_%' OR url_publicacion LIKE 'https://_%'))),
    CONSTRAINT ck_aviso__orden_fechas CHECK (
        fecha_consejo_tecnico IS NULL OR fecha_vacantes IS NULL OR fecha_consejo_tecnico <= fecha_vacantes),
    -- Datos obligatorios antes del primer envío a revisión.
    CONSTRAINT ck_aviso__datos_envio CHECK (
           estado = 'CREADO'
        OR (    modalidad_recepcion_id IS NOT NULL
            AND requisitos IS NOT NULL
            AND correo_contacto IS NOT NULL
            AND nombre_titular IS NOT NULL
            AND fecha_consejo_tecnico IS NOT NULL
            AND fecha_vacantes IS NOT NULL)),
    CONSTRAINT ck_aviso__datos_publicacion CHECK (
        estado <> 'PUBLICADO' OR (fecha_publicacion IS NOT NULL AND url_publicacion IS NOT NULL)),
    CONSTRAINT ck_aviso__cancelacion CHECK (
           (estado <> 'CANCELADO' AND cancelado_en IS NULL AND cancelado_por_usuario_id IS NULL AND motivo_cancelacion IS NULL)
        OR (estado = 'CANCELADO' AND cancelado_en IS NOT NULL AND cancelado_por_usuario_id IS NOT NULL AND motivo_cancelacion IS NOT NULL AND LEN(TRIM(motivo_cancelacion)) > 0)),
    CONSTRAINT ck_aviso__archivado CHECK (
           (archivado_en IS NULL AND archivado_por_usuario_id IS NULL)
        OR (archivado_en IS NOT NULL AND archivado_por_usuario_id IS NOT NULL AND estado IN ('PUBLICADO', 'CANCELADO')))
);

CREATE INDEX ix_aviso__entidad_academica_id ON plazas.aviso (entidad_academica_id);
CREATE INDEX ix_aviso__periodo_escolar_id ON plazas.aviso (periodo_escolar_id);
CREATE INDEX ix_aviso__sistema_educativo_id ON plazas.aviso (sistema_educativo_id);
CREATE INDEX ix_aviso__articulo_id ON plazas.aviso (articulo_id);
CREATE INDEX ix_aviso__estado ON plazas.aviso (estado);

CREATE TABLE plazas.horario_recepcion_requisito
(
    id          int IDENTITY(1,1) NOT NULL,
    aviso_id    int               NOT NULL,
    fecha       date              NOT NULL,
    hora_inicio time(0)           NOT NULL,
    hora_fin    time(0)           NOT NULL,
    CONSTRAINT pk_horario_recepcion_requisito PRIMARY KEY CLUSTERED (id),
    -- Índice por Aviso y fecha; los traslapes se validan en la aplicación.
    CONSTRAINT uq_horario_recepcion_requisito__aviso_fecha_hora_inicio UNIQUE (aviso_id, fecha, hora_inicio),
    CONSTRAINT fk_horario_recepcion_requisito__aviso FOREIGN KEY (aviso_id) REFERENCES plazas.aviso (id),
    CONSTRAINT ck_horario_recepcion_requisito__rango_horas CHECK (hora_inicio < hora_fin)
);

CREATE TABLE plazas.aviso_oferta
(
    id             int IDENTITY(1,1) NOT NULL,
    aviso_id       int               NOT NULL,
    oferta_id      int               NOT NULL,
    incorporado_en datetime2(0)      NOT NULL,
    cerrado_en     datetime2(0)      NULL,
    causa_cierre   varchar(20)       NULL,
    CONSTRAINT pk_aviso_oferta PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_aviso_oferta__aviso_oferta UNIQUE (aviso_id, oferta_id),
    CONSTRAINT fk_aviso_oferta__aviso FOREIGN KEY (aviso_id) REFERENCES plazas.aviso (id),
    CONSTRAINT fk_aviso_oferta__oferta FOREIGN KEY (oferta_id) REFERENCES plazas.oferta (id),
    CONSTRAINT ck_aviso_oferta__causa_cierre CHECK (causa_cierre IS NULL OR causa_cierre IN ('DESIGNADA', 'DESIERTA', 'SIN_ASPIRANTES', 'CANCELADO')),
    CONSTRAINT ck_aviso_oferta__cierre_conjunto CHECK (
        (cerrado_en IS NULL AND causa_cierre IS NULL) OR (cerrado_en IS NOT NULL AND causa_cierre IS NOT NULL)),
    CONSTRAINT ck_aviso_oferta__orden_fechas CHECK (cerrado_en IS NULL OR cerrado_en >= incorporado_en)
);

CREATE UNIQUE INDEX ux_aviso_oferta__oferta_abierta ON plazas.aviso_oferta (oferta_id) WHERE cerrado_en IS NULL;
CREATE INDEX ix_aviso_oferta__oferta_id ON plazas.aviso_oferta (oferta_id);

CREATE TABLE plazas.documento_aviso
(
    id                     int IDENTITY(1,1) NOT NULL,
    aviso_id               int               NOT NULL,
    tipo                   varchar(10)       NOT NULL,
    nombre                 nvarchar(260)     NOT NULL,
    mime                   varchar(255)      NOT NULL,
    tamano                 bigint            NOT NULL,
    checksum_sha256        binary(32)        NOT NULL,
    clave_almacenamiento   nvarchar(500)     NOT NULL,
    numero_version         int               NOT NULL,
    es_vigente             bit               NOT NULL,
    cargado_en             datetime2(0)      NOT NULL,
    cargado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_documento_aviso PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_documento_aviso__aviso_tipo_version UNIQUE (aviso_id, tipo, numero_version),
    CONSTRAINT uq_documento_aviso__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_documento_aviso__aviso FOREIGN KEY (aviso_id) REFERENCES plazas.aviso (id),
    CONSTRAINT fk_documento_aviso__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_documento_aviso__tipo CHECK (tipo IN ('ORIGINAL', 'FIRMADO')),
    CONSTRAINT ck_documento_aviso__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_documento_aviso__mime_no_vacio CHECK (LEN(TRIM(mime)) > 0),
    CONSTRAINT ck_documento_aviso__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_documento_aviso__numero_version_positivo CHECK (numero_version > 0),
    CONSTRAINT ck_documento_aviso__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE UNIQUE INDEX ux_documento_aviso__vigente ON plazas.documento_aviso (aviso_id, tipo) WHERE es_vigente = 1;

CREATE TABLE plazas.revision_aviso
(
    id                      int IDENTITY(1,1) NOT NULL,
    aviso_id                int               NOT NULL,
    numero_revision         int               NOT NULL,
    documento_original_id   int               NOT NULL,
    enviado_por_usuario_id  int               NOT NULL,
    enviado_en              datetime2(0)      NOT NULL,
    resuelto_por_usuario_id int               NULL,
    resuelto_en             datetime2(0)      NULL,
    resultado               varchar(10)       NULL,
    comentarios             nvarchar(max)     NULL,
    CONSTRAINT pk_revision_aviso PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_revision_aviso__aviso_numero_revision UNIQUE (aviso_id, numero_revision),
    CONSTRAINT fk_revision_aviso__aviso FOREIGN KEY (aviso_id) REFERENCES plazas.aviso (id),
    CONSTRAINT fk_revision_aviso__documento_aviso FOREIGN KEY (documento_original_id) REFERENCES plazas.documento_aviso (id),
    CONSTRAINT fk_revision_aviso__usuario__enviado_por FOREIGN KEY (enviado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_revision_aviso__usuario__resuelto_por FOREIGN KEY (resuelto_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_revision_aviso__numero_revision_positivo CHECK (numero_revision > 0),
    CONSTRAINT ck_revision_aviso__resultado CHECK (resultado IS NULL OR resultado IN ('AVALADA', 'DEVUELTA')),
    CONSTRAINT ck_revision_aviso__resolucion_conjunta CHECK (
           (resuelto_en IS NULL AND resuelto_por_usuario_id IS NULL AND resultado IS NULL)
        OR (resuelto_en IS NOT NULL AND resuelto_por_usuario_id IS NOT NULL AND resultado IS NOT NULL AND resuelto_en >= enviado_en)),
    CONSTRAINT ck_revision_aviso__comentarios_devolucion CHECK (resultado IS NULL OR resultado <> 'DEVUELTA' OR (comentarios IS NOT NULL AND LEN(TRIM(comentarios)) > 0))
);

CREATE UNIQUE INDEX ux_revision_aviso__abierta ON plazas.revision_aviso (aviso_id) WHERE resultado IS NULL;
CREATE INDEX ix_revision_aviso__documento_original_id ON plazas.revision_aviso (documento_original_id);

-- 11. Plazas: Aspirantes y Solicitudes (§15.7)

CREATE TABLE plazas.aspirante
(
    id int IDENTITY(1,1) NOT NULL,
    CONSTRAINT pk_aspirante PRIMARY KEY CLUSTERED (id)
);

CREATE TABLE plazas.perfil_aspirante
(
    id                    int IDENTITY(1,1) NOT NULL,
    aspirante_id          int               NOT NULL,
    numero_version        int               NOT NULL,
    nombre                nvarchar(200)     NOT NULL,
    correo                varchar(254) COLLATE Latin1_General_100_CI_AI NOT NULL,
    puesto_actual         nvarchar(200)     NULL,
    descripcion_perfil    nvarchar(max)     NOT NULL,
    es_vigente            bit               NOT NULL,
    creado_en             datetime2(0)      NOT NULL,
    creado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_perfil_aspirante PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_perfil_aspirante__aspirante_version UNIQUE (aspirante_id, numero_version),
    CONSTRAINT fk_perfil_aspirante__aspirante FOREIGN KEY (aspirante_id) REFERENCES plazas.aspirante (id),
    CONSTRAINT fk_perfil_aspirante__usuario FOREIGN KEY (creado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_perfil_aspirante__numero_version_positivo CHECK (numero_version > 0),
    CONSTRAINT ck_perfil_aspirante__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_perfil_aspirante__correo_formato CHECK (
            DATALENGTH(correo) = LEN(correo)
        AND correo LIKE '_%@_%._%'
        AND correo NOT LIKE '%@%@%'
        AND CHARINDEX(' ', correo) = 0),
    CONSTRAINT ck_perfil_aspirante__puesto_actual_no_vacio CHECK (puesto_actual IS NULL OR LEN(TRIM(puesto_actual)) > 0),
    CONSTRAINT ck_perfil_aspirante__descripcion_perfil_no_vacia CHECK (LEN(TRIM(descripcion_perfil)) > 0)
);

CREATE UNIQUE INDEX ux_perfil_aspirante__vigente ON plazas.perfil_aspirante (aspirante_id) WHERE es_vigente = 1;
-- Ignora mayúsculas por la intercalación de la columna.
CREATE UNIQUE INDEX ux_perfil_aspirante__correo_vigente ON plazas.perfil_aspirante (correo) WHERE es_vigente = 1;

CREATE TABLE plazas.formacion_aspirante
(
    id                  int IDENTITY(1,1) NOT NULL,
    perfil_aspirante_id int               NOT NULL,
    grado_academico_id  int               NOT NULL,
    descripcion         nvarchar(500)     NOT NULL,
    CONSTRAINT pk_formacion_aspirante PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_formacion_aspirante__perfil_aspirante FOREIGN KEY (perfil_aspirante_id) REFERENCES plazas.perfil_aspirante (id),
    CONSTRAINT fk_formacion_aspirante__grado_academico FOREIGN KEY (grado_academico_id) REFERENCES academico.grado_academico (id),
    CONSTRAINT ck_formacion_aspirante__descripcion_no_vacia CHECK (LEN(TRIM(descripcion)) > 0)
);

CREATE INDEX ix_formacion_aspirante__perfil_aspirante_id ON plazas.formacion_aspirante (perfil_aspirante_id);

CREATE TABLE plazas.documento_aspirante
(
    id                           int IDENTITY(1,1) NOT NULL,
    aspirante_id                 int               NOT NULL,
    tipo_documento_expediente_id int               NOT NULL,
    CONSTRAINT pk_documento_aspirante PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_documento_aspirante__aspirante FOREIGN KEY (aspirante_id) REFERENCES plazas.aspirante (id),
    CONSTRAINT fk_documento_aspirante__tipo_documento_expediente FOREIGN KEY (tipo_documento_expediente_id) REFERENCES academico.tipo_documento_expediente (id)
);

CREATE INDEX ix_documento_aspirante__aspirante_id ON plazas.documento_aspirante (aspirante_id);

CREATE TABLE plazas.version_documento_aspirante
(
    id                     int IDENTITY(1,1) NOT NULL,
    documento_aspirante_id int               NOT NULL,
    nombre                 nvarchar(260)     NOT NULL,
    mime                   varchar(255)      NOT NULL,
    tamano                 bigint            NOT NULL,
    checksum_sha256        binary(32)        NOT NULL,
    clave_almacenamiento   nvarchar(500)     NOT NULL,
    numero_version         int               NOT NULL,
    es_vigente             bit               NOT NULL,
    cargado_en             datetime2(0)      NOT NULL,
    cargado_por_usuario_id int               NOT NULL,
    CONSTRAINT pk_version_documento_aspirante PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_version_documento_aspirante__documento_version UNIQUE (documento_aspirante_id, numero_version),
    CONSTRAINT uq_version_documento_aspirante__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_version_documento_aspirante__documento_aspirante FOREIGN KEY (documento_aspirante_id) REFERENCES plazas.documento_aspirante (id),
    CONSTRAINT fk_version_documento_aspirante__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_version_documento_aspirante__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_version_documento_aspirante__mime_no_vacio CHECK (LEN(TRIM(mime)) > 0),
    CONSTRAINT ck_version_documento_aspirante__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_version_documento_aspirante__numero_version_positivo CHECK (numero_version > 0),
    CONSTRAINT ck_version_documento_aspirante__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE UNIQUE INDEX ux_version_documento_aspirante__vigente ON plazas.version_documento_aspirante (documento_aspirante_id) WHERE es_vigente = 1;

CREATE TABLE plazas.solicitud
(
    id                         int IDENTITY(1,1) NOT NULL,
    aviso_oferta_id            int               NOT NULL,
    aspirante_id               int               NOT NULL,
    perfil_aspirante_id        int               NOT NULL,
    registrada_en              datetime2(0)      NOT NULL,
    estado                     varchar(20)       NOT NULL,
    observaciones              nvarchar(max)     NULL,
    admitida_en                datetime2(0)      NULL,
    admitida_por_usuario_id    int               NULL,
    no_admitida_en             datetime2(0)      NULL,
    no_admitida_por_usuario_id int               NULL,
    motivo_no_admision         nvarchar(1000)    NULL,
    retirada_en                datetime2(0)      NULL,
    motivo_retiro              nvarchar(1000)    NULL,
    CONSTRAINT pk_solicitud PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_solicitud__aviso_oferta_aspirante UNIQUE (aviso_oferta_id, aspirante_id),
    CONSTRAINT fk_solicitud__aviso_oferta FOREIGN KEY (aviso_oferta_id) REFERENCES plazas.aviso_oferta (id),
    CONSTRAINT fk_solicitud__aspirante FOREIGN KEY (aspirante_id) REFERENCES plazas.aspirante (id),
    CONSTRAINT fk_solicitud__perfil_aspirante FOREIGN KEY (perfil_aspirante_id) REFERENCES plazas.perfil_aspirante (id),
    CONSTRAINT fk_solicitud__usuario__admitida_por FOREIGN KEY (admitida_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_solicitud__usuario__no_admitida_por FOREIGN KEY (no_admitida_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_solicitud__estado CHECK (estado IN ('REGISTRADA', 'ADMITIDA', 'NO_ADMITIDA', 'RETIRADA')),
    CONSTRAINT ck_solicitud__admision_conjunta CHECK (
        (admitida_en IS NULL AND admitida_por_usuario_id IS NULL) OR (admitida_en IS NOT NULL AND admitida_por_usuario_id IS NOT NULL)),
    CONSTRAINT ck_solicitud__no_admision_conjunta CHECK (
           (no_admitida_en IS NULL AND no_admitida_por_usuario_id IS NULL AND motivo_no_admision IS NULL)
        OR (no_admitida_en IS NOT NULL AND no_admitida_por_usuario_id IS NOT NULL AND motivo_no_admision IS NOT NULL AND LEN(TRIM(motivo_no_admision)) > 0)),
    CONSTRAINT ck_solicitud__motivo_retiro CHECK (
        motivo_retiro IS NULL OR (retirada_en IS NOT NULL AND LEN(TRIM(motivo_retiro)) > 0)),
    CONSTRAINT ck_solicitud__estado_consistente CHECK (
           (estado = 'REGISTRADA' AND admitida_en IS NULL AND no_admitida_en IS NULL AND retirada_en IS NULL)
        OR (estado = 'ADMITIDA' AND admitida_en IS NOT NULL AND no_admitida_en IS NULL AND retirada_en IS NULL)
        OR (estado = 'NO_ADMITIDA' AND no_admitida_en IS NOT NULL AND admitida_en IS NULL AND retirada_en IS NULL)
        OR (estado = 'RETIRADA' AND retirada_en IS NOT NULL AND no_admitida_en IS NULL))
);

CREATE INDEX ix_solicitud__aspirante_id ON plazas.solicitud (aspirante_id);
CREATE INDEX ix_solicitud__aviso_oferta_id_estado ON plazas.solicitud (aviso_oferta_id, estado);
CREATE INDEX ix_solicitud__perfil_aspirante_id ON plazas.solicitud (perfil_aspirante_id);

CREATE TABLE plazas.solicitud_documento
(
    solicitud_id                   int NOT NULL,
    version_documento_aspirante_id int NOT NULL,
    CONSTRAINT pk_solicitud_documento PRIMARY KEY CLUSTERED (solicitud_id, version_documento_aspirante_id),
    CONSTRAINT fk_solicitud_documento__solicitud FOREIGN KEY (solicitud_id) REFERENCES plazas.solicitud (id),
    CONSTRAINT fk_solicitud_documento__version_documento_aspirante FOREIGN KEY (version_documento_aspirante_id) REFERENCES plazas.version_documento_aspirante (id)
);

CREATE INDEX ix_solicitud_documento__version_documento_aspirante_id ON plazas.solicitud_documento (version_documento_aspirante_id);

-- 12. Plazas: Actas del Consejo Técnico (§15.8-15.9)

CREATE TABLE plazas.acta_consejo_tecnico
(
    id                       int IDENTITY(1,1) NOT NULL,
    aviso_id                 int               NOT NULL,
    entidad_academica_id     int               NOT NULL,
    folio                    varchar(100)      NOT NULL,
    fecha                    date              NOT NULL,
    lugar                    nvarchar(300)     NULL,
    hora_inicio              time(0)           NULL,
    hora_fin                 time(0)           NULL,
    asuntos_generales        nvarchar(max)     NULL,
    estado                   varchar(30)       NOT NULL,
    archivado_en             datetime2(0)      NULL,
    archivado_por_usuario_id int               NULL,
    fecha_eliminacion        datetime2(0)      NULL,
    eliminado_por_usuario_id int               NULL,
    CONSTRAINT pk_acta_consejo_tecnico PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_acta_consejo_tecnico__aviso FOREIGN KEY (aviso_id) REFERENCES plazas.aviso (id),
    CONSTRAINT fk_acta_consejo_tecnico__entidad_academica FOREIGN KEY (entidad_academica_id) REFERENCES academico.entidad_academica (id),
    CONSTRAINT fk_acta_consejo_tecnico__usuario__archivado_por FOREIGN KEY (archivado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_acta_consejo_tecnico__usuario__eliminado_por FOREIGN KEY (eliminado_por_usuario_id) REFERENCES usuarios.usuario (id),
    -- Letras, números, guiones y diagonales.
    CONSTRAINT ck_acta_consejo_tecnico__folio_formato CHECK (
            LEN(folio) > 0
        AND DATALENGTH(folio) = LEN(folio)
        AND folio COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9/-]%'),
    CONSTRAINT ck_acta_consejo_tecnico__estado CHECK (estado IN (
        'CREADA', 'EN_REVISION_DGAA', 'DEVUELTA_DGAA', 'AVALADA_DGAA', 'FIRMADA')),
    CONSTRAINT ck_acta_consejo_tecnico__lugar_no_vacio CHECK (lugar IS NULL OR LEN(TRIM(lugar)) > 0),
    CONSTRAINT ck_acta_consejo_tecnico__rango_horas CHECK (hora_inicio IS NULL OR hora_fin IS NULL OR hora_inicio < hora_fin),
    -- Lugar y horario son obligatorios antes del primer envío.
    CONSTRAINT ck_acta_consejo_tecnico__datos_envio CHECK (
        estado = 'CREADA' OR (lugar IS NOT NULL AND hora_inicio IS NOT NULL AND hora_fin IS NOT NULL)),
    CONSTRAINT ck_acta_consejo_tecnico__archivado CHECK (
           (archivado_en IS NULL AND archivado_por_usuario_id IS NULL)
        OR (archivado_en IS NOT NULL AND archivado_por_usuario_id IS NOT NULL AND estado = 'FIRMADA')),
    CONSTRAINT ck_acta_consejo_tecnico__eliminacion CHECK (
           (fecha_eliminacion IS NULL AND eliminado_por_usuario_id IS NULL)
        OR (fecha_eliminacion IS NOT NULL AND eliminado_por_usuario_id IS NOT NULL AND estado = 'CREADA'))
);

-- El folio puede reutilizarse después de eliminar un Acta.
CREATE UNIQUE INDEX ux_acta_consejo_tecnico__entidad_folio_activo ON plazas.acta_consejo_tecnico (entidad_academica_id, folio) WHERE fecha_eliminacion IS NULL;
CREATE INDEX ix_acta_consejo_tecnico__aviso_id ON plazas.acta_consejo_tecnico (aviso_id);
CREATE INDEX ix_acta_consejo_tecnico__entidad_academica_id_fecha ON plazas.acta_consejo_tecnico (entidad_academica_id, fecha);
CREATE INDEX ix_acta_consejo_tecnico__estado ON plazas.acta_consejo_tecnico (estado);

CREATE TABLE plazas.acta_oferta
(
    id                       int IDENTITY(1,1) NOT NULL,
    acta_consejo_tecnico_id  int               NOT NULL,
    aviso_oferta_id          int               NOT NULL,
    resultado                varchar(20)       NOT NULL,
    observaciones            nvarchar(max)     NULL,
    solicitud_designada_id   int               NULL,
    docente_asignado_id      int               NULL,
    fecha_eliminacion        datetime2(0)      NULL,
    eliminado_por_usuario_id int               NULL,
    CONSTRAINT pk_acta_oferta PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_acta_oferta__acta_consejo_tecnico FOREIGN KEY (acta_consejo_tecnico_id) REFERENCES plazas.acta_consejo_tecnico (id),
    CONSTRAINT fk_acta_oferta__aviso_oferta FOREIGN KEY (aviso_oferta_id) REFERENCES plazas.aviso_oferta (id),
    CONSTRAINT fk_acta_oferta__solicitud FOREIGN KEY (solicitud_designada_id) REFERENCES plazas.solicitud (id),
    CONSTRAINT fk_acta_oferta__docente FOREIGN KEY (docente_asignado_id) REFERENCES academico.docente (id),
    CONSTRAINT fk_acta_oferta__usuario FOREIGN KEY (eliminado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_acta_oferta__resultado CHECK (resultado IN ('PENDIENTE', 'DESIGNADA', 'DESIERTA', 'SIN_ASPIRANTES')),
    -- Solo DESIGNADA lleva Solicitud designada; el Docente se completa al avalar.
    CONSTRAINT ck_acta_oferta__designacion CHECK (
           (resultado = 'DESIGNADA' AND solicitud_designada_id IS NOT NULL)
        OR (resultado <> 'DESIGNADA' AND solicitud_designada_id IS NULL AND docente_asignado_id IS NULL)),
    CONSTRAINT ck_acta_oferta__observaciones_sin_designacion CHECK (
        resultado NOT IN ('DESIERTA', 'SIN_ASPIRANTES') OR (observaciones IS NOT NULL AND LEN(TRIM(observaciones)) > 0)),
    CONSTRAINT ck_acta_oferta__eliminacion_conjunta CHECK (
        (fecha_eliminacion IS NULL AND eliminado_por_usuario_id IS NULL) OR (fecha_eliminacion IS NOT NULL AND eliminado_por_usuario_id IS NOT NULL))
);

CREATE UNIQUE INDEX ux_acta_oferta__aviso_oferta_activo ON plazas.acta_oferta (aviso_oferta_id) WHERE fecha_eliminacion IS NULL;
CREATE INDEX ix_acta_oferta__acta_consejo_tecnico_id ON plazas.acta_oferta (acta_consejo_tecnico_id);
CREATE INDEX ix_acta_oferta__solicitud_designada_id ON plazas.acta_oferta (solicitud_designada_id) WHERE solicitud_designada_id IS NOT NULL;
CREATE INDEX ix_acta_oferta__docente_asignado_id ON plazas.acta_oferta (docente_asignado_id) WHERE docente_asignado_id IS NOT NULL;

CREATE TABLE plazas.votacion_solicitud
(
    acta_oferta_id int NOT NULL,
    solicitud_id   int NOT NULL,
    votos          int NOT NULL,
    CONSTRAINT pk_votacion_solicitud PRIMARY KEY CLUSTERED (acta_oferta_id, solicitud_id),
    CONSTRAINT fk_votacion_solicitud__acta_oferta FOREIGN KEY (acta_oferta_id) REFERENCES plazas.acta_oferta (id),
    CONSTRAINT fk_votacion_solicitud__solicitud FOREIGN KEY (solicitud_id) REFERENCES plazas.solicitud (id),
    CONSTRAINT ck_votacion_solicitud__votos_no_negativos CHECK (votos >= 0)
);

CREATE INDEX ix_votacion_solicitud__solicitud_id ON plazas.votacion_solicitud (solicitud_id);

CREATE TABLE plazas.acta_asistencia
(
    id                            int IDENTITY(1,1) NOT NULL,
    acta_consejo_tecnico_id       int               NOT NULL,
    integrante_consejo_tecnico_id int               NOT NULL,
    nombre                        nvarchar(200)     NOT NULL,
    tratamiento                   nvarchar(30)      NOT NULL,
    cargo                         nvarchar(200)     NOT NULL,
    asistio                       bit               NOT NULL,
    firmo                         bit               NOT NULL,
    CONSTRAINT pk_acta_asistencia PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_acta_asistencia__acta_integrante UNIQUE (acta_consejo_tecnico_id, integrante_consejo_tecnico_id),
    CONSTRAINT fk_acta_asistencia__acta_consejo_tecnico FOREIGN KEY (acta_consejo_tecnico_id) REFERENCES plazas.acta_consejo_tecnico (id),
    CONSTRAINT fk_acta_asistencia__integrante_consejo_tecnico FOREIGN KEY (integrante_consejo_tecnico_id) REFERENCES plazas.integrante_consejo_tecnico (id),
    CONSTRAINT ck_acta_asistencia__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_acta_asistencia__tratamiento_no_vacio CHECK (LEN(TRIM(tratamiento)) > 0),
    CONSTRAINT ck_acta_asistencia__cargo_no_vacio CHECK (LEN(TRIM(cargo)) > 0),
    CONSTRAINT ck_acta_asistencia__firma_requiere_asistencia CHECK (firmo = 0 OR asistio = 1)
);

CREATE INDEX ix_acta_asistencia__integrante_consejo_tecnico_id ON plazas.acta_asistencia (integrante_consejo_tecnico_id);

CREATE TABLE plazas.documento_acta
(
    id                      int IDENTITY(1,1) NOT NULL,
    acta_consejo_tecnico_id int               NOT NULL,
    tipo                    varchar(10)       NOT NULL,
    nombre                  nvarchar(260)     NOT NULL,
    mime                    varchar(255)      NOT NULL,
    tamano                  bigint            NOT NULL,
    checksum_sha256         binary(32)        NOT NULL,
    clave_almacenamiento    nvarchar(500)     NOT NULL,
    numero_version          int               NOT NULL,
    es_vigente              bit               NOT NULL,
    cargado_en              datetime2(0)      NOT NULL,
    cargado_por_usuario_id  int               NOT NULL,
    CONSTRAINT pk_documento_acta PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_documento_acta__acta_tipo_version UNIQUE (acta_consejo_tecnico_id, tipo, numero_version),
    CONSTRAINT uq_documento_acta__clave_almacenamiento UNIQUE (clave_almacenamiento),
    CONSTRAINT fk_documento_acta__acta_consejo_tecnico FOREIGN KEY (acta_consejo_tecnico_id) REFERENCES plazas.acta_consejo_tecnico (id),
    CONSTRAINT fk_documento_acta__usuario FOREIGN KEY (cargado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_documento_acta__tipo CHECK (tipo IN ('ORIGINAL', 'FIRMADO')),
    CONSTRAINT ck_documento_acta__nombre_no_vacio CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT ck_documento_acta__mime_no_vacio CHECK (LEN(TRIM(mime)) > 0),
    CONSTRAINT ck_documento_acta__tamano_positivo CHECK (tamano > 0),
    CONSTRAINT ck_documento_acta__numero_version_positivo CHECK (numero_version > 0),
    CONSTRAINT ck_documento_acta__clave_almacenamiento_no_vacia CHECK (LEN(TRIM(clave_almacenamiento)) > 0)
);

CREATE UNIQUE INDEX ux_documento_acta__vigente ON plazas.documento_acta (acta_consejo_tecnico_id, tipo) WHERE es_vigente = 1;

CREATE TABLE plazas.revision_acta
(
    id                      int IDENTITY(1,1) NOT NULL,
    acta_consejo_tecnico_id int               NOT NULL,
    numero_revision         int               NOT NULL,
    documento_original_id   int               NOT NULL,
    enviado_por_usuario_id  int               NOT NULL,
    enviado_en              datetime2(0)      NOT NULL,
    resuelto_por_usuario_id int               NULL,
    resuelto_en             datetime2(0)      NULL,
    resultado               varchar(10)       NULL,
    comentarios             nvarchar(max)     NULL,
    CONSTRAINT pk_revision_acta PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uq_revision_acta__acta_numero_revision UNIQUE (acta_consejo_tecnico_id, numero_revision),
    CONSTRAINT fk_revision_acta__acta_consejo_tecnico FOREIGN KEY (acta_consejo_tecnico_id) REFERENCES plazas.acta_consejo_tecnico (id),
    CONSTRAINT fk_revision_acta__documento_acta FOREIGN KEY (documento_original_id) REFERENCES plazas.documento_acta (id),
    CONSTRAINT fk_revision_acta__usuario__enviado_por FOREIGN KEY (enviado_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT fk_revision_acta__usuario__resuelto_por FOREIGN KEY (resuelto_por_usuario_id) REFERENCES usuarios.usuario (id),
    CONSTRAINT ck_revision_acta__numero_revision_positivo CHECK (numero_revision > 0),
    CONSTRAINT ck_revision_acta__resultado CHECK (resultado IS NULL OR resultado IN ('AVALADA', 'DEVUELTA')),
    CONSTRAINT ck_revision_acta__resolucion_conjunta CHECK (
           (resuelto_en IS NULL AND resuelto_por_usuario_id IS NULL AND resultado IS NULL)
        OR (resuelto_en IS NOT NULL AND resuelto_por_usuario_id IS NOT NULL AND resultado IS NOT NULL AND resuelto_en >= enviado_en)),
    CONSTRAINT ck_revision_acta__comentarios_devolucion CHECK (resultado IS NULL OR resultado <> 'DEVUELTA' OR (comentarios IS NOT NULL AND LEN(TRIM(comentarios)) > 0))
);

CREATE UNIQUE INDEX ux_revision_acta__abierta ON plazas.revision_acta (acta_consejo_tecnico_id) WHERE resultado IS NULL;
CREATE INDEX ix_revision_acta__documento_original_id ON plazas.revision_acta (documento_original_id);

-- 13. Académico: asignación docente (§15.4)

CREATE TABLE academico.asignacion_docente
(
    id                        int IDENTITY(1,1) NOT NULL,
    programacion_academica_id int               NOT NULL,
    docente_id                int               NOT NULL,
    origen                    varchar(10)       NOT NULL,
    fecha_inicio              date              NOT NULL,
    fecha_fin                 date              NULL,
    sincronizacion_planea_id  int               NULL,
    acta_oferta_id            int               NULL,
    CONSTRAINT pk_asignacion_docente PRIMARY KEY CLUSTERED (id),
    CONSTRAINT fk_asignacion_docente__programacion_academica FOREIGN KEY (programacion_academica_id) REFERENCES academico.programacion_academica (id),
    CONSTRAINT fk_asignacion_docente__docente FOREIGN KEY (docente_id) REFERENCES academico.docente (id),
    CONSTRAINT fk_asignacion_docente__sincronizacion_planea FOREIGN KEY (sincronizacion_planea_id) REFERENCES integracion.sincronizacion_planea (id),
    CONSTRAINT fk_asignacion_docente__acta_oferta FOREIGN KEY (acta_oferta_id) REFERENCES plazas.acta_oferta (id),
    CONSTRAINT ck_asignacion_docente__origen CHECK (origen IN ('PLANEA', 'SGPLA')),
    CONSTRAINT ck_asignacion_docente__rango_fechas CHECK (fecha_fin IS NULL OR fecha_fin >= fecha_inicio),
    CONSTRAINT ck_asignacion_docente__origen_referencia CHECK (
           (origen = 'PLANEA' AND sincronizacion_planea_id IS NOT NULL AND acta_oferta_id IS NULL)
        OR (origen = 'SGPLA' AND acta_oferta_id IS NOT NULL AND sincronizacion_planea_id IS NULL))
);

-- Un ActaOferta produce como máximo una asignación.
CREATE UNIQUE INDEX ux_asignacion_docente__acta_oferta_id ON academico.asignacion_docente (acta_oferta_id) WHERE acta_oferta_id IS NOT NULL;
CREATE INDEX ix_asignacion_docente__programacion_academica_id ON academico.asignacion_docente (programacion_academica_id, fecha_inicio);
CREATE INDEX ix_asignacion_docente__docente_id ON academico.asignacion_docente (docente_id);
CREATE INDEX ix_asignacion_docente__sincronizacion_planea_id ON academico.asignacion_docente (sincronizacion_planea_id) WHERE sincronizacion_planea_id IS NOT NULL;
