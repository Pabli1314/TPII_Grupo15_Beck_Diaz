-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Ejecutar dos veces el INSERT de consultas_y_registros.sql duplicaba los métodos de pago.
-- Este script pasa los hospedajes y ventas que usan una copia al método de menor id con el
-- mismo nombre, borra las copias y agrega un UNIQUE sobre nom_metodo_pago.

-- 1. Reasignar hospedajes y ventas que usen una copia
UPDATE h
SET h.id_metodo = (SELECT MIN(m2.id_metodo)
                   FROM metodo_pago m2
                   WHERE LTRIM(RTRIM(m2.nom_metodo_pago)) = LTRIM(RTRIM(m.nom_metodo_pago)))
FROM hospedaje h
JOIN metodo_pago m ON m.id_metodo = h.id_metodo
WHERE EXISTS (SELECT 1
              FROM metodo_pago otro
              WHERE LTRIM(RTRIM(otro.nom_metodo_pago)) = LTRIM(RTRIM(m.nom_metodo_pago))
                AND otro.id_metodo < m.id_metodo);
GO

UPDATE v
SET v.id_metodo = (SELECT MIN(m2.id_metodo)
                   FROM metodo_pago m2
                   WHERE LTRIM(RTRIM(m2.nom_metodo_pago)) = LTRIM(RTRIM(m.nom_metodo_pago)))
FROM venta v
JOIN metodo_pago m ON m.id_metodo = v.id_metodo
WHERE EXISTS (SELECT 1
              FROM metodo_pago otro
              WHERE LTRIM(RTRIM(otro.nom_metodo_pago)) = LTRIM(RTRIM(m.nom_metodo_pago))
                AND otro.id_metodo < m.id_metodo);
GO

-- 2. Borrar las copias (ya no las usa ningún hospedaje ni venta)
DELETE m
FROM metodo_pago m
WHERE EXISTS (SELECT 1
              FROM metodo_pago otro
              WHERE LTRIM(RTRIM(otro.nom_metodo_pago)) = LTRIM(RTRIM(m.nom_metodo_pago))
                AND otro.id_metodo < m.id_metodo);
GO

-- 3. Restricción UNIQUE para que no vuelva a pasar
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_metodo_pago_nom'
                                           AND object_id = OBJECT_ID('metodo_pago'))
    ALTER TABLE metodo_pago ADD CONSTRAINT UQ_metodo_pago_nom UNIQUE (nom_metodo_pago);
GO
