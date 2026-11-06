-- Se puede ejecutar más de una vez contra beck_diaz_db: cada paso verifica antes de aplicar.
--
-- Largos y formatos de la tabla Usuario, alineados con GestionUsuarios.ValidarDatosBasicos:
--   nombre / apellido: 2 a 30 caracteres, sin dígitos
--   dirección:         5 a 50 caracteres
--   teléfono:          exactamente 10 dígitos
--   correo:            hasta 50 caracteres, con formato x@y.z

-- 1. Ampliar columnas. UQ_usuario_correo se recrea porque un índice impide el ALTER COLUMN.
IF COL_LENGTH('Usuario', 'nom_usuario') < 30
    ALTER TABLE Usuario ALTER COLUMN nom_usuario VARCHAR(30) NOT NULL;
GO

IF COL_LENGTH('Usuario', 'ape_usuario') < 30
    ALTER TABLE Usuario ALTER COLUMN ape_usuario VARCHAR(30) NOT NULL;
GO

IF COL_LENGTH('Usuario', 'correo_usuario') < 50
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_usuario_correo' AND object_id = OBJECT_ID('Usuario'))
        ALTER TABLE Usuario DROP CONSTRAINT UQ_usuario_correo;

    ALTER TABLE Usuario ALTER COLUMN correo_usuario VARCHAR(50) NOT NULL;
    ALTER TABLE Usuario ADD CONSTRAINT UQ_usuario_correo UNIQUE (correo_usuario);
END
GO

-- 2. Restricciones CHECK. Si algún registro no cumple, se avisa y no se agrega esa restricción.
IF OBJECT_ID('CK_usuario_nom_largo') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE LEN(nom_usuario) < 2)
        PRINT 'Hay usuarios con nombre de menos de 2 caracteres: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_nom_largo CHECK (LEN(nom_usuario) >= 2);
END
GO

IF OBJECT_ID('CK_usuario_nom_sin_digitos') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE nom_usuario LIKE '%[0-9]%')
        PRINT 'Hay usuarios con números en el nombre: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_nom_sin_digitos CHECK (nom_usuario NOT LIKE '%[0-9]%');
END
GO

IF OBJECT_ID('CK_usuario_ape_largo') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE LEN(ape_usuario) < 2)
        PRINT 'Hay usuarios con apellido de menos de 2 caracteres: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_ape_largo CHECK (LEN(ape_usuario) >= 2);
END
GO

IF OBJECT_ID('CK_usuario_ape_sin_digitos') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE ape_usuario LIKE '%[0-9]%')
        PRINT 'Hay usuarios con números en el apellido: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_ape_sin_digitos CHECK (ape_usuario NOT LIKE '%[0-9]%');
END
GO

IF OBJECT_ID('CK_usuario_direccion_largo') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE LEN(direccion) < 5)
        PRINT 'Hay usuarios con dirección de menos de 5 caracteres: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_direccion_largo CHECK (LEN(direccion) >= 5);
END
GO

IF OBJECT_ID('CK_usuario_telefono_largo') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE LEN(telefono_usuario) <> 10)
        PRINT 'Hay usuarios con teléfono que no tiene 10 dígitos: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_telefono_largo CHECK (LEN(telefono_usuario) = 10);
END
GO

IF OBJECT_ID('CK_usuario_correo_formato') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE correo_usuario NOT LIKE '%_@_%._%' OR correo_usuario LIKE '% %')
        PRINT 'Hay usuarios con correo inválido: corregirlos y volver a ejecutar.';
    ELSE
        ALTER TABLE Usuario ADD CONSTRAINT CK_usuario_correo_formato
            CHECK (correo_usuario LIKE '%_@_%._%' AND correo_usuario NOT LIKE '% %');
END
GO
