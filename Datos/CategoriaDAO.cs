using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class CategoriaDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<Categoria> ObtenerTodas()
        {
            var lista = new List<Categoria>();
            string query = "SELECT id_categoria, descripcion_cat FROM categoria_producto ORDER BY descripcion_cat";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Categoria
                        {
                            IdCategoria = Convert.ToInt32(reader["id_categoria"]),
                            Descripcion = reader["descripcion_cat"].ToString() ?? string.Empty
                        });
                    }
                }
            }

            return lista;
        }

        public static string ConsultarNombreCategoria(int idCategoria)
        {
            // Se corrigieron los nombres de las columnas a id_categoria y descripcion_cat
            string query = "SELECT descripcion_cat FROM categoria_producto WHERE id_categoria = @id";

            try
            {
                // Se usa el método ObtenerConexion() igual que en ObtenerTodas()
                using (SqlConnection conexion = _conexion.ObtenerConexion())
                {
                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        comando.Parameters.AddWithValue("@id", idCategoria);
                        conexion.Open();

                        object? resultado = comando.ExecuteScalar();

                        if (resultado != null && resultado != DBNull.Value)
                        {
                            return resultado.ToString()!;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la categoría: {ex.Message}");
            }

            return "Sin categoría";
        }
    }
}