using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class HabitacionesDAO
    {
        private readonly Conexion conexion = new Conexion();

        /// <summary>
        /// Obtiene los contadores necesarios para calcular los KPIs de habitaciones.
        /// </summary>
        public (int Total, int Ocupadas, int Disponibles, int Mantenimiento) ObtenerMetricasHabitaciones()
        {
            int total = 0;
            int ocupadas = 0;
            int disponibles = 0;
            int mantenimiento = 0;

            string query = @"
                SELECT 
                    COUNT(*) AS Total,
                    ISNULL(SUM(CASE WHEN LOWER(TRIM(eh.nom_estado_habitacion)) LIKE '%ocupad%' THEN 1 ELSE 0 END), 0) AS Ocupadas,
                    ISNULL(SUM(CASE WHEN LOWER(TRIM(eh.nom_estado_habitacion)) LIKE '%disponib%' THEN 1 ELSE 0 END), 0) AS Disponibles,
                    ISNULL(SUM(CASE WHEN LOWER(TRIM(eh.nom_estado_habitacion)) NOT LIKE '%ocupad%' 
                                    AND LOWER(TRIM(eh.nom_estado_habitacion)) NOT LIKE '%disponib%' THEN 1 ELSE 0 END), 0) AS Mantenimiento
                FROM habitacion h
                INNER JOIN Estado_habitacion eh ON h.id_estado = eh.id_estado;";

            using (SqlConnection con = conexion.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        total = dr["Total"] != DBNull.Value ? Convert.ToInt32(dr["Total"]) : 0;
                        ocupadas = dr["Ocupadas"] != DBNull.Value ? Convert.ToInt32(dr["Ocupadas"]) : 0;
                        disponibles = dr["Disponibles"] != DBNull.Value ? Convert.ToInt32(dr["Disponibles"]) : 0;
                        mantenimiento = dr["Mantenimiento"] != DBNull.Value ? Convert.ToInt32(dr["Mantenimiento"]) : 0;
                    }
                }
            }

            return (total, ocupadas, disponibles, mantenimiento);
        }

        /// <summary>
        /// Obtiene la cantidad de habitaciones agrupadas por el nombre del estado (para gráficos de torta/métricas).
        /// </summary>
        public Dictionary<string, int> ObtenerEstadoHabitaciones()
        {
            var resultado = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            string query = @"
                SELECT 
                    eh.nom_estado_habitacion AS Estado, 
                    COUNT(*) AS Cantidad
                FROM habitacion h
                INNER JOIN Estado_habitacion eh ON h.id_estado = eh.id_estado
                GROUP BY eh.nom_estado_habitacion;";

            using (SqlConnection con = conexion.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string estado = reader["Estado"]?.ToString()?.Trim() ?? "Desconocido";
                        int cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToInt32(reader["Cantidad"]) : 0;

                        if (resultado.ContainsKey(estado))
                            resultado[estado] += cantidad;
                        else
                            resultado.Add(estado, cantidad);
                    }
                }
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene el listado completo de habitaciones para la grilla.
        /// </summary>
        public List<Habitacion> ListarHabitaciones()
        {
            List<Habitacion> lista = new List<Habitacion>();

            string query = @"
                SELECT 
                    nro_habitacion,
                    piso,
                    cant_camas,
                    tarifa_base,
                    id_tipo_habitacion,
                    id_estado
                FROM habitacion
                ORDER BY nro_habitacion ASC;";

            using (SqlConnection con = conexion.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(MapearHabitacion(dr));
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Obtiene el listado de habitaciones filtrado por el nombre del estado de manera flexible.
        /// </summary>
        public List<Habitacion> ListarHabitacionesPorEstado(string nombreEstado)
        {
            List<Habitacion> lista = new List<Habitacion>();

            if (string.IsNullOrWhiteSpace(nombreEstado))
                return lista;

            string query = @"
                SELECT 
                    h.nro_habitacion,
                    h.piso,
                    h.cant_camas,
                    h.tarifa_base,
                    h.id_tipo_habitacion,
                    h.id_estado
                FROM habitacion h
                INNER JOIN Estado_habitacion eh ON h.id_estado = eh.id_estado
                WHERE LOWER(TRIM(eh.nom_estado_habitacion)) LIKE '%' + LOWER(TRIM(@nombreEstado)) + '%'
                   OR LOWER(TRIM(@nombreEstado)) LIKE '%' + LOWER(TRIM(eh.nom_estado_habitacion)) + '%'
                ORDER BY h.nro_habitacion ASC;";

            using (SqlConnection con = conexion.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@nombreEstado", nombreEstado.Trim());
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(MapearHabitacion(dr));
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Método auxiliar centralizado para mapear filas a la entidad Habitacion.
        /// </summary>
        private Habitacion MapearHabitacion(SqlDataReader dr)
        {
            return new Habitacion
            {
                NroHabitacion = dr["nro_habitacion"] != DBNull.Value ? Convert.ToInt32(dr["nro_habitacion"]) : 0,
                Piso = dr["piso"] != DBNull.Value ? Convert.ToInt32(dr["piso"]) : 0,
                CantCamas = dr["cant_camas"] != DBNull.Value ? Convert.ToInt32(dr["cant_camas"]) : 0,
                TarifaBase = dr["tarifa_base"] != DBNull.Value ? Convert.ToDecimal(dr["tarifa_base"]) : 0m,
                IdTipoHabitacion = dr["id_tipo_habitacion"] != DBNull.Value ? Convert.ToInt32(dr["id_tipo_habitacion"]) : 0,
                IdEstado = dr["id_estado"] != DBNull.Value ? Convert.ToInt32(dr["id_estado"]) : 0
            };
        }
    }
}