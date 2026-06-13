using System.Data;
using Microsoft.Data.SqlClient;

namespace FestCine.Cliente;

/// <summary>
/// Capa de acceso a datos de la aplicación cliente.
///
/// REGLA DEL PROYECTO: esta clase NO contiene ninguna sentencia SQL.
/// Toda interacción con la base de datos se realiza exclusivamente
/// invocando los procedimientos almacenados del servidor
/// (CommandType.StoredProcedure), con parámetros de entrada y de
/// salida (OUTPUT), igual que los ejemplos vistos en clase.
/// </summary>
public static class ProcedimientosBD
{
    private static SqlConnection AbrirConexion()
    {
        var conn = new SqlConnection(ConexionBD.CadenaConexion);
        conn.Open();
        return conn;
    }

    /// <summary>Invoca un SP que devuelve una colección de filas.</summary>
    private static DataTable EjecutarSpTabla(string nombreSp, params SqlParameter[] parametros)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand(nombreSp, conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddRange(parametros);

        var dt = new DataTable();
        using var da = new SqlDataAdapter(comando);
        da.Fill(dt);
        return dt;
    }

    /* ── Listados (llenan combos y grillas) ─────────────────── */

    public static DataTable ListarPeliculas()   => EjecutarSpTabla("sp_ListarPeliculas");
    public static DataTable ListarPeliculasPorAnio(int? anio = null) =>
        anio.HasValue
            ? EjecutarSpTabla("sp_ListarPeliculasPorAnio", new SqlParameter("@AnioEdicion", anio.Value))
            : EjecutarSpTabla("sp_ListarPeliculasPorAnio");
    public static DataTable ListarTarifas()     => EjecutarSpTabla("sp_ListarTarifas");
    public static DataTable ListarAsistentes()  => EjecutarSpTabla("sp_ListarAsistentes");
    public static DataTable ListarSalas()       => EjecutarSpTabla("sp_ListarSalas");
    public static DataTable ListarTiposAbono()  => EjecutarSpTabla("sp_ListarTiposAbono");
    public static DataTable ListarAbonosVendidos() => EjecutarSpTabla("sp_ListarAbonosVendidos");

    public static DataTable ListarProyecciones(int? idPelicula = null) =>
        EjecutarSpTabla("sp_ListarProyecciones",
            new SqlParameter("@IdPelicula", (object?)idPelicula ?? DBNull.Value));

    /* ── P1: Proceso de Compra de Entrada ───────────────────── */

    public static string ComprarEntrada(int idAsistente, int idProyeccion, int idTarifa)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("ComprarEntrada", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdAsistente",  idAsistente);
        comando.Parameters.AddWithValue("@IdProyeccion", idProyeccion);
        comando.Parameters.AddWithValue("@IdTarifa",     idTarifa);

        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);

        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    /* ── T1: Transacción crítica "Venta de Abono" ───────────── */

    public static string VenderAbono(int idAsistente, int idTipoAbono, bool pagoExitoso)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("VenderAbono", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdAsistente", idAsistente);
        comando.Parameters.AddWithValue("@IdTipoAbono", idTipoAbono);
        comando.Parameters.AddWithValue("@PagoExitoso", pagoExitoso);

        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);

        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    /* ── Módulo 2: Programar Proyección (Trigger TR1 valida) ── */

    public static string ProgramarProyeccion(int idPelicula, int idSala, DateTime fechaHora, bool tieneQA)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("ProgramarProyeccion", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@IdSala",     idSala);
        comando.Parameters.AddWithValue("@FechaHora",  fechaHora);
        comando.Parameters.AddWithValue("@TieneQA",    tieneQA);

        var idNuevo = new SqlParameter("@IdNuevo", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(idNuevo);
        comando.Parameters.Add(respuesta);

        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    /* ── Reportes (filas + parámetro OUTPUT con el resultado) ── */

    private static (DataTable Datos, string Respuesta) EjecutarReporte(string nombreSp, int? anioEdicion = null)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand(nombreSp, conn);
        comando.CommandType = CommandType.StoredProcedure;

        if (anioEdicion.HasValue)
            comando.Parameters.AddWithValue("@AnioEdicion", anioEdicion.Value);

        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 200)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);

        var dt = new DataTable();
        using var da = new SqlDataAdapter(comando);
        da.Fill(dt);
        return (dt, respuesta.Value?.ToString() ?? "Reporte generado.");
    }

    public static (DataTable Datos, string Respuesta) ReporteRanking(int? anioEdicion = null)    => EjecutarReporte("sp_ReporteRanking", anioEdicion);
    public static (DataTable Datos, string Respuesta) ReportePremiacion(int? anioEdicion = null) => EjecutarReporte("sp_ReportePremiacion", anioEdicion);
    public static (DataTable Datos, string Respuesta) ReporteFinanciero(int? anioEdicion = null) => EjecutarReporte("sp_ReporteFinanciero", anioEdicion);

    /* ── Eventos Paralelos ──────────────────────────────────── */

    public static DataTable ListarEventosParalelos() => EjecutarSpTabla("sp_ListarEventosParalelos");
    public static DataTable ListarPersonal()         => EjecutarSpTabla("sp_ListarPersonal");

    public static DataTable ListarExpositoresPorEvento(int idEvento) =>
        EjecutarSpTabla("sp_ListarExpositoresPorEvento",
            new SqlParameter("@IdEvento", idEvento));

    public static DataTable ListarAsistentesPorEvento(int idEvento) =>
        EjecutarSpTabla("sp_ListarAsistentesPorEvento",
            new SqlParameter("@IdEvento", idEvento));

    public static string AgregarExpositorEvento(int idEvento, int idPersonal)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("AgregarExpositorEvento", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdEvento", idEvento);
        comando.Parameters.AddWithValue("@IdPersonal", idPersonal);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string EliminarExpositorEvento(int idEvento, int idPersonal)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("EliminarExpositorEvento", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdEvento", idEvento);
        comando.Parameters.AddWithValue("@IdPersonal", idPersonal);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string RegistrarAsistenteEvento(int idAsistente, int idEvento)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarAsistenteEvento", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdAsistente", idAsistente);
        comando.Parameters.AddWithValue("@IdEvento", idEvento);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string CrearEventoParalelo(string nombreEvento, string tipoEvento, DateTime fechaHora, int aforo, decimal costoInscripcion)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("sp_CrearEventoParalelo", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@NombreEvento", nombreEvento);
        comando.Parameters.AddWithValue("@TipoEvento", tipoEvento);
        comando.Parameters.AddWithValue("@FechaHora", fechaHora);
        comando.Parameters.AddWithValue("@Aforo", aforo);
        comando.Parameters.AddWithValue("@CostoInscripcion", costoInscripcion);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    /* ── Competencia y Jurados ─────────────────────────────── */

    public static DataTable ListarCategorias()          => EjecutarSpTabla("sp_ListarCategorias");

    public static DataTable ListarCategoriasPorAnio(int? anio = null) =>
        anio.HasValue
            ? EjecutarSpTabla("sp_ListarCategoriasPorAnio", new SqlParameter("@AnioEdicion", anio.Value))
            : EjecutarSpTabla("sp_ListarCategoriasPorAnio");
    public static DataTable ListarMiembrosJurado()       => EjecutarSpTabla("sp_ListarMiembrosJurado");
    public static DataTable ListarPeliculasCompetencia() => EjecutarSpTabla("sp_ListarPeliculas");

    public static DataTable ListarPeliculasEnCompetencia(int idCategoria) =>
        EjecutarSpTabla("sp_ListarPeliculasEnCompetencia",
            new SqlParameter("@IdCategoria", idCategoria));

    public static DataTable ListarPeliculasEnCompetenciaPorAnio(int idCategoria, int? anioEdicion = null)
    {
        var ps = new List<SqlParameter> { new("@IdCategoria", idCategoria) };
        if (anioEdicion.HasValue)
            ps.Add(new("@AnioEdicion", anioEdicion.Value));
        return EjecutarSpTabla("sp_ListarPeliculasEnCompetenciaPorAnio", ps.ToArray());
    }

    public static DataTable ListarMiembrosPorCategoria(int idCategoria) =>
        EjecutarSpTabla("sp_ListarMiembrosPorCategoria",
            new SqlParameter("@IdCategoria", idCategoria));

    public static DataTable ListarEvaluacionesPorCategoria(int idCategoria) =>
        EjecutarSpTabla("sp_ListarEvaluacionesPorCategoria",
            new SqlParameter("@IdCategoria", idCategoria));

    public static DataTable ListarPremios() => EjecutarSpTabla("sp_ListarPremios");

    public static DataTable ObtenerGanadorCategoria(int idCategoria) =>
        EjecutarSpTabla("sp_ObtenerGanadorCategoria",
            new SqlParameter("@IdCategoria", idCategoria));

    public static string RegistrarEvaluacion(int idMiembro, int idPelicula, int idCategoria, int puntuacion, string comentario)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarEvaluacion", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdMiembro", idMiembro);
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@IdCategoria", idCategoria);
        comando.Parameters.AddWithValue("@Puntuacion", puntuacion);
        comando.Parameters.AddWithValue("@Comentario", comentario);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string RegistrarPremio(int idCategoria, int idPelicula, int anioEdicion)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarPremio", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdCategoria", idCategoria);
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@AnioEdicion", anioEdicion);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    /* ── Logística y Patrocinios ───────────────────────────── */

    public static DataTable ListarHoteles()        => EjecutarSpTabla("sp_ListarHoteles");
    public static DataTable ListarEdiciones()      => EjecutarSpTabla("sp_ListarEdiciones");
    public static DataTable ListarPatrocinadores()  => EjecutarSpTabla("sp_ListarPatrocinadores");
    public static DataTable ListarAlojamientos()    => EjecutarSpTabla("sp_ListarAlojamientos");
    public static DataTable ListarTraslados()       => EjecutarSpTabla("sp_ListarTraslados");
    public static DataTable ListarPatrocinioEdicion() => EjecutarSpTabla("sp_ListarPatrocinioEdicion");

    public static string RegistrarAlojamiento(int idPersonal, int idHotel, string nroHabitacion, DateTime checkIn, DateTime checkOut)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarAlojamiento", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPersonal", idPersonal);
        comando.Parameters.AddWithValue("@IdHotel", idHotel);
        comando.Parameters.AddWithValue("@NroHabitacion", nroHabitacion);
        comando.Parameters.AddWithValue("@CheckIn", checkIn);
        comando.Parameters.AddWithValue("@CheckOut", checkOut);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string RegistrarTraslado(int idPersonal, string tipo, string origen, string destino, DateTime fechaHora, string nroVuelo)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarTraslado", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPersonal", idPersonal);
        comando.Parameters.AddWithValue("@TipoTraslado", tipo);
        comando.Parameters.AddWithValue("@Origen", origen);
        comando.Parameters.AddWithValue("@Destino", destino);
        comando.Parameters.AddWithValue("@FechaHora", fechaHora);
        comando.Parameters.AddWithValue("@NroVuelo", nroVuelo ?? "");
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string RegistrarPatrocinio(int idPatrocinador, int idEdicion, string tipoAporte, decimal? monto, string descripcion)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("RegistrarPatrocinio", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPatrocinador", idPatrocinador);
        comando.Parameters.AddWithValue("@IdEdicion", idEdicion);
        comando.Parameters.AddWithValue("@TipoAporte", tipoAporte);
        comando.Parameters.AddWithValue("@Monto", (object?)monto ?? DBNull.Value);
        comando.Parameters.AddWithValue("@Descripcion", descripcion);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static DataTable ListarSedes() => EjecutarSpTabla("sp_ListarSedes");

    public static (string Respuesta, int IdPelicula) CrearPelicula(string titulo, int anioProd, int duracion,
        string paisOrigen, string sinopsis, string clasificacion, string formato, string estado)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("CrearPelicula", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@Titulo", titulo);
        comando.Parameters.AddWithValue("@AnioProd", anioProd);
        comando.Parameters.AddWithValue("@Duracion", duracion);
        comando.Parameters.AddWithValue("@PaisOrigen", paisOrigen);
        comando.Parameters.AddWithValue("@Sinopsis", sinopsis ?? "");
        comando.Parameters.AddWithValue("@Clasificacion", clasificacion);
        comando.Parameters.AddWithValue("@Formato", formato);
        comando.Parameters.AddWithValue("@Estado", estado);
        var idPelicula = new SqlParameter("@IdPelicula", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(idPelicula);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return (respuesta.Value?.ToString() ?? "Operacion completada.",
                Convert.ToInt32(idPelicula.Value));
    }

    public static DataTable ListarGeneros() => EjecutarSpTabla("sp_ListarGeneros");

    public static string AgregarGeneroPelicula(int idPelicula, int idGenero)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("AgregarGeneroPelicula", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@IdGenero", idGenero);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string CrearSala(string nombreSala, int capacidad, int idSede)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("CrearSala", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@NombreSala", nombreSala);
        comando.Parameters.AddWithValue("@Capacidad", capacidad);
        comando.Parameters.AddWithValue("@IdSede", idSede);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string CrearSede(string nombreSede, string direccion, string ciudad, string sitioWeb)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("CrearSede", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@NombreSede", nombreSede);
        comando.Parameters.AddWithValue("@Direccion", direccion ?? "");
        comando.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
        comando.Parameters.AddWithValue("@SitioWeb", sitioWeb ?? "");
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string EditarPelicula(int idPelicula, string titulo, int anioProd, int duracion,
        string paisOrigen, string sinopsis, string clasificacion, string formato, string estado)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("EditarPelicula", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@Titulo", titulo);
        comando.Parameters.AddWithValue("@AnioProd", anioProd);
        comando.Parameters.AddWithValue("@Duracion", duracion);
        comando.Parameters.AddWithValue("@PaisOrigen", paisOrigen);
        comando.Parameters.AddWithValue("@Sinopsis", sinopsis ?? "");
        comando.Parameters.AddWithValue("@Clasificacion", clasificacion);
        comando.Parameters.AddWithValue("@Formato", formato);
        comando.Parameters.AddWithValue("@Estado", estado);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string EditarSede(int idSede, string nombreSede, string direccion, string ciudad, string sitioWeb)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("EditarSede", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdSede", idSede);
        comando.Parameters.AddWithValue("@NombreSede", nombreSede);
        comando.Parameters.AddWithValue("@Direccion", direccion ?? "");
        comando.Parameters.AddWithValue("@Ciudad", ciudad ?? "");
        comando.Parameters.AddWithValue("@SitioWeb", sitioWeb ?? "");
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string EditarSala(int idSala, string nombreSala, int capacidad, int idSede)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("EditarSala", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdSala", idSala);
        comando.Parameters.AddWithValue("@NombreSala", nombreSala);
        comando.Parameters.AddWithValue("@Capacidad", capacidad);
        comando.Parameters.AddWithValue("@IdSede", idSede);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string CrearAsistente(string nombre, string email, string telefono, string tipoAsistente, string profesion = "")
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("CrearAsistente", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@Nombre", nombre);
        comando.Parameters.AddWithValue("@Email", email);
        comando.Parameters.AddWithValue("@Telefono", telefono);
        comando.Parameters.AddWithValue("@TipoAsistente", tipoAsistente);
        comando.Parameters.AddWithValue("@Profesion", profesion ?? "");
        var idAsistente = new SqlParameter("@IdAsistente", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(idAsistente);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }

    public static string AsignarCategoriaJurado(string emailJurado, int idCategoria)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("AsignarCategoriaJurado", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@EmailJurado", emailJurado);
        comando.Parameters.AddWithValue("@IdCategoria", idCategoria);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Categoria asignada.";
    }

    public static string AsignarPeliculaCategoria(int idPelicula, int idCategoria)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand("AsignarPeliculaCategoria", conn);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.AddWithValue("@IdPelicula", idPelicula);
        comando.Parameters.AddWithValue("@IdCategoria", idCategoria);
        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 300)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);
        comando.ExecuteNonQuery();
        return respuesta.Value?.ToString() ?? "Operacion completada.";
    }
}
