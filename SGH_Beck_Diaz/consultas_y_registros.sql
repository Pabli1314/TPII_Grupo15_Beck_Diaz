USE beck_diaz_db;



-- Insertar roles
INSERT INTO Rol (nom_rol) VALUES ('Administrador'), ('Gerente'), ('Recepcionista');
SELECT * FROM Rol;



 

-- Cargar Tipos de Habitación
INSERT INTO Tipo_habitacion (descripcion) VALUES ('Individual'),('Doble Matrimonial'),('Suite Executive'),('Estandar');

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
('35123456', 'Carlos', 'Gómez', 'Av. San Martín 123', '3794123456', 'cgomez@hotel.com', 'hash_password_admin_123', 1, 1), -- Administrador
('38987654', 'María', 'Fernández', 'Calle Pellegrini 456', '3794987654', 'mfernandez@hotel.com', 'hash_password_gerente_456', 1, 2), -- Gerente
('40555666', 'Juan', 'López', 'Calle Junín 789', '3794555666', 'jlopez@hotel.com', 'hash_password_recep_789', 1, 3); -- Recepcionista

--Registro de habitaciones
-- Nota: Respeta el CHECK (piso IN (1,2,3))
INSERT INTO habitacion (nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado) VALUES 
-- Piso 1
(101, 1, 1, 15000.00, 1, 1), -- Individual, Disponible
(102, 1, 1, 15000.00, 1, 1), -- Individual, Disponible
(103, 1, 2, 22000.00, 2, 1);-- Doble, Limpieza

INSERT INTO habitacion (nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado) VALUES 
-- Piso 2
(201, 2, 2, 22000.00, 2, 1), -- Doble, Disponible
(202, 2, 2, 22000.00, 2, 1), -- Doble, Disponible
(203, 2, 3, 35000.00, 3, 1); -- Suite, Limpieza


INSERT INTO habitacion (nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado) VALUES
-- Piso 3
(301, 3, 3, 35000.00, 3, 1), -- Suite, Disponible
(302, 3, 3, 35000.00, 3, 1); -- Suite, Disponible
GO

INSERT INTO habitacion (nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado) VALUES
(303, 3, 2, 20000.00, 4, 1);


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
SELECT 
    h.nro_habitacion,
    h.piso,
    h.cant_camas,
    h.tarifa_base,
    th.descripcion AS tipo,
    eh.nom_estado_habitacion AS estado
FROM habitacion h
INNER JOIN Tipo_habitacion th ON h.id_tipo_habitacion = th.id_tipo_habitacion
INNER JOIN Estado_habitacion eh ON h.id_estado = eh.id_estado;


------------------- INSERTAR METODOS DE PAGO
INSERT INTO metodo_pago (nom_metodo_pago) VALUES ('Efectivo'), ('Transferencia'), ('Tarjeta');
SELECT * FROM metodo_pago;

INSERT INTO categoria_producto (descripcion_cat) VALUES ('Bebida'), ('Snacks');

-- Corrección: 102 y 302 se cargaron como Ocupadas sin un hospedaje asociado, por lo que
-- aparecían en Check-out pero no se podía cerrar su estadía. Pasan a Disponible (Check-in).
-- Solo se tocan si siguen Ocupadas y no tienen hospedaje (no pisa estadías reales).
UPDATE habitacion
SET id_estado = (SELECT id_estado FROM Estado_habitacion WHERE nom_estado_habitacion = 'Disponible')
WHERE nro_habitacion IN (102, 302)
  AND id_estado = (SELECT id_estado FROM Estado_habitacion WHERE nom_estado_habitacion = 'Ocupada')
  AND NOT EXISTS (SELECT 1 FROM hospedaje h WHERE h.nro_habitacion = habitacion.nro_habitacion);
