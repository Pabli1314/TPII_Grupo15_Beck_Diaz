using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class UsuarioDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static Usuario? ObtenerPorUsername(string nomUsuario)
        {
            if (string.IsNullOrWhiteSpace(nomUsuario))
                return null;

            Usuario? usuario = null;

            string query = @"
                SELECT u.dni_usuario, u.nom_usuario, u.ape_usuario, u.direccion, 
                       u.telefono_usuario, u.correo_usuario, u.pasword, u.estado, 
                       u.id_rol, u.alta_usuario, r.nom_rol 
                FROM Usuario u 
                INNER JOIN Rol r ON u.id_rol = r.id_rol 
                WHERE u.nom_usuario = @nomUsuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("@nomUsuario", SqlDbType.VarChar, 20).Value = nomUsuario.Trim();

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = MapearUsuario(reader);
                        }
                    }
                }
            }

            return usuario;
        }

        public static Usuario? ObtenerPorDni(string dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                return null;

            Usuario? usuario = null;

            string query = @"
                SELECT u.dni_usuario, u.nom_usuario, u.ape_usuario, u.direccion, 
                       u.telefono_usuario, u.correo_usuario, u.pasword, u.estado, 
                       u.id_rol, u.alta_usuario, r.nom_rol 
                FROM Usuario u 
                INNER JOIN Rol r ON u.id_rol = r.id_rol 
                WHERE u.dni_usuario = @dniUsuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniUsuario.Trim();

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = MapearUsuario(reader);
                        }
                    }
                }
            }

            return usuario;
        }

        public static List<Usuario> ObtenerTodos()
        {
            List<Usuario> listaUsuarios = new List<Usuario>();

            string query = @"
                SELECT u.dni_usuario, u.nom_usuario, u.ape_usuario, u.direccion, 
                       u.telefono_usuario, u.correo_usuario, u.pasword, u.estado, 
                       u.id_rol, u.alta_usuario, r.nom_rol 
                FROM Usuario u 
                INNER JOIN Rol r ON u.id_rol = r.id_rol 
                ORDER BY u.ape_usuario, u.nom_usuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaUsuarios.Add(MapearUsuario(reader));
                        }
                    }
                }
            }

            return listaUsuarios;
        }

        public static List<KeyValuePair<string, string>> ObtenerRecepcionista()
        {
            var lista = new List<KeyValuePair<string, string>>();

            string query = @"
                SELECT dni_usuario, CONCAT(ape_usuario, ' ', nom_usuario) AS nombre_completo 
                FROM Usuario 
                WHERE estado = 1 
                ORDER BY ape_usuario, nom_usuario ASC";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dni = reader["dni_usuario"]?.ToString() ?? string.Empty;
                            string nombreCompleto = reader["nombre_completo"]?.ToString() ?? string.Empty;

                            if (!string.IsNullOrEmpty(dni))
                            {
                                lista.Add(new KeyValuePair<string, string>(dni, nombreCompleto));
                            }
                        }
                    }
                }
            }
            return lista;
        }

        public static bool ExisteNombreUsuario(string nomUsuario, string dniExcluir = "")
        {
            if (string.IsNullOrWhiteSpace(nomUsuario)) return false;

            string query = @"
                SELECT COUNT(1) 
                FROM Usuario 
                WHERE nom_usuario = @nomUsuario AND dni_usuario <> @dniExcluir";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@nomUsuario", SqlDbType.VarChar, 20).Value = nomUsuario.Trim();
                    cmd.Parameters.Add("@dniExcluir", SqlDbType.VarChar, 8).Value = dniExcluir.Trim();

                    con.Open();
                    int cantidad = Convert.ToInt32(cmd.ExecuteScalar());
                    return cantidad > 0;
                }
            }
        }

        public static bool ExisteCorreo(string correoUsuario, string dniExcluir = "")
        {
            if (string.IsNullOrWhiteSpace(correoUsuario)) return false;

            string query = @"
                SELECT COUNT(1) 
                FROM Usuario 
                WHERE correo_usuario = @correoUsuario AND dni_usuario <> @dniExcluir";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@correoUsuario", SqlDbType.VarChar, 20).Value = correoUsuario.Trim();
                    cmd.Parameters.Add("@dniExcluir", SqlDbType.VarChar, 8).Value = dniExcluir.Trim();

                    con.Open();
                    int cantidad = Convert.ToInt32(cmd.ExecuteScalar());
                    return cantidad > 0;
                }
            }
        }

        public static bool ExisteTelefono(string telefonoUsuario, string dniExcluir = "")
        {
            if (string.IsNullOrWhiteSpace(telefonoUsuario)) return false;

            string query = @"
                SELECT COUNT(1) 
                FROM Usuario 
                WHERE telefono_usuario = @telefonoUsuario AND dni_usuario <> @dniExcluir";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@telefonoUsuario", SqlDbType.VarChar, 10).Value = telefonoUsuario.Trim();
                    cmd.Parameters.Add("@dniExcluir", SqlDbType.VarChar, 8).Value = dniExcluir.Trim();

                    con.Open();
                    int cantidad = Convert.ToInt32(cmd.ExecuteScalar());
                    return cantidad > 0;
                }
            }
        }

        public static void Insertar(Usuario usuario)
        {
            string query = @"
                INSERT INTO Usuario (dni_usuario, nom_usuario, ape_usuario, direccion, telefono_usuario, correo_usuario, pasword, estado, id_rol) 
                VALUES (@dniUsuario, @nomUsuario, @apeUsuario, @direccion, @telefonoUsuario, @correoUsuario, @pasword, @estado, @idRol)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = usuario.DniUsuario.Trim();
                    cmd.Parameters.Add("@nomUsuario", SqlDbType.VarChar, 20).Value = usuario.NomUsuario.Trim();
                    cmd.Parameters.Add("@apeUsuario", SqlDbType.VarChar, 20).Value = usuario.ApeUsuario.Trim();
                    cmd.Parameters.Add("@direccion", SqlDbType.VarChar, 50).Value = usuario.Direccion.Trim();
                    cmd.Parameters.Add("@telefonoUsuario", SqlDbType.VarChar, 10).Value = usuario.TelefonoUsuario.Trim();
                    cmd.Parameters.Add("@correoUsuario", SqlDbType.VarChar, 20).Value = usuario.CorreoUsuario.Trim();
                    cmd.Parameters.Add("@pasword", SqlDbType.VarChar, 256).Value = usuario.Pasword;
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = usuario.Estado;
                    cmd.Parameters.Add("@idRol", SqlDbType.Int).Value = usuario.IdRol;

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void Actualizar(Usuario usuario, bool actualizarPassword)
        {
            string query = actualizarPassword
                ? @"UPDATE Usuario 
                    SET nom_usuario = @nomUsuario, ape_usuario = @apeUsuario, direccion = @direccion, 
                        telefono_usuario = @telefonoUsuario, correo_usuario = @correoUsuario, 
                        pasword = @pasword, estado = @estado, id_rol = @idRol 
                    WHERE dni_usuario = @dniUsuario"
                : @"UPDATE Usuario 
                    SET nom_usuario = @nomUsuario, ape_usuario = @apeUsuario, direccion = @direccion, 
                        telefono_usuario = @telefonoUsuario, correo_usuario = @correoUsuario, 
                        estado = @estado, id_rol = @idRol 
                    WHERE dni_usuario = @dniUsuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = usuario.DniUsuario.Trim();
                    cmd.Parameters.Add("@nomUsuario", SqlDbType.VarChar, 20).Value = usuario.NomUsuario.Trim();
                    cmd.Parameters.Add("@apeUsuario", SqlDbType.VarChar, 20).Value = usuario.ApeUsuario.Trim();
                    cmd.Parameters.Add("@direccion", SqlDbType.VarChar, 50).Value = usuario.Direccion.Trim();
                    cmd.Parameters.Add("@telefonoUsuario", SqlDbType.VarChar, 10).Value = usuario.TelefonoUsuario.Trim();
                    cmd.Parameters.Add("@correoUsuario", SqlDbType.VarChar, 20).Value = usuario.CorreoUsuario.Trim();
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = usuario.Estado;
                    cmd.Parameters.Add("@idRol", SqlDbType.Int).Value = usuario.IdRol;

                    if (actualizarPassword)
                    {
                        cmd.Parameters.Add("@pasword", SqlDbType.VarChar, 256).Value = usuario.Pasword;
                    }

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CambiarEstado(string dniUsuario, bool activo)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario)) return;

            string query = "UPDATE Usuario SET estado = @estado WHERE dni_usuario = @dniUsuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = activo;
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniUsuario.Trim();

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void Eliminar(string dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario)) return;

            string query = "DELETE FROM Usuario WHERE dni_usuario = @dniUsuario";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@dniUsuario", SqlDbType.VarChar, 8).Value = dniUsuario.Trim();

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Usuario MapearUsuario(SqlDataReader reader)
        {
            return new Usuario
            {
                DniUsuario = reader["dni_usuario"]?.ToString() ?? string.Empty,
                NomUsuario = reader["nom_usuario"]?.ToString() ?? string.Empty,
                ApeUsuario = reader["ape_usuario"]?.ToString() ?? string.Empty,
                Direccion = reader["direccion"]?.ToString() ?? string.Empty,
                TelefonoUsuario = reader["telefono_usuario"]?.ToString() ?? string.Empty,
                CorreoUsuario = reader["correo_usuario"]?.ToString() ?? string.Empty,
                Pasword = reader["pasword"]?.ToString() ?? string.Empty,
                Estado = reader["estado"] != DBNull.Value && Convert.ToBoolean(reader["estado"]),
                IdRol = reader["id_rol"] != DBNull.Value ? Convert.ToInt32(reader["id_rol"]) : 0,
                AltaUsuario = reader["alta_usuario"] != DBNull.Value ? Convert.ToDateTime(reader["alta_usuario"]) : DateTime.MinValue,
                Rol = new Rol
                {
                    IdRol = reader["id_rol"] != DBNull.Value ? Convert.ToInt32(reader["id_rol"]) : 0,
                    NomRol = reader["nom_rol"]?.ToString() ?? string.Empty
                }
            };
        }
    }
}