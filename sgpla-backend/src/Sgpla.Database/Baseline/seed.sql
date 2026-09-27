-- SGPLa: datos iniciales de los catálogos.
-- Se ejecuta junto con baseline.sql, en la misma transacción, solo sobre una
-- base vacía. Los cambios de datos posteriores van en Scripts/, no aquí.
-- Las FK se resuelven por clave de negocio, no por id.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. usuarios.rol
-- Fijos por DATABASE.md §6.16 y ck_rol__catalogo_fijo.

INSERT INTO usuarios.rol (id, nombre)
VALUES (1, N'Superusuario'),
       (2, N'DGAA'),
       (3, N'Entidad Académica');

-- 2. academico.municipio
-- Municipios de Veracruz. Id = clave municipal de INEGI (entidad 30).

SET IDENTITY_INSERT academico.municipio ON;

INSERT INTO academico.municipio (id, nombre)
VALUES
       (  1, N'Acajete'),
       (  2, N'Acatlan'),
       (  3, N'Acayucan'),
       (  4, N'Actopan'),
       (  5, N'Acula'),
       (  6, N'Acultzingo'),
       (  7, N'Camaron De Tejeda'),
       (  8, N'Alpatlahuac'),
       (  9, N'Alto Lucero De Gutierrez Barrios'),
       ( 10, N'Altotonga'),
       ( 11, N'Alvarado'),
       ( 12, N'Amatitlan'),
       ( 13, N'Naranjos Amatlan'),
       ( 14, N'Amatlan De Los Reyes'),
       ( 15, N'Angel R. Cabada'),
       ( 16, N'La Antigua'),
       ( 17, N'Apazapan'),
       ( 18, N'Aquila'),
       ( 19, N'Astacinga'),
       ( 20, N'Atlahuilco'),
       ( 21, N'Atoyac'),
       ( 22, N'Atzacan'),
       ( 23, N'Atzalan'),
       ( 24, N'Tlaltetela'),
       ( 25, N'Ayahualulco'),
       ( 26, N'Banderilla'),
       ( 27, N'Benito Juarez'),
       ( 28, N'Boca Del Rio'),
       ( 29, N'Calcahualco'),
       ( 30, N'Camerino Z. Mendoza'),
       ( 31, N'Carrillo Puerto'),
       ( 32, N'Catemaco'),
       ( 33, N'Cazones De Herrera'),
       ( 34, N'Cerro Azul'),
       ( 35, N'Citlaltepetl'),
       ( 36, N'Coacoatzintla'),
       ( 37, N'Coahuitlan'),
       ( 38, N'Coatepec'),
       ( 39, N'Coatzacoalcos'),
       ( 40, N'Coatzintla'),
       ( 41, N'Coetzala'),
       ( 42, N'Colipa'),
       ( 43, N'Comapa'),
       ( 44, N'Cordoba'),
       ( 45, N'Cosamaloapan'),
       ( 46, N'Cosautlan De Carvajal'),
       ( 47, N'Coscomatepec'),
       ( 48, N'Cosoleacaque'),
       ( 49, N'Cotaxtla'),
       ( 50, N'Coxquihui'),
       ( 51, N'Coyutla'),
       ( 52, N'Cuichapa'),
       ( 53, N'Cuitlahuac'),
       ( 54, N'Chacaltianguis'),
       ( 55, N'Chalma'),
       ( 56, N'Chiconamel'),
       ( 57, N'Chiconquiaco'),
       ( 58, N'Chicontepec'),
       ( 59, N'Chinameca'),
       ( 60, N'Chinampa De Gorostiza'),
       ( 61, N'Las Choapas'),
       ( 62, N'Chocaman'),
       ( 63, N'Chontla'),
       ( 64, N'Chumatlan'),
       ( 65, N'Emiliano Zapata'),
       ( 66, N'Espinal'),
       ( 67, N'Filomeno Mata'),
       ( 68, N'Fortin'),
       ( 69, N'Gutierrez Zamora'),
       ( 70, N'Hidalgotitlan'),
       ( 71, N'Huatusco'),
       ( 72, N'Huayacocotla'),
       ( 73, N'Hueyapan De Ocampo'),
       ( 74, N'Huiloapan De Cuauhtemoc'),
       ( 75, N'Ignacio De La Llave'),
       ( 76, N'Ilamatlan'),
       ( 77, N'Isla'),
       ( 78, N'Ixcatepec'),
       ( 79, N'Ixhuacan De Los Reyes'),
       ( 80, N'Ixhuatlan Del Cafe'),
       ( 81, N'Ixhuatlancillo'),
       ( 82, N'Ixhuatlan Del Sureste'),
       ( 83, N'Ixhuatlan De Madero'),
       ( 84, N'Ixmatlahuacan'),
       ( 85, N'Ixtaczoquitlan'),
       ( 86, N'Jalacingo'),
       ( 87, N'Xalapa'),
       ( 88, N'Jalcomulco'),
       ( 89, N'Jaltipan'),
       ( 90, N'Jamapa'),
       ( 91, N'Jesus Carranza'),
       ( 92, N'Xico'),
       ( 93, N'Jilotepec'),
       ( 94, N'Juan Rodriguez Clara'),
       ( 95, N'Juchique De Ferrer'),
       ( 96, N'Landero Y Coss'),
       ( 97, N'Lerdo De Tejada'),
       ( 98, N'Magdalena'),
       ( 99, N'Maltrata'),
       (100, N'Manlio Fabio Altamirano'),
       (101, N'Mariano Escobedo'),
       (102, N'Martinez De La Torre'),
       (103, N'Mecatlan'),
       (104, N'Mecayapan'),
       (105, N'Medellin De Bravo'),
       (106, N'Miahuatlan'),
       (107, N'Las Minas'),
       (108, N'Minatitlan'),
       (109, N'Misantla'),
       (110, N'Mixtla De Altamirano'),
       (111, N'Moloacan'),
       (112, N'Naolinco'),
       (113, N'Naranjal'),
       (114, N'Nautla'),
       (115, N'Nogales'),
       (116, N'Oluta'),
       (117, N'Omealca'),
       (118, N'Orizaba'),
       (119, N'Otatitlan'),
       (120, N'Oteapan'),
       (121, N'Ozuluama'),
       (122, N'Pajapan'),
       (123, N'Panuco'),
       (124, N'Papantla'),
       (125, N'Paso Del Macho'),
       (126, N'Paso De Ovejas'),
       (127, N'La Perla'),
       (128, N'Perote'),
       (129, N'Platon Sanchez'),
       (130, N'Playa Vicente'),
       (131, N'Poza Rica De Hidalgo'),
       (132, N'Las Vigas De Ramirez'),
       (133, N'Pueblo Viejo'),
       (134, N'Puente Nacional'),
       (135, N'Rafael Delgado'),
       (136, N'Rafael Lucio'),
       (137, N'Los Reyes'),
       (138, N'Rio Blanco'),
       (139, N'Saltabarranca'),
       (140, N'San Andres Tenejapan'),
       (141, N'San Andres Tuxtla'),
       (142, N'San Juan Evangelista'),
       (143, N'Santiago Tuxtla'),
       (144, N'Sayula De Aleman'),
       (145, N'Soconusco'),
       (146, N'Sochiapa'),
       (147, N'Soledad Atzompa'),
       (148, N'Soledad De Doblado'),
       (149, N'Soteapan'),
       (150, N'Tamalin'),
       (151, N'Tamiahua'),
       (152, N'Tampico Alto'),
       (153, N'Tancoco'),
       (154, N'Tantima'),
       (155, N'Tantoyuca'),
       (156, N'Tatatila'),
       (157, N'Castillo De Teayo'),
       (158, N'Tecolutla'),
       (159, N'Tehuipango'),
       (160, N'Alamo Temapache'),
       (161, N'Tempoal'),
       (162, N'Tenampa'),
       (163, N'Tenochtitlan'),
       (164, N'Teocelo'),
       (165, N'Tepatlaxco'),
       (166, N'Tepetlan'),
       (167, N'Tepetzintla'),
       (168, N'Tequila'),
       (169, N'Jose Azueta'),
       (170, N'Texcatepec'),
       (171, N'Texhuacan'),
       (172, N'Texistepec'),
       (173, N'Tezonapa'),
       (174, N'Tierra Blanca'),
       (175, N'Tihuatlan'),
       (176, N'Tlacojalpan'),
       (177, N'Tlacolulan'),
       (178, N'Tlacotalpan'),
       (179, N'Tlacotepec De Mejia'),
       (180, N'Tlachichilco'),
       (181, N'Tlalixcoyan'),
       (182, N'Tlalnelhuayocan'),
       (183, N'Tlapacoyan'),
       (184, N'Tlaquilpa'),
       (185, N'Tlilapan'),
       (186, N'Tomatlan'),
       (187, N'Tonayan'),
       (188, N'Totutla'),
       (189, N'Tuxpan'),
       (190, N'Tuxtilla'),
       (191, N'Ursulo Galvan'),
       (192, N'Vega De Alatorre'),
       (193, N'Veracruz'),
       (194, N'Villa Aldama'),
       (195, N'Xoxocotla'),
       (196, N'Yanga'),
       (197, N'Yecuatla'),
       (198, N'Zacualpan'),
       (199, N'Zaragoza'),
       (200, N'Zentla'),
       (201, N'Zongolica'),
       (202, N'Zontecomatlan'),
       (203, N'Zozocolco De Hidalgo'),
       (204, N'Agua Dulce'),
       (205, N'El Higo'),
       (206, N'Nanchital De Lazaro Cardenas Del Rio'),
       (207, N'Tres Valles'),
       (208, N'Carlos A. Carrillo'),
       (209, N'Tatahuicapan De Juarez'),
       (210, N'Uxpanapa'),
       (211, N'San Rafael'),
       (212, N'Santiago Sochiapan');

SET IDENTITY_INSERT academico.municipio OFF;

-- 3. academico.sistema_educativo
-- Modalidades de https://www.uv.mx/ofertaeducativa/

SET IDENTITY_INSERT academico.sistema_educativo ON;

INSERT INTO academico.sistema_educativo (id, nombre)
VALUES (1, N'Escolarizada'),
       (2, N'Abierta'),
       (3, N'Virtual'),
       (4, N'Mixta'),
       (5, N'A distancia'),
       (6, N'Semiescolarizada');

SET IDENTITY_INSERT academico.sistema_educativo OFF;

-- 4. academico.nivel_formacion
-- Niveles de https://www.uv.mx/ofertaeducativa/ (sin Especialidad Médica).

SET IDENTITY_INSERT academico.nivel_formacion ON;

INSERT INTO academico.nivel_formacion (id, clave, nombre)
VALUES (1, 'TEC', N'Técnico'),
       (2, 'TSU', N'Técnico Superior Universitario'),
       (3, 'LIC', N'Licenciatura'),
       (4, 'ESP', N'Especialización'),
       (5, 'MAE', N'Maestría'),
       (6, 'DOC', N'Doctorado');

SET IDENTITY_INSERT academico.nivel_formacion OFF;

-- 5. academico.area_formacion
-- Áreas del Modelo Educativo Integral y Flexible; clave = sigla oficial.

SET IDENTITY_INSERT academico.area_formacion ON;

INSERT INTO academico.area_formacion (id, clave, nombre)
VALUES (1, 'AFBG', N'Área de Formación Básica General'),
       (2, 'AID',  N'Área de Iniciación a la Disciplina'),
       (3, 'AFD',  N'Área de Formación Disciplinar'),
       (4, 'AFT',  N'Área de Formación Terminal'),
       (5, 'AFEL', N'Área de Formación de Elección Libre');

SET IDENTITY_INSERT academico.area_formacion OFF;

-- 6. academico.area_academica
-- Sin datos: las registra el Superusuario.

-- 7. academico.region
-- Regiones universitarias de la UV; id = clave = código de región.

SET IDENTITY_INSERT academico.region ON;

INSERT INTO academico.region (id, clave, nombre)
VALUES (1, 1, N'Xalapa'),
       (2, 2, N'Veracruz'),
       (3, 3, N'Orizaba-Córdoba'),
       (4, 4, N'Poza Rica-Tuxpan'),
       (5, 5, N'Coatzacoalcos-Minatitlán');

SET IDENTITY_INSERT academico.region OFF;

-- 8. academico.campus
-- Campus de la UV agrupados por región.

SET IDENTITY_INSERT academico.campus ON;

INSERT INTO academico.campus (id, clave, nombre, region_id)
VALUES ( 1, 'X', N'Xalapa',                         (SELECT id FROM academico.region WHERE clave = 1)),
       ( 2, 'E', N'Martínez de la Torre',           (SELECT id FROM academico.region WHERE clave = 1)),
       ( 3, 'K', N'Coatepec',                       (SELECT id FROM academico.region WHERE clave = 1)),
       ( 4, 'Q', N'Naolinco',                       (SELECT id FROM academico.region WHERE clave = 1)),
       ( 5, 'V', N'Veracruz',                       (SELECT id FROM academico.region WHERE clave = 2)),
       ( 6, 'B', N'Boca del Río',                   (SELECT id FROM academico.region WHERE clave = 2)),
       ( 7, 'M', N'Ciudad Mendoza',                 (SELECT id FROM academico.region WHERE clave = 3)),
       ( 8, 'N', N'Nogales',                        (SELECT id FROM academico.region WHERE clave = 3)),
       ( 9, 'C', N'Córdoba',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (10, 'O', N'Orizaba',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (11, 'P', N'Peñuela',                        (SELECT id FROM academico.region WHERE clave = 3)),
       (12, 'L', N'Río Blanco',                     (SELECT id FROM academico.region WHERE clave = 3)),
       (13, 'G', N'Grandes Montañas (Tequila)',     (SELECT id FROM academico.region WHERE clave = 3)),
       (14, 'J', N'Ixtaczoquitlán',                 (SELECT id FROM academico.region WHERE clave = 3)),
       (15, 'H', N'Huasteca (Ixhuatlán de Madero)', (SELECT id FROM academico.region WHERE clave = 4)),
       (16, 'F', N'Totonacapan (Espinal)',          (SELECT id FROM academico.region WHERE clave = 4)),
       (17, 'T', N'Tuxpan',                         (SELECT id FROM academico.region WHERE clave = 4)),
       (18, 'R', N'Poza Rica',                      (SELECT id FROM academico.region WHERE clave = 4)),
       (19, 'U', N'Papantla',                       (SELECT id FROM academico.region WHERE clave = 4)),
       (20, 'Z', N'Coatzacoalcos',                  (SELECT id FROM academico.region WHERE clave = 5)),
       (21, 'I', N'Minatitlán',                     (SELECT id FROM academico.region WHERE clave = 5)),
       (22, 'A', N'Acayucan',                       (SELECT id FROM academico.region WHERE clave = 5)),
       (23, 'D', N'Catemaco',                       (SELECT id FROM academico.region WHERE clave = 5)),
       (24, 'S', N'Selvas (Huazuntlán)',            (SELECT id FROM academico.region WHERE clave = 5));

SET IDENTITY_INSERT academico.campus OFF;

-- 9. academico.periodo_escolar
-- Sin datos por ahora.

-- 10. academico.entidad_academica
-- Sin datos: requiere area_academica, que registra el Superusuario.

-- 11. academico.grado_academico
-- Catálogo fijo (DATABASE.md §6.21): sin altas, modificaciones ni bajas en la
-- operación normal. Ids estables en orden de jerarquía académica.

SET IDENTITY_INSERT academico.grado_academico ON;

INSERT INTO academico.grado_academico (id, nombre)
VALUES (1, N'Licenciatura'),
       (2, N'Especialidad'),
       (3, N'Maestría'),
       (4, N'Doctorado');

SET IDENTITY_INSERT academico.grado_academico OFF;

-- 12. plazas.tratamiento_academico
-- Tratamientos académicos asociados al grado correspondiente.

SET IDENTITY_INSERT plazas.tratamiento_academico ON;

INSERT INTO plazas.tratamiento_academico (id, nombre, grado_academico_id)
VALUES (1, N'Lic',  (SELECT id FROM academico.grado_academico WHERE nombre = N'Licenciatura')),
       (2, N'Mtro', (SELECT id FROM academico.grado_academico WHERE nombre = N'Maestría')),
       (3, N'Mtra', (SELECT id FROM academico.grado_academico WHERE nombre = N'Maestría')),
       (4, N'Dr',   (SELECT id FROM academico.grado_academico WHERE nombre = N'Doctorado')),
       (5, N'Dra',  (SELECT id FROM academico.grado_academico WHERE nombre = N'Doctorado'));

SET IDENTITY_INSERT plazas.tratamiento_academico OFF;

-- 13. Catálogos fijos pendientes de valores (DATABASE.md §6.22 y §15.2):
-- academico.tipo_documento_expediente, plazas.modalidad_recepcion,
-- plazas.tipo_plaza y plazas.tipo_contratacion.
