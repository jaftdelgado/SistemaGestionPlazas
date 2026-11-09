-- SGPLa: datos iniciales de los catálogos.
-- Se ejecuta junto con baseline.sql, en la misma transacción, solo sobre una
-- base vacía. Los cambios de datos posteriores van en Scripts/, no aquí.
-- Las FK se resuelven por clave de negocio, no por id.

-- 1. usuarios.rol
-- Fijos por ck_rol__catalogo_fijo.

INSERT INTO usuarios.rol (id, nombre)
VALUES (1, 'Superusuario'),
       (2, 'DGAA'),
       (3, 'Entidad Académica');

-- 2. academico.municipio
-- Municipios de Veracruz. Id = clave municipal de INEGI (entidad 30).

INSERT INTO academico.municipio (id, nombre)
VALUES
       (  1, 'Acajete'),
       (  2, 'Acatlan'),
       (  3, 'Acayucan'),
       (  4, 'Actopan'),
       (  5, 'Acula'),
       (  6, 'Acultzingo'),
       (  7, 'Camaron De Tejeda'),
       (  8, 'Alpatlahuac'),
       (  9, 'Alto Lucero De Gutierrez Barrios'),
       ( 10, 'Altotonga'),
       ( 11, 'Alvarado'),
       ( 12, 'Amatitlan'),
       ( 13, 'Naranjos Amatlan'),
       ( 14, 'Amatlan De Los Reyes'),
       ( 15, 'Angel R. Cabada'),
       ( 16, 'La Antigua'),
       ( 17, 'Apazapan'),
       ( 18, 'Aquila'),
       ( 19, 'Astacinga'),
       ( 20, 'Atlahuilco'),
       ( 21, 'Atoyac'),
       ( 22, 'Atzacan'),
       ( 23, 'Atzalan'),
       ( 24, 'Tlaltetela'),
       ( 25, 'Ayahualulco'),
       ( 26, 'Banderilla'),
       ( 27, 'Benito Juarez'),
       ( 28, 'Boca Del Rio'),
       ( 29, 'Calcahualco'),
       ( 30, 'Camerino Z. Mendoza'),
       ( 31, 'Carrillo Puerto'),
       ( 32, 'Catemaco'),
       ( 33, 'Cazones De Herrera'),
       ( 34, 'Cerro Azul'),
       ( 35, 'Citlaltepetl'),
       ( 36, 'Coacoatzintla'),
       ( 37, 'Coahuitlan'),
       ( 38, 'Coatepec'),
       ( 39, 'Coatzacoalcos'),
       ( 40, 'Coatzintla'),
       ( 41, 'Coetzala'),
       ( 42, 'Colipa'),
       ( 43, 'Comapa'),
       ( 44, 'Cordoba'),
       ( 45, 'Cosamaloapan'),
       ( 46, 'Cosautlan De Carvajal'),
       ( 47, 'Coscomatepec'),
       ( 48, 'Cosoleacaque'),
       ( 49, 'Cotaxtla'),
       ( 50, 'Coxquihui'),
       ( 51, 'Coyutla'),
       ( 52, 'Cuichapa'),
       ( 53, 'Cuitlahuac'),
       ( 54, 'Chacaltianguis'),
       ( 55, 'Chalma'),
       ( 56, 'Chiconamel'),
       ( 57, 'Chiconquiaco'),
       ( 58, 'Chicontepec'),
       ( 59, 'Chinameca'),
       ( 60, 'Chinampa De Gorostiza'),
       ( 61, 'Las Choapas'),
       ( 62, 'Chocaman'),
       ( 63, 'Chontla'),
       ( 64, 'Chumatlan'),
       ( 65, 'Emiliano Zapata'),
       ( 66, 'Espinal'),
       ( 67, 'Filomeno Mata'),
       ( 68, 'Fortin'),
       ( 69, 'Gutierrez Zamora'),
       ( 70, 'Hidalgotitlan'),
       ( 71, 'Huatusco'),
       ( 72, 'Huayacocotla'),
       ( 73, 'Hueyapan De Ocampo'),
       ( 74, 'Huiloapan De Cuauhtemoc'),
       ( 75, 'Ignacio De La Llave'),
       ( 76, 'Ilamatlan'),
       ( 77, 'Isla'),
       ( 78, 'Ixcatepec'),
       ( 79, 'Ixhuacan De Los Reyes'),
       ( 80, 'Ixhuatlan Del Cafe'),
       ( 81, 'Ixhuatlancillo'),
       ( 82, 'Ixhuatlan Del Sureste'),
       ( 83, 'Ixhuatlan De Madero'),
       ( 84, 'Ixmatlahuacan'),
       ( 85, 'Ixtaczoquitlan'),
       ( 86, 'Jalacingo'),
       ( 87, 'Xalapa'),
       ( 88, 'Jalcomulco'),
       ( 89, 'Jaltipan'),
       ( 90, 'Jamapa'),
       ( 91, 'Jesus Carranza'),
       ( 92, 'Xico'),
       ( 93, 'Jilotepec'),
       ( 94, 'Juan Rodriguez Clara'),
       ( 95, 'Juchique De Ferrer'),
       ( 96, 'Landero Y Coss'),
       ( 97, 'Lerdo De Tejada'),
       ( 98, 'Magdalena'),
       ( 99, 'Maltrata'),
       (100, 'Manlio Fabio Altamirano'),
       (101, 'Mariano Escobedo'),
       (102, 'Martinez De La Torre'),
       (103, 'Mecatlan'),
       (104, 'Mecayapan'),
       (105, 'Medellin De Bravo'),
       (106, 'Miahuatlan'),
       (107, 'Las Minas'),
       (108, 'Minatitlan'),
       (109, 'Misantla'),
       (110, 'Mixtla De Altamirano'),
       (111, 'Moloacan'),
       (112, 'Naolinco'),
       (113, 'Naranjal'),
       (114, 'Nautla'),
       (115, 'Nogales'),
       (116, 'Oluta'),
       (117, 'Omealca'),
       (118, 'Orizaba'),
       (119, 'Otatitlan'),
       (120, 'Oteapan'),
       (121, 'Ozuluama'),
       (122, 'Pajapan'),
       (123, 'Panuco'),
       (124, 'Papantla'),
       (125, 'Paso Del Macho'),
       (126, 'Paso De Ovejas'),
       (127, 'La Perla'),
       (128, 'Perote'),
       (129, 'Platon Sanchez'),
       (130, 'Playa Vicente'),
       (131, 'Poza Rica De Hidalgo'),
       (132, 'Las Vigas De Ramirez'),
       (133, 'Pueblo Viejo'),
       (134, 'Puente Nacional'),
       (135, 'Rafael Delgado'),
       (136, 'Rafael Lucio'),
       (137, 'Los Reyes'),
       (138, 'Rio Blanco'),
       (139, 'Saltabarranca'),
       (140, 'San Andres Tenejapan'),
       (141, 'San Andres Tuxtla'),
       (142, 'San Juan Evangelista'),
       (143, 'Santiago Tuxtla'),
       (144, 'Sayula De Aleman'),
       (145, 'Soconusco'),
       (146, 'Sochiapa'),
       (147, 'Soledad Atzompa'),
       (148, 'Soledad De Doblado'),
       (149, 'Soteapan'),
       (150, 'Tamalin'),
       (151, 'Tamiahua'),
       (152, 'Tampico Alto'),
       (153, 'Tancoco'),
       (154, 'Tantima'),
       (155, 'Tantoyuca'),
       (156, 'Tatatila'),
       (157, 'Castillo De Teayo'),
       (158, 'Tecolutla'),
       (159, 'Tehuipango'),
       (160, 'Alamo Temapache'),
       (161, 'Tempoal'),
       (162, 'Tenampa'),
       (163, 'Tenochtitlan'),
       (164, 'Teocelo'),
       (165, 'Tepatlaxco'),
       (166, 'Tepetlan'),
       (167, 'Tepetzintla'),
       (168, 'Tequila'),
       (169, 'Jose Azueta'),
       (170, 'Texcatepec'),
       (171, 'Texhuacan'),
       (172, 'Texistepec'),
       (173, 'Tezonapa'),
       (174, 'Tierra Blanca'),
       (175, 'Tihuatlan'),
       (176, 'Tlacojalpan'),
       (177, 'Tlacolulan'),
       (178, 'Tlacotalpan'),
       (179, 'Tlacotepec De Mejia'),
       (180, 'Tlachichilco'),
       (181, 'Tlalixcoyan'),
       (182, 'Tlalnelhuayocan'),
       (183, 'Tlapacoyan'),
       (184, 'Tlaquilpa'),
       (185, 'Tlilapan'),
       (186, 'Tomatlan'),
       (187, 'Tonayan'),
       (188, 'Totutla'),
       (189, 'Tuxpan'),
       (190, 'Tuxtilla'),
       (191, 'Ursulo Galvan'),
       (192, 'Vega De Alatorre'),
       (193, 'Veracruz'),
       (194, 'Villa Aldama'),
       (195, 'Xoxocotla'),
       (196, 'Yanga'),
       (197, 'Yecuatla'),
       (198, 'Zacualpan'),
       (199, 'Zaragoza'),
       (200, 'Zentla'),
       (201, 'Zongolica'),
       (202, 'Zontecomatlan'),
       (203, 'Zozocolco De Hidalgo'),
       (204, 'Agua Dulce'),
       (205, 'El Higo'),
       (206, 'Nanchital De Lazaro Cardenas Del Rio'),
       (207, 'Tres Valles'),
       (208, 'Carlos A. Carrillo'),
       (209, 'Tatahuicapan De Juarez'),
       (210, 'Uxpanapa'),
       (211, 'San Rafael'),
       (212, 'Santiago Sochiapan');

-- 3. academico.sistema_educativo
-- Modalidades de https://www.uv.mx/ofertaeducativa/

INSERT INTO academico.sistema_educativo (id, nombre)
VALUES (1, 'Escolarizada'),
       (2, 'Abierta'),
       (3, 'Virtual'),
       (4, 'Mixta'),
       (5, 'A distancia'),
       (6, 'Semiescolarizada');

-- 4. academico.nivel_formacion
-- Niveles de https://www.uv.mx/ofertaeducativa/ (sin Especialidad Médica).

INSERT INTO academico.nivel_formacion (id, clave, nombre)
VALUES (1, 'TEC', 'Técnico'),
       (2, 'TSU', 'Técnico Superior Universitario'),
       (3, 'LIC', 'Licenciatura'),
       (4, 'ESP', 'Especialización'),
       (5, 'MAE', 'Maestría'),
       (6, 'DOC', 'Doctorado');

-- 5. academico.area_formacion
-- Áreas de formación de los planes de la UV; clave = CODE_AREA_F del formato de plan de estudios.

INSERT INTO academico.area_formacion (id, clave, nombre)
VALUES (1, '111', 'Área de Formación Básica'),
       (2, '112', 'Área de Formación Disciplinaria'),
       (3, '113', 'Área de Formación Terminal');

-- 6. academico.area_academica
-- Áreas académicas de la UV; clave en orden alfabético. El teléfono es un marcador.

INSERT INTO academico.area_academica (id, clave, nombre, telefono)
VALUES (1, 1, 'Artes',                    '2288420000'),
       (2, 2, 'Biológico-Agropecuaria',   '2288420000'),
       (3, 3, 'Ciencias de la Salud',     '2288420000'),
       (4, 4, 'Económico-Administrativa', '2288420000'),
       (5, 5, 'Humanidades',              '2288420000'),
       (6, 6, 'Técnica',                  '2288420000');

-- 7. academico.region
-- Regiones universitarias de la UV; id = clave = código de región.

INSERT INTO academico.region (id, clave, nombre)
VALUES (1, 1, 'Xalapa'),
       (2, 2, 'Veracruz'),
       (3, 3, 'Orizaba-Córdoba'),
       (4, 4, 'Poza Rica-Tuxpan'),
       (5, 5, 'Coatzacoalcos-Minatitlán');

-- 8. academico.campus
-- Campus de la UV agrupados por región.

INSERT INTO academico.campus (id, clave, nombre, region_id)
VALUES ( 1, 'X', 'Xalapa',                         (SELECT id FROM academico.region WHERE clave = 1)),
       ( 2, 'E', 'Martínez de la Torre',           (SELECT id FROM academico.region WHERE clave = 1)),
       ( 3, 'K', 'Coatepec',                       (SELECT id FROM academico.region WHERE clave = 1)),
       ( 4, 'Q', 'Naolinco',                       (SELECT id FROM academico.region WHERE clave = 1)),
       ( 5, 'V', 'Veracruz',                       (SELECT id FROM academico.region WHERE clave = 2)),
       ( 6, 'B', 'Boca del Río',                   (SELECT id FROM academico.region WHERE clave = 2)),
       ( 7, 'M', 'Ciudad Mendoza',                 (SELECT id FROM academico.region WHERE clave = 3)),
       ( 8, 'N', 'Nogales',                        (SELECT id FROM academico.region WHERE clave = 3)),
       ( 9, 'C', 'Córdoba',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (10, 'O', 'Orizaba',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (11, 'P', 'Peñuela',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (12, 'L', 'Río Blanco',                     (SELECT id FROM academico.region WHERE clave = 3)),
       (13, 'G', 'Grandes Montañas (Tequila)',     (SELECT id FROM academico.region WHERE clave = 3)),
       (14, 'J', 'Ixtaczoquitlán',                 (SELECT id FROM academico.region WHERE clave = 3)),
       (15, 'H', 'Huasteca (Ixhuatlán de Madero)', (SELECT id FROM academico.region WHERE clave = 4)),
       (16, 'F', 'Totonacapan (Espinal)',          (SELECT id FROM academico.region WHERE clave = 4)),
       (17, 'T', 'Tuxpan',                         (SELECT id FROM academico.region WHERE clave = 4)),
       (18, 'R', 'Poza Rica',                      (SELECT id FROM academico.region WHERE clave = 4)),
       (19, 'U', 'Papantla',                       (SELECT id FROM academico.region WHERE clave = 4)),
       (20, 'Z', 'Coatzacoalcos',                  (SELECT id FROM academico.region WHERE clave = 5)),
       (21, 'I', 'Minatitlán',                     (SELECT id FROM academico.region WHERE clave = 5)),
       (22, 'A', 'Acayucan',                       (SELECT id FROM academico.region WHERE clave = 5)),
       (23, 'D', 'Catemaco',                       (SELECT id FROM academico.region WHERE clave = 5)),
       (24, 'S', 'Selvas (Huazuntlán)',            (SELECT id FROM academico.region WHERE clave = 5));

-- 9. academico.periodo_escolar
-- Sin datos por ahora.

-- 10. academico.entidad_academica
-- Sin datos: las registra el Superusuario.

-- 11. academico.grado_academico
-- Catálogo fijo: sin altas, modificaciones ni bajas en la
-- operación normal. Ids estables en orden de jerarquía académica.

INSERT INTO academico.grado_academico (id, nombre)
VALUES (1, 'Licenciatura'),
       (2, 'Especialidad'),
       (3, 'Maestría'),
       (4, 'Doctorado');

-- 12. plazas.tratamiento_academico
-- Tratamientos académicos asociados al grado correspondiente.

INSERT INTO plazas.tratamiento_academico (id, nombre, grado_academico_id)
VALUES (1, 'Lic',  (SELECT id FROM academico.grado_academico WHERE nombre = 'Licenciatura')),
       (2, 'Mtro', (SELECT id FROM academico.grado_academico WHERE nombre = 'Maestría')),
       (3, 'Mtra', (SELECT id FROM academico.grado_academico WHERE nombre = 'Maestría')),
       (4, 'Dr',   (SELECT id FROM academico.grado_academico WHERE nombre = 'Doctorado')),
       (5, 'Dra',  (SELECT id FROM academico.grado_academico WHERE nombre = 'Doctorado'));

-- 13. Catálogos fijos pendientes de valores:
-- academico.tipo_documento_expediente, plazas.modalidad_recepcion,
-- plazas.tipo_plaza y plazas.tipo_contratacion.

-- 14. Secuencias
-- Reajusta la identidad de las tablas que recibieron ids explícitos, para que
-- la primera alta posterior continúe después del último id del seed.

SELECT setval(pg_get_serial_sequence('academico.municipio', 'id'), (SELECT max(id) FROM academico.municipio));
SELECT setval(pg_get_serial_sequence('academico.sistema_educativo', 'id'), (SELECT max(id) FROM academico.sistema_educativo));
SELECT setval(pg_get_serial_sequence('academico.nivel_formacion', 'id'), (SELECT max(id) FROM academico.nivel_formacion));
SELECT setval(pg_get_serial_sequence('academico.area_formacion', 'id'), (SELECT max(id) FROM academico.area_formacion));
SELECT setval(pg_get_serial_sequence('academico.area_academica', 'id'), (SELECT max(id) FROM academico.area_academica));
SELECT setval(pg_get_serial_sequence('academico.region', 'id'), (SELECT max(id) FROM academico.region));
SELECT setval(pg_get_serial_sequence('academico.campus', 'id'), (SELECT max(id) FROM academico.campus));
SELECT setval(pg_get_serial_sequence('academico.grado_academico', 'id'), (SELECT max(id) FROM academico.grado_academico));
SELECT setval(pg_get_serial_sequence('plazas.tratamiento_academico', 'id'), (SELECT max(id) FROM plazas.tratamiento_academico));
