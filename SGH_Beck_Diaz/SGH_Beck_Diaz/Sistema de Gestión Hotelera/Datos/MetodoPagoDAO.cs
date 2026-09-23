using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class MetodoPagoDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<MetodoPago> ObtenerTodos()
        {
            var metodos = new List<MetodoPago>();

            string query = "SELECT id_metodo, nom_metodo_pago FROM metodo_pago";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        metodos.Add(new MetodoPago
                        {
                            IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                            NomMetodoPago = reader["nom_metodo_pago"].ToString() ?? string.Empty
                        });
                    }
                }
            }

            return metodos;
        }
    }
}
