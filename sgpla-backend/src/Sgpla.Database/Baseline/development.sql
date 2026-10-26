-- SGPLa: datos de prueba solo para desarrollo.
-- No forma parte del baseline ni del migrador: se ejecuta a mano, después de
-- baseline.sql y seed.sql, y nunca en producción.
-- Las FK se resuelven por clave de negocio, no por id.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. academico.entidad_academica

INSERT INTO academico.entidad_academica
    (clave, nombre, calle, numero_exterior, colonia, codigo_postal, telefono, extension,
     campus_id, area_academica_id, municipio_id)
VALUES ('FEI',
        N'Facultad de Estadística e Informática',
        N'Avenida Xalapa esquina Avenida Manuel Ávila Camacho',
        NULL,
        N'Obrero Campesina',
        '91020',
        '2288421700',
        NULL,
        (SELECT id FROM academico.campus WHERE clave = 'X'),
        (SELECT id FROM academico.area_academica WHERE clave = 4),
        (SELECT id FROM academico.municipio WHERE nombre = N'Xalapa'));

-- 2. usuarios.usuario

INSERT INTO usuarios.usuario (correo, nombre, rol_id)
VALUES ('zs22013630@estudiantes.uv.mx', N'Iván Jafeth Carballo Delgado', 3),
       ('zs22013663@estudiantes.uv.mx', N'Ian Kaleb Moctezuma Rojas',    2);

-- 3. usuarios.usuario_entidad_academica

INSERT INTO usuarios.usuario_entidad_academica (usuario_id, entidad_academica_id)
VALUES ((SELECT id FROM usuarios.usuario WHERE correo = 'zs22013630@estudiantes.uv.mx'),
        (SELECT id FROM academico.entidad_academica WHERE clave = 'FEI'));

-- 4. usuarios.usuario_dgaa

INSERT INTO usuarios.usuario_dgaa (usuario_id, area_academica_id)
VALUES ((SELECT id FROM usuarios.usuario WHERE correo = 'zs22013663@estudiantes.uv.mx'),
        (SELECT id FROM academico.area_academica WHERE clave = 4));
