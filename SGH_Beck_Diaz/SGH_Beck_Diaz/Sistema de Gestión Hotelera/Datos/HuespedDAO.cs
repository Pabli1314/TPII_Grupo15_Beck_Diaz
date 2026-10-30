using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class HuespedDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        private const string Columnas = @"dni_huesped, nombre_huesped, apellido_huesped, telefono_huesped,
                               direccion_huesped, correo_huesped, alta_huesped";

        public static List<Huesped> ObtenerTodos()
        {
            var huespedes = new List<Huesped>();

            string query = $"SELECT {Columnas} FROM Huesped ORDER BY apellido_huesped, nombre_huesped";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        huespedes.Add(MapearHuesped(reader));
                    }
                }
            }

            return huespedes;
        }

        public static List<Huesped> Buscar(string termino)
        {
            return BuscarFiltrado(termino, "Todos", null);
        }

        /// <summary>
        /// Búsqueda avanzada de huéspedes con filtros combinados por término, estado de estadía y habitación.
        /// </summary>
        public static List<Huesped> BuscarFiltrado(string? termino, string? estadoEstadia, int? nroHabitacion)
        {
            var huespedes = new List<Huesped>();

            string query = $@"
                SELECT DISTINCT h.dni_huesped, h.nombre_huesped, h.apellido_huesped, h.telefono_huesped,
                                h.direccion_huesped, h.correo_huesped, h.alta_huesped
                FROM Huesped h
                LEFT JOIN hospedaje ho ON h.dni_huesped = ho.dni_huesped
                LEFT JOIN habitacion ha ON ho.nro_habitacion = ha.nro_habitacion
                LEFT JOIN Estado_habitacion eh ON ha.id_estado = eh.id_estado
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(termino))
            {
                query += @" AND (h.dni_huesped LIKE @termino 
                             OR h.nombre_huesped LIKE @termino 
                             OR h.apellido_huesped LIKE @termino 
                             OR h.correo_huesped LIKE @termino)";
            }

            if (!string.IsNullOrWhiteSpace(estadoEstadia) && estadoEstadia != "Todos")
            {
                if (estadoEstadia.Equals("Alojado", StringComparison.OrdinalIgnoreCase))
                {
                    query += " AND LOWER(TRIM(eh.nom_estado_habitacion)) = 'ocupada'";
                }
                else if (estadoEstadia.Equals("Sin estadía activa", StringComparison.OrdinalIgnoreCase))
                {
                    query += @" AND h.dni_huesped NOT IN (
                                    SELECT ho2.dni_huesped 
                                    FROM hospedaje ho2
                                    INNER JOIN habitacion ha2 ON ho2.nro_habitacion = ha2.nro_habitacion
                                    INNER JOIN Estado_habitacion eh2 ON ha2.id_estado = eh2.id_estado
                                    WHERE LOWER(TRIM(eh2.nom_estado_habitacion)) = 'ocupada'
                                )";
                }
            }

            if (nroHabitacion.HasValue && nroHabitacion.Value > 0)
            {
                query += " AND ho.nro_habitacion = @nroHabitacion AND LOWER(TRIM(eh.nom_estado_habitacion)) = 'ocupada'";
            }

            query += " ORDER BY h.apellido_huesped, h.nombre_huesped";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(termino))
                {
                    cmd.Parameters.AddWithValue("@termino", $"%{termino.Trim()}%");
                }

                if (nroHabitacion.HasValue && nroHabitacion.Value > 0)
                {
                    cmd.Parameters.AddWithValue("@nroHabitacion", nroHabitacion.Value);
                }

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        huespedes.Add(MapearHuesped(reader));
                    }
                }
            }

            return huespedes;
        }

        public static Huesped? ObtenerPorDni(string dniHuesped)
        {
            Huesped? huesped = null;

            string query = $@"
                SELECT {Columnas}
                FROM Huesped
                WHERE dni_huesped = @dniHuesped";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@dniHuesped", dniHuesped);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        huesped = MapearHuesped(reader);
                    }
                }
            }

            return huesped;
        }

        public static bool ExisteTelefono(string telefono, string? dniExcluido = null)
        {
            return ExisteValor("telefono_huesped", telefono, dniExcluido);
        }

        public static bool ExisteCorreo(string correo, string? dniExcluido = null)
        {
            return ExisteValor("correo_huesped", correo, dniExcluido);
        }

        private static bool ExisteValor(string columna, string valor, string? dniExcluido)
        {
            string query = $@"
                SELECT COUNT(1)
                FROM Huesped
                WHERE {columna} = @valor AND (@dniExcluido IS NULL OR dni_huesped <> @dniExcluido)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@valor", valor);
                cmd.Parameters.Add("@dniExcluido", System.Data.SqlDbType.VarChar, Huesped.LargoDni).Value = (object?)dniExcluido ?? DBNull.Value;

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public static void Crear(Huesped huesped)
        {
            string query = @"
                INSERT INTO Huesped (dni_huesped, nombre_huesped, apellido_huesped, telefono_huesped, direccion_huesped, correo_huesped)
                VALUES (@dni, @nombre, @apellido, @telefono, @direccion, @correo)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                AgregarParametros(cmd, huesped);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void Actualizar(Huesped huesped)
        {
            string query = @"
                UPDATE Huesped
                SET nombre_huesped = @nombre, apellido_huesped = @apellido, telefono_huesped = @telefono,
                    direccion_huesped = @direccion, correo_huesped = @correo
                WHERE dni_huesped = @dni";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                AgregarParametros(cmd, huesped);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static void AgregarParametros(SqlCommand cmd, Huesped huesped)
        {
            cmd.Parameters.AddWithValue("@dni", huesped.DniHuesped);
            cmd.Parameters.AddWithValue("@nombre", huesped.Nombre);
            cmd.Parameters.AddWithValue("@apellido", huesped.Apellido);
            cmd.Parameters.AddWithValue("@telefono", huesped.Telefono);
            cmd.Parameters.AddWithValue("@direccion", huesped.Direccion);
            cmd.Parameters.AddWithValue("@correo", huesped.Correo);
        }

        public static List<Huesped> ObtenerDisponiblesParaCheckIn()
        {
            var huespedes = new List<Huesped>();

            string query = @"
        SELECT 
            h.dni_huesped, 
            h.nombre_huesped, 
            h.apellido_huesped, 
            h.telefono_huesped,
            h.direccion_huesped, 
            h.correo_huesped, 
            h.alta_huesped
        FROM Huesped h
        WHERE h.dni_huesped NOT IN (
            SELECT ho.dni_huesped 
            FROM hospedaje ho
            INNER JOIN habitacion ha ON ho.nro_habitacion = ha.nro_habitacion
            INNER JOIN Estado_habitacion eh ON ha.id_estado = eh.id_estado
            WHERE LOWER(TRIM(eh.nom_estado_habitacion)) = 'ocupada'
        )
        ORDER BY h.apellido_huesped, h.nombre_huesped";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        huespedes.Add(new Huesped
                        {
                            DniHuesped = reader["dni_huesped"] != DBNull.Value ? reader["dni_huesped"].ToString()! : string.Empty,
                            Nombre = reader["nombre_huesped"] != DBNull.Value ? reader["nombre_huesped"].ToString()! : string.Empty,
                            Apellido = reader["apellido_huesped"] != DBNull.Value ? reader["apellido_huesped"].ToString()! : string.Empty,
                            Telefono = reader["telefono_huesped"] != DBNull.Value ? reader["telefono_huesped"].ToString()! : string.Empty,
                            Direccion = reader["direccion_huesped"] != DBNull.Value ? reader["direccion_huesped"].ToString()! : string.Empty,
                            Correo = reader["correo_huesped"] != DBNull.Value ? reader["correo_huesped"].ToString()! : string.Empty,
                            AltaHuesped = reader["alta_huesped"] != DBNull.Value ? Convert.ToDateTime(reader["alta_huesped"]) : null
                        });
                    }
                }
            }

            return huespedes;
        }

        private static Huesped MapearHuesped(SqlDataReader reader)
        {
            return new Huesped
            {
                DniHuesped = reader["dni_huesped"].ToString() ?? string.Empty,
                Nombre = reader["nombre_huesped"].ToString() ?? string.Empty,
                Apellido = reader["apellido_huesped"].ToString() ?? string.Empty,
                Telefono = reader["telefono_huesped"].ToString() ?? string.Empty,
                Direccion = reader["direccion_huesped"].ToString() ?? string.Empty,
                Correo = reader["correo_huesped"].ToString() ?? string.Empty,
                AltaHuesped = reader["alta_huesped"] is DBNull ? null : Convert.ToDateTime(reader["alta_huesped"])
            };
        }
    }
}