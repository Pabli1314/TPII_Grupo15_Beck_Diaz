using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    public class Habitacion
    {
        public int NroHabitacion { get; set; }
        public int Piso { get; set; }
        public int CantCamas { get; set; }
        public decimal TarifaBase { get; set; }
        public int IdTipoHabitacion { get; set; } // Clave Foránea
        public int IdEstado { get; set; }
    }
}
