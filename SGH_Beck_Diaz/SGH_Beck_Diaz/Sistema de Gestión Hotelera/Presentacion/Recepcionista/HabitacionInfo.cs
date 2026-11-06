using System;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Vista de una habitación para la UI (tarjetas del tablero y pantalla de gestión), armada
    /// a partir de Logica.GestionHabitaciones.ObtenerResumenHabitaciones() (ver VistaHabitaciones).
    /// </summary>
    public class HabitacionInfo
    {
        public string NroHabitacion { get; set; } = string.Empty;
        public int Piso { get; set; }
        public int CantCamas { get; set; }
        public decimal Tarifa { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public EstadoOcupacion Estado { get; set; }
        public string Huesped { get; set; } = string.Empty;
        public DateTime? HoraIngreso { get; set; }
        public DateTime? HoraSalidaEstimada { get; set; }
    }
}
