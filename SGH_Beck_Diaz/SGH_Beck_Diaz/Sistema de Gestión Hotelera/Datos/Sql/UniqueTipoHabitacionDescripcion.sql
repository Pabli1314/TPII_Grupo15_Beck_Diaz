-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Ejecutar dos veces el INSERT de consultas_y_registros.sql duplicaba los tipos de habitación,
-- y GestionTarifas mostraba una tarifa repetida por cada copia. Este script borra las copias
-- que no usa ninguna habitación y agrega un UNIQUE sobre descripcion para que no vuelva a pasar.

-- 1. Borrar duplicados sin habitaciones asociadas (se conserva el de menor id, o el que esté en uso)
DELETE th
FROM Tipo_habitacion th
WHERE NOT EXISTS (SELECT 1 FROM habitacion h WHERE h.id_tipo_habitacion = th.id_tipo_habitacion)
  AND EXISTS (
      SELECT 1
      FROM Tipo_habitacion otro
      WHERE LTRIM(RTRIM(otro.descripcion)) = LTRIM(RTRIM(th.descripcion))
        AND otro.id_tipo_habitacion <> th.id_tipo_habitacion
        AND (otro.id_tipo_habitacion < th.id_tipo_habitacion
             OR EXISTS (SELECT 1 FROM habitacion h WHERE h.id_tipo_habitacion = otro.id_tipo_habitacion)));
GO

-- 2. Restricción UNIQUE. Si quedan duplicados con habitaciones asociadas en ambos, hay que
-- reasignar esas habitaciones a mano: el script no lo hace para no cambiarles la tarifa.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Tipo_habitacion_descripcion'
                                           AND object_id = OBJECT_ID('Tipo_habitacion'))
BEGIN
    IF EXISTS (SELECT 1 FROM Tipo_habitacion GROUP BY descripcion HAVING COUNT(*) > 1)
        PRINT 'Quedan tipos de habitación duplicados en uso: reasignar sus habitaciones y volver a ejecutar.';
    ELSE
        ALTER TABLE Tipo_habitacion ADD CONSTRAINT UQ_Tipo_habitacion_descripcion UNIQUE (descripcion);
END
GO
