using Entidades;
using Datos;
using System;

namespace Logica
{
    public class IniciarSesion
    {
        public Usuario? AutenticarUsuarioPorDni(string dniUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            Usuario? usuario = UsuarioDAO.ObtenerPorDni(dniUsuario.Trim());

            if (usuario == null || usuario.Pasword != password || !usuario.Estado)
            {
                return null;
            }

            return usuario;
        }

        public (Usuario? Usuario, int Rol) ValidarDniYRol(string dniUsuario, string password)
        {
            Usuario? usuario = AutenticarUsuarioPorDni(dniUsuario, password);

            if (usuario == null)
            {
                return (null, 0);
            }

            // Política de cierre manual obligatorio: el login NO cierra turnos. Un turno vencido queda
            // "Pendiente de cierre" y se regulariza desde Gestión de Caja (ver GestionTurnoCaja).

            int idRol = usuario.IdRol switch
            {
                1 => 1, // Administrador
                2 => 2, // Gerente 
                3 => 3, // Recepcionista
                _ => 0
            };

            return (usuario, idRol);
        }
    }
}
