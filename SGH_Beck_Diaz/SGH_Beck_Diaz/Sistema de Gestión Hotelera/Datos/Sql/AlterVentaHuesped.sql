-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB).
-- Agrega el vínculo opcional entre una venta y el huésped que realiza la compra, para que
-- "Ventas Adicionales" del Recepcionista pueda asignar la venta a un huésped alojado.
-- NULL = venta de mostrador, sin huésped asignado.

IF COL_LENGTH('venta', 'dni_huesped') IS NULL
BEGIN
    ALTER TABLE venta ADD dni_huesped VARCHAR(8) NULL;
END
GO

IF OBJECT_ID('FK_venta_huesped', 'F') IS NULL
BEGIN
    ALTER TABLE venta ADD CONSTRAINT FK_venta_huesped FOREIGN KEY (dni_huesped) REFERENCES Huesped(dni_huesped);
END
GO

IF OBJECT_ID('CK_venta_dniHuesped', 'C') IS NULL
BEGIN
    ALTER TABLE venta ADD CONSTRAINT CK_venta_dniHuesped CHECK (dni_huesped NOT LIKE '%[^0-9]%');
END
GO
