using Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Datos
{
    public class RolDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<Rol> ObtenerTodos()
        {
            List<Rol> roles = new List<Rol>();

            string query = "SELECT id_rol, nom_rol FROM Rol ORDER BY id_rol";

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
                            roles.Add(new Rol
                            {
                                IdRol = Convert.ToInt32(reader["id_rol"]),
                                NomRol = reader["nom_rol"].ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            return roles;
        }
    }
}
