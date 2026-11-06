-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Ejecutar dos veces el INSERT de consultas_y_registros.sql duplicaba los estados de habitación,
-- y el combo "Estado" de FModalHabitacion mostraba cada estado repetido. Este script unifica
-- las copias en la de menor id y agrega un UNIQUE sobre nom_estado_habitacion.

-- 1. Reasignar las habitaciones que usan una copia al estado de menor id con el mismo nombre
UPDATE h
SET h.id_estado = (SELECT MIN(e2.id_estado)
                   FROM Estado_habitacion e2
                   WHERE LTRIM(RTRIM(e2.nom_estado_habitacion)) = LTRIM(RTRIM(e.nom_estado_habitacion)))
FROM habitacion h
JOIN Estado_habitacion e ON e.id_estado = h.id_estado
WHERE EXISTS (SELECT 1
              FROM Estado_habitacion otro
              WHERE LTRIM(RTRIM(otro.nom_estado_habitacion)) = LTRIM(RTRIM(e.nom_estado_habitacion))
                AND otro.id_estado < e.id_estado);
GO

-- 2. Borrar las copias (ya no las usa ninguna habitación)
DELETE e
FROM Estado_habitacion e
WHERE EXISTS (SELECT 1
              FROM Estado_habitacion otro
              WHERE LTRIM(RTRIM(otro.nom_estado_habitacion)) = LTRIM(RTRIM(e.nom_estado_habitacion))
                AND otro.id_estado < e.id_estado);
GO

-- 3. Restricción UNIQUE para que no vuelva a pasar
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Estado_habitacion_nom'
                                           AND object_id = OBJECT_ID('Estado_habitacion'))
    ALTER TABLE Estado_habitacion ADD CONSTRAINT UQ_Estado_habitacion_nom UNIQUE (nom_estado_habitacion);
GO
