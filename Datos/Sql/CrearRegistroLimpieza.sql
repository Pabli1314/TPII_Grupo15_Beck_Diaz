-- Ejecutar una sola vez contra la base beck_diaz_db (LocalDB): crea la tabla
-- registro_limpieza que usa RegistroLimpiezaDAO (RF-05.3, marcar habitación
-- como disponible tras la limpieza) y que todavía no existe en la base.
--
-- No se agrega FK a Usuario porque esa tabla todavía usa dni_usuario como PK
-- (ver Datos/Sql/AlterUsuario.sql) y el resto de la capa de datos espera
-- id_usuario; hasta que esa migración se haga, id_usuario queda sin FK acá.
CREATE TABLE registro_limpieza (
    id_limpieza INT IDENTITY(1,1),
    fecha_limpieza DATE NOT NULL CONSTRAINT DF_registro_limpieza_fecha DEFAULT CONVERT(DATE, GETDATE()),
    hora_inicio TIME NOT NULL,
    hora_fin TIME NOT NULL,
    nro_habitacion INT NOT NULL,
    id_usuario INT NOT NULL,
    CONSTRAINT PK_registro_limpieza_id PRIMARY KEY (id_limpieza),
    CONSTRAINT FK_registro_limpieza_habitacion FOREIGN KEY (nro_habitacion) REFERENCES habitacion(nro_habitacion)
);
