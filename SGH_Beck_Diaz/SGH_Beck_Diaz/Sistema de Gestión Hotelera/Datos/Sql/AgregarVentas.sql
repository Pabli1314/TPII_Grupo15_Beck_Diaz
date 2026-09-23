-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB) antes de usar la
-- sección "Ventas Adicionales" y el resumen de Caja con datos reales.
--
-- producto/venta/detalle_venta ya existían (del script base), pero con un
-- esquema que no coincide con el que usan ProductoDAO/VentaDAO/DetalleVentaDAO
-- (p. ej. producto tenía id_categoria + descripcion_product en vez de
-- categoria + nom_producto, y venta no tenía id_turno). Las tres están vacías,
-- así que se recrean directamente con el esquema correcto.

DROP TABLE IF EXISTS detalle_venta;
DROP TABLE IF EXISTS venta;
DROP TABLE IF EXISTS producto;
GO

CREATE TABLE producto (
    cod_producto VARCHAR(20) NOT NULL PRIMARY KEY,
    nom_producto VARCHAR(100) NOT NULL,
    categoria VARCHAR(50) NOT NULL,
    precio DECIMAL(10,2) NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    stock_minimo INT NOT NULL DEFAULT 0,
    activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE venta (
    id_venta INT IDENTITY(1,1) PRIMARY KEY,
    fecha_venta DATE NOT NULL DEFAULT CONVERT(DATE, GETDATE()),
    hora_venta TIME NOT NULL DEFAULT CONVERT(TIME, GETDATE()),
    total DECIMAL(10,2) NOT NULL,
    id_metodo INT NOT NULL FOREIGN KEY REFERENCES metodo_pago(id_metodo),
    id_turno INT NOT NULL FOREIGN KEY REFERENCES Turno_caja(id_turno)
);

CREATE TABLE detalle_venta (
    id_venta INT NOT NULL FOREIGN KEY REFERENCES venta(id_venta) ON DELETE CASCADE,
    cod_producto VARCHAR(20) NOT NULL FOREIGN KEY REFERENCES producto(cod_producto),
    precio_unitario DECIMAL(10,2) NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    subtotal DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (id_venta, cod_producto)
);
GO

INSERT INTO producto (cod_producto, nom_producto, categoria, precio, stock, stock_minimo, activo) VALUES
    ('BEB-001', 'Coca-Cola 500ml', 'Bebidas', 1500, 30, 10, 1),
    ('BEB-002', 'Agua Mineral 500ml', 'Bebidas', 1000, 30, 10, 1),
    ('BEB-003', 'Cerveza 473ml', 'Bebidas', 2200, 20, 8, 1),
    ('SNK-001', 'Papas Fritas', 'Snacks', 1800, 15, 5, 1),
    ('SNK-002', 'Barra de Cereal', 'Snacks', 1200, 18, 8, 1),
    ('SNK-003', 'Alfajor', 'Snacks', 900, 25, 10, 1),
    ('VAR-001', 'Kit de Amenities', 'Varios', 2500, 25, 10, 1),
    ('VAR-002', 'Servicio de Lavandería', 'Varios', 3500, 999, 0, 1);
GO
