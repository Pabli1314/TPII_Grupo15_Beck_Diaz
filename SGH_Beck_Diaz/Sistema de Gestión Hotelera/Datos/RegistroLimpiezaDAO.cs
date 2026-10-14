using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class RegistroLimpiezaDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static void Registrar(RegistroLimpieza registro)
        {
            string query = @"
                INSERT INTO registro_limpieza (hora_inicio, hora_fin, nro_habitacion, dni_usuario)
                VALUES (@horaInicio, @horaFin, @nroHabitacion, @dniUsuario)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@horaInicio", registro.HoraInicio);
                cmd.Parameters.AddWithValue("@horaFin", registro.HoraFin);
                cmd.Parameters.AddWithValue("@nroHabitacion", registro.NroHabitacion);
                cmd.Parameters.AddWithValue("@dniUsuario", registro.DniUsuario);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Último registro de limpieza de una habitación (por fecha/hora de inicio), o null si nunca se limpió.</summary>
        public static RegistroLimpieza? ObtenerUltimaPorHabitacion(int nroHabitacion)
        {
            RegistroLimpieza? registro = null;

            string query = @"
                SELECT TOP 1 id_limpieza, fecha_limpieza, hora_inicio, hora_fin, nro_habitacion, dni_usuario
                FROM registro_limpieza
                WHERE nro_habitacion = @nroHabitacion
                ORDER BY id_limpieza DESC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nroHabitacion", nroHabitacion);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        registro = Mapear(reader);
                    }
                }
            }

            return registro;
        }

        /// <summary>Todos los registros de limpieza cuya fecha cae dentro del rango (inclusive), para reportes.</summary>
        public static List<RegistroLimpieza> ObtenerEnRango(DateTime desde, DateTime hasta)
        {
            List<RegistroLimpieza> registros = new List<RegistroLimpieza>();

            string query = @"
                SELECT id_limpieza, fecha_limpieza, hora_inicio, hora_fin, nro_habitacion, dni_usuario
                FROM registro_limpieza
                WHERE fecha_limpieza BETWEEN @desde AND @hasta
                ORDER BY fecha_limpieza";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@desde", desde.Date);
                cmd.Parameters.AddWithValue("@hasta", hasta.Date);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        registros.Add(Mapear(reader));
                    }
                }
            }

            return registros;
        }

        private static RegistroLimpieza Mapear(SqlDataReader reader) => new RegistroLimpieza
        {
            IdLimpieza = Convert.ToInt32(reader["id_limpieza"]),
            FechaLimpieza = Convert.ToDateTime(reader["fecha_limpieza"]),
            HoraInicio = (TimeSpan)reader["hora_inicio"],
            HoraFin = (TimeSpan)reader["hora_fin"],
            NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
            DniUsuario = reader["dni_usuario"]?.ToString() ?? string.Empty
        };
    }
}
