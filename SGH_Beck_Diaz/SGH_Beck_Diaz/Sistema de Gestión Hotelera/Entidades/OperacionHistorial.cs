using System;

namespace Entidades
{
    public class OperacionHistorial
    {
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Huesped { get; set; }
        public int? NroHabitacion { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public decimal? Monto { get; set; }
    }
}
