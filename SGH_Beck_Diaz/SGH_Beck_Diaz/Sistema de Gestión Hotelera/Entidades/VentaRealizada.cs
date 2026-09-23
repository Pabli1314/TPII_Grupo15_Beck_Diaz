using System;
using System.Collections.Generic;
using System.Linq;

namespace Entidades
{
    /// <summary>DTO de solo lectura para listar las ventas adicionales con el huésped que realizó la
    /// consumición y los productos consumidos (venta + Huesped + metodo_pago + detalle_venta + producto).</summary>
    public class VentaRealizada
    {
        public int IdVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public TimeSpan HoraVenta { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = string.Empty;

        /// <summary>Null = venta de mostrador, sin huésped asignado.</summary>
        public string? DniHuesped { get; set; }
        public string? NombreHuesped { get; set; }

        /// <summary>Habitación en la que estaba alojado el huésped al momento de la venta (si se encontró).</summary>
        public int? NroHabitacion { get; set; }

        public List<ItemConsumido> Items { get; set; } = new List<ItemConsumido>();

        public DateTime Momento => FechaVenta.Date + HoraVenta;
        public bool EsDeMostrador => DniHuesped == null;
        public string ResumenProductos => string.Join(", ", Items.Select(i => $"{i.Cantidad} x {i.Producto}"));
    }

    public class ItemConsumido
    {
        public string CodProducto { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
