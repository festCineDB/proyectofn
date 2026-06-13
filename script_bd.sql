CREATE OR ALTER PROCEDURE sp_CrearEventoParalelo
    @NombreEvento VARCHAR(200),
    @TipoEvento   VARCHAR(50),
    @FechaHora    DATETIME,
    @Aforo        INT,
    @CostoInscripcion DECIMAL(10,2),
    @Respuesta    VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO EventosParalelos (NombreEvento, TipoEvento, FechaHora, Aforo, CostoInscripcion)
    VALUES (@NombreEvento, @TipoEvento, @FechaHora, @Aforo, @CostoInscripcion);

    SET @Respuesta = 'Evento paralelo creado correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE RegistrarAsistenteEvento
    @IdAsistente INT,
    @IdEvento    INT,
    @Respuesta   VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Aforo INT, @NombreEvento VARCHAR(200), @Ocupados INT;

    SELECT @Aforo = Aforo, @NombreEvento = NombreEvento
      FROM EventosParalelos WHERE IdEvento = @IdEvento;
    IF @@ROWCOUNT = 0 THROW 50030, N'El evento indicado no existe.', 1;

    IF NOT EXISTS (SELECT 1 FROM Asistentes WHERE IdAsistente = @IdAsistente)
        THROW 50031, N'El asistente indicado no existe.', 1;

    IF EXISTS (SELECT 1 FROM Entradas WHERE IdAsistente = @IdAsistente AND IdEvento = @IdEvento)
        THROW 50032, N'El asistente ya está registrado en este evento.', 1;

    SELECT @Ocupados = COUNT(*) FROM Entradas WHERE IdEvento = @IdEvento;
    IF @Ocupados >= @Aforo THROW 50033, N'El evento ha alcanzado su aforo máximo.', 1;

    INSERT INTO Entradas (IdAsistente, IdEvento, IdTarifa)
    VALUES (@IdAsistente, @IdEvento, 1);

    SET @Respuesta = 'Asistente registrado exitosamente en "' + @NombreEvento + '".';
END;
GO
