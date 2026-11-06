-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB).
--
-- AlterUsuario.sql había dejado correo_usuario como VARCHAR(20) NULL y sin uso
-- (UsuarioDAO.Insertar no la completaba). Ahora el alta de usuario pide un
-- correo real, así que se amplía el tamaño para que entren direcciones de
-- email normales.
ALTER TABLE Usuario ALTER COLUMN correo_usuario VARCHAR(100) NULL;
GO
