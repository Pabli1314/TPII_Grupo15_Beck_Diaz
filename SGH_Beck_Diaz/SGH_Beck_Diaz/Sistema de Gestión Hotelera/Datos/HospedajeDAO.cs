using Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

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
        /// Devuelve el hospedaje más reciente de la habitación (se asume que mientras la habitación esté Ocupada, el último hospedaje es el vigente).
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

        /// <summary>Hospedajes cargados durante un turno de caja, con la tarifa de la habitación.</summary>
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

        /// <summary>Busca hospedajes con filtros opcionales y combinables.</summary>
        public static List<Hospedaje> BuscarDetalle(string? dni = null, string? nombre = null, int? nroHabitacion = null, DateTime? fecha = null, string? termino = null)
        {
            var resultado = new List<Hospedaje>();

            dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim();
            nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            termino = string.IsNullOrWhiteSpace(termino) ? null : termino.Trim();

            if (termino == null && dni != null && dni == nombre)
            {
                termino = dni;
                dni = null;
                nombre = null;
            }

            var queryBuilder = new StringBuilder(@"
                SELECT 
                    h.id_hospedaje,
                    h.fecha_entrada,
                    h.hora_entrada,
                    h.fecha_salida,
                    h.hora_salida,
                    h.id_metodo,
                    h.nro_habitacion,
                    h.id_turno,
                    h.dni_huesped
                FROM hospedaje h
                INNER JOIN Huesped hue ON h.dni_huesped = hue.dni_huesped
                WHERE 1=1 ");

            using (var con = _conexion.ObtenerConexion())
            using (var cmd = new SqlCommand())
            {
                if (dni != null)
                {
                    queryBuilder.Append(" AND h.dni_huesped = @dni ");
                    cmd.Parameters.Add("@dni", SqlDbType.VarChar, 50).Value = dni;
                }

                if (nombre != null)
                {
                    queryBuilder.Append(" AND (hue.nombre_huesped LIKE @nombre OR hue.apellido_huesped LIKE @nombre) ");
                    cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = $"%{nombre}%";
                }

                if (nroHabitacion.HasValue)
                {
                    queryBuilder.Append(" AND h.nro_habitacion = @nroHabitacion ");
                    cmd.Parameters.Add("@nroHabitacion", SqlDbType.Int).Value = nroHabitacion.Value;
                }

                if (fecha.HasValue)
                {
                    queryBuilder.Append(" AND (h.fecha_entrada = @fecha OR h.fecha_salida = @fecha) ");
                    cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = fecha.Value.Date;
                }

                if (termino != null)
                {
                    queryBuilder.Append(@" AND (
                        h.dni_huesped LIKE @termino OR 
                        hue.nombre_huesped LIKE @termino OR 
                        hue.apellido_huesped LIKE @termino OR 
                        CAST(h.nro_habitacion AS VARCHAR) LIKE @termino
                    ) ");
                    cmd.Parameters.Add("@termino", SqlDbType.VarChar, 100).Value = $"%{termino}%";
                }

                queryBuilder.Append(" ORDER BY h.fecha_entrada DESC, h.hora_entrada DESC");

                cmd.CommandText = queryBuilder.ToString();
                cmd.Connection = con;

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var hospedaje = new Hospedaje
                        {
                            IdHospedaje = reader["id_hospedaje"] != DBNull.Value ? Convert.ToInt32(reader["id_hospedaje"]) : 0,
                            FechaEntrada = reader["fecha_entrada"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_entrada"]) : DateTime.MinValue,
                            HoraEntrada = reader["hora_entrada"] != DBNull.Value ? (TimeSpan)reader["hora_entrada"] : TimeSpan.Zero,
                            FechaSalida = reader["fecha_salida"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_salida"]) : DateTime.MinValue,
                            HoraSalida = reader["hora_salida"] != DBNull.Value ? (TimeSpan)reader["hora_salida"] : TimeSpan.Zero,
                            IdMetodo = reader["id_metodo"] != DBNull.Value ? Convert.ToInt32(reader["id_metodo"]) : 0,
                            NroHabitacion = reader["nro_habitacion"] != DBNull.Value ? Convert.ToInt32(reader["nro_habitacion"]) : 0,
                            IdTurno = reader["id_turno"] != DBNull.Value ? Convert.ToInt32(reader["id_turno"]) : 0,
                            DniHuesped = reader["dni_huesped"] != DBNull.Value ? reader["dni_huesped"].ToString()! : string.Empty
                        };

                        resultado.Add(hospedaje);
                    }
                }
            }

            return resultado;
        }

        /// <summary>Registra la salida real del huésped (check-out).</summary>
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