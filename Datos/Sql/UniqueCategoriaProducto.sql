-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Ejecutar dos veces el INSERT de consultas_y_registros.sql duplicaba las categorías, y el combo
-- "Categoría" al dar de alta un producto mostraba cada una repetida. Este script unifica las
-- copias en la de menor id y agrega un UNIQUE sobre descripcion_cat.

-- 1. Reasignar los productos que tengan una copia a la categoría de menor id con la misma descripción
UPDATE p
SET p.id_categoria = (SELECT MIN(c2.id_categoria)
                      FROM categoria_producto c2
                      WHERE LTRIM(RTRIM(c2.descripcion_cat)) = LTRIM(RTRIM(c.descripcion_cat)))
FROM producto p
JOIN categoria_producto c ON c.id_categoria = p.id_categoria
WHERE EXISTS (SELECT 1
              FROM categoria_producto otro
              WHERE LTRIM(RTRIM(otro.descripcion_cat)) = LTRIM(RTRIM(c.descripcion_cat))
                AND otro.id_categoria < c.id_categoria);
GO

-- 2. Borrar las copias (ya no las usa ningún producto)
DELETE c
FROM categoria_producto c
WHERE EXISTS (SELECT 1
              FROM categoria_producto otro
              WHERE LTRIM(RTRIM(otro.descripcion_cat)) = LTRIM(RTRIM(c.descripcion_cat))
                AND otro.id_categoria < c.id_categoria);
GO

-- 3. Restricción UNIQUE para que no vuelva a pasar
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_categoria_descripcion'
                                           AND object_id = OBJECT_ID('categoria_producto'))
    ALTER TABLE categoria_producto ADD CONSTRAINT UQ_categoria_descripcion UNIQUE (descripcion_cat);
GO
