using System;
using System.Collections.Generic;
using System.Data;
using Entidades;
using Microsoft.Data.SqlClient;

namespace Datos
{
    /// <summary>
    /// DAO para la persistencia de prórrogas/extensiones de estadía en la base de datos.
    /// Garantiza la existencia de la tabla prorroga_estadia de forma transparente.
    /// </summary>
    public static class ProrrogaDAO
    {
        private static readonly Conexion _conexion = new Conexion();
        private static bool _tablaVerificada = false;

        private static void AsegurarTabla()
        {
            if (_tablaVerificada) return;

            string script = @"
                IF OBJECT_ID('prorroga_estadia', 'U') IS NULL
                BEGIN
                    CREATE TABLE prorroga_estadia (
                        id_prorroga INT IDENTITY(1,1) PRIMARY KEY,
                        id_hospedaje INT NOT NULL,
                        fecha_prorroga DATE NOT NULL DEFAULT CONVERT(DATE, GETDATE()),
                        hora_prorroga TIME NOT NULL DEFAULT CONVERT(TIME, GETDATE()),
                        fecha_salida_anterior DATE NOT NULL,
                        hora_salida_anterior TIME NOT NULL,
                        nueva_fecha_salida DATE NOT NULL,
                        nueva_hora_salida TIME NOT NULL,
                        dias_adicionales INT NOT NULL,
                        monto_adicional DECIMAL(10, 2) NOT NULL,
                        id_metodo INT NOT NULL,
                        id_turno INT NOT NULL,
                        CONSTRAINT FK_prorroga_hospedaje FOREIGN KEY (id_hospedaje) REFERENCES hospedaje(id_hospedaje),
                        CONSTRAINT FK_prorroga_metodo FOREIGN KEY (id_metodo) REFERENCES metodo_pago(id_metodo),
                        CONSTRAINT FK_prorroga_turno FOREIGN KEY (id_turno) REFERENCES Turno_caja(id_turno)
                    );
                END";

            try
            {
                using SqlConnection con = _conexion.ObtenerConexion();
                using SqlCommand cmd = new SqlCommand(script, con);
                con.Open();
                cmd.ExecuteNonQuery();
                _tablaVerificada = true;
            }
            catch
            {
                // Si la conexión o tabla no puede crearse por permisos, se reintentará en la siguiente operación
            }
        }

        /// <summary>
        /// Inserta el registro de la prórroga de estadía.
        /// </summary>
        public static void Crear(ProrrogaEstadia prorroga)
        {
            if (prorroga == null) throw new ArgumentNullException(nameof(prorroga));

            AsegurarTabla();

            string query = @"
                INSERT INTO prorroga_estadia (
                    id_hospedaje, fecha_prorroga, hora_prorroga,
                    fecha_salida_anterior, hora_salida_anterior,
                    nueva_fecha_salida, nueva_hora_salida,
                    dias_adicionales, monto_adicional, id_metodo, id_turno
                )
                VALUES (
                    @idHospedaje, @fechaProrroga, @horaProrroga,
                    @fechaAnt, @horaAnt,
                    @fechaNueva, @horaNueva,
                    @diasAdic, @montoAdic, @idMetodo, @idTurno
                )";

            using SqlConnection con = _conexion.ObtenerConexion();
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@idHospedaje", prorroga.IdHospedaje);
            cmd.Parameters.AddWithValue("@fechaProrroga", prorroga.FechaProrroga.Date);
            cmd.Parameters.AddWithValue("@horaProrroga", prorroga.HoraProrroga);
            cmd.Parameters.AddWithValue("@fechaAnt", prorroga.FechaSalidaAnterior.Date);
            cmd.Parameters.AddWithValue("@horaAnt", prorroga.HoraSalidaAnterior);
            cmd.Parameters.AddWithValue("@fechaNueva", prorroga.NuevaFechaSalida.Date);
            cmd.Parameters.AddWithValue("@horaNueva", prorroga.NuevaHoraSalida);
            cmd.Parameters.AddWithValue("@diasAdic", prorroga.DiasAdicionales);
            cmd.Parameters.AddWithValue("@montoAdic", prorroga.MontoAdicional);
            cmd.Parameters.AddWithValue("@idMetodo", prorroga.IdMetodo);
            cmd.Parameters.AddWithValue("@idTurno", prorroga.IdTurno);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Obtiene todas las prórrogas registradas uniendo con las tablas de hospedaje y método de pago.
        /// Utilizado para el historial de operaciones de recepción.
        /// </summary>
        public static List<ProrrogaEstadia> ObtenerTodas()
        {
            AsegurarTabla();

            var lista = new List<ProrrogaEstadia>();

            string query = @"
                SELECT p.id_prorroga, p.id_hospedaje, p.fecha_prorroga, p.hora_prorroga,
                       p.fecha_salida_anterior, p.hora_salida_anterior,
                       p.nueva_fecha_salida, p.nueva_hora_salida,
                       p.dias_adicionales, p.monto_adicional, p.id_metodo, p.id_turno,
                       mp.nom_metodo_pago, h.nro_habitacion, h.dni_huesped,
                       hue.nombre_huesped, hue.apellido_huesped
                FROM prorroga_estadia p
                INNER JOIN hospedaje h ON h.id_hospedaje = p.id_hospedaje
                INNER JOIN metodo_pago mp ON mp.id_metodo = p.id_metodo
                INNER JOIN Huesped hue ON hue.dni_huesped = h.dni_huesped
                ORDER BY p.fecha_prorroga DESC, p.hora_prorroga DESC";

            try
            {
                using SqlConnection con = _conexion.ObtenerConexion();
                using SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string nom = reader["nombre_huesped"] != DBNull.Value ? reader["nombre_huesped"].ToString()! : string.Empty;
                    string ape = reader["apellido_huesped"] != DBNull.Value ? reader["apellido_huesped"].ToString()! : string.Empty;

                    lista.Add(new ProrrogaEstadia
                    {
                        IdProrroga = Convert.ToInt32(reader["id_prorroga"]),
                        IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                        FechaProrroga = Convert.ToDateTime(reader["fecha_prorroga"]),
                        HoraProrroga = (TimeSpan)reader["hora_prorroga"],
                        FechaSalidaAnterior = Convert.ToDateTime(reader["fecha_salida_anterior"]),
                        HoraSalidaAnterior = (TimeSpan)reader["hora_salida_anterior"],
                        NuevaFechaSalida = Convert.ToDateTime(reader["nueva_fecha_salida"]),
                        NuevaHoraSalida = (TimeSpan)reader["nueva_hora_salida"],
                        DiasAdicionales = Convert.ToInt32(reader["dias_adicionales"]),
                        MontoAdicional = Convert.ToDecimal(reader["monto_adicional"]),
                        IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                        IdTurno = Convert.ToInt32(reader["id_turno"]),
                        MetodoPago = reader["nom_metodo_pago"] != DBNull.Value ? reader["nom_metodo_pago"].ToString() : null,
                        NroHabitacion = reader["nro_habitacion"] != DBNull.Value ? Convert.ToInt32(reader["nro_habitacion"]) : null,
                        DniHuesped = reader["dni_huesped"] != DBNull.Value ? reader["dni_huesped"].ToString() : null,
                        NombreHuesped = $"{nom} {ape}".Trim()
                    });
                }
            }
            catch
            {
                // Si la tabla aún no contiene datos o hubo un error de conexión, retorna lista vacía
            }

            return lista;
        }

        /// <summary>
        /// Obtiene las prórrogas registradas para un hospedaje específico.
        /// </summary>
        public static List<ProrrogaEstadia> ObtenerPorHospedaje(int idHospedaje)
        {
            AsegurarTabla();

            var lista = new List<ProrrogaEstadia>();

            string query = @"
                SELECT p.id_prorroga, p.id_hospedaje, p.fecha_prorroga, p.hora_prorroga,
                       p.fecha_salida_anterior, p.hora_salida_anterior,
                       p.nueva_fecha_salida, p.nueva_hora_salida,
                       p.dias_adicionales, p.monto_adicional, p.id_metodo, p.id_turno,
                       mp.nom_metodo_pago
                FROM prorroga_estadia p
                INNER JOIN metodo_pago mp ON mp.id_metodo = p.id_metodo
                WHERE p.id_hospedaje = @idHospedaje
                ORDER BY p.id_prorroga ASC";

            try
            {
                using SqlConnection con = _conexion.ObtenerConexion();
                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idHospedaje", idHospedaje);
                con.Open();
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new ProrrogaEstadia
                    {
                        IdProrroga = Convert.ToInt32(reader["id_prorroga"]),
                        IdHospedaje = Convert.ToInt32(reader["id_hospedaje"]),
                        FechaProrroga = Convert.ToDateTime(reader["fecha_prorroga"]),
                        HoraProrroga = (TimeSpan)reader["hora_prorroga"],
                        FechaSalidaAnterior = Convert.ToDateTime(reader["fecha_salida_anterior"]),
                        HoraSalidaAnterior = (TimeSpan)reader["hora_salida_anterior"],
                        NuevaFechaSalida = Convert.ToDateTime(reader["nueva_fecha_salida"]),
                        NuevaHoraSalida = (TimeSpan)reader["nueva_hora_salida"],
                        DiasAdicionales = Convert.ToInt32(reader["dias_adicionales"]),
                        MontoAdicional = Convert.ToDecimal(reader["monto_adicional"]),
                        IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                        IdTurno = Convert.ToInt32(reader["id_turno"]),
                        MetodoPago = reader["nom_metodo_pago"] != DBNull.Value ? reader["nom_metodo_pago"].ToString() : null
                    });
                }
            }
            catch
            {
                // Retorna lista vacía si falla la consulta
            }

            return lista;
        }
    }
}
