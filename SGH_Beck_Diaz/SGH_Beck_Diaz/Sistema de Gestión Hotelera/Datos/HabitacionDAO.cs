using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class HabitacionDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<Habitacion> ObtenerTodas()
        {
            var habitaciones = new List<Habitacion>();

            string query = @"
                SELECT nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado
                FROM habitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        habitaciones.Add(MapearHabitacion(reader));
                    }
                }
            }

            return habitaciones;
        }

        public static Habitacion? ObtenerPorNumero(int nroHabitacion)
        {
            Habitacion? habitacion = null;

            string query = @"
                SELECT nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado
                FROM habitacion
                WHERE nro_habitacion = @nroHabitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nroHabitacion", nroHabitacion);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        habitacion = MapearHabitacion(reader);
                    }
                }
            }

            return habitacion;
        }

        public static void Crear(Habitacion habitacion)
        {
            string query = @"
                INSERT INTO habitacion (nro_habitacion, piso, cant_camas, tarifa_base, id_tipo_habitacion, id_estado)
                VALUES (@nroHabitacion, @piso, @cantCamas, @tarifaBase, @idTipoHabitacion, @idEstado)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nroHabitacion", habitacion.NroHabitacion);
                cmd.Parameters.AddWithValue("@piso", habitacion.Piso);
                cmd.Parameters.AddWithValue("@cantCamas", habitacion.CantCamas);
                cmd.Parameters.AddWithValue("@tarifaBase", habitacion.TarifaBase);
                cmd.Parameters.AddWithValue("@idTipoHabitacion", habitacion.IdTipoHabitacion);
                cmd.Parameters.AddWithValue("@idEstado", habitacion.IdEstado);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void ActualizarEstado(int nroHabitacion, int idEstado)
        {
            string query = @"
                UPDATE habitacion
                SET id_estado = @idEstado
                WHERE nro_habitacion = @nroHabitacion";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idEstado", idEstado);
                cmd.Parameters.AddWithValue("@nroHabitacion", nroHabitacion);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static Habitacion MapearHabitacion(SqlDataReader reader)
        {
            return new Habitacion
            {
                NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
                Piso = Convert.ToInt32(reader["piso"]),
                CantCamas = Convert.ToInt32(reader["cant_camas"]),
                TarifaBase = Convert.ToDecimal(reader["tarifa_base"]),
                IdTipoHabitacion = Convert.ToInt32(reader["id_tipo_habitacion"]),
                IdEstado = Convert.ToInt32(reader["id_estado"])
            };
        }
    }
}
