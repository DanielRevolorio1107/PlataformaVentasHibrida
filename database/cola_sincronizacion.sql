USE PlataformaVentasDb;
GO

IF OBJECT_ID('dbo.ColaSincronizacion', 'U') IS NULL
BEGIN

    CREATE TABLE ColaSincronizacion
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_ColaSincronizacion
            PRIMARY KEY
            DEFAULT NEWSEQUENTIALID(),

        Entidad NVARCHAR(50) NOT NULL,

        EntidadId UNIQUEIDENTIFIER NOT NULL,

        TipoOperacion NVARCHAR(20) NOT NULL,

        Payload NVARCHAR(MAX) NULL,

        Estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ColaSincronizacion_Estado
            DEFAULT 'PENDIENTE',

        Intentos INT NOT NULL
            CONSTRAINT DF_ColaSincronizacion_Intentos
            DEFAULT 0,

        FechaCreacion DATETIME2 NOT NULL
            CONSTRAINT DF_ColaSincronizacion_FechaCreacion
            DEFAULT SYSDATETIME(),

        FechaUltimoIntento DATETIME2 NULL,

        FechaSincronizacion DATETIME2 NULL,

        UltimoError NVARCHAR(1000) NULL,

        CONSTRAINT CK_ColaSincronizacion_TipoOperacion
            CHECK (
                TipoOperacion IN (
                    'CREAR',
                    'ACTUALIZAR',
                    'ELIMINAR'
                )
            ),

        CONSTRAINT CK_ColaSincronizacion_Estado
            CHECK (
                Estado IN (
                    'PENDIENTE',
                    'PROCESANDO',
                    'SINCRONIZADO',
                    'ERROR'
                )
            )
    );


    CREATE INDEX IX_ColaSincronizacion_Estado
        ON ColaSincronizacion(Estado);


    CREATE INDEX IX_ColaSincronizacion_Entidad
        ON ColaSincronizacion(
            Entidad,
            EntidadId
        );

END;
GO

SELECT *
FROM ColaSincronizacion;