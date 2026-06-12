namespace FestCine.Cliente;

/// <summary>
/// Cadena de conexión a la base de datos FestCine.
/// </summary>
public static class ConexionBD
{
    // SQL Server LocalDB (incluido con Visual Studio).
    // Para una instancia de SQL Server Express / SSMS, cambiar por algo como:
    //   "Data Source=MI-PC\\SQLEXPRESS;Initial Catalog=FestCine;Integrated Security=True;TrustServerCertificate=True"
    // o con usuario y contraseña:
    //   "Data Source=MI-PC;Initial Catalog=FestCine;User ID=usuario;Password=clave;TrustServerCertificate=True"
    public const string CadenaConexion =
        "Server=HpVictusRyzen\\SQLEXPRESS;Database=FestCine;Integrated Security=True;TrustServerCertificate=True";
}
