using Entidades;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalPerfil : FormModalBase
    {
        public FModalPerfil(Usuario usuario)
        {
            Size = new Size(420, 390);
            EstablecerTitulo("Mi perfil");

            var avatar = new AvatarCircular
            {
                Size = new Size(72, 72),
                Location = new Point(0, 0),
                Iniciales = ObtenerIniciales(usuario)
            };
            Contenido.Controls.Add(avatar);

            string nombreCompleto = $"{usuario.ApeUsuario} {usuario.NomUsuario}".Trim();
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                nombreCompleto = usuario.NomUsuario;
            }

            var lblNombre = new Label
            {
                Text = nombreCompleto,
                Font = new Font("Segoe UI Semibold", 14f),
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(90, 10)
            };

            var badgeRol = new BadgeEstado { Location = new Point(90, 40) };
            badgeRol.FijarEstado(usuario.Rol?.NomRol?.ToUpperInvariant() ?? "ADMINISTRADOR", Paleta.Primario);

            Contenido.Controls.Add(lblNombre);
            Contenido.Controls.Add(badgeRol);

            int y = 100;
            AgregarDato("DNI", usuario.DniUsuario, ref y);
            AgregarDato("Correo", string.IsNullOrWhiteSpace(usuario.CorreoUsuario) ? "-" : usuario.CorreoUsuario, ref y);
            AgregarDato("Rol", usuario.Rol?.NomRol ?? "-", ref y);
            AgregarDato("Estado", usuario.Estado ? "Activo" : "Inactivo", ref y);

            // Se utiliza AltaUsuario en sustitución de UltimoAcceso conforme a la estructura de la tabla Usuario
            string fechaAlta = usuario.AltaUsuario != default
                ? usuario.AltaUsuario.ToString("dd/MM/yyyy HH:mm")
                : "-";
            AgregarDato("Fecha de Alta", fechaAlta, ref y);

            var btnCerrar = EstiloBoton.Secundario(new Button
            {
                Text = "Cerrar",
                Size = new Size(110, 36),
                Location = new Point(270, y + 10)
            });
            btnCerrar.Click += (s, e) => Close();
            Contenido.Controls.Add(btnCerrar);
        }

        private void AgregarDato(string etiqueta, string valor, ref int y)
        {
            Contenido.Controls.Add(new Label
            {
                Text = etiqueta,
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Location = new Point(0, y)
            });
            Contenido.Controls.Add(new Label
            {
                Text = valor,
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, y + 18)
            });
            y += 46;
        }

        private static string ObtenerIniciales(Usuario usuario)
        {
            string apellido = usuario.ApeUsuario ?? string.Empty;
            string nombre = usuario.NomUsuario ?? string.Empty;

            char inicial1 = apellido.Length > 0 ? apellido[0] : (nombre.Length > 0 ? nombre[0] : 'A');
            char inicial2 = nombre.Length > 0 ? nombre[0] : (apellido.Length > 1 ? apellido[1] : 'D');

            return $"{inicial1}{inicial2}".ToUpperInvariant();
        }
    }
}