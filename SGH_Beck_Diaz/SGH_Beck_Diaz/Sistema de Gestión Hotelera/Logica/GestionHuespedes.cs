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

        /// <summary>
        /// Búsqueda avanzada que integra texto de búsqueda, filtro por estado de estadía y número de habitación.
        /// </summary>
        public List<Huesped> BuscarFiltrado(string? termino, string? estadoEstadia, int? nroHabitacion)
        {
            return HuespedDAO.BuscarFiltrado(termino, estadoEstadia, nroHabitacion);
        }

        public void RegistrarHuesped(Huesped huesped)
        {
            Validar(huesped, esNuevo: true);
            HuespedDAO.Crear(huesped);
        }

        public void ActualizarHuesped(Huesped huesped)
        {
            Validar(huesped, esNuevo: false);
            HuespedDAO.Actualizar(huesped);
        }

        public static void Validar(Huesped huesped, bool esNuevo)
        {
            if (huesped == null) throw new ArgumentNullException(nameof(huesped));

            huesped.DniHuesped = (huesped.DniHuesped ?? string.Empty).Trim();
            huesped.Nombre = NormalizarEspacios(huesped.Nombre);
            huesped.Apellido = NormalizarEspacios(huesped.Apellido);
            huesped.Telefono = (huesped.Telefono ?? string.Empty).Trim();
            huesped.Direccion = NormalizarEspacios(huesped.Direccion);
            huesped.Correo = (huesped.Correo ?? string.Empty).Trim().ToLowerInvariant();

            if (huesped.DniHuesped.Length == 0)
                throw new ArgumentException("Debe ingresar el DNI del huésped.");
            if (!SoloDigitos.IsMatch(huesped.DniHuesped))
                throw new ArgumentException("El DNI debe contener solo números (sin puntos ni espacios).");
            if (huesped.DniHuesped.Length < LargoMinimoDni || huesped.DniHuesped.Length > Huesped.LargoDni)
                throw new ArgumentException($"El DNI debe tener entre {LargoMinimoDni} y {Huesped.LargoDni} dígitos.");

            ValidarNombre(huesped.Nombre, "el nombre", Huesped.LargoNombre);
            ValidarNombre(huesped.Apellido, "el apellido", Huesped.LargoApellido);

            if (huesped.Telefono.Length == 0)
                throw new ArgumentException("Debe ingresar el teléfono.");
            if (!SoloDigitos.IsMatch(huesped.Telefono))
                throw new ArgumentException("El teléfono debe contener solo números (sin espacios, guiones ni +).");
            if (huesped.Telefono.Length < LargoMinimoTelefono || huesped.Telefono.Length > Huesped.LargoTelefono)
                throw new ArgumentException($"El teléfono debe tener entre {LargoMinimoTelefono} y {Huesped.LargoTelefono} dígitos.");

            if (huesped.Direccion.Length == 0)
                throw new ArgumentException("Debe ingresar la dirección.");
            if (huesped.Direccion.Length > Huesped.LargoDireccion)
                throw new ArgumentException($"La dirección no puede superar los {Huesped.LargoDireccion} caracteres.");

            if (huesped.Correo.Length == 0)
                throw new ArgumentException("Debe ingresar el correo electrónico.");
            if (huesped.Correo.Length > Huesped.LargoCorreo)
                throw new ArgumentException($"El correo no puede superar los {Huesped.LargoCorreo} caracteres.");
            if (!CorreoValido.IsMatch(huesped.Correo))
                throw new ArgumentException("El correo electrónico no tiene un formato válido (ej: nombre@dominio.com).");

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

        public List<Huesped> ObtenerDisponiblesParaCheckIn()
        {
            return HuespedDAO.ObtenerDisponiblesParaCheckIn();
        }
    }
}