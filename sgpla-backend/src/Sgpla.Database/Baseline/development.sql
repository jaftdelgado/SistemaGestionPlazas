-- SGPLa: datos de prueba solo para desarrollo.
-- No forma parte del baseline ni del migrador: se ejecuta a mano, después de
-- baseline.sql y seed.sql, y nunca en producción.
-- Las FK se resuelven por clave de negocio, no por id.

-- 1. academico.entidad_academica

INSERT INTO academico.entidad_academica
    (clave, nombre, calle, numero_exterior, colonia, codigo_postal, telefono, extension,
     campus_id, area_academica_id, municipio_id)
VALUES ('FEI',
        'Facultad de Estadística e Informática',
        'Avenida Xalapa esquina Avenida Manuel Ávila Camacho',
        NULL,
        'Obrero Campesina',
        '91020',
        '2288421700',
        NULL,
        (SELECT id FROM academico.campus WHERE clave = 'X'),
        (SELECT id FROM academico.area_academica WHERE clave = 4),
        (SELECT id FROM academico.municipio WHERE nombre = 'Xalapa'));

-- 2. usuarios.usuario

INSERT INTO usuarios.usuario (correo, nombre, rol_id)
VALUES ('zs22013630@estudiantes.uv.mx', 'Iván Jafeth Carballo Delgado', 3),
       ('zs22013663@estudiantes.uv.mx', 'Ian Kaleb Moctezuma Rojas',    2);

-- 3. usuarios.usuario_entidad_academica

INSERT INTO usuarios.usuario_entidad_academica (usuario_id, entidad_academica_id)
VALUES ((SELECT id FROM usuarios.usuario WHERE correo = 'zs22013630@estudiantes.uv.mx'),
        (SELECT id FROM academico.entidad_academica WHERE clave = 'FEI'));

-- 4. usuarios.usuario_dgaa

INSERT INTO usuarios.usuario_dgaa (usuario_id, area_academica_id)
VALUES ((SELECT id FROM usuarios.usuario WHERE correo = 'zs22013663@estudiantes.uv.mx'),
        (SELECT id FROM academico.area_academica WHERE clave = 4));

-- 5. Superusuario de pruebas: usuarios.usuario y usuarios.credencial_superusuario
-- La contraseña es una cadena PHC de Argon2id (m=19456, t=2, p=1). Con
-- fecha_actualizacion asignada la credencial es definitiva, no una temporal pendiente de cambio.

INSERT INTO usuarios.usuario (correo, nombre, rol_id)
VALUES ('superusuario@sgpla-dev.mx', 'Axel Luna Hernández', 1);

INSERT INTO usuarios.credencial_superusuario (usuario_id, contrasena, fecha_actualizacion)
VALUES ((SELECT id FROM usuarios.usuario WHERE correo = 'superusuario@sgpla-dev.mx'),
        '$argon2id$v=19$m=19456,t=2,p=1$c3prRVJHR1B3Q0txWFF1cw$+8+VHXHkJ+LLSlouxD2NYpnmq4Cfz9ahlkSKvGH1n3s',
        now());
