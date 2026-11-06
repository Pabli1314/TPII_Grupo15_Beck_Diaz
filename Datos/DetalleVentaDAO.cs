using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Entidades;

namespace Datos
{
    public class DetalleVentaDAO
    {
        /// <summary>Inserta los ítems de una venta dentro de una transacción existente.</summary>
        public static void CrearVarios(SqlConnection con, SqlTransaction tx, int idVenta, List<DetalleVenta> detalles)
        {
            string query = @"
                INSERT INTO detalle_venta (id_venta, cod_producto, precio_unitario, cantidad, subtotal)
                VALUES (@idVenta, @codProducto, @precioUnitario, @cantidad, @subtotal)";

            foreach (DetalleVenta detalle in detalles)
            {
                using (SqlCommand cmd = new SqlCommand(query, con, tx))
                {
                    cmd.Parameters.AddWithValue("@idVenta", idVenta);
                    cmd.Parameters.AddWithValue("@codProducto", detalle.CodProducto);
                    cmd.Parameters.AddWithValue("@precioUnitario", detalle.PrecioUnitario);
                    cmd.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
                    cmd.Parameters.AddWithValue("@subtotal", detalle.Subtotal);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
