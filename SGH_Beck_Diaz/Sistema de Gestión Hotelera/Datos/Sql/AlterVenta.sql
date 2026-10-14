-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB), después de AgregarVentas.sql.
-- Agrega el vínculo opcional entre una venta y el hospedaje (habitación/huésped) al que se
-- carga el consumo, para que "Ventas Adicionales" pueda asociar la venta a la cuenta activa de
-- una habitación ocupada en vez de ser siempre una venta de mostrador.

ALTER TABLE venta ADD id_hospedaje INT NULL;
GO

ALTER TABLE venta ADD CONSTRAINT FK_venta_hospedaje FOREIGN KEY (id_hospedaje) REFERENCES hospedaje(id_hospedaje);
GO
