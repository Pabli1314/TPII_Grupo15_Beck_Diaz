namespace Entidades
{
    public class Producto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int IdCategoria { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;

        public bool Agotado => Stock <= 0;
        public bool StockBajo => !Agotado && Stock <= StockMinimo;
    }
}