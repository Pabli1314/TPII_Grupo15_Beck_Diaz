using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class ProductoDAO
    {
        private static readonly Conexion _conexion = new Conexion();

        public static List<Producto> ObtenerTodos()
        {
            var productos = new List<Producto>();

            string query = @"
                SELECT cod_producto, descripcion_product, id_categoria, stock_disponible, stock_min, precio, estado_producto
                FROM producto
                ORDER BY id_categoria, descripcion_product";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(MapearProducto(reader));
                    }
                }
            }

            return productos;
        }

        public static Producto? ObtenerPorCodigo(string codProducto)
        {
            Producto? producto = null;

            string query = @"
                SELECT cod_producto, descripcion_product, id_categoria, stock_disponible, stock_min, precio, estado_producto
                FROM producto
                WHERE cod_producto = @codProducto";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@codProducto", codProducto);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        producto = MapearProducto(reader);
                    }
                }
            }

            return producto;
        }

        public static void DescontarStock(string codProducto, int cantidad)
        {
            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                con.Open();
                DescontarStock(con, null, codProducto, cantidad);
            }
        }

        /// <summary>Descuenta stock respetando CH_producto_disponible (stock_disponible &gt;= stock_min):
        /// si la venta dejaría el stock por debajo del mínimo, no actualiza y lanza la excepción.</summary>
        public static void DescontarStock(SqlConnection con, SqlTransaction? tx, string codProducto, int cantidad)
        {
            string query = @"
                UPDATE producto
                SET stock_disponible = stock_disponible - @cantidad
                WHERE cod_producto = @codProducto AND stock_disponible - @cantidad >= stock_min";

            using (SqlCommand cmd = tx != null ? new SqlCommand(query, con, tx) : new SqlCommand(query, con))
            {
                if (tx == null && con.State != System.Data.ConnectionState.Open)
                {
                    con.Open();
                }

                cmd.Parameters.AddWithValue("@cantidad", cantidad);
                cmd.Parameters.AddWithValue("@codProducto", codProducto);

                int filas = cmd.ExecuteNonQuery();
                if (filas == 0)
                {
                    throw new InvalidOperationException($"No hay stock suficiente del producto con código \"{codProducto}\".");
                }
            }
        }

        public static void Crear(Producto producto)
        {
            string query = @"
                INSERT INTO producto (cod_producto, descripcion_product, id_categoria, stock_disponible, stock_min, precio, estado_producto)
                VALUES (@cod, @nombre, @idCategoria, @stock, @stockMinimo, @precio, @activo)";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@cod", producto.Codigo);
                cmd.Parameters.AddWithValue("@nombre", producto.Nombre);
                cmd.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
                cmd.Parameters.AddWithValue("@precio", producto.Precio);
                cmd.Parameters.AddWithValue("@stock", producto.Stock);
                cmd.Parameters.AddWithValue("@stockMinimo", producto.StockMinimo);
                cmd.Parameters.AddWithValue("@activo", producto.Activo);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void Actualizar(Producto producto)
        {
            string query = @"
                UPDATE producto
                SET descripcion_product = @nombre, 
                    id_categoria = @idCategoria, 
                    precio = @precio,
                    stock_disponible = @stock, 
                    stock_min = @stockMinimo, 
                    estado_producto = @activo
                WHERE cod_producto = @cod";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@cod", producto.Codigo);
                cmd.Parameters.AddWithValue("@nombre", producto.Nombre);
                cmd.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
                cmd.Parameters.AddWithValue("@precio", producto.Precio);
                cmd.Parameters.AddWithValue("@stock", producto.Stock);
                cmd.Parameters.AddWithValue("@stockMinimo", producto.StockMinimo);
                cmd.Parameters.AddWithValue("@activo", producto.Activo);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>True si el producto figura en algún detalle_venta (FK_detalle_producto impide borrarlo).</summary>
        public static bool TieneVentas(string codProducto)
        {
            string query = "SELECT COUNT(1) FROM detalle_venta WHERE cod_producto = @cod";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@cod", codProducto);

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public static void Eliminar(string codProducto)
        {
            string query = "DELETE FROM producto WHERE cod_producto = @cod";

            using (SqlConnection con = _conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@cod", codProducto);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static Producto MapearProducto(SqlDataReader reader)
        {
            return new Producto
            {
                Codigo = reader["cod_producto"].ToString() ?? string.Empty,
                Nombre = reader["descripcion_product"].ToString() ?? string.Empty,
                IdCategoria = Convert.ToInt32(reader["id_categoria"]),
                Precio = Convert.ToDecimal(reader["precio"]),
                Stock = Convert.ToInt32(reader["stock_disponible"]),
                StockMinimo = Convert.ToInt32(reader["stock_min"]),
                Activo = Convert.ToBoolean(reader["estado_producto"])
            };
        }
    }
}