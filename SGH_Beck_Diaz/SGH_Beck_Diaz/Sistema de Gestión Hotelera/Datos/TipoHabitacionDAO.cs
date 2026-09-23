using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class TipoHabitacionDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<TipoHabitacion> ObtenerTodos()
        {
            var tipos = new List<TipoHabitacion>();

            string query = "SELECT id_tipo_habitacion, descripcion FROM Tipo_habitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tipos.Add(new TipoHabitacion
                        {
                            IdTipoHabitacion = Convert.ToInt32(reader["id_tipo_habitacion"]),
                            Descripcion = reader["descripcion"].ToString() ?? string.Empty
                        });
                    }
                }
            }

            return tipos;
        }
    }
}
