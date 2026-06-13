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

CREATE OR ALTER PROCEDURE EditarPelicula
    @IdPelicula    INT,
    @Titulo        VARCHAR(150),
    @AnioProd      INT,
    @Duracion      INT,
    @PaisOrigen    VARCHAR(60),
    @Sinopsis      VARCHAR(MAX),
    @Clasificacion VARCHAR(10),
    @Formato       VARCHAR(10),
    @Estado        VARCHAR(15),
    @Respuesta     VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Peliculas
       SET Titulo = @Titulo, AnioProd = @AnioProd, Duracion = @Duracion,
           PaisOrigen = @PaisOrigen, Sinopsis = @Sinopsis,
           Clasificacion = @Clasificacion, Formato = @Formato, Estado = @Estado
     WHERE IdPelicula = @IdPelicula;
    IF @@ROWCOUNT = 0 THROW 50040, N'La película indicada no existe.', 1;
    SET @Respuesta = 'Película actualizada correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE EditarSede
    @IdSede     INT,
    @NombreSede VARCHAR(100),
    @Direccion  VARCHAR(200),
    @Ciudad     VARCHAR(60),
    @SitioWeb   VARCHAR(100),
    @Respuesta  VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Sedes
       SET NombreSede = @NombreSede, Direccion = @Direccion,
           Ciudad = @Ciudad, SitioWeb = @SitioWeb
     WHERE IdSede = @IdSede;
    IF @@ROWCOUNT = 0 THROW 50041, N'La sede indicada no existe.', 1;
    SET @Respuesta = 'Sede actualizada correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE EditarSala
    @IdSala     INT,
    @NombreSala VARCHAR(60),
    @Capacidad  INT,
    @IdSede     INT,
    @Respuesta  VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM Proyecciones pr
        WHERE pr.IdSala = @IdSala
          AND (SELECT COUNT(*) FROM Entradas e WHERE e.IdProyeccion = pr.IdProyeccion) > @Capacidad
    )
        THROW 50043, N'No se puede reducir la capacidad: hay proyecciones con más entradas vendidas que la nueva capacidad.', 1;
    UPDATE Salas
       SET NombreSala = @NombreSala, Capacidad = @Capacidad, IdSede = @IdSede
     WHERE IdSala = @IdSala;
    IF @@ROWCOUNT = 0 THROW 50042, N'La sala indicada no existe.', 1;
    SET @Respuesta = 'Sala actualizada correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE sp_ListarSedes AS
BEGIN SET NOCOUNT ON; SELECT IdSede, NombreSede, Direccion, Ciudad, SitioWeb FROM Sedes ORDER BY NombreSede; END;
GO

CREATE OR ALTER PROCEDURE sp_ObtenerGanadorCategoria
    @IdCategoria INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 e.IdPelicula, p.Titulo, AVG(CAST(e.Puntuacion AS DECIMAL(10,2))) AS Promedio
    FROM Evaluaciones e
    INNER JOIN Peliculas p ON p.IdPelicula = e.IdPelicula
    WHERE e.IdCategoria = @IdCategoria
    GROUP BY e.IdPelicula, p.Titulo
    ORDER BY Promedio DESC;
END;
GO

CREATE OR ALTER PROCEDURE sp_ListarCategoriasPorAnio
    @AnioEdicion INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdCategoria, NombreCategoria, AnioEdicion
    FROM Categorias
    WHERE @AnioEdicion IS NULL OR AnioEdicion = @AnioEdicion
    ORDER BY NombreCategoria;
END;
GO

CREATE OR ALTER PROCEDURE sp_ListarPeliculasPorAnio
    @AnioEdicion INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdPelicula, Titulo, AnioProd
    FROM Peliculas
    WHERE @AnioEdicion IS NULL OR AnioProd = @AnioEdicion
    ORDER BY Titulo;
END;
GO

CREATE OR ALTER PROCEDURE AsignarPeliculaCategoria
    @IdPelicula INT,
    @IdCategoria INT,
    @Respuesta VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM CompetenciaPelicula WHERE IdPelicula = @IdPelicula AND IdCategoria = @IdCategoria)
        THROW 50060, N'La película ya está asignada a esta categoría.', 1;
    INSERT INTO CompetenciaPelicula (IdPelicula, IdCategoria) VALUES (@IdPelicula, @IdCategoria);
    SET @Respuesta = 'Película asignada a la categoría correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE sp_ListarPeliculasEnCompetenciaPorAnio
    @IdCategoria INT,
    @AnioEdicion INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.IdPelicula, p.Titulo
    FROM CompetenciaPelicula cp
    INNER JOIN Peliculas p ON p.IdPelicula = cp.IdPelicula
    WHERE cp.IdCategoria = @IdCategoria
      AND (@AnioEdicion IS NULL OR p.AnioProd = @AnioEdicion)
    ORDER BY p.Titulo;
END;
GO
