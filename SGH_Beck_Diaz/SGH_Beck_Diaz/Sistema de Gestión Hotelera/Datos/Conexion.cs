using System;
using Microsoft.Data.SqlClient;

namespace Datos
{
    public class Conexion
    {
        private string stringConexion = "Server=(localdb)\\MSSQLLocalDB;Database=beck_diaz_db;Integrated Security=True;TrustServerCertificate=True;";

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(stringConexion);
        }

        // M�todo para probar la conexi�n desde WinForms
        public bool ProbarConexion()
        {
            using (SqlConnection con = ObtenerConexion())
            {
                con.Open(); // Si falla, lanza SqlException
                return true;
            }
        }
    }
}
