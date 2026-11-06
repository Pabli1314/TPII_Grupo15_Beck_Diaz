using System;

namespace Entidades
{
    public class Usuario
    {
        public string DniUsuario { get; set; } = string.Empty;
        public string NomUsuario { get; set; } = string.Empty;
        public string ApeUsuario { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string TelefonoUsuario { get; set; } = string.Empty;
        public string CorreoUsuario { get; set; } = string.Empty;
        public string Pasword { get; set; } = string.Empty;
        public bool Estado { get; set; } // BIT (DEFAULT 1 = true)
        public int IdRol { get; set; }
        public DateTime AltaUsuario { get; set; }

        public Rol Rol { get; set; } = new Rol();
    }
}