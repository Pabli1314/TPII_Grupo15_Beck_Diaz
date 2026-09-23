using System;

namespace Entidades
{
    /// <summary>Hospedaje (estadía) con el nombre del huésped ya resuelto, para listarlo en
    /// pantalla sin hacer una consulta aparte por cada fila.</summary>
    public class HospedajeDetalle
    {
        public int IdHospedaje { get; set; }
        public int IdTurno { get; set; }
        public string DniHuesped { get; set; } = string.Empty;
        public string NombreHuesped { get; set; } = string.Empty;
        public int NroHabitacion { get; set; }
        public DateTime FechaEntrada { get; set; }
        public TimeSpan HoraEntrada { get; set; }
        public DateTime FechaSalida { get; set; }
        public TimeSpan HoraSalida { get; set; }

        public bool Finalizada => FechaSalida.Date + HoraSalida <= DateTime.Now;
        public string Estado => Finalizada ? "Finalizada" : "En curso";
    }
}
