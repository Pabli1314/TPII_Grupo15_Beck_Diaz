using System;

namespace Entidades
{
    /// <summary>Tabla Huesped. Límites de la base: DNI VARCHAR(8) solo dígitos (PK), nombre/apellido
    /// VARCHAR(50), teléfono VARCHAR(10) solo dígitos y único, dirección VARCHAR(50), correo
    /// VARCHAR(50) único. Todos NOT NULL; alta_huesped la completa el DEFAULT de la base.</summary>
    public class Huesped
    {
        public const int LargoDni = 8;
        public const int LargoNombre = 50;
        public const int LargoApellido = 50;
        public const int LargoTelefono = 10;
        public const int LargoDireccion = 50;
        public const int LargoCorreo = 50;

        public string DniHuesped { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public DateTime? AltaHuesped { get; set; }
    }
}
