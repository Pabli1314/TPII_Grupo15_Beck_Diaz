using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    public class Hospedaje
    {
        public int IdHospedaje { get; set; }
        public DateTime FechaEntrada { get; set; } // Tiene DEFAULT en BD
        public TimeSpan HoraEntrada { get; set; }   // Tiene DEFAULT en BD
        public DateTime FechaSalida { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public int IdMetodo { get; set; }
        public int NroHabitacion { get; set; }
        public int IdTurno { get; set; }
        public string DniHuesped { get; set; }
    }
}
