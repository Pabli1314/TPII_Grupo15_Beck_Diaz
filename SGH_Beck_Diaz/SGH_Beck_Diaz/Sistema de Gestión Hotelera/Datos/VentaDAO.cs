using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class VentaDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        private const string Columnas = "id_venta, fecha_venta, hora_venta, total, id_metodo, dni_huesped";

        /// <summary>
        /// Registra la venta completa (cabecera + detalle + descuento de stock) en una única
        /// transacción SQL: si cualquier paso falla (por ejemplo no hay stock suficiente de algún
        /// producto) se revierte todo y no queda nada grabado. fecha_venta/hora_venta quedan a
        /// cargo del DEFAULT de la base. Devuelve el id de la venta creada.
        /// </summary>
        public static int RegistrarConDetalle(Venta venta, List<DetalleVenta> detalles)
        {
            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        string queryVenta = @"
                            INSERT INTO venta (total, id_metodo, dni_huesped)
                            OUTPUT INSERTED.id_venta
                            VALUES (@total, @idMetodo, @dniHuesped)";

                        int idVenta;
                        using (SqlCommand cmd = new SqlCommand(queryVenta, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@total", venta.Total);
                            cmd.Parameters.AddWithValue("@idMetodo", venta.IdMetodo);
                            cmd.Parameters.Add("@dniHuesped", System.Data.SqlDbType.VarChar, 8).Value = (object?)venta.DniHuesped ?? DBNull.Value;
                            idVenta = (int)cmd.ExecuteScalar();
                        }

                        DetalleVentaDAO.CrearVarios(con, tx, idVenta, detalles);

                        foreach (DetalleVenta detalle in detalles)
                        {
                            ProductoDAO.DescontarStock(con, tx, detalle.CodProducto, detalle.Cantidad);
                        }

                        tx.Commit();
                        return idVenta;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static List<Venta> ObtenerTodas()
        {
            string query = $"SELECT {Columnas} FROM venta ORDER BY fecha_venta DESC, hora_venta DESC";
            return Consultar(query, _ => { });
        }

        /// <summary>Ventas hechas entre dos momentos (inclusive). <paramref name="hasta"/> null = hasta ahora.
        /// Se usa para asignar las ventas a un turno de caja por su horario.</summary>
        public static List<Venta> ObtenerEnPeriodo(DateTime desde, DateTime? hasta)
        {
            string query = $@"
                SELECT {Columnas}
                FROM venta
                WHERE CAST(fecha_venta AS DATETIME) + CAST(hora_venta AS DATETIME) >= @desde
                  AND (@hasta IS NULL OR CAST(fecha_venta AS DATETIME) + CAST(hora_venta AS DATETIME) <= @hasta)
                ORDER BY fecha_venta, hora_venta";

            return Consultar(query, cmd =>
            {
                cmd.Parameters.Add("@desde", System.Data.SqlDbType.DateTime).Value = desde;
                cmd.Parameters.Add("@hasta", System.Data.SqlDbType.DateTime).Value = (object?)hasta ?? DBNull.Value;
            });
        }

        public static List<Venta> ObtenerEnRango(DateTime desde, DateTime hasta)
        {
            string query = $@"
                SELECT {Columnas}
                FROM venta
                WHERE fecha_venta BETWEEN @desde AND @hasta
                ORDER BY fecha_venta, hora_venta";

            return Consultar(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@desde", desde.Date);
                cmd.Parameters.AddWithValue("@hasta", hasta.Date);
            });
        }

        /// <summary>
        /// Ventas con el huésped que compró, el método de pago y los productos consumidos, de la más
        /// reciente a la más antigua. Una sola consulta: una fila por renglón de detalle_venta, que se
        /// agrupan por id_venta. La habitación no se guarda en venta: la completa la capa Lógica.
        /// </summary>
        /// <param name="dniHuesped">Si se indica, solo las ventas asignadas a ese huésped.</param>
        /// <param name="desde">Si se indica, solo las ventas hechas a partir de ese momento (ej. el check-in).</param>
        public static List<VentaRealizada> ObtenerRealizadas(string? dniHuesped = null, DateTime? desde = null)
        {
            var ventas = new List<VentaRealizada>();
            var porId = new Dictionary<int, VentaRealizada>();

            string query = @"
                SELECT v.id_venta, v.fecha_venta, v.hora_venta, v.total, v.dni_huesped,
                       hu.nombre_huesped, hu.apellido_huesped, m.nom_metodo_pago,
                       d.cod_producto, p.descripcion_product, d.cantidad, d.precio_unitario, d.subtotal
                FROM venta v
                INNER JOIN metodo_pago m ON m.id_metodo = v.id_metodo
                LEFT JOIN Huesped hu ON hu.dni_huesped = v.dni_huesped
                LEFT JOIN detalle_venta d ON d.id_venta = v.id_venta
                LEFT JOIN producto p ON p.cod_producto = d.cod_producto
                WHERE (@dniHuesped IS NULL OR v.dni_huesped = @dniHuesped)
                  AND (@desde IS NULL OR CAST(v.fecha_venta AS DATETIME) + CAST(v.hora_venta AS DATETIME) >= @desde)
                ORDER BY v.fecha_venta DESC, v.hora_venta DESC, v.id_venta DESC, p.descripcion_product";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.Add("@dniHuesped", System.Data.SqlDbType.VarChar, 8).Value = (object?)dniHuesped ?? DBNull.Value;
                cmd.Parameters.Add("@desde", System.Data.SqlDbType.DateTime).Value = (object?)desde ?? DBNull.Value;

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idVenta = Convert.ToInt32(reader["id_venta"]);
                        if (!porId.TryGetValue(idVenta, out VentaRealizada? venta))
                        {
                            venta = new VentaRealizada
                            {
                                IdVenta = idVenta,
                                FechaVenta = reader["fecha_venta"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["fecha_venta"]),
                                HoraVenta = reader["hora_venta"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["hora_venta"],
                                Total = Convert.ToDecimal(reader["total"]),
                                MetodoPago = reader["nom_metodo_pago"].ToString() ?? string.Empty,
                                DniHuesped = reader["dni_huesped"] is DBNull ? null : reader["dni_huesped"].ToString(),
                                NombreHuesped = reader["nombre_huesped"] is DBNull
                                    ? null
                                    : $"{reader["nombre_huesped"]} {reader["apellido_huesped"]}".Trim()
                            };
                            porId[idVenta] = venta;
                            ventas.Add(venta);
                        }

                        if (!(reader["cod_producto"] is DBNull))
                        {
                            venta.Items.Add(new ItemConsumido
                            {
                                CodProducto = reader["cod_producto"].ToString() ?? string.Empty,
                                Producto = reader["descripcion_product"] is DBNull ? reader["cod_producto"].ToString() ?? string.Empty : reader["descripcion_product"].ToString() ?? string.Empty,
                                Cantidad = Convert.ToInt32(reader["cantidad"]),
                                PrecioUnitario = Convert.ToDecimal(reader["precio_unitario"]),
                                Subtotal = Convert.ToDecimal(reader["subtotal"])
                            });
                        }
                    }
                }
            }

            return ventas;
        }

        private static List<Venta> Consultar(string query, Action<SqlCommand> agregarParametros)
        {
            var ventas = new List<Venta>();

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                agregarParametros(cmd);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ventas.Add(new Venta
                        {
                            IdVenta = Convert.ToInt32(reader["id_venta"]),
                            FechaVenta = reader["fecha_venta"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["fecha_venta"]),
                            HoraVenta = reader["hora_venta"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["hora_venta"],
                            Total = Convert.ToDecimal(reader["total"]),
                            IdMetodo = Convert.ToInt32(reader["id_metodo"]),
                            DniHuesped = reader["dni_huesped"] is DBNull ? null : reader["dni_huesped"].ToString()
                        });
                    }
                }
            }

            return ventas;
        }
    }
}
