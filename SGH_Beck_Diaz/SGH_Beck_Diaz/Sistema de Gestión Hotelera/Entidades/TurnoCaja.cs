using System;

namespace Entidades
{
    public class TurnoCaja
    {
        public int IdTurno { get; set; }
        public DateTime FechaApertura { get; set; }
        public TimeSpan HoraApertura { get; set; }
        public DateTime? FechaCierre { get; set; }  // Permite NULL si el turno está abierto
        public TimeSpan? HoraCierre { get; set; }   // Permite NULL si el turno está abierto
        public decimal MontoInicial { get; set; }
        public decimal? MontoFinal { get; set; }   // Permite NULL
        public string Observaciones { get; set; }  // Permite NULL
        public string DniUsuario { get; set; }     // Mapeado a VARCHAR(8) de la BD
    }
}