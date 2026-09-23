namespace Entidades
{
    public class Categoria
    {
        public int IdCategoria { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        // Sobrescribimos ToString para que el ComboBox muestre directamente la descripción
        public override string ToString() => Descripcion;
    }
}