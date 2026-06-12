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

    private static (DataTable Datos, string Respuesta) EjecutarReporte(string nombreSp)
    {
        using SqlConnection conn = AbrirConexion();
        using var comando = new SqlCommand(nombreSp, conn);
        comando.CommandType = CommandType.StoredProcedure;

        var respuesta = new SqlParameter("@Respuesta", SqlDbType.VarChar, 200)
        { Direction = ParameterDirection.Output };
        comando.Parameters.Add(respuesta);

        var dt = new DataTable();
        using var da = new SqlDataAdapter(comando);
        da.Fill(dt);
        return (dt, respuesta.Value?.ToString() ?? "Reporte generado.");
    }

    public static (DataTable Datos, string Respuesta) ReporteRanking()    => EjecutarReporte("sp_ReporteRanking");
    public static (DataTable Datos, string Respuesta) ReportePremiacion() => EjecutarReporte("sp_ReportePremiacion");
    public static (DataTable Datos, string Respuesta) ReporteFinanciero() => EjecutarReporte("sp_ReporteFinanciero");
}
