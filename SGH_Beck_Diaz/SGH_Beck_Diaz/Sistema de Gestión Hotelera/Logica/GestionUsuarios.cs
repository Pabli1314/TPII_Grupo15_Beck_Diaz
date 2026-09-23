using Datos;
using Entidades;
using System;
using System.Collections.Generic;

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

        private static void ValidarDatosBasicos(Usuario usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentException("No se recibieron datos del usuario.");
            }

            if (string.IsNullOrWhiteSpace(usuario.DniUsuario))
            {
                throw new ArgumentException("Debe ingresar el DNI.");
            }

            if (usuario.DniUsuario.Trim().Length > 8)
            {
                throw new ArgumentException("El DNI no puede superar los 8 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(usuario.NomUsuario))
            {
                throw new ArgumentException("Debe ingresar el nombre de usuario.");
            }

            if (string.IsNullOrWhiteSpace(usuario.ApeUsuario))
            {
                throw new ArgumentException("Debe ingresar el apellido.");
            }

            if (string.IsNullOrWhiteSpace(usuario.CorreoUsuario))
            {
                throw new ArgumentException("Debe ingresar el correo.");
            }

            if (!usuario.CorreoUsuario.Contains('@') || !usuario.CorreoUsuario.Contains('.'))
            {
                throw new ArgumentException("El correo ingresado no es válido.");
            }

            if (usuario.IdRol <= 0)
            {
                throw new ArgumentException("Debe seleccionar un rol para el usuario.");
            }
        }

        private static void ValidarPassword(string password, string confirmarPassword, bool esObligatoria)
        {
            if (esObligatoria && string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Debe ingresar una contraseña.");
            }

            if (password != confirmarPassword)
            {
                throw new ArgumentException("Las contraseñas no coinciden.");
            }
        }
    }
}