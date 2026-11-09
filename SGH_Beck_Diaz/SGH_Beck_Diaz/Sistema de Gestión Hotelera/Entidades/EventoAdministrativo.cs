using System;

namespace Entidades
{
    /// <summary>
    /// Evento del histórico del Administrador, centrado en los huéspedes: altas, ingresos (check-in),
    /// egresos (check-out) y consumos. A diferencia del histórico de Gerencia no lleva montos: la
    /// recaudación y los arqueos de caja quedan reservados a ese rol.
    /// </summary>
    public class EventoAdministrativo
    {
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int? NroHabitacion { get; set; }
        public string? Producto { get; set; }
        public int? Cantidad { get; set; }
        public string Huesped { get; set; } = string.Empty;
        public string DniHuesped { get; set; } = string.Empty;

        public DateTime Momento => Fecha.Date + Hora;
    }
}
