/*CREATE DATABASE beck_diaz_db;
GO

USE beck_diaz_db;
GO*/



----------------------------------------------------
-- 1. TABLAS INDEPENDIENTES
----------------------------------------------------

CREATE TABLE Tipo_habitacion (
    id_tipo_habitacion INT IDENTITY(1,1),
    descripcion VARCHAR(100) NOT NULL,
    CONSTRAINT PK_tipo_habitacion_id PRIMARY KEY (id_tipo_habitacion)
);

CREATE TABLE Estado_habitacion (
    id_estado INT IDENTITY(1,1),
    nom_estado_habitacion VARCHAR(50) NOT NULL,
    CONSTRAINT PK_estado_habitacion_id PRIMARY KEY (id_estado)
);

CREATE TABLE Rol (
    id_rol INT IDENTITY(1,1),
    nom_rol VARCHAR(50) NOT NULL,
    CONSTRAINT PK_rol_id PRIMARY KEY (id_rol)
);

CREATE TABLE metodo_pago (
    id_metodo INT IDENTITY(1,1),
    nom_metodo_pago VARCHAR(50) NOT NULL,
    CONSTRAINT PK_metodo_pago_id PRIMARY KEY (id_metodo)
);

CREATE TABLE Huesped (
    dni_huesped VARCHAR(8),
    nombre_huesped VARCHAR(50) NOT NULL,
    apellido_huesped VARCHAR(50) NOT NULL,
    telefono_huesped VARCHAR(10) NOT NULL,
    direccion_huesped VARCHAR(50) NOT NULL,
    correo_huesped VARCHAR(50) NOT NULL,
    alta_huesped DATE DEFAULT CONVERT(DATE, GETDATE()),
    CONSTRAINT PK_huesped_dni PRIMARY KEY (dni_huesped),
    CONSTRAINT CK_huesped_dni CHECK (dni_huesped NOT LIKE '%[^0-9]%'),
    CONSTRAINT UQ_huesped_telefono UNIQUE(telefono_huesped),
    CONSTRAINT UQ_huesped_correo UNIQUE(correo_huesped),
    CONSTRAINT CK_huesped_telefono CHECK (telefono_huesped NOT LIKE '%[^0-9]%') 
);


CREATE TABLE categoria_producto(
    id_categoria INT IDENTITY(1,1),
    descripcion_cat VARCHAR(15) NOT NULL,
    CONSTRAINT PK_categoria_id PRIMARY KEY(id_categoria)
);

----------------------------------------------------
-- 2. TABLAS CON DEPENDENCIAS PRIMARIAS
----------------------------------------------------

-- Tabla: habitacion
CREATE TABLE habitacion (
    nro_habitacion INT,
    piso INT NOT NULL,
    cant_camas INT NOT NULL,
    tarifa_base DECIMAL(10, 2) NOT NULL,
    id_tipo_habitacion INT NOT NULL,
    id_estado INT NOT NULL,
    CONSTRAINT PK_habitacion_nro PRIMARY KEY(nro_habitacion),
    CONSTRAINT CH_habitacion_piso CHECK (piso IN (1,2,3)),
    CONSTRAINT FK_Habitacion_Tipo FOREIGN KEY (id_tipo_habitacion) REFERENCES Tipo_habitacion(id_tipo_habitacion),
    CONSTRAINT FK_Habitacion_Estado FOREIGN KEY (id_estado) REFERENCES Estado_habitacion(id_estado)
);

-- Tabla: Usuario
CREATE TABLE Usuario (
    dni_usuario VARCHAR(8),
    nom_usuario VARCHAR(20) NOT NULL,
    ape_usuario VARCHAR(20) NOT NULL,
    direccion VARCHAR(50) NOT NULL,
    telefono_usuario VARCHAR(10) NOT NULL,
    correo_usuario VARCHAR(20) NOT NULL,
    pasword VARCHAR(256) NOT NULL,
    estado BIT NOT NULL DEFAULT 1,
    id_rol INT NOT NULL,
    alta_usuario DATE DEFAULT CONVERT(DATE, GETDATE()),
    CONSTRAINT PK_usuario_id PRIMARY KEY(dni_usuario),
    CONSTRAINT CK_usuario_dni CHECK (dni_usuario NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_usuario_telefono CHECK (telefono_usuario NOT LIKE '%[^0-9]%'),
    CONSTRAINT UQ_usuario_telefono UNIQUE (telefono_usuario),
    CONSTRAINT UQ_usuario_correo UNIQUE (correo_usuario),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (id_rol) REFERENCES Rol(id_rol)
);

-- Tabla: Turno_caja
CREATE TABLE Turno_caja (
    id_turno INT IDENTITY(1,1),
    fecha_apertura DATE NOT NULL,
    hora_apertura TIME NOT NULL,
    fecha_cierre DATE NULL,
    hora_cierre TIME NULL,
    monto_inicial DECIMAL(10, 2) NOT NULL,
    monto_final DECIMAL(10, 2) NULL,
    observaciones VARCHAR(255) NULL,
    dni_usuario VARCHAR(8) NOT NULL,
    CONSTRAINT PK_turno_caja_id PRIMARY KEY (id_turno),
    CONSTRAINT CK_TurnoCaja_dniUsuario CHECK (dni_usuario NOT LIKE '%[^0-9]%'),
    CONSTRAINT FK_TurnoCaja_Usuario FOREIGN KEY (dni_usuario) REFERENCES Usuario(dni_usuario)
);

-- Tabla: producto (Cambiado a DECIMAL y agregada la FK correspondiente)
CREATE TABLE producto(
    cod_producto VARCHAR(15),
    descripcion_product VARCHAR(20) NOT NULL,
    precio DECIMAL(10, 2) NOT NULL,
    stock_min INT NOT NULL,
    stock_disponible INT NOT NULL,
    estado_producto BIT NOT NULL,
    id_categoria INT NOT NULL,
    CONSTRAINT PK_producto_cod PRIMARY KEY(cod_producto),
    CONSTRAINT CH_producto_disponible CHECK(stock_disponible >= stock_min),
    CONSTRAINT FK_producto_categoria FOREIGN KEY (id_categoria) REFERENCES categoria_producto(id_categoria)
);

----------------------------------------------------
-- 3. TABLAS CON DEPENDENCIAS MÚLTIPLES
----------------------------------------------------
CREATE TABLE registro_limpieza (
    id_limpieza INT IDENTITY(1,1),
    fecha_limpieza DATE NOT NULL DEFAULT CONVERT(DATE, GETDATE()),
    hora_inicio TIME NOT NULL,
    hora_fin TIME NOT NULL,
    nro_habitacion INT NOT NULL,
    dni_usuario VARCHAR(8) NOT NULL,
    CONSTRAINT PK_registro_limpieza_id PRIMARY KEY (id_limpieza),
    CONSTRAINT CK_RegistroLimpieza_dniUsuario CHECK (dni_usuario NOT LIKE '%[^0-9]%'),
    CONSTRAINT FK_RegistroLimpieza_Usuario FOREIGN KEY (dni_usuario) REFERENCES Usuario(dni_usuario)
);

-- Tabla: hospedaje
CREATE TABLE hospedaje (
    id_hospedaje INT IDENTITY(1,1),
    fecha_entrada DATE CONSTRAINT DF_fecha_entrada DEFAULT CONVERT(DATE, GETDATE()),
    hora_entrada TIME CONSTRAINT DF_hora_entrada DEFAULT CONVERT(TIME, GETDATE()),
    fecha_salida DATE NOT NULL,
    hora_salida TIME NOT NULL,
    id_metodo INT NOT NULL,
    nro_habitacion INT NOT NULL,
    id_turno INT NOT NULL,
    dni_huesped VARCHAR(8) NOT NULL,
    CONSTRAINT PK_Rhospedaje_id PRIMARY KEY(id_hospedaje),
    CONSTRAINT FK_hospedaje_Metodo FOREIGN KEY (id_metodo) REFERENCES metodo_pago(id_metodo),
    CONSTRAINT FK_hospedaje_Habitacion FOREIGN KEY (nro_habitacion) REFERENCES habitacion(nro_habitacion),
    CONSTRAINT FK_hospedaje_Turno FOREIGN KEY (id_turno) REFERENCES Turno_caja(id_turno),
    CONSTRAINT FK_hospedaje_Huesped FOREIGN KEY (dni_huesped) REFERENCES Huesped(dni_huesped)
);

CREATE TABLE venta(
    id_venta INT IDENTITY(1,1),
    fecha_venta DATE DEFAULT CONVERT(DATE, GETDATE()),
    hora_venta TIME DEFAULT CONVERT(TIME, GETDATE()),
    total DECIMAL(10, 2) NOT NULL,
    id_metodo INT NOT NULL,
    dni_huesped VARCHAR(8) NULL, -- Huésped que realiza la compra (NULL = venta de mostrador)
    CONSTRAINT PK_venta_id PRIMARY KEY(id_venta),
    CONSTRAINT FK_venta_metodo FOREIGN KEY (id_metodo) REFERENCES metodo_pago(id_metodo),
    CONSTRAINT FK_venta_huesped FOREIGN KEY (dni_huesped) REFERENCES Huesped(dni_huesped),
    CONSTRAINT CK_venta_dniHuesped CHECK (dni_huesped NOT LIKE '%[^0-9]%')
);

-- Tabla: detalle_venta (Cambiado a DECIMAL y agregada restricción CHECK de cantidad)
CREATE TABLE detalle_venta(
    id_venta INT NOT NULL,
    cod_producto VARCHAR(15) NOT NULL,
    precio_unitario DECIMAL(10, 2) NOT NULL,
    cantidad INT NOT NULL,
    subtotal DECIMAL(10, 2) NOT NULL,
    CONSTRAINT PK_detalle_venta PRIMARY KEY(id_venta, cod_producto),
    CONSTRAINT FK_detalle_venta FOREIGN KEY(id_venta) REFERENCES venta(id_venta) ON DELETE CASCADE,
    CONSTRAINT FK_detalle_producto FOREIGN KEY(cod_producto) REFERENCES producto(cod_producto),
    CONSTRAINT CK_detalle_cantidad CHECK (cantidad > 0)
);
