using System;

namespace Entidades
{
    public enum TipoNotificacion
    {
        Info,
        Advertencia,
        Exito,
        Error
    }

    public class NotificacionItem
    {
        public int Id { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Info;
        public bool Leida { get; set; }
    }
}
