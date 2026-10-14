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

            string query = "SELECT id_tipo_habitacion, descripcion, tarifa FROM Tipo_habitacion";

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
                            Descripcion = reader["descripcion"].ToString() ?? string.Empty,
                            Tarifa = Convert.ToDecimal(reader["tarifa"])
                        });
                    }
                }
            }

            return tipos;
        }

        /// <summary>Actualiza la tarifa de un tipo de habitación específico.</summary>
        public static void ActualizarTarifa(int idTipoHabitacion, decimal nuevaTarifa)
        {
            string query = @"
                UPDATE Tipo_habitacion
                SET tarifa = @tarifa
                WHERE id_tipo_habitacion = @idTipoHabitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@tarifa", nuevaTarifa);
                cmd.Parameters.AddWithValue("@idTipoHabitacion", idTipoHabitacion);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
