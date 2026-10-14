-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- TipoHabitacionDAO, HabitacionDAO.ActualizarTarifa y HospedajeDAO.ObtenerPorTurno leen y
-- escriben la tarifa en Tipo_habitacion.tarifa, pero en bases creadas con el script original la
-- tarifa sigue en habitacion.tarifa_base. Sin esta columna fallan con "Invalid column name
-- 'tarifa'" el mapa de habitaciones, la Caja (resumen del turno), Tarifas, Reportes y Gerencia.

-- 1. Columna nueva en Tipo_habitacion
IF COL_LENGTH('Tipo_habitacion', 'tarifa') IS NULL
BEGIN
    ALTER TABLE Tipo_habitacion ADD tarifa DECIMAL(10,2) NOT NULL
        CONSTRAINT DF_Tipo_habitacion_tarifa DEFAULT 0;
END
GO

-- 2. Backfill desde la tarifa que ya tenían las habitaciones de cada tipo
IF COL_LENGTH('habitacion', 'tarifa_base') IS NOT NULL
BEGIN
    EXEC('
        UPDATE th
        SET th.tarifa = h.tarifa
        FROM Tipo_habitacion th
        INNER JOIN (
            SELECT id_tipo_habitacion, MAX(tarifa_base) AS tarifa
            FROM habitacion
            GROUP BY id_tipo_habitacion
        ) h ON h.id_tipo_habitacion = th.id_tipo_habitacion
        WHERE th.tarifa = 0');
END
GO

-- 3. HabitacionDAO.Crear ya no completa tarifa_base (la tarifa vive en el tipo): al ser NOT NULL
-- sin DEFAULT, dar de alta una habitación fallaría. Se le agrega un DEFAULT.
IF COL_LENGTH('habitacion', 'tarifa_base') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.default_constraints dc
       INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
       WHERE dc.parent_object_id = OBJECT_ID('habitacion') AND c.name = 'tarifa_base')
BEGIN
    ALTER TABLE habitacion ADD CONSTRAINT DF_habitacion_tarifa_base DEFAULT 0 FOR tarifa_base;
END
GO
