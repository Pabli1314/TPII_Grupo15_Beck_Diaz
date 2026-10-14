using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class EstadoHabitacionDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<EstadoHabitacion> ObtenerTodos()
        {
            var estados = new List<EstadoHabitacion>();

            string query = "SELECT id_estado, nom_estado_habitacion FROM Estado_habitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        estados.Add(new EstadoHabitacion
                        {
                            IdEstado = Convert.ToInt32(reader["id_estado"]),
                            NomEstadoHabitacion = reader["nom_estado_habitacion"].ToString() ?? string.Empty
                        });
                    }
                }
            }

            return estados;
        }
    }
}
