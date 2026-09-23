-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB) antes de usar el
-- módulo de Huéspedes / Check-in con datos reales.
--
-- Huesped todavía tiene el esquema viejo (nombre_huesped, apellido_huesped,
-- telefono_huesped, direccion_huesped); HuespedDAO ya trabaja con nombre,
-- apellido, telefono, direccion (dni_huesped se mantiene, sigue siendo la PK
-- y así la usa todo el resto del código vía FK). La tabla está vacía, no hace
-- falta backfill.

ALTER TABLE Huesped ADD nombre VARCHAR(50) NOT NULL DEFAULT('');
ALTER TABLE Huesped ADD apellido VARCHAR(50) NOT NULL DEFAULT('');
ALTER TABLE Huesped ADD telefono VARCHAR(10) NOT NULL DEFAULT('');
ALTER TABLE Huesped ADD direccion VARCHAR(50) NOT NULL DEFAULT('');
GO

-- Las columnas viejas (y correo_huesped, que quedó sin reemplazo y sin uso) ya
-- no las completa HuespedDAO.Crear; al ser NOT NULL/UNIQUE, un alta nueva
-- fallaría. Se dejan nullable y sin la restricción de unicidad.
ALTER TABLE Huesped DROP CONSTRAINT UQ_huesped_telefono;
ALTER TABLE Huesped DROP CONSTRAINT UQ_huesped_correo;

ALTER TABLE Huesped ALTER COLUMN nombre_huesped VARCHAR(50) NULL;
ALTER TABLE Huesped ALTER COLUMN apellido_huesped VARCHAR(50) NULL;
ALTER TABLE Huesped ALTER COLUMN telefono_huesped VARCHAR(10) NULL;
ALTER TABLE Huesped ALTER COLUMN direccion_huesped VARCHAR(50) NULL;
ALTER TABLE Huesped ALTER COLUMN correo_huesped VARCHAR(50) NULL;
GO
