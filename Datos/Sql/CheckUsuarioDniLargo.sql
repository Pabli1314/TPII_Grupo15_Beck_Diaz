-- Se puede ejecutar más de una vez contra beck_diaz_db: verifica antes de aplicar.
--
-- El DNI de un usuario debe tener exactamente 8 dígitos. CK_usuario_dni ya exige que sean
-- solo números; esta restricción agrega el largo exacto.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_usuario_dni_largo'
                                                   AND parent_object_id = OBJECT_ID('Usuario'))
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE LEN(dni_usuario) <> 8)
        PRINT 'Hay usuarios con un DNI que no tiene 8 dígitos: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_dni_largo CHECK (LEN(dni_usuario) = 8);
END
GO
