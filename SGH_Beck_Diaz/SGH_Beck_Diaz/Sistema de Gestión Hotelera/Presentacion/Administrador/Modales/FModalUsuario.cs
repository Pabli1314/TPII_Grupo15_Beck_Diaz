using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalUsuario : FormModalBase
    {
        private readonly GestionUsuarios _gestionUsuarios = new();
        private readonly Usuario? _usuarioOriginal;
        private readonly TextBox _txtDni;
        private readonly TextBox _txtNombre;
        private readonly TextBox _txtApellido;
        private readonly TextBox _txtDireccion;
        private readonly TextBox _txtTelefono;
        private readonly TextBox _txtCorreo;
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtConfirmar;
        private readonly ComboBox _cmbRol;
        private readonly ToggleSwitch _switchEstado;
        private readonly Label _lblErrorGeneral;

        public Usuario? UsuarioGuardado { get; private set; }

        public FModalUsuario(Usuario? usuario)
        {
            _usuarioOriginal = usuario;
            Size = new Size(610, 852);
            EstablecerTitulo(usuario == null ? "Nuevo usuario" : "Editar usuario");

            int y = 0;
            const int alto = 62;
            const int ancho = 400;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(0, y)));
            _txtDni = CamposFormulario.Texto(new Point(0, y + 22), ancho);

            // Si es edición, el DNI actúa como Clave Primaria y no debe modificarse
            if (usuario != null)
            {
                _txtDni.ReadOnly = true;
            }
            else
            {
                _txtDni.KeyPress += (s, e) =>
                {
                    // Permitir solo dígitos y control de retroceso
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                    }
                };
            }
            Contenido.Controls.Add(_txtDni);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtNombre);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(0, y)));
            _txtApellido = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtApellido);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(0, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtDireccion);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(0, y)));
            _txtTelefono = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtTelefono);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Correo", new Point(0, y)));
            _txtCorreo = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtCorreo);
            y += alto;

            string etiquetaPassword = usuario == null ? "Contraseña" : "Nueva contraseña (dejar vacío para no cambiarla)";
            Contenido.Controls.Add(CamposFormulario.Etiqueta(etiquetaPassword, new Point(0, y)));
            _txtPassword = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: true);
            Contenido.Controls.Add(_txtPassword);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Confirmar contraseña", new Point(0, y)));
            _txtConfirmar = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: true);
            Contenido.Controls.Add(_txtConfirmar);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Rol", new Point(0, y)));
            _cmbRol = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            foreach (Rol rol in _gestionUsuarios.ObtenerRoles())
            {
                _cmbRol.Items.Add(rol);
            }
            _cmbRol.DisplayMember = nameof(Rol.NomRol);
            Contenido.Controls.Add(_cmbRol);
            y += alto;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Estado", new Point(0, y)));
            _switchEstado = new ToggleSwitch { Location = new Point(0, y + 24), Checked = true };
            Contenido.Controls.Add(_switchEstado);
            y += 56;

            _lblErrorGeneral = new Label
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 20),
                ForeColor = Paleta.Peligro,
                Font = Paleta.FuenteChica,
                Visible = false
            };
            Contenido.Controls.Add(_lblErrorGeneral);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Guardar", Size = new Size(120, 38), Location = new Point(ancho - 120, y) });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);

            CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            if (_usuarioOriginal == null)
            {
                if (_cmbRol.Items.Count > 0) _cmbRol.SelectedIndex = 0;
                return;
            }

            _txtDni.Text = _usuarioOriginal.DniUsuario;
            _txtNombre.Text = _usuarioOriginal.NomUsuario;
            _txtApellido.Text = _usuarioOriginal.ApeUsuario;
            _txtDireccion.Text = _usuarioOriginal.Direccion;
            _txtTelefono.Text = _usuarioOriginal.TelefonoUsuario;
            _txtCorreo.Text = _usuarioOriginal.CorreoUsuario;
            _switchEstado.Checked = _usuarioOriginal.Estado;

            foreach (var item in _cmbRol.Items)
            {
                if (item is Rol rol && rol.IdRol == _usuarioOriginal.IdRol)
                {
                    _cmbRol.SelectedItem = rol;
                    break;
                }
            }
        }

        private void Guardar()
        {
            try
            {
                _lblErrorGeneral.Visible = false;

                // Comprueba que ningún campo obligatorio esté vacío
                if (string.IsNullOrWhiteSpace(_txtNombre.Text) ||
                    string.IsNullOrWhiteSpace(_txtApellido.Text) ||
                    string.IsNullOrWhiteSpace(_txtDni.Text) ||
                    string.IsNullOrWhiteSpace(_txtDireccion.Text) ||
                    string.IsNullOrWhiteSpace(_txtTelefono.Text) ||
                    string.IsNullOrWhiteSpace(_txtCorreo.Text) ||
                    _cmbRol.SelectedItem == null)
                {
                    throw new ArgumentException("Todos los campos son obligatorios.");
                }

                if (_txtDni.Text.Trim().Length > 8)
                {
                    throw new ArgumentException("El DNI no puede superar los 8 caracteres.");
                }

                // Si es un usuario nuevo, la contraseña y confirmación son obligatorias
                if (_usuarioOriginal == null && (string.IsNullOrWhiteSpace(_txtPassword.Text) || string.IsNullOrWhiteSpace(_txtConfirmar.Text)))
                {
                    throw new ArgumentException("Debe ingresar y confirmar la contraseña para el nuevo usuario.");
                }

                var usuario = new Usuario
                {
                    DniUsuario = _txtDni.Text.Trim(),
                    NomUsuario = _txtNombre.Text.Trim(),
                    ApeUsuario = _txtApellido.Text.Trim(),
                    Direccion = _txtDireccion.Text.Trim(),
                    TelefonoUsuario = _txtTelefono.Text.Trim(),
                    CorreoUsuario = _txtCorreo.Text.Trim(),
                    IdRol = (_cmbRol.SelectedItem as Rol)?.IdRol ?? 0,
                    Estado = _switchEstado.Checked
                };

                if (_usuarioOriginal == null)
                {
                    _gestionUsuarios.CrearUsuario(
                        usuario.DniUsuario,
                        usuario.NomUsuario,
                        usuario.ApeUsuario,
                        usuario.Direccion,
                        usuario.TelefonoUsuario,
                        usuario.CorreoUsuario,
                        usuario.IdRol,
                        _txtPassword.Text,
                        _txtConfirmar.Text
                    );
                }
                else
                {
                    _gestionUsuarios.EditarUsuario(usuario, _txtPassword.Text, _txtConfirmar.Text);
                }

                UsuarioGuardado = usuario;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                _lblErrorGeneral.Text = ex.Message;
                _lblErrorGeneral.Visible = true;
            }
            catch (Exception ex)
            {
                _lblErrorGeneral.Text = $"Error al guardar: {ex.Message}";
                _lblErrorGeneral.Visible = true;
            }
        }

        public Usuario? ObtenerUsuarioGuardado()
        {
            return UsuarioGuardado;
        }
    }
}