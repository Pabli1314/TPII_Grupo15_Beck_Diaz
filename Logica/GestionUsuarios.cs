using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Logica
{
    public class GestionUsuarios
    {
        public List<Usuario> ObtenerUsuarios() => UsuarioDAO.ObtenerTodos();

        public List<Rol> ObtenerRoles() => RolDAO.ObtenerTodos();

        /// <summary>Primer usuario activo de la base con ese rol (por nombre de rol), o null si no hay.
        /// Lo usan los módulos cuando se abren sin pasar por el login, para operar con un usuario que
        /// exista de verdad (las FK de Turno_caja y registro_limpieza exigen un dni_usuario real).</summary>
        public Usuario? ObtenerUsuarioActivoPorRol(string nomRol)
        {
            foreach (Usuario usuario in UsuarioDAO.ObtenerTodos())
            {
                if (usuario.Estado && string.Equals(usuario.Rol?.NomRol, nomRol, StringComparison.OrdinalIgnoreCase))
                {
                    return usuario;
                }
            }

            return null;
        }

        public void CrearUsuario(
        string dniUsuario,
        string nomUsuario,
        string apeUsuario,
        string direccion,
        string telefonoUsuario,
        string correoUsuario,
        int idRol,
        string password,
        string confirmarPassword)
        {
            // 1. Instanciar la entidad agrupando los datos recibidos
            var usuario = new Usuario
            {
                DniUsuario = dniUsuario?.Trim(),
                NomUsuario = nomUsuario?.Trim(),
                ApeUsuario = apeUsuario?.Trim(),
                Direccion = direccion?.Trim(),
                TelefonoUsuario = telefonoUsuario?.Trim(),
                CorreoUsuario = correoUsuario?.Trim(),
                IdRol = idRol,
                Estado = true // Asignación del campo bit 'estado' (1 = activo)
            };

            // 2. Validar que los campos cumplan con el formato y longitud de la tabla
            ValidarDatosBasicos(usuario);
            ValidarPassword(password, confirmarPassword, esObligatoria: true);

            // 3. Validar Clave Primaria (PK_usuario_id / dni_usuario)
            if (UsuarioDAO.ObtenerPorDni(usuario.DniUsuario) != null)
            {
                throw new ArgumentException($"Ya existe un usuario registrado con el DNI \"{usuario.DniUsuario}\".");
            }

            // 4. Validar Unicidad de Nombre de Usuario
            if (UsuarioDAO.ExisteNombreUsuario(usuario.NomUsuario))
            {
                throw new ArgumentException($"Ya existe un usuario con el nombre de usuario \"{usuario.NomUsuario}\".");
            }

            // 5. Validar Unicidad de Correo (UQ_usuario_correo)
            if (UsuarioDAO.ExisteCorreo(usuario.CorreoUsuario))
            {
                throw new ArgumentException($"Ya existe un usuario registrado con el correo \"{usuario.CorreoUsuario}\".");
            }

            // 6. Validar Unicidad de Teléfono (UQ_usuario_telefono)
            if (UsuarioDAO.ExisteTelefono(usuario.TelefonoUsuario))
            {
                throw new ArgumentException($"Ya existe un usuario registrado con el teléfono \"{usuario.TelefonoUsuario}\".");
            }

            // 7. Asignar la contraseña
            usuario.Pasword = password;

            // 8. Inserción (el campo 'alta_usuario' lo genera automáticamente la BD)
            UsuarioDAO.Insertar(usuario);
        }

        public void EditarUsuario(Usuario usuario, string password, string confirmarPassword)
        {
            ValidarDatosBasicos(usuario);

            if (UsuarioDAO.ExisteNombreUsuario(usuario.NomUsuario, usuario.DniUsuario))
            {
                throw new ArgumentException($"Ya existe otro usuario con el nombre de usuario \"{usuario.NomUsuario}\".");
            }

            if (UsuarioDAO.ExisteCorreo(usuario.CorreoUsuario, usuario.DniUsuario))
            {
                throw new ArgumentException($"Ya existe otro usuario registrado con el correo \"{usuario.CorreoUsuario}\".");
            }

            if (UsuarioDAO.ExisteTelefono(usuario.TelefonoUsuario, usuario.DniUsuario))
            {
                throw new ArgumentException($"Ya existe otro usuario registrado con el teléfono \"{usuario.TelefonoUsuario}\".");
            }

            bool cambiaPassword = !string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmarPassword);
            if (cambiaPassword)
            {
                ValidarPassword(password, confirmarPassword, esObligatoria: true);
                usuario.Pasword = password;
            }

            UsuarioDAO.Actualizar(usuario, actualizarPassword: cambiaPassword);
        }

        public void CambiarEstado(string dniUsuario, bool activo)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                throw new ArgumentException("El DNI del usuario es requerido.");

            UsuarioDAO.CambiarEstado(dniUsuario, activo);
        }

        public void CambiarPassword(string dniUsuario, string passwordActual, string passwordNueva, string confirmarPassword)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                throw new ArgumentException("El DNI del usuario es requerido.");

            Usuario usuario = UsuarioDAO.ObtenerPorDni(dniUsuario)
                ?? throw new InvalidOperationException("No se encontró el usuario.");

            if (usuario.Pasword != passwordActual)
            {
                throw new ArgumentException("La contraseña actual no es correcta.");
            }

            ValidarPassword(passwordNueva, confirmarPassword, esObligatoria: true);

            usuario.Pasword = passwordNueva;
            UsuarioDAO.Actualizar(usuario, actualizarPassword: true);
        }

        public void EliminarUsuario(string dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
                throw new ArgumentException("El DNI del usuario es requerido.");

            UsuarioDAO.Eliminar(dniUsuario);
        }

        // Largos de la tabla Usuario. Los parámetros de UsuarioDAO tienen tamaño fijo: un valor más
        // largo se truncaría en silencio en vez de fallar, por eso se valida antes acá.
        // Son públicos para que FModalUsuario use los mismos valores como MaxLength.
        public const int LargoDni = 8;
        public const int LargoNombre = 30;
        public const int LargoApellido = 30;
        public const int LargoDireccion = 50;
        public const int LargoTelefono = 10;
        public const int LargoCorreo = 50;
        public const int LargoPasswordMaximo = 50;
        private const int LargoNombreMinimo = 2;
        private const int LargoDireccionMinimo = 5;
        private const int LargoPasswordMinimo = 8;

        private static void ValidarDatosBasicos(Usuario usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentException("No se recibieron datos del usuario.");
            }

            usuario.DniUsuario = (usuario.DniUsuario ?? string.Empty).Trim();
            usuario.NomUsuario = (usuario.NomUsuario ?? string.Empty).Trim();
            usuario.ApeUsuario = (usuario.ApeUsuario ?? string.Empty).Trim();
            usuario.Direccion = (usuario.Direccion ?? string.Empty).Trim();
            usuario.TelefonoUsuario = (usuario.TelefonoUsuario ?? string.Empty).Trim();
            usuario.CorreoUsuario = (usuario.CorreoUsuario ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(usuario.DniUsuario))
            {
                throw new ArgumentException("Debe ingresar el DNI.");
            }

            // CK_usuario_dni: solo dígitos / CK_usuario_dni_largo: exactamente 8
            if (usuario.DniUsuario.Length != LargoDni || !EsSoloDigitos(usuario.DniUsuario))
            {
                throw new ArgumentException($"El DNI debe contener exactamente {LargoDni} dígitos numéricos.");
            }

            if (string.IsNullOrWhiteSpace(usuario.NomUsuario))
            {
                throw new ArgumentException("Debe ingresar el nombre de usuario.");
            }

            // CK_usuario_nom_largo / CK_usuario_nom_sin_digitos
            if (usuario.NomUsuario.Length < LargoNombreMinimo || usuario.NomUsuario.Length > LargoNombre)
            {
                throw new ArgumentException($"El nombre debe tener entre {LargoNombreMinimo} y {LargoNombre} caracteres.");
            }

            if (!EsSoloLetras(usuario.NomUsuario))
            {
                throw new ArgumentException("El nombre solo puede contener letras y espacios.");
            }

            if (string.IsNullOrWhiteSpace(usuario.ApeUsuario))
            {
                throw new ArgumentException("Debe ingresar el apellido.");
            }

            // CK_usuario_ape_largo / CK_usuario_ape_sin_digitos
            if (usuario.ApeUsuario.Length < LargoNombreMinimo || usuario.ApeUsuario.Length > LargoApellido)
            {
                throw new ArgumentException($"El apellido debe tener entre {LargoNombreMinimo} y {LargoApellido} caracteres.");
            }

            if (!EsSoloLetras(usuario.ApeUsuario))
            {
                throw new ArgumentException("El apellido solo puede contener letras y espacios.");
            }

            if (string.IsNullOrWhiteSpace(usuario.Direccion))
            {
                throw new ArgumentException("Debe ingresar la dirección.");
            }

            // CK_usuario_direccion_largo
            if (usuario.Direccion.Length < LargoDireccionMinimo || usuario.Direccion.Length > LargoDireccion)
            {
                throw new ArgumentException($"La dirección debe tener entre {LargoDireccionMinimo} y {LargoDireccion} caracteres.");
            }

            // CK_usuario_telefono: solo dígitos / CK_usuario_telefono_largo: exactamente 10
            if (string.IsNullOrWhiteSpace(usuario.TelefonoUsuario))
            {
                throw new ArgumentException("Debe ingresar el teléfono.");
            }

            if (usuario.TelefonoUsuario.Length != LargoTelefono || !EsSoloDigitos(usuario.TelefonoUsuario))
            {
                throw new ArgumentException($"El teléfono debe tener exactamente {LargoTelefono} dígitos (código de área sin 0 y número sin 15).");
            }

            if (string.IsNullOrWhiteSpace(usuario.CorreoUsuario))
            {
                throw new ArgumentException("Debe ingresar el correo.");
            }

            // CK_usuario_correo_formato
            if (!Regex.IsMatch(usuario.CorreoUsuario, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("El correo ingresado no es válido.");
            }

            if (usuario.CorreoUsuario.Length > LargoCorreo)
            {
                throw new ArgumentException($"El correo no puede superar los {LargoCorreo} caracteres.");
            }

            if (usuario.IdRol <= 0)
            {
                throw new ArgumentException("Debe seleccionar un rol para el usuario.");
            }
        }

        private static bool EsSoloDigitos(string valor)
        {
            foreach (char c in valor)
            {
                if (c < '0' || c > '9') return false;
            }

            return true;
        }

        private static bool EsSoloLetras(string valor)
        {
            foreach (char c in valor)
            {
                if (!char.IsLetter(c) && c != ' ') return false;
            }

            return true;
        }

        private static void ValidarPassword(string password, string confirmarPassword, bool esObligatoria)
        {
            if (esObligatoria && string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Debe ingresar una contraseña.");
            }

            // Si no es obligatoria y viene vacía, no se cambia: el largo solo se controla cuando se ingresa una
            if (!string.IsNullOrEmpty(password) &&
                (password.Length < LargoPasswordMinimo || password.Length > LargoPasswordMaximo))
            {
                throw new ArgumentException($"La contraseña debe tener entre {LargoPasswordMinimo} y {LargoPasswordMaximo} caracteres.");
            }

            if (password != confirmarPassword)
            {
                throw new ArgumentException("Las contraseñas no coinciden.");
            }
        }
    }
}