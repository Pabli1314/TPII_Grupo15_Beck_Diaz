using System;

namespace Entidades
{
    /// <summary>
    /// DTO de solo lectura para las pantallas de Dashboard y Supervisión del Administrador.
    /// Combina Habitacion + EstadoHabitacion + TipoHabitacion + Hospedaje/Huesped vigente +
    /// último RegistroLimpieza. Lo arma Logica.GestionHabitaciones.ObtenerResumenHabitaciones().
    /// </summary>
    public class HabitacionResumen
    {
        public int NroHabitacion { get; set; }
        public int Piso { get; set; }
        public string TipoHabitacion { get; set; } = string.Empty;
        public int IdEstado { get; set; }
        public string Estado { get; set; } = string.Empty;

        public string? Huesped { get; set; }
        public string? DniHuesped { get; set; }
        public DateTime? HoraEntrada { get; set; }
        public DateTime? HoraSalidaEstimada { get; set; }

        public DateTime? UltimaLimpieza { get; set; }
        public TimeSpan? DuracionUltimaLimpieza { get; set; }

        public bool Ocupada => string.Equals(Estado, "Ocupada", StringComparison.OrdinalIgnoreCase);
        public bool EnLimpieza => string.Equals(Estado, "Limpieza", StringComparison.OrdinalIgnoreCase);

        /// <summary>Tiempo restante hasta la salida estimada (negativo si ya está vencida).</summary>
        public TimeSpan? TiempoRestante => HoraSalidaEstimada.HasValue ? HoraSalidaEstimada.Value - DateTime.Now : null;
    }
}
