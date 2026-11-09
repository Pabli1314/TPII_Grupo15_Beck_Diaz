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
        public string Tipo { get; set; } = string.Empty;
        public int Piso { get; set; }
        public int CantCamas { get; set; }
        public decimal TarifaPorDia { get; set; }
        public EstadoOcupacion Estado { get; set; }
        public string Huesped { get; set; } = string.Empty;
        public DateTime? HoraIngreso { get; set; }
        public DateTime? HoraSalidaEstimada { get; set; }
        public int? Noches { get; set; }
        public decimal? CostoHospedaje { get; set; }

        /// <summary>Resumen de características para mostrar en una línea: "Doble · 2 camas · Piso 1".</summary>
        public string Caracteristicas => $"{Tipo} · {CantCamas} cama{(CantCamas == 1 ? string.Empty : "s")} · Piso {Piso}";
    }
}
