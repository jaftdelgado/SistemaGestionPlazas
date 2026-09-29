# Diagrama completo de la base de datos de SGPLa

Este documento presenta una vista entidad-relación consolidada del modelo definido en `DATABASE.md`. Incluye los esquemas `academico`, `integracion`, `usuarios` y `plazas`.

## Convenciones

- Los nombres de las entidades usan el prefijo del esquema porque Mermaid no representa esquemas SQL de forma nativa.
- `PK` identifica llaves primarias, `FK` llaves foráneas y `UK` unicidad simple.
- Las restricciones compuestas, índices filtrados, reglas por estado y validaciones transaccionales permanecen documentadas en `DATABASE.md`.
- Una relación opcional hacia `USUARIOS_USUARIO` representa campos de actor como `cancelado_por_usuario_id`, `archivado_por_usuario_id` o `resuelto_por_usuario_id`.
- Las relaciones derivadas —por ejemplo, la región de una Oferta a través de su Programación Académica— no se dibujan como llaves foráneas directas.

## Diagrama ER consolidado

```mermaid
erDiagram
    ACADEMICO_REGION ||--o{ ACADEMICO_CAMPUS : contiene
    ACADEMICO_CAMPUS ||--o{ ACADEMICO_ENTIDAD_ACADEMICA : ubica
    ACADEMICO_AREA_ACADEMICA ||--o{ ACADEMICO_ENTIDAD_ACADEMICA : clasifica
    ACADEMICO_MUNICIPIO ||--o{ ACADEMICO_ENTIDAD_ACADEMICA : localiza
    ACADEMICO_ENTIDAD_ACADEMICA ||--o{ ACADEMICO_PROGRAMA_EDUCATIVO : ofrece
    ACADEMICO_SISTEMA_EDUCATIVO ||--o{ ACADEMICO_PROGRAMA_EDUCATIVO : clasifica
    ACADEMICO_NIVEL_FORMACION ||--o{ ACADEMICO_PROGRAMA_EDUCATIVO : clasifica
    ACADEMICO_PROGRAMA_EDUCATIVO ||--o{ ACADEMICO_PLAN_ESTUDIOS : contiene
    ACADEMICO_PLAN_ESTUDIOS ||--o{ ACADEMICO_EXPERIENCIA_EDUCATIVA : contiene
    ACADEMICO_AREA_FORMACION ||--o{ ACADEMICO_EXPERIENCIA_EDUCATIVA : clasifica
    ACADEMICO_EXPERIENCIA_EDUCATIVA ||--o{ ACADEMICO_PROGRAMACION_ACADEMICA : oferta
    ACADEMICO_EXPERIENCIA_EDUCATIVA ||--o{ ACADEMICO_SOLICITUD_APERTURA : solicita
    ACADEMICO_PERIODO_ESCOLAR ||--o{ ACADEMICO_PROGRAMACION_ACADEMICA : agrupa
    ACADEMICO_PERIODO_ESCOLAR ||--o{ ACADEMICO_SOLICITUD_APERTURA : destina
    ACADEMICO_PROGRAMACION_ACADEMICA ||--o{ ACADEMICO_HORARIO_PROGRAMACION : calendariza
    ACADEMICO_PERIODO_ESCOLAR ||--o{ INTEGRACION_SINCRONIZACION_PLANEA : sincroniza
    INTEGRACION_SINCRONIZACION_PLANEA ||--o{ ACADEMICO_HORARIO_PROGRAMACION : produce

    USUARIOS_ROL ||--o{ USUARIOS_USUARIO : asigna
    USUARIOS_USUARIO ||--o| USUARIOS_CREDENCIAL_SUPERUSUARIO : autentica
    USUARIOS_USUARIO ||--o| USUARIOS_USUARIO_DGAA : delimita
    USUARIOS_USUARIO ||--o| USUARIOS_USUARIO_ENTIDAD_ACADEMICA : delimita
    USUARIOS_USUARIO ||--o{ ACADEMICO_SOLICITUD_APERTURA : crea
    USUARIOS_USUARIO o|--o{ ACADEMICO_SOLICITUD_APERTURA : actualiza
    USUARIOS_USUARIO o|--o{ ACADEMICO_SOLICITUD_APERTURA : resuelve
    USUARIOS_USUARIO o|--o{ ACADEMICO_SOLICITUD_APERTURA : cancela
    USUARIOS_USUARIO o|--o{ ACADEMICO_SOLICITUD_APERTURA : vincula
    USUARIOS_USUARIO ||--o{ ACADEMICO_ARCHIVO_SOLICITUD_APERTURA : carga
    ACADEMICO_AREA_ACADEMICA ||--o{ USUARIOS_USUARIO_DGAA : autoriza
    ACADEMICO_ENTIDAD_ACADEMICA ||--o{ USUARIOS_USUARIO_ENTIDAD_ACADEMICA : autoriza

    ACADEMICO_DOCENTE ||--o{ ACADEMICO_ASIGNACION_DOCENTE : ocupa
    ACADEMICO_DOCENTE ||--o{ ACADEMICO_FORMACION_DOCENTE : acredita
    ACADEMICO_DOCENTE ||--o{ ACADEMICO_DOCUMENTO_DOCENTE : conserva
    ACADEMICO_GRADO_ACADEMICO ||--o{ ACADEMICO_FORMACION_DOCENTE : clasifica
    ACADEMICO_TIPO_DOCUMENTO_EXPEDIENTE ||--o{ ACADEMICO_DOCUMENTO_DOCENTE : clasifica
    ACADEMICO_DOCUMENTO_DOCENTE ||--o{ ACADEMICO_VERSION_DOCUMENTO_DOCENTE : versiona
    ACADEMICO_ARCHIVO_SOLICITUD_APERTURA ||--o| ACADEMICO_SOLICITUD_APERTURA : respalda
    ACADEMICO_PROGRAMACION_ACADEMICA ||--o{ ACADEMICO_ASIGNACION_DOCENTE : recibe
    ACADEMICO_PROGRAMACION_ACADEMICA o|--o| ACADEMICO_SOLICITUD_APERTURA : materializa
    INTEGRACION_SINCRONIZACION_PLANEA o|--o{ ACADEMICO_ASIGNACION_DOCENTE : detecta
    PLAZAS_ACTA_OFERTA o|--o| ACADEMICO_ASIGNACION_DOCENTE : formaliza

    ACADEMICO_ENTIDAD_ACADEMICA ||--o{ PLAZAS_INTEGRANTE_CONSEJO_TECNICO : integra
    ACADEMICO_GRADO_ACADEMICO ||--o{ PLAZAS_TRATAMIENTO_ACADEMICO : contiene
    PLAZAS_TRATAMIENTO_ACADEMICO ||--o{ PLAZAS_INTEGRANTE_CONSEJO_TECNICO : identifica

    ACADEMICO_PROGRAMACION_ACADEMICA ||--o{ PLAZAS_OFERTA : origina
    PLAZAS_TIPO_PLAZA ||--o{ PLAZAS_OFERTA : clasifica
    PLAZAS_TIPO_CONTRATACION ||--o{ PLAZAS_OFERTA : contrata

    ACADEMICO_ENTIDAD_ACADEMICA ||--o{ PLAZAS_AVISO : emite
    ACADEMICO_PERIODO_ESCOLAR ||--o{ PLAZAS_AVISO : delimita
    ACADEMICO_SISTEMA_EDUCATIVO ||--o{ PLAZAS_AVISO : delimita
    PLAZAS_ARTICULO ||--o{ PLAZAS_AVISO : fundamenta
    PLAZAS_MODALIDAD_RECEPCION o|--o{ PLAZAS_AVISO : configura
    USUARIOS_USUARIO o|--o{ PLAZAS_AVISO : cancela
    USUARIOS_USUARIO o|--o{ PLAZAS_AVISO : archiva
    PLAZAS_AVISO ||--o{ PLAZAS_HORARIO_RECEPCION_REQUISITO : agenda
    PLAZAS_AVISO ||--o{ PLAZAS_AVISO_OFERTA : publica
    PLAZAS_OFERTA ||--o{ PLAZAS_AVISO_OFERTA : participa

    PLAZAS_AVISO ||--o{ PLAZAS_DOCUMENTO_AVISO : versiona
    USUARIOS_USUARIO ||--o{ PLAZAS_DOCUMENTO_AVISO : carga
    PLAZAS_AVISO ||--o{ PLAZAS_REVISION_AVISO : revisa
    PLAZAS_DOCUMENTO_AVISO ||--o{ PLAZAS_REVISION_AVISO : evalua
    USUARIOS_USUARIO ||--o{ PLAZAS_REVISION_AVISO : envia
    USUARIOS_USUARIO o|--o{ PLAZAS_REVISION_AVISO : resuelve

    PLAZAS_ASPIRANTE ||--o{ PLAZAS_PERFIL_ASPIRANTE : versiona
    USUARIOS_USUARIO ||--o{ PLAZAS_PERFIL_ASPIRANTE : captura
    PLAZAS_PERFIL_ASPIRANTE ||--o{ PLAZAS_FORMACION_ASPIRANTE : describe
    ACADEMICO_GRADO_ACADEMICO ||--o{ PLAZAS_FORMACION_ASPIRANTE : clasifica
    PLAZAS_ASPIRANTE ||--o{ PLAZAS_DOCUMENTO_ASPIRANTE : conserva
    ACADEMICO_TIPO_DOCUMENTO_EXPEDIENTE ||--o{ PLAZAS_DOCUMENTO_ASPIRANTE : clasifica
    PLAZAS_DOCUMENTO_ASPIRANTE ||--o{ PLAZAS_VERSION_DOCUMENTO_ASPIRANTE : versiona
    USUARIOS_USUARIO ||--o{ PLAZAS_VERSION_DOCUMENTO_ASPIRANTE : carga

    PLAZAS_AVISO_OFERTA ||--o{ PLAZAS_SOLICITUD : recibe
    PLAZAS_ASPIRANTE ||--o{ PLAZAS_SOLICITUD : presenta
    PLAZAS_PERFIL_ASPIRANTE ||--o{ PLAZAS_SOLICITUD : fija
    USUARIOS_USUARIO o|--o{ PLAZAS_SOLICITUD : admite
    USUARIOS_USUARIO o|--o{ PLAZAS_SOLICITUD : rechaza
    PLAZAS_SOLICITUD ||--o{ PLAZAS_SOLICITUD_DOCUMENTO : presenta
    PLAZAS_VERSION_DOCUMENTO_ASPIRANTE ||--o{ PLAZAS_SOLICITUD_DOCUMENTO : evidencia

    PLAZAS_AVISO ||--o{ PLAZAS_ACTA_CONSEJO_TECNICO : documenta
    ACADEMICO_ENTIDAD_ACADEMICA ||--o{ PLAZAS_ACTA_CONSEJO_TECNICO : celebra
    USUARIOS_USUARIO o|--o{ PLAZAS_ACTA_CONSEJO_TECNICO : archiva
    USUARIOS_USUARIO o|--o{ PLAZAS_ACTA_CONSEJO_TECNICO : elimina
    PLAZAS_ACTA_CONSEJO_TECNICO ||--o{ PLAZAS_ACTA_OFERTA : resuelve
    PLAZAS_AVISO_OFERTA ||--o{ PLAZAS_ACTA_OFERTA : recibe
    PLAZAS_SOLICITUD o|--o{ PLAZAS_ACTA_OFERTA : designa
    ACADEMICO_DOCENTE o|--o{ PLAZAS_ACTA_OFERTA : asigna
    USUARIOS_USUARIO o|--o{ PLAZAS_ACTA_OFERTA : elimina
    PLAZAS_ACTA_OFERTA ||--o{ PLAZAS_VOTACION_SOLICITUD : totaliza
    PLAZAS_SOLICITUD ||--o{ PLAZAS_VOTACION_SOLICITUD : obtiene
    PLAZAS_ACTA_CONSEJO_TECNICO ||--o{ PLAZAS_ACTA_ASISTENCIA : registra
    PLAZAS_INTEGRANTE_CONSEJO_TECNICO ||--o{ PLAZAS_ACTA_ASISTENCIA : participa

    PLAZAS_ACTA_CONSEJO_TECNICO ||--o{ PLAZAS_DOCUMENTO_ACTA : versiona
    USUARIOS_USUARIO ||--o{ PLAZAS_DOCUMENTO_ACTA : carga
    PLAZAS_ACTA_CONSEJO_TECNICO ||--o{ PLAZAS_REVISION_ACTA : revisa
    PLAZAS_DOCUMENTO_ACTA ||--o{ PLAZAS_REVISION_ACTA : evalua
    USUARIOS_USUARIO ||--o{ PLAZAS_REVISION_ACTA : envia
    USUARIOS_USUARIO o|--o{ PLAZAS_REVISION_ACTA : resuelve

    ACADEMICO_REGION {
        int id PK
        int clave UK
        nvarchar nombre
    }

    ACADEMICO_CAMPUS {
        int id PK
        varchar clave UK
        nvarchar nombre
        int region_id FK
    }

    ACADEMICO_AREA_ACADEMICA {
        int id PK
        int clave UK
        nvarchar nombre
        char telefono
        varchar extension
        datetime2 fecha_eliminacion
    }

    ACADEMICO_MUNICIPIO {
        int id PK
        nvarchar nombre UK
    }

    ACADEMICO_ENTIDAD_ACADEMICA {
        int id PK
        varchar clave UK
        nvarchar nombre
        nvarchar calle
        nvarchar numero_exterior
        nvarchar colonia
        char codigo_postal
        char telefono
        varchar extension
        int campus_id FK
        int area_academica_id FK
        int municipio_id FK
        datetime2 fecha_eliminacion
    }

    ACADEMICO_SISTEMA_EDUCATIVO {
        int id PK
        nvarchar nombre UK
    }

    ACADEMICO_NIVEL_FORMACION {
        int id PK
        varchar clave UK
        nvarchar nombre UK
    }

    ACADEMICO_PROGRAMA_EDUCATIVO {
        int id PK
        nvarchar nombre
        int entidad_academica_id FK
        int sistema_educativo_id FK
        int nivel_formacion_id FK
        datetime2 fecha_eliminacion
    }

    ACADEMICO_PLAN_ESTUDIOS {
        int id PK
        varchar codigo
        int programa_educativo_id FK
        datetime2 fecha_eliminacion
    }

    ACADEMICO_AREA_FORMACION {
        int id PK
        varchar clave UK
        nvarchar nombre
    }

    ACADEMICO_EXPERIENCIA_EDUCATIVA {
        int id PK
        nvarchar nombre
        varchar materia_ee
        varchar curso_ee
        int horas_teoricas
        int horas_practicas
        int creditos
        int cupo_minimo
        int cupo_maximo
        nvarchar perfil_docente
        int area_formacion_id FK
        int plan_estudios_id FK
        datetime2 fecha_eliminacion
    }

    ACADEMICO_ARCHIVO_SOLICITUD_APERTURA {
        int id PK
        nvarchar nombre
        varchar mime
        bigint tamano
        binary checksum_sha256
        nvarchar clave_almacenamiento UK
        datetime2 cargado_en
        int cargado_por_usuario_id FK
    }

    ACADEMICO_SOLICITUD_APERTURA {
        int id PK
        int experiencia_educativa_id FK
        int periodo_escolar_id FK
        varchar seccion
        int cantidad_estudiantes
        nvarchar justificacion
        int oficio_respaldo_id FK, UK
        varchar estado
        datetime2 creada_en
        int creada_por_usuario_id FK
        datetime2 actualizada_en "nullable"
        int actualizada_por_usuario_id FK "nullable"
        datetime2 resuelta_en "nullable"
        int resuelta_por_usuario_id FK "nullable"
        nvarchar comentarios_resolucion "nullable"
        datetime2 cancelada_en "nullable"
        int cancelada_por_usuario_id FK "nullable"
        nvarchar motivo_cancelacion "nullable"
        int programacion_academica_id FK "nullable, UK filtrada"
        datetime2 vinculada_en "nullable"
        int vinculada_por_usuario_id FK "nullable"
    }

    ACADEMICO_PERIODO_ESCOLAR {
        int id PK
        char clave UK
        date fecha_inicio
        date fecha_fin
        datetime2 fecha_eliminacion
    }

    ACADEMICO_PROGRAMACION_ACADEMICA {
        int id PK
        varchar nrc
        int periodo_escolar_id FK
        int experiencia_educativa_id FK
        datetime2 fecha_eliminacion
    }

    INTEGRACION_SINCRONIZACION_PLANEA {
        int id PK
        int periodo_escolar_id FK
        varchar estado
        datetime2 iniciada_en
        datetime2 finalizada_en
        int registros_recibidos
        int registros_ignorados
        int sesiones_generadas
        int duplicados_descartados
        int advertencias
        nvarchar mensaje_error
    }

    ACADEMICO_HORARIO_PROGRAMACION {
        int id PK
        int programacion_academica_id FK
        int sincronizacion_planea_id FK
        tinyint dia_semana
        time hora_inicio
        time hora_fin
        date fecha_inicio
        date fecha_fin
        nvarchar edificio
        nvarchar aula
    }

    USUARIOS_ROL {
        tinyint id PK
        nvarchar nombre UK
    }

    USUARIOS_USUARIO {
        int id PK
        varchar correo
        nvarchar nombre
        tinyint rol_id FK
        datetime2 fecha_eliminacion
    }

    USUARIOS_USUARIO_DGAA {
        int usuario_id PK, FK
        int area_academica_id FK
    }

    USUARIOS_USUARIO_ENTIDAD_ACADEMICA {
        int usuario_id PK, FK
        int entidad_academica_id FK
    }

    USUARIOS_CREDENCIAL_SUPERUSUARIO {
        int usuario_id PK, FK
        varchar contrasena
        datetime2 fecha_actualizacion
        datetime2 fecha_eliminacion
    }

    ACADEMICO_DOCENTE {
        int id PK
        nvarchar nombre
        varchar num_personal UK
        nvarchar puesto
        nvarchar descripcion_perfil
    }

    ACADEMICO_FORMACION_DOCENTE {
        int id PK
        int docente_id FK
        int grado_academico_id FK
        nvarchar descripcion
    }

    ACADEMICO_DOCUMENTO_DOCENTE {
        int id PK
        int docente_id FK
        int tipo_documento_expediente_id FK
    }

    ACADEMICO_VERSION_DOCUMENTO_DOCENTE {
        int id PK
        int documento_docente_id FK
        nvarchar nombre
        varchar mime
        bigint tamano
        binary checksum_sha256
        nvarchar clave_almacenamiento UK
        int numero_version
        bit es_vigente
        datetime2 cargado_en
        int cargado_por_usuario_id FK
    }

    ACADEMICO_ASIGNACION_DOCENTE {
        int id PK
        int programacion_academica_id FK
        int docente_id FK
        varchar origen
        date fecha_inicio
        date fecha_fin
        int sincronizacion_planea_id FK
        int acta_oferta_id FK
    }

    ACADEMICO_GRADO_ACADEMICO {
        int id PK
        nvarchar nombre UK
    }

    PLAZAS_TRATAMIENTO_ACADEMICO {
        int id PK
        nvarchar nombre UK
        int grado_academico_id FK
    }

    PLAZAS_ARTICULO {
        int id PK
        varchar numero UK
        nvarchar descripcion
    }

    PLAZAS_MODALIDAD_RECEPCION {
        int id PK
        nvarchar nombre UK
        bit requiere_lugar
    }

    PLAZAS_TIPO_PLAZA {
        int id PK
        nvarchar nombre UK
    }

    PLAZAS_TIPO_CONTRATACION {
        int id PK
        nvarchar nombre UK
    }

    ACADEMICO_TIPO_DOCUMENTO_EXPEDIENTE {
        int id PK
        nvarchar nombre UK
    }

    PLAZAS_INTEGRANTE_CONSEJO_TECNICO {
        int id PK
        int entidad_academica_id FK
        nvarchar nombre
        nvarchar cargo
        int tratamiento_academico_id FK
        date fecha_inicio
        date fecha_fin
    }

    PLAZAS_OFERTA {
        int id PK
        int programacion_academica_id FK
        varchar clave_plaza
        int tipo_plaza_id FK
        int tipo_contratacion_id FK
        nvarchar perfil_solicitado
        nvarchar justificacion
        varchar estado
        datetime2 cerrada_en
    }

    PLAZAS_AVISO {
        int id PK
        int entidad_academica_id FK
        int periodo_escolar_id FK
        int sistema_educativo_id FK
        int articulo_id FK
        varchar tipo_comunicado
        int modalidad_recepcion_id FK
        nvarchar requisitos
        nvarchar lugar_recepcion
        varchar correo_contacto
        nvarchar nombre_titular
        datetime2 creado_en
        date fecha_publicacion
        date fecha_consejo_tecnico
        date fecha_vacantes
        varchar url_publicacion
        varchar estado
        datetime2 cancelado_en
        int cancelado_por_usuario_id FK
        nvarchar motivo_cancelacion
        datetime2 archivado_en
        int archivado_por_usuario_id FK
    }

    PLAZAS_HORARIO_RECEPCION_REQUISITO {
        int id PK
        int aviso_id FK
        date fecha
        time hora_inicio
        time hora_fin
    }

    PLAZAS_AVISO_OFERTA {
        int id PK
        int aviso_id FK
        int oferta_id FK
        datetime2 incorporado_en
        datetime2 cerrado_en
        varchar causa_cierre
    }

    PLAZAS_DOCUMENTO_AVISO {
        int id PK
        int aviso_id FK
        varchar tipo
        nvarchar nombre
        varchar mime
        bigint tamano
        binary checksum_sha256
        nvarchar clave_almacenamiento UK
        int numero_version
        bit es_vigente
        datetime2 cargado_en
        int cargado_por_usuario_id FK
    }

    PLAZAS_REVISION_AVISO {
        int id PK
        int aviso_id FK
        int numero_revision
        int documento_original_id FK
        int enviado_por_usuario_id FK
        datetime2 enviado_en
        int resuelto_por_usuario_id FK
        datetime2 resuelto_en
        varchar resultado
        nvarchar comentarios
    }

    PLAZAS_ASPIRANTE {
        int id PK
    }

    PLAZAS_PERFIL_ASPIRANTE {
        int id PK
        int aspirante_id FK
        int numero_version
        nvarchar nombre
        varchar correo
        nvarchar puesto_actual
        nvarchar descripcion_perfil
        bit es_vigente
        datetime2 creado_en
        int creado_por_usuario_id FK
    }

    PLAZAS_FORMACION_ASPIRANTE {
        int id PK
        int perfil_aspirante_id FK
        int grado_academico_id FK
        nvarchar descripcion
    }

    PLAZAS_DOCUMENTO_ASPIRANTE {
        int id PK
        int aspirante_id FK
        int tipo_documento_expediente_id FK
    }

    PLAZAS_VERSION_DOCUMENTO_ASPIRANTE {
        int id PK
        int documento_aspirante_id FK
        nvarchar nombre
        varchar mime
        bigint tamano
        binary checksum_sha256
        nvarchar clave_almacenamiento UK
        int numero_version
        bit es_vigente
        datetime2 cargado_en
        int cargado_por_usuario_id FK
    }

    PLAZAS_SOLICITUD {
        int id PK
        int aviso_oferta_id FK
        int aspirante_id FK
        int perfil_aspirante_id FK
        datetime2 registrada_en
        varchar estado
        nvarchar observaciones
        datetime2 admitida_en
        int admitida_por_usuario_id FK
        datetime2 no_admitida_en
        int no_admitida_por_usuario_id FK
        nvarchar motivo_no_admision
        datetime2 retirada_en
        nvarchar motivo_retiro
    }

    PLAZAS_SOLICITUD_DOCUMENTO {
        int solicitud_id PK, FK
        int version_documento_aspirante_id PK, FK
    }

    PLAZAS_ACTA_CONSEJO_TECNICO {
        int id PK
        int aviso_id FK
        int entidad_academica_id FK
        varchar folio
        date fecha
        nvarchar lugar
        time hora_inicio
        time hora_fin
        nvarchar asuntos_generales
        varchar estado
        datetime2 archivado_en
        int archivado_por_usuario_id FK
        datetime2 fecha_eliminacion
        int eliminado_por_usuario_id FK
    }

    PLAZAS_ACTA_OFERTA {
        int id PK
        int acta_consejo_tecnico_id FK
        int aviso_oferta_id FK
        varchar resultado
        nvarchar observaciones
        int solicitud_designada_id FK
        int docente_asignado_id FK
        datetime2 fecha_eliminacion
        int eliminado_por_usuario_id FK
    }

    PLAZAS_VOTACION_SOLICITUD {
        int acta_oferta_id PK, FK
        int solicitud_id PK, FK
        int votos
    }

    PLAZAS_ACTA_ASISTENCIA {
        int id PK
        int acta_consejo_tecnico_id FK
        int integrante_consejo_tecnico_id FK
        nvarchar nombre
        nvarchar tratamiento
        nvarchar cargo
        bit asistio
        bit firmo
    }

    PLAZAS_DOCUMENTO_ACTA {
        int id PK
        int acta_consejo_tecnico_id FK
        varchar tipo
        nvarchar nombre
        varchar mime
        bigint tamano
        binary checksum_sha256
        nvarchar clave_almacenamiento UK
        int numero_version
        bit es_vigente
        datetime2 cargado_en
        int cargado_por_usuario_id FK
    }

    PLAZAS_REVISION_ACTA {
        int id PK
        int acta_consejo_tecnico_id FK
        int numero_revision
        int documento_original_id FK
        int enviado_por_usuario_id FK
        datetime2 enviado_en
        int resuelto_por_usuario_id FK
        datetime2 resuelto_en
        varchar resultado
        nvarchar comentarios
    }
```

## Correspondencia de prefijos

| Prefijo Mermaid | Esquema SQL Server |
|---|---|
| `ACADEMICO_` | `academico` |
| `INTEGRACION_` | `integracion` |
| `USUARIOS_` | `usuarios` |
| `PLAZAS_` | `plazas` |

## Reglas que complementan el diagrama

El diagrama muestra la estructura relacional, pero las siguientes reglas no pueden expresarse completamente con la notación ER de Mermaid:

- solo puede existir una Oferta abierta por `(programacion_academica_id, clave_plaza)`;
- una Oferta puede participar en varios Avisos históricos, pero solo en un `aviso_oferta` abierto;
- SolicitudApertura almacena la EE y el periodo; Plan, Programa, Entidad y modalidad se derivan de la cadena académica;
- solo puede existir una SolicitudApertura PENDIENTE por EE, periodo y sección; puede haber múltiples aceptadas, rechazadas o canceladas;
- una SolicitudApertura ACEPTADA puede vincularse una sola vez con una Programación compatible y el vínculo es inmutable;
- los cupos se validan con los valores vigentes al crear, editar y aceptar;
- el periodo siguiente se obtiene de configuración externa global y el Superusuario no participa en este proceso;
- cada `aviso_oferta` puede tener como máximo un `acta_oferta` activo;
- la Solicitud designada debe pertenecer al mismo `aviso_oferta` que el `acta_oferta`;
- las versiones vigentes de perfiles y documentos se controlan con restricciones únicas filtradas;
- las relaciones opcionales se validan según el estado, por ejemplo cancelación, archivado, resolución, designación y asignación docente;
- las bajas, restauraciones, cierres y designaciones que afectan varias tablas se ejecutan transaccionalmente.
- el articulo pertenece al Aviso: la Oferta no tiene articulo_id y puede republicarse posteriormente bajo otro articulo.
- la primera incorporacion de una Oferta congela en el Aviso la entidad, periodo, sistema y articulo; la compatibilidad se valida transaccionalmente contra Programacion Academica.
- los archivos del Plan de Estudios son un registro no versionado; los documentos de Docente y Aspirante conservan sus versiones.

La definición normativa de atributos, restricciones y reglas de negocio permanece en `DATABASE.md`.