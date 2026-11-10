USE beck_diaz_db;

-- Insertar roles
INSERT INTO Rol (nom_rol) VALUES ('Administrador'), ('Gerente'), ('Recepcionista');
SELECT * FROM Rol;


-- Crgar Tipos de Habitación
INSERT INTO Tipo_habitacion (descripcion, tarifa) VALUES ('Individual', 15000.00),('Doble Matrimonial', 22000.00),('Suite Executive', 35000.00),('Estandar', 10000.00);

SELECT * FROM Tipo_habitacion;

-- Cargar Estados (Siguiendo la codificación de colores del ERS)
INSERT INTO Estado_habitacion (nom_estado_habitacion) VALUES 
('Disponible'),    -- id_estado: 1 (Verde)
('Ocupada'),       -- id_estado: 2 (Rojo)
('Limpieza'),     -- id_estado: 3 (Amarillo)
('Mantenimiento'); --id_estado: 4 (azul)

SELECT * FROM Estado_habitacion;

-- Registros de usuarios
INSERT INTO Usuario (dni_usuario, nom_usuario, ape_usuario, direccion, telefono_usuario, correo_usuario, pasword, estado, id_rol) 
VALUES 
('35123456', 'Carlos', 'Gómez', 'Av. San Martín 123', '3794123456', 'cgomez@hotel.com', 'administrador2', 1, 1), -- Administrador
('38987654', 'María', 'Fernández', 'Calle Pellegrini 456', '3794987654', 'mfernandez@hotel.com', 'gerente1', 1, 2), -- Gerente
('40555666', 'Juan', 'López', 'Calle Junín 789', '3794555666', 'jlopez@hotel.com', 'recepcionista3', 1, 3); -- Recepcionista

SELECT * FROM Usuario

--Registro de habitaciones
-- Nota: Respeta el CHECK (piso IN (1,2,3))
INSERT INTO habitacion (nro_habitacion, piso, cant_camas,  id_tipo_habitacion, id_estado) VALUES 
-- Piso 1
(101, 1, 1, 1, 1), -- Individual, Disponible
(102, 1, 1, 1, 1), -- Individual, Disponible
(103, 1, 2, 2, 1);-- Doble, Limpieza

INSERT INTO habitacion (nro_habitacion, piso, cant_camas,  id_tipo_habitacion, id_estado) VALUES 
-- Piso 2
(201, 2, 2, 2, 1), -- Doble, Disponible
(202, 2, 2, 2, 1), -- Doble, Disponible
(203, 2, 3, 3, 1); -- Suite, Limpieza


INSERT INTO habitacion (nro_habitacion, piso, cant_camas,  id_tipo_habitacion, id_estado) VALUES
-- Piso 3
(301, 3, 3, 3, 1), -- Suite, Disponible
(302, 3, 3, 3, 1); -- Suite, Disponible
GO

INSERT INTO habitacion (nro_habitacion, piso, cant_camas,  id_tipo_habitacion, id_estado) VALUES
(303, 3, 2, 4, 1);


---------------------------- Movimientos en caja
INSERT INTO Turno_caja (fecha_apertura, hora_apertura, fecha_cierre, hora_cierre, monto_inicial, monto_final, observaciones, dni_usuario) VALUES 
-- Turno cerrado por Recepcionista
('2026-03-01', '08:00:00', '2026-03-01', '16:00:00', 15000.00, 48500.00, 'Turno mañana sin novedades', '40555666'), 
-- Turno cerrado por Gerente
('2026-03-01', '16:00:00', '2026-03-02', '00:00:00', 20000.00, 62000.00, 'Arqueo de caja correcto', '38987654'), 
-- Turno en curso/abierto por Recepcionista
('2026-03-02', '08:00:00', NULL, NULL, 15000.00, NULL, 'Turno activo', '40555666');

SELECT * FROM Turno_caja;
SELECT h.nro_habitacion, h.piso, th.descripcion  FROM habitacion h INNER JOIN Tipo_habitacion th ON th.id_tipo_habitacion = h.id_tipo_habitacion 
WHERE h.id_tipo_habitacion = 2;

--------------------------------------------------
-- 3. VERIFICACIÓN DE DATOS
--------------------------------------------------


------------------- INSERTAR METODOS DE PAGO
INSERT INTO metodo_pago (nom_metodo_pago) VALUES ('Efectivo'), ('Transferencia'), ('Tarjeta');
SELECT * FROM metodo_pago;

INSERT INTO categoria_producto (descripcion_cat) VALUES ('Bebida'), ('Snacks');

SELECT * FROM categoria_producto;

-- Corrección: 102 y 302 se cargaron como Ocupadas sin un hospedaje asociado, por lo que
-- aparecían en Check-out pero no se podía cerrar su estadía. Pasan a Disponible (Check-in).
-- Solo se tocan si siguen Ocupadas y no tienen hospedaje (no pisa estadías reales).
UPDATE habitacion
SET id_estado = (SELECT id_estado FROM Estado_habitacion WHERE nom_estado_habitacion = 'Disponible')
WHERE nro_habitacion IN (102, 302)
  AND id_estado = (SELECT id_estado FROM Estado_habitacion WHERE nom_estado_habitacion = 'Ocupada')
  AND NOT EXISTS (SELECT 1 FROM hospedaje h WHERE h.nro_habitacion = habitacion.nro_habitacion);

--------------------------------------------------
-- REGISTRO DE HUÉSPEDES
--------------------------------------------------
INSERT INTO Huesped (dni_huesped, nombre_huesped, apellido_huesped, telefono_huesped, direccion_huesped, correo_huesped, alta_huesped) 
VALUES 
('28456123', 'Lucas', 'Benítez', '3794112233', 'Calle Córdoba 1024, Corrientes', 'lucas.benitez@gmail.com', '2026-02-10'),
('32789456', 'Valeria', 'Romero', '3794223344', 'Av. 3 de Abril 1580, Corrientes', 'valeria.romero@gmail.com', '2026-02-12'),
('34123789', 'Gonzalo', 'Acosta', '3794334455', 'Belgrano 452, Resistencia', 'gonzalo.acosta@gmail.com', '2026-02-14'),
('36985214', 'Camila', 'Torres', '3794445566', '9 de Julio 876, Corrientes', 'camila.torres@gmail.com', '2026-02-15'),
('39147258', 'Esteban', 'Morales', '3794556677', 'Santa Fe 1240, Corrientes', 'esteban.morales@gmail.com', '2026-02-18'),
('41258963', 'Sofía', 'Castillo', '3794667788', 'Sarmiento 630, Resistencia', 'sofia.castillo@gmail.com', '2026-02-20'),
('42369852', 'Martín', 'Giménez', '3794778899', 'San Lorenzo 890, Corrientes', 'martin.gimenez@gmail.com', '2026-02-22'),
('43125698', 'Lucía', 'Navarro', '3794889900', 'Mitre 1420, Corrientes', 'lucia.navarro@gmail.com', '2026-02-25'),
('44589632', 'Rodrigo', 'Vázquez', '3794102030', 'Av. Libertad 3200, Corrientes', 'rodrigo.vazquez@gmail.com', '2026-02-27'),
('45214789', 'Florencia', 'Silva', '3794203040', 'Junín 1550, Corrientes', 'florencia.silva@gmail.com', '2026-03-01');

SELECT * FROM Huesped;

INSERT INTO producto (cod_producto, descripcion_product, precio, stock_min, stock_disponible, estado_producto, id_categoria) 
VALUES 
-- Categoría: Bebida
('BEB-001', 'Agua Mineral 500ml', 1500.00, 10, 50, 1, 1),
('BEB-002', 'Agua c/ Gas 500ml', 1500.00, 10, 40, 1, 1),
('BEB-003', 'Coca-Cola 500ml', 2500.00, 15, 60, 1, 1),
('BEB-004', 'Coca-Cola Zero 500', 2500.00, 10, 35, 1, 1),
('BEB-005', 'Sprite 500ml', 2400.00, 10, 30, 1, 1),
('BEB-006', 'Cerveza Quilmes 1L', 3800.00, 10, 25, 1, 1),
('BEB-007', 'Cerveza Corona 330', 4200.00, 8, 24, 1, 1),
('BEB-008', 'Jugo Naranja 500ml', 2000.00, 5, 20, 1, 1),
('BEB-009', 'Agua Tónica 500ml', 2200.00, 5, 15, 1, 1),
('BEB-010', 'Vino Malbec 750ml', 8500.00, 5, 12, 1, 1),

-- Categoría: Snacks
('SNK-001', 'Papas Lays Clásicas', 2800.00, 10, 30, 1, 2),
('SNK-002', 'Papas Lays Crema', 2900.00, 8, 25, 1, 2),
('SNK-003', 'Maní Salado 100g', 1400.00, 10, 40, 1, 2),
('SNK-004', 'Alfajor Havanna', 2200.00, 15, 45, 1, 2),
('SNK-005', 'Alfajor Jorgito', 1200.00, 15, 50, 1, 2),
('SNK-006', 'Galletitas Oreo', 2100.00, 10, 30, 1, 2),
('SNK-007', 'Barra Cereal Frutos', 1100.00, 10, 35, 1, 2),
('SNK-008', 'Chocolatín Águila', 1700.00, 10, 25, 1, 2),
('SNK-009', 'Doritos Queso 90g', 3100.00, 8, 20, 1, 2),
('SNK-010', 'Nachos Salados 120g', 2600.00, 8, 22, 1, 2);

SELECT p.cod_producto, p.descripcion_product, c.descripcion_cat, p.precio, p.stock_min, p.stock_disponible, p.estado_producto
FROM producto p
INNER JOIN categoria_producto c ON p.id_categoria = c.id_categoria;

