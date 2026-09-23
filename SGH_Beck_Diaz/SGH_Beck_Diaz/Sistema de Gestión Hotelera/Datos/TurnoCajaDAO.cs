using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class TurnoCajaDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        /// <summary>
        /// Obtiene un turno específico por su identificador primario.
        /// Requerido por GestionTurnoCaja.ObtenerResumen.
        /// </summary>
        public static TurnoCaja? ObtenerPorId(int idTurno)
        {
            if (idTurno <= 0)
                return null;

            TurnoCaja? turno = null;

            string query = @"
                SELECT id_turno, fecha_apertura, hora_apertura, fecha_cierre, hora_cierre,
                       monto_inicial, monto_final, observaciones, dni_usuario
                FROM Turno_caja
                WHERE id_turno = @idTurno";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@idTurno", SqlDbType.Int).Value = idTurno;

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            turno = MapearTurno(reader);
                        }
                    }
                }
            }

            return turno;
        }

        /// <summary>
        /// Un turno está abierto mientras no tenga fecha de cierre.
        /// </summary>
        public static TurnoCaja? ObtenerAbierto(string dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                return null;

            TurnoCaja? turno = null;

            string query = @"
                SELECT id_turno, fecha_apertura, hora_apertura, fecha_cierre, hora_cierre,
                       monto_inicial, monto_final, observaciones, dni_usuario
                FROM Turno_caja
                WHERE dni_usuario = @dniUsuario AND fecha_cierre IS NULL
                ORDER BY id_turno DESC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniUsuario.Trim();

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            turno = MapearTurno(reader);
                        }
                    }
                }
            }

            return turno;
        }

        public static TurnoCaja Abrir(string dniUsuario, decimal montoInicial)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                throw new ArgumentException("El DNI del usuario no puede estar vacío al abrir un turno.", nameof(dniUsuario));

            string query = @"
                INSERT INTO Turno_caja (fecha_apertura, hora_apertura, monto_inicial, dni_usuario)
                OUTPUT INSERTED.id_turno, INSERTED.fecha_apertura, INSERTED.hora_apertura, INSERTED.dni_usuario
                VALUES (CONVERT(date, GETDATE()), CONVERT(time, GETDATE()), @montoInicial, @dniUsuario)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@montoInicial", SqlDbType.Decimal).Value = montoInicial;
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniUsuario.Trim();

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        return new TurnoCaja
                        {
                            IdTurno = Convert.ToInt32(reader["id_turno"]),
                            FechaApertura = Convert.ToDateTime(reader["fecha_apertura"]),
                            HoraApertura = (TimeSpan)reader["hora_apertura"],
                            MontoInicial = montoInicial,
                            DniUsuario = reader["dni_usuario"]?.ToString() ?? string.Empty
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Registra la fecha, hora de cierre, el monto final contabilizado y las observaciones.
        /// Requerido por GestionTurnoCaja.CerrarTurno.
        /// </summary>
        public static void Cerrar(int idTurno, DateTime fechaCierre, TimeSpan horaCierre, decimal montoFinal, string observaciones)
        {
            if (idTurno <= 0)
                throw new ArgumentException("El ID del turno debe ser un valor válido.", nameof(idTurno));

            string query = @"
                UPDATE Turno_caja
                SET fecha_cierre = @fechaCierre,
                    hora_cierre = @horaCierre,
                    monto_final = @montoFinal,
                    observaciones = @observaciones
                WHERE id_turno = @idTurno";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@fechaCierre", SqlDbType.Date).Value = fechaCierre.Date;
                    cmd.Parameters.Add("@horaCierre", SqlDbType.Time).Value = horaCierre;
                    cmd.Parameters.Add("@montoFinal", SqlDbType.Decimal).Value = montoFinal;
                    cmd.Parameters.Add("@observaciones", SqlDbType.VarChar, 255).Value = (object?)observaciones ?? DBNull.Value;
                    cmd.Parameters.Add("@idTurno", SqlDbType.Int).Value = idTurno;

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Obtiene todos los turnos de caja registrados para el análisis financiero.
        /// </summary>
        public static List<TurnoCaja> ObtenerTodos()
        {
            List<TurnoCaja> lista = new List<TurnoCaja>();

            string query = @"
                SELECT id_turno, fecha_apertura, hora_apertura, fecha_cierre, hora_cierre,
                       monto_inicial, monto_final, observaciones, dni_usuario
                FROM Turno_caja
                ORDER BY id_turno DESC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearTurno(reader));
                        }
                    }
                }
            }

            return lista;
        }

        public static Dictionary<string, decimal> ObtenerIngresosPorMes()
        {
            var resultado = new Dictionary<string, decimal>();

            string query = @"
                SELECT 
                    FORMAT(fecha_cierre, 'yyyy-MM') AS Mes,
                    SUM(monto_final) AS TotalRecaudado
                FROM Turno_caja
                WHERE fecha_cierre IS NOT NULL AND monto_final IS NOT NULL
                GROUP BY FORMAT(fecha_cierre, 'yyyy-MM')
                ORDER BY Mes ASC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string mes = reader["Mes"]?.ToString() ?? string.Empty;
                            decimal total = Convert.ToDecimal(reader["TotalRecaudado"]);
                            if (!string.IsNullOrEmpty(mes))
                            {
                                resultado.Add(mes, total);
                            }
                        }
                    }
                }
            }

            return resultado;
        }

        public static List<TurnoCaja> ObtenerHistorialFiltrado(DateTime fechaDesde, DateTime fechaHasta, string? dniUsuario = null)
        {
            List<TurnoCaja> lista = new List<TurnoCaja>();
            string? dniFiltro = string.IsNullOrWhiteSpace(dniUsuario) ? null : dniUsuario.Trim();

            string query = @"
                SELECT id_turno, fecha_apertura, hora_apertura, fecha_cierre, hora_cierre,
                       monto_inicial, monto_final, observaciones, dni_usuario
                FROM Turno_caja
                WHERE fecha_apertura BETWEEN @fechaDesde AND @fechaHasta";

            if (dniFiltro != null)
            {
                query += " AND dni_usuario = @dniUsuario";
            }

            query += " ORDER BY id_turno DESC";

            DateTime desde = fechaDesde.Date;
            DateTime hasta = fechaHasta.Date.AddDays(1).AddTicks(-1);

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@fechaDesde", SqlDbType.DateTime).Value = desde;
                    cmd.Parameters.Add("@fechaHasta", SqlDbType.DateTime).Value = hasta;

                    if (dniFiltro != null)
                    {
                        cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniFiltro;
                    }

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearTurno(reader));
                        }
                    }
                }
            }

            return lista;
        }

        private static TurnoCaja MapearTurno(SqlDataReader reader)
        {
            return new TurnoCaja
            {
                IdTurno = Convert.ToInt32(reader["id_turno"]),
                FechaApertura = Convert.ToDateTime(reader["fecha_apertura"]),
                HoraApertura = (TimeSpan)reader["hora_apertura"],
                FechaCierre = reader["fecha_cierre"] is DBNull ? null : Convert.ToDateTime(reader["fecha_cierre"]),
                HoraCierre = reader["hora_cierre"] is DBNull ? null : (TimeSpan)reader["hora_cierre"],
                MontoInicial = Convert.ToDecimal(reader["monto_inicial"]),
                MontoFinal = reader["monto_final"] is DBNull ? null : Convert.ToDecimal(reader["monto_final"]),
                Observaciones = reader["observaciones"] is DBNull ? null : reader["observaciones"].ToString(),
                DniUsuario = reader["dni_usuario"] is DBNull ? string.Empty : reader["dni_usuario"].ToString()!
            };
        }
    }
}