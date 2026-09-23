using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Entidades;
using Datos;

namespace Logica
{
    public class GestionHuespedes
    {
        private const int LargoMinimoDni = 7;
        private const int LargoMinimoTelefono = 7;

        private static readonly Regex SoloDigitos = new Regex(@"^[0-9]+$");
        // Letras (con acentos y ñ), espacios, apóstrofo y guion: "María José", "O'Connor", "Pérez-Gómez".
        private static readonly Regex NombreValido = new Regex(@"^[\p{L}][\p{L} '\-]*$");
        private static readonly Regex CorreoValido = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public List<Huesped> ObtenerTodos()
        {
            return HuespedDAO.ObtenerTodos();
        }

        public List<Huesped> Buscar(string termino)
        {
            return string.IsNullOrWhiteSpace(termino) ? ObtenerTodos() : HuespedDAO.Buscar(termino);
        }

        /// <summary>Alta de un huésped nuevo. El check-in ya sabe dar de alta un huésped inline;
        /// esto es para registrarlo desde la pantalla de Huéspedes sin pasar por un check-in.</summary>
        public void RegistrarHuesped(Huesped huesped)
        {
            Validar(huesped, esNuevo: true);
            HuespedDAO.Crear(huesped);
        }

        /// <summary>Corrige los datos de contacto de un huésped existente (el DNI no cambia).</summary>
        public void ActualizarHuesped(Huesped huesped)
        {
            Validar(huesped, esNuevo: false);
            HuespedDAO.Actualizar(huesped);
        }

        /// <summary>
        /// Normaliza (trim, correo en minúsculas) y valida el huésped contra las restricciones de la
        /// tabla Huesped: campos NOT NULL, largos de VARCHAR, CHECK de solo dígitos en DNI/teléfono y
        /// UNIQUE de teléfono y correo. Así el error llega con un mensaje claro en vez de una
        /// SqlException. Si <paramref name="esNuevo"/> es true, además exige que el DNI no exista;
        /// si es false, exige que exista.
        /// </summary>
        public static void Validar(Huesped huesped, bool esNuevo)
        {
            if (huesped == null) throw new ArgumentNullException(nameof(huesped));

            huesped.DniHuesped = (huesped.DniHuesped ?? string.Empty).Trim();
            huesped.Nombre = NormalizarEspacios(huesped.Nombre);
            huesped.Apellido = NormalizarEspacios(huesped.Apellido);
            huesped.Telefono = (huesped.Telefono ?? string.Empty).Trim();
            huesped.Direccion = NormalizarEspacios(huesped.Direccion);
            huesped.Correo = (huesped.Correo ?? string.Empty).Trim().ToLowerInvariant();

            // DNI
            if (huesped.DniHuesped.Length == 0)
                throw new ArgumentException("Debe ingresar el DNI del huésped.");
            if (!SoloDigitos.IsMatch(huesped.DniHuesped))
                throw new ArgumentException("El DNI debe contener solo números (sin puntos ni espacios).");
            if (huesped.DniHuesped.Length < LargoMinimoDni || huesped.DniHuesped.Length > Huesped.LargoDni)
                throw new ArgumentException($"El DNI debe tener entre {LargoMinimoDni} y {Huesped.LargoDni} dígitos.");

            // Nombre y apellido
            ValidarNombre(huesped.Nombre, "el nombre", Huesped.LargoNombre);
            ValidarNombre(huesped.Apellido, "el apellido", Huesped.LargoApellido);

            // Teléfono
            if (huesped.Telefono.Length == 0)
                throw new ArgumentException("Debe ingresar el teléfono.");
            if (!SoloDigitos.IsMatch(huesped.Telefono))
                throw new ArgumentException("El teléfono debe contener solo números (sin espacios, guiones ni +).");
            if (huesped.Telefono.Length < LargoMinimoTelefono || huesped.Telefono.Length > Huesped.LargoTelefono)
                throw new ArgumentException($"El teléfono debe tener entre {LargoMinimoTelefono} y {Huesped.LargoTelefono} dígitos.");

            // Dirección
            if (huesped.Direccion.Length == 0)
                throw new ArgumentException("Debe ingresar la dirección.");
            if (huesped.Direccion.Length > Huesped.LargoDireccion)
                throw new ArgumentException($"La dirección no puede superar los {Huesped.LargoDireccion} caracteres.");

            // Correo
            if (huesped.Correo.Length == 0)
                throw new ArgumentException("Debe ingresar el correo electrónico.");
            if (huesped.Correo.Length > Huesped.LargoCorreo)
                throw new ArgumentException($"El correo no puede superar los {Huesped.LargoCorreo} caracteres.");
            if (!CorreoValido.IsMatch(huesped.Correo))
                throw new ArgumentException("El correo electrónico no tiene un formato válido (ej: nombre@dominio.com).");

            // Existencia y unicidad contra la base
            bool existe = HuespedDAO.ObtenerPorDni(huesped.DniHuesped) != null;
            if (esNuevo && existe)
                throw new InvalidOperationException($"Ya existe un huésped registrado con DNI {huesped.DniHuesped}.");
            if (!esNuevo && !existe)
                throw new InvalidOperationException($"No existe un huésped registrado con DNI {huesped.DniHuesped}.");

            if (HuespedDAO.ExisteTelefono(huesped.Telefono, huesped.DniHuesped))
                throw new InvalidOperationException($"El teléfono {huesped.Telefono} ya está registrado para otro huésped.");
            if (HuespedDAO.ExisteCorreo(huesped.Correo, huesped.DniHuesped))
                throw new InvalidOperationException($"El correo {huesped.Correo} ya está registrado para otro huésped.");
        }

        private static void ValidarNombre(string valor, string etiqueta, int largoMaximo)
        {
            if (valor.Length == 0)
                throw new ArgumentException($"Debe ingresar {etiqueta}.");
            if (valor.Length > largoMaximo)
                throw new ArgumentException($"{Capitalizar(etiqueta)} no puede superar los {largoMaximo} caracteres.");
            if (!NombreValido.IsMatch(valor))
                throw new ArgumentException($"{Capitalizar(etiqueta)} solo puede contener letras, espacios, apóstrofos o guiones.");
        }

        private static string NormalizarEspacios(string? valor)
        {
            return Regex.Replace((valor ?? string.Empty).Trim(), @"\s+", " ");
        }

        private static string Capitalizar(string texto)
        {
            return texto.Length == 0 ? texto : char.ToUpper(texto[0]) + texto.Substring(1);
        }
    }
}
