IF DB_ID('PlataformaVentasDb') IS NULL
BEGIN
    CREATE DATABASE PlataformaVentasDb;
END;
GO

USE PlataformaVentasDb;
GO

CREATE TABLE Roles
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Usuarios
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    RolId UNIQUEIDENTIFIER NOT NULL,
    NombreCompleto NVARCHAR(120) NOT NULL,
    NombreUsuario NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2 NULL,

    CONSTRAINT FK_Usuarios_Roles
        FOREIGN KEY (RolId) REFERENCES Roles(Id)
);
GO

CREATE TABLE Productos
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    Precio DECIMAL(10,2) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2 NULL,

    CONSTRAINT CK_Productos_Precio
        CHECK (Precio >= 0)
);
GO

CREATE TABLE MetodosPago
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Ventas
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UsuarioId UNIQUEIDENTIFIER NOT NULL,
    MetodoPagoId UNIQUEIDENTIFIER NOT NULL,
    FechaVenta DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Total DECIMAL(12,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'REGISTRADA',
    Observaciones NVARCHAR(250) NULL,
    Sincronizado BIT NOT NULL DEFAULT 0,
    FechaSincronizacion DATETIME2 NULL,

    CONSTRAINT FK_Ventas_Usuarios
        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),

    CONSTRAINT FK_Ventas_MetodosPago
        FOREIGN KEY (MetodoPagoId) REFERENCES MetodosPago(Id),

    CONSTRAINT CK_Ventas_Total
        CHECK (Total >= 0),

    CONSTRAINT CK_Ventas_Estado
        CHECK (Estado IN ('REGISTRADA', 'ANULADA'))
);
GO

CREATE TABLE DetalleVentas
(
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    VentaId UNIQUEIDENTIFIER NOT NULL,
    ProductoId UNIQUEIDENTIFIER NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(12,2) NOT NULL,

    CONSTRAINT FK_DetalleVentas_Ventas
        FOREIGN KEY (VentaId) REFERENCES Ventas(Id),

    CONSTRAINT FK_DetalleVentas_Productos
        FOREIGN KEY (ProductoId) REFERENCES Productos(Id),

    CONSTRAINT CK_DetalleVentas_Cantidad
        CHECK (Cantidad > 0),

    CONSTRAINT CK_DetalleVentas_Precios
        CHECK (PrecioUnitario >= 0 AND Subtotal >= 0)
);
GO

CREATE INDEX IX_Productos_Nombre
ON Productos(Nombre);
GO

CREATE INDEX IX_Ventas_FechaVenta
ON Ventas(FechaVenta);
GO

CREATE INDEX IX_DetalleVentas_VentaId
ON DetalleVentas(VentaId);
GO

INSERT INTO Roles (Nombre)
VALUES ('Administrador'), ('Cajero');
GO

INSERT INTO MetodosPago (Nombre)
VALUES ('Efectivo'), ('Tarjeta'), ('Transferencia');
GO