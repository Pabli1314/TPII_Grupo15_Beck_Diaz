using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class HospedajeDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        /// <summary>Hospedajes cuya fecha de entrada cae dentro del rango (inclusive), para reportes.</summary>
        public static List<Hospedaje> ObtenerEnRango(DateTime desde, DateTime hasta)
        {
            List<Hospedaje> hospedajes = new List<Hospedaje>();

            string query = @"
                SELECT id_hospedaje, fecha_entrada, hora_entrada, fecha_salida, hora_salida,
                       id_metodo, nro_habitacion, id_turno, dni_huesped
                FROM hospedaje
                WHERE fecha_entrada BETWEEN @desde AND @hasta
                ORDER BY fecha_entrada";

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
                        hospedajes.Add(new Hospedaje
                        {
                            IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                            FechaEntrada = Convert.ToDateTime(reader["fecha_entrada"]),
                            HoraEntrada = (TimeSpan)reader["hora_entrada"],
                            FechaSalida = Convert.ToDateTime(reader["fecha_salida"]),
                            HoraSalida = (TimeSpan)reader["hora_salida"],
                            IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                            NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
                            IdTurno = Convert.ToInt32(reader["id_turno"]),
                            DniHuesped = reader["dni_huesped"].ToString() ?? string.Empty
                        });
                    }
                }
            }

            return hospedajes;
        }

        /// <summary>Inserta el hospedaje. fecha_entrada/hora_entrada quedan a cargo del DEFAULT de la base.</summary>
        public static void Crear(Hospedaje hospedaje)
        {
            string query = @"
                INSERT INTO hospedaje (fecha_salida, hora_salida, id_metodo, nro_habitacion, id_turno, dni_huesped)
                VALUES (@fechaSalida, @horaSalida, @idMetodo, @nroHabitacion, @idTurno, @dniHuesped)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@fechaSalida", hospedaje.FechaSalida.Date);
                cmd.Parameters.AddWithValue("@horaSalida", hospedaje.HoraSalida);
                cmd.Parameters.AddWithValue("@idMetodo", hospedaje.IdMetodo);
                cmd.Parameters.AddWithValue("@nroHabitacion", hospedaje.NroHabitacion);
                cmd.Parameters.AddWithValue("@idTurno", hospedaje.IdTurno);
                cmd.Parameters.AddWithValue("@dniHuesped", hospedaje.DniHuesped);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Devuelve el hospedaje más reciente de la habitación (no hay una columna de "activo";
        /// se asume que mientras la habitación esté Ocupada, el último hospedaje cargado es el vigente).
        /// </summary>
        public static Hospedaje? ObtenerActivoPorHabitacion(int nroHabitacion)
        {
            Hospedaje? hospedaje = null;

            string query = @"
                SELECT TOP 1 id_hospedaje, fecha_entrada, hora_entrada, fecha_salida, hora_salida,
                       id_metodo, nro_habitacion, id_turno, dni_huesped
                FROM hospedaje
                WHERE nro_habitacion = @nroHabitacion
                ORDER BY id_hospedaje DESC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nroHabitacion", nroHabitacion);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        hospedaje = new Hospedaje
                        {
                            IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                            FechaEntrada = Convert.ToDateTime(reader["fecha_entrada"]),
                            HoraEntrada = (TimeSpan)reader["hora_entrada"],
                            FechaSalida = Convert.ToDateTime(reader["fecha_salida"]),
                            HoraSalida = (TimeSpan)reader["hora_salida"],
                            IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                            NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
                            IdTurno = Convert.ToInt32(reader["id_turno"]),
                            DniHuesped = reader["dni_huesped"].ToString() ?? string.Empty
                        };
                    }
                }
            }

            return hospedaje;
        }

        /// <summary>Hospedajes cargados durante un turno de caja, con la tarifa de la habitación
        /// (para calcular el cobro de alojamiento por método de pago en el resumen de turno).</summary>
        public static List<(Hospedaje Hospedaje, decimal TarifaBase)> ObtenerPorTurno(int idTurno)
        {
            var resultado = new List<(Hospedaje, decimal)>();

            string query = @"
                SELECT h.id_hospedaje, h.fecha_entrada, h.hora_entrada, h.fecha_salida, h.hora_salida,
                       h.id_metodo, h.nro_habitacion, h.id_turno, h.dni_huesped, hab.tarifa_base
                FROM hospedaje h
                INNER JOIN habitacion hab ON hab.nro_habitacion = h.nro_habitacion
                WHERE h.id_turno = @idTurno";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idTurno", idTurno);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Hospedaje hospedaje = new Hospedaje
                        {
                            IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                            FechaEntrada = Convert.ToDateTime(reader["fecha_entrada"]),
                            HoraEntrada = (TimeSpan)reader["hora_entrada"],
                            FechaSalida = Convert.ToDateTime(reader["fecha_salida"]),
                            HoraSalida = (TimeSpan)reader["hora_salida"],
                            IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                            NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
                            IdTurno = Convert.ToInt32(reader["id_turno"]),
                            DniHuesped = reader["dni_huesped"].ToString() ?? string.Empty
                        };

                        resultado.Add((hospedaje, Convert.ToDecimal(reader["tarifa_base"])));
                    }
                }
            }

            return resultado;
        }

        /// <summary>Busca hospedajes con el nombre del huésped ya resuelto, para la pantalla de Reservas.
        /// Todos los filtros son opcionales y combinables.</summary>
        public static List<HospedajeDetalle> BuscarDetalle(string? dni = null, string? nombre = null, int? nroHabitacion = null, DateTime? fecha = null)
        {
            var resultado = new List<HospedajeDetalle>();

            string query = @"
                SELECT h.id_hospedaje, h.id_turno, h.dni_huesped, hu.nombre_huesped, hu.apellido_huesped, h.nro_habitacion,
                       h.fecha_entrada, h.hora_entrada, h.fecha_salida, h.hora_salida
                FROM hospedaje h
                INNER JOIN Huesped hu ON hu.dni_huesped = h.dni_huesped
                WHERE (@dni IS NULL OR h.dni_huesped LIKE @dni)
                  AND (@nombre IS NULL OR hu.nombre_huesped LIKE @nombre OR hu.apellido_huesped LIKE @nombre)
                  AND (@nroHabitacion IS NULL OR h.nro_habitacion = @nroHabitacion)
                  AND (@fecha IS NULL OR h.fecha_entrada = @fecha)
                ORDER BY h.fecha_entrada DESC, h.hora_entrada DESC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.Add("@dni", System.Data.SqlDbType.VarChar).Value = (object?)(dni != null ? $"%{dni}%" : null) ?? DBNull.Value;
                cmd.Parameters.Add("@nombre", System.Data.SqlDbType.VarChar).Value = (object?)(nombre != null ? $"%{nombre}%" : null) ?? DBNull.Value;
                cmd.Parameters.Add("@nroHabitacion", System.Data.SqlDbType.Int).Value = (object?)nroHabitacion ?? DBNull.Value;
                cmd.Parameters.Add("@fecha", System.Data.SqlDbType.Date).Value = (object?)fecha?.Date ?? DBNull.Value;

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultado.Add(new HospedajeDetalle
                        {
                            IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                            IdTurno = Convert.ToInt32(reader["id_turno"]),
                            DniHuesped = reader["dni_huesped"].ToString() ?? string.Empty,
                            NombreHuesped = $"{reader["nombre_huesped"]} {reader["apellido_huesped"]}".Trim(),
                            NroHabitacion = Convert.ToInt32(reader["nro_habitacion"]),
                            FechaEntrada = Convert.ToDateTime(reader["fecha_entrada"]),
                            HoraEntrada = (TimeSpan)reader["hora_entrada"],
                            FechaSalida = Convert.ToDateTime(reader["fecha_salida"]),
                            HoraSalida = (TimeSpan)reader["hora_salida"]
                        });
                    }
                }
            }

            return resultado;
        }

        /// <summary>Registra la salida real del huésped (check-out), pisando la fecha/hora planificadas al check-in.</summary>
        public static void RegistrarSalida(int idHospedaje, DateTime fechaSalida, TimeSpan horaSalida)
        {
            string query = @"
                UPDATE hospedaje
                SET fecha_salida = @fechaSalida, hora_salida = @horaSalida
                WHERE id_hospedaje = @idHospedaje";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@fechaSalida", fechaSalida.Date);
                cmd.Parameters.AddWithValue("@horaSalida", horaSalida);
                cmd.Parameters.AddWithValue("@idHospedaje", idHospedaje);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
