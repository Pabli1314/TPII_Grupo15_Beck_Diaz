-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB) antes de usar la
-- sección Usuarios del Administrador con datos reales.
--
-- Usuario todavía tenía el esquema viejo (dni_usuario como PK, sin id_usuario);
-- UsuarioDAO/GestionUsuarios ya trabajan con id_usuario, nombre, apellido, dni,
-- telefono y ultimo_acceso. Este script agrega esas columnas, migra los datos de
-- los usuarios semilla y hace de id_usuario la clave primaria real.

-- 1. Columnas nuevas que usa el módulo Administrador
ALTER TABLE Usuario ADD id_usuario INT IDENTITY(1,1) NOT NULL;
ALTER TABLE Usuario ADD nombre NVARCHAR(50) NOT NULL DEFAULT('');
ALTER TABLE Usuario ADD apellido NVARCHAR(50) NOT NULL DEFAULT('');
ALTER TABLE Usuario ADD dni VARCHAR(8) NOT NULL DEFAULT('');
ALTER TABLE Usuario ADD telefono VARCHAR(20) NOT NULL DEFAULT('');
ALTER TABLE Usuario ADD ultimo_acceso DATETIME NULL;
GO

-- 2. Backfill de los usuarios existentes desde las columnas viejas equivalentes
-- (direccion no se toca: ya existe con ese mismo nombre y UsuarioDAO ya la usa).
UPDATE Usuario
SET dni = dni_usuario,
    nombre = nom_usuario,
    apellido = ape_usuario,
    telefono = telefono_usuario;
GO

-- 3. UsuarioDAO.Insertar ya no completa dni_usuario/ape_usuario/telefono_usuario/
-- correo_usuario (los reemplazan dni/apellido/telefono; correo_usuario quedó sin
-- reemplazo y sin uso). Al ser NOT NULL/UNIQUE, un alta nueva desde el
-- Administrador fallaría. Se dejan como historial: nullable y sin la restricción
-- de unicidad (que con múltiples altas en NULL también rompería el insert).
ALTER TABLE Turno_caja DROP CONSTRAINT FK_TurnoCaja_Usuario;
ALTER TABLE Usuario DROP CONSTRAINT PK_usuario_id;
ALTER TABLE Usuario DROP CONSTRAINT UQ_usuario_telefono;
ALTER TABLE Usuario DROP CONSTRAINT UQ_usuario_correo;

ALTER TABLE Usuario ALTER COLUMN dni_usuario VARCHAR(8) NULL;
ALTER TABLE Usuario ALTER COLUMN ape_usuario VARCHAR(20) NULL;
ALTER TABLE Usuario ALTER COLUMN telefono_usuario VARCHAR(10) NULL;
ALTER TABLE Usuario ALTER COLUMN correo_usuario VARCHAR(20) NULL;
GO

-- 4. id_usuario pasa a ser la clave primaria real (lo que usa toda la capa de datos).
-- Turno_caja queda sin la FK a Usuario(dni_usuario): esa tabla todavía no tiene su
-- propia columna id_usuario (TurnoCajaDAO la espera pero no existe todavía), así
-- que hoy esa FK ya estaba desconectada del código; es una migración aparte.
ALTER TABLE Usuario ADD CONSTRAINT PK_usuario_id_usuario PRIMARY KEY (id_usuario);
GO
