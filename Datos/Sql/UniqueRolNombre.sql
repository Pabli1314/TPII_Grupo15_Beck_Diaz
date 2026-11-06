-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Ejecutar dos veces el INSERT de consultas_y_registros.sql duplicaba los roles, y el combo
-- "Rol" de FModalUsuario mostraba cada rol repetido. Este script unifica las copias en la de
-- menor id y agrega un UNIQUE sobre nom_rol.

-- 1. Reasignar los usuarios que tengan una copia al rol de menor id con el mismo nombre
UPDATE u
SET u.id_rol = (SELECT MIN(r2.id_rol)
                FROM Rol r2
                WHERE LTRIM(RTRIM(r2.nom_rol)) = LTRIM(RTRIM(r.nom_rol)))
FROM Usuario u
JOIN Rol r ON r.id_rol = u.id_rol
WHERE EXISTS (SELECT 1
              FROM Rol otro
              WHERE LTRIM(RTRIM(otro.nom_rol)) = LTRIM(RTRIM(r.nom_rol))
                AND otro.id_rol < r.id_rol);
GO

-- 2. Borrar las copias (ya no las usa ningún usuario)
DELETE r
FROM Rol r
WHERE EXISTS (SELECT 1
              FROM Rol otro
              WHERE LTRIM(RTRIM(otro.nom_rol)) = LTRIM(RTRIM(r.nom_rol))
                AND otro.id_rol < r.id_rol);
GO

-- 3. Restricción UNIQUE para que no vuelva a pasar
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Rol_nom'
                                           AND object_id = OBJECT_ID('Rol'))
    ALTER TABLE Rol ADD CONSTRAINT UQ_Rol_nom UNIQUE (nom_rol);
GO
