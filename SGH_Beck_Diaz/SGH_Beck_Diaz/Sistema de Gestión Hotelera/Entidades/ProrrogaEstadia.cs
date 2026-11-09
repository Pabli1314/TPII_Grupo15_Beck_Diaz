using System;

namespace Entidades
{
    /// <summary>
    /// Representa una prórroga o extensión de estadía registrada para un hospedaje activo.
    /// </summary>
    public class ProrrogaEstadia
    {
        public int IdProrroga { get; set; }
        public int IdHospedaje { get; set; }
        public DateTime FechaProrroga { get; set; } = DateTime.Today;
        public TimeSpan HoraProrroga { get; set; } = DateTime.Now.TimeOfDay;
        public DateTime FechaSalidaAnterior { get; set; }
        public TimeSpan HoraSalidaAnterior { get; set; }
        public DateTime NuevaFechaSalida { get; set; }
        public TimeSpan NuevaHoraSalida { get; set; }
        public int DiasAdicionales { get; set; }
        public decimal MontoAdicional { get; set; }
        public int IdMetodo { get; set; }
        public int IdTurno { get; set; }

        // Propiedades de apoyo para vistas, tickets e historial
        public string? MetodoPago { get; set; }
        public string? DniHuesped { get; set; }
        public string? NombreHuesped { get; set; }
        public int? NroHabitacion { get; set; }
    }
}
