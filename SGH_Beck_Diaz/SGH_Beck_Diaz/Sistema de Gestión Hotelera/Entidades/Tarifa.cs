using System;

namespace Entidades
{
    /// <summary>
    /// Todavía no existe una tabla "Tarifa" en la base: Logica.GestionTarifas la administra
    /// en memoria (sembrada desde TipoHabitacionDAO) hasta que se agregue la persistencia real.
    /// </summary>
    public class Tarifa
    {
        public int IdTarifa { get; set; }
        public int IdTipoHabitacion { get; set; }
        public string TipoHabitacionNombre { get; set; } = string.Empty;
        public decimal PrecioPorHora { get; set; }
        public decimal PrecioPorFraccion { get; set; }
        public decimal PrecioAdicional { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime UltimaModificacion { get; set; } = DateTime.Now;
    }
}
