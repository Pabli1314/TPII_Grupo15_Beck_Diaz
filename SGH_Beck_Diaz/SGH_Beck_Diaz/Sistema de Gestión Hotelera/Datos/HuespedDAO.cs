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

        /// <summary>Búsqueda libre por DNI, nombre, apellido o correo (coincidencia parcial).</summary>
        public static List<Huesped> Buscar(string termino)
        {
            var huespedes = new List<Huesped>();

            string query = $@"
                SELECT {Columnas}
                FROM Huesped
                WHERE dni_huesped LIKE @termino OR nombre_huesped LIKE @termino
                   OR apellido_huesped LIKE @termino OR correo_huesped LIKE @termino
                ORDER BY apellido_huesped, nombre_huesped";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@termino", $"%{termino}%");

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

        /// <summary>True si otro huésped (distinto de <paramref name="dniExcluido"/>) ya usa ese teléfono (UQ_huesped_telefono).</summary>
        public static bool ExisteTelefono(string telefono, string? dniExcluido = null)
        {
            return ExisteValor("telefono_huesped", telefono, dniExcluido);
        }

        /// <summary>True si otro huésped (distinto de <paramref name="dniExcluido"/>) ya usa ese correo (UQ_huesped_correo).</summary>
        public static bool ExisteCorreo(string correo, string? dniExcluido = null)
        {
            return ExisteValor("correo_huesped", correo, dniExcluido);
        }

        private static bool ExisteValor(string columna, string valor, string? dniExcluido)
        {
            // "columna" nunca viene del usuario: solo la pasan ExisteTelefono/ExisteCorreo.
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

        /// <summary>Inserta el huésped. alta_huesped queda a cargo del DEFAULT de la base.</summary>
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

        /// <summary>Actualiza nombre/apellido/teléfono/dirección/correo de un huésped ya existente (el DNI no cambia).</summary>
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
