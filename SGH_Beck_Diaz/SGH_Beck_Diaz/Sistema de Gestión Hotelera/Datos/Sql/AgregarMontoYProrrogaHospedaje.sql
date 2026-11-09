-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB). El módulo
-- Recepcionista guarda en hospedaje.monto_total lo cobrado por la estadía
-- (check-in + prórrogas) y registra cada extensión en prorroga_estadia.
-- Sin esto, el check-in, el check-out y la caja fallan con
-- "Invalid column name 'monto_total'".

-- 1. Columna nueva (con default para poder agregarla con filas existentes)
IF COL_LENGTH('hospedaje', 'monto_total') IS NULL
BEGIN
    ALTER TABLE hospedaje ADD monto_total FLOAT NOT NULL
        CONSTRAINT DF_hospedaje_monto_total DEFAULT 0;
END
GO

-- 2. Backfill de los hospedajes existentes: tarifa por día del tipo de
-- habitación × noches (mínimo 1), mismo criterio que GestionHospedajes.CalcularNoches.
UPDATE h
SET h.monto_total = th.tarifa *
    CASE WHEN DATEDIFF(DAY, h.fecha_entrada, h.fecha_salida) < 1 THEN 1
         ELSE DATEDIFF(DAY, h.fecha_entrada, h.fecha_salida) END
FROM hospedaje h
INNER JOIN habitacion hab ON hab.nro_habitacion = h.nro_habitacion
INNER JOIN Tipo_habitacion th ON th.id_tipo_habitacion = hab.id_tipo_habitacion
WHERE h.monto_total = 0;
GO

-- 3. Tabla prorroga_estadia (extensión de estadía de habitaciones ocupadas).
-- ProrrogaDAO también la crea si no existe; se deja acá para que quede documentada.
IF OBJECT_ID('prorroga_estadia', 'U') IS NULL
BEGIN
    CREATE TABLE prorroga_estadia (
        id_prorroga INT IDENTITY(1,1),
        id_hospedaje INT NOT NULL,
        fecha_prorroga DATE NOT NULL DEFAULT CONVERT(DATE, GETDATE()),
        hora_prorroga TIME NOT NULL DEFAULT CONVERT(TIME, GETDATE()),
        fecha_salida_anterior DATE NOT NULL,
        hora_salida_anterior TIME NOT NULL,
        nueva_fecha_salida DATE NOT NULL,
        nueva_hora_salida TIME NOT NULL,
        dias_adicionales INT NOT NULL,
        monto_adicional DECIMAL(10, 2) NOT NULL,
        id_metodo INT NOT NULL,
        id_turno INT NOT NULL,
        CONSTRAINT PK_prorroga_estadia PRIMARY KEY (id_prorroga),
        CONSTRAINT FK_prorroga_hospedaje FOREIGN KEY (id_hospedaje) REFERENCES hospedaje(id_hospedaje),
        CONSTRAINT FK_prorroga_metodo FOREIGN KEY (id_metodo) REFERENCES metodo_pago(id_metodo),
        CONSTRAINT FK_prorroga_turno FOREIGN KEY (id_turno) REFERENCES Turno_caja(id_turno)
    );
END
GO
