USE PlataformaVentasRemotaDb;
GO

IF OBJECT_ID('MenusDiarios', 'U') IS NULL
BEGIN
    CREATE TABLE MenusDiarios
    (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Fecha DATE NOT NULL,
        UsuarioId UNIQUEIDENTIFIER NOT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        FechaActualizacion DATETIME2 NULL,

        CONSTRAINT UQ_MenusDiarios_Fecha
            UNIQUE (Fecha),

        CONSTRAINT FK_MenusDiarios_Usuarios
            FOREIGN KEY (UsuarioId)
            REFERENCES Usuarios(Id)
    );
END;
GO

IF OBJECT_ID('MenuDetalles', 'U') IS NULL
BEGIN
    CREATE TABLE MenuDetalles
    (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        MenuDiarioId UNIQUEIDENTIFIER NOT NULL,
        ProductoId UNIQUEIDENTIFIER NOT NULL,
        Disponible BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_MenuDetalles_MenusDiarios
            FOREIGN KEY (MenuDiarioId)
            REFERENCES MenusDiarios(Id),

        CONSTRAINT FK_MenuDetalles_Productos
            FOREIGN KEY (ProductoId)
            REFERENCES Productos(Id),

        CONSTRAINT UQ_MenuDetalles_Menu_Producto
            UNIQUE (MenuDiarioId, ProductoId)
    );
END;
GO