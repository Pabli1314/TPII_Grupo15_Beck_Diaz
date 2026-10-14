using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalCambiarPassword : FormModalBase
    {
        private readonly GestionUsuarios _gestionUsuarios = new();
        private readonly Usuario _usuario;

        private readonly TextBox _txtActual;
        private readonly TextBox _txtNueva;
        private readonly TextBox _txtConfirmar;
        private readonly Label _lblError;

        public FModalCambiarPassword(Usuario usuario)
        {
            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
            Size = new Size(440, 400);
            EstablecerTitulo("Cambiar contraseña");

            const int ancho = 360;
            int y = 0;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Contraseña actual", new Point(0, y)));
            _txtActual = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: true);
            Contenido.Controls.Add(_txtActual);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nueva contraseña", new Point(0, y)));
            _txtNueva = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: true);
            Contenido.Controls.Add(_txtNueva);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Confirmar nueva contraseña", new Point(0, y)));
            _txtConfirmar = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: true);
            Contenido.Controls.Add(_txtConfirmar);
            y += 62;

            _lblError = new Label
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 20),
                ForeColor = Paleta.Peligro,
                Font = Paleta.FuenteChica,
                Visible = false
            };
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 38),
                Location = new Point(ancho - 120 - 130, y)
            });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button
            {
                Text = "Guardar",
                Size = new Size(120, 38),
                Location = new Point(ancho - 120, y)
            });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);
        }

        private void Guardar()
        {
            try
            {
                // Se invoca el método pasando la propiedad identificadora DniUsuario (string)
                _gestionUsuarios.CambiarPassword(_usuario.DniUsuario, _txtActual.Text, _txtNueva.Text, _txtConfirmar.Text);

                MessageBox.Show("Contraseña actualizada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
        }
    }
}