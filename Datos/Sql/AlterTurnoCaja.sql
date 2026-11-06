-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB), después de
-- AlterUsuario.sql. Turno_caja todavía identificaba al usuario por
-- dni_usuario; TurnoCajaDAO ya trabaja con id_usuario (igual que ya se migró
-- Usuario). Sin esto, abrir el módulo Recepcionista (Dashboard/Caja) falla
-- con "Invalid column name 'id_usuario'".

-- 1. Columna nueva
ALTER TABLE Turno_caja ADD id_usuario INT NULL;
GO

-- 2. Backfill de los turnos existentes a partir del dni_usuario viejo
UPDATE tc
SET tc.id_usuario = u.id_usuario
FROM Turno_caja tc
INNER JOIN Usuario u ON u.dni_usuario = tc.dni_usuario;
GO

-- 3. id_usuario pasa a ser obligatoria y queda referenciando a Usuario(id_usuario)
ALTER TABLE Turno_caja ALTER COLUMN id_usuario INT NOT NULL;
ALTER TABLE Turno_caja ADD CONSTRAINT FK_TurnoCaja_Usuario_Id FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario);
GO

-- 4. TurnoCajaDAO.Abrir ya no completa dni_usuario; al quedar NOT NULL sin
-- default, abrir un turno nuevo fallaría. Se deja como historial y nullable.
ALTER TABLE Turno_caja ALTER COLUMN dni_usuario VARCHAR(8) NULL;
GO
