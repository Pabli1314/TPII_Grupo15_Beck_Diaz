using Entidades;
using Datos;
using System;

namespace Logica
{
    public class IniciarSesion
    {
        public Usuario? AutenticarUsuario(string nomUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(nomUsuario) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            Usuario? usuario = UsuarioDAO.ObtenerPorUsername(nomUsuario.Trim());

            if (usuario == null || usuario.Pasword != password || !usuario.Estado)
            {
                return null;
            }

            return usuario;
        }

        public int ValidarUsuarioYRol(string nomUsuario, string password)
        {
            Usuario? usuario = AutenticarUsuario(nomUsuario, password);

            if (usuario == null)
            {
                return 0;
            }

            // Evaluar según el IdRol
            switch (usuario.IdRol)
            {
                case 1:
                    return 1; // Administrador

                case 2:
                    return 2; // Supervisor / Gerente

                case 3:
                    return 3; // Recepcionista / Empleado

                default:
                    return 0; // Rol no reconocido o sin privilegios
            }
        }
    }
}