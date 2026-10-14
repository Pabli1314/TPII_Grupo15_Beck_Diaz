using System;

namespace Entidades
{
    /// <summary>
    /// Evento del histórico del Administrador: solo operaciones de las áreas que gestiona ese rol
    /// (habitaciones, limpieza, inventario y usuarios). A diferencia del histórico de Gerencia no
    /// lleva montos: la recaudación y los arqueos de caja quedan reservados a ese rol.
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
        public string Usuario { get; set; } = string.Empty;
        public string DniUsuario { get; set; } = string.Empty;

        public DateTime Momento => Fecha.Date + Hora;
    }
}
