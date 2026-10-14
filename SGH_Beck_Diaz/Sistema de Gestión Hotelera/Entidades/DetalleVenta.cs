namespace Entidades
{
    public class DetalleVenta
    {
        public int IdVenta { get; set; }
        public string CodProducto { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }
}
