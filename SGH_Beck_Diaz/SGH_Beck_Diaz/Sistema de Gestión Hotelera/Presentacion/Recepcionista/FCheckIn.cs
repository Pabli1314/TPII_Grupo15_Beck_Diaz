using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>Alta de (huésped + hospedaje + pase de la habitación a Ocupada).
    /// Al perder el foco el campo DNI, busca si el huésped ya existe y precarga sus datos.</summary>
    internal class FCheckIn : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly string _dniUsuario;
        private readonly GestionHospedajes _gestionHospedajes = new GestionHospedajes();

        private readonly TextBox _txtDni;
        private readonly TextBox _txtNombre;
        private readonly TextBox _txtApellido;
        private readonly TextBox _txtTelefono;
        private readonly TextBox _txtDireccion;
        private readonly TextBox _txtCorreo;
        private readonly ComboBox _cmbMetodoPago;
        private readonly DateTimePicker _dtpFechaSalida;
        private readonly DateTimePicker _dtpHoraSalida;
        private readonly Label _lblEncontrado;
        private readonly Label _lblError;

        public string NombreCompleto { get; private set; } = string.Empty;
        public DateTime HoraIngreso { get; private set; }

        public FCheckIn(string nroHabitacion, string dniUsuario)
        {
            _nroHabitacion = int.Parse(nroHabitacion);
            _dniUsuario = dniUsuario;

            Size = new Size(520, 734);
            EstablecerTitulo($"Check-in — Habitación {nroHabitacion}");

            const int ancho = 440;
            int y = 0;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(0, y)));
            _txtDni = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 22), ancho), Huesped.LargoDni);
            _txtDni.Leave += (s, e) => BuscarHuespedPorDni();
            Contenido.Controls.Add(_txtDni);
            y += 46;

            _lblEncontrado = new Label { Location = new Point(0, y), AutoSize = true, Font = Paleta.FuenteChica, Visible = false };
            Contenido.Controls.Add(_lblEncontrado);
            y += 22;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtNombre.MaxLength = Huesped.LargoNombre;
            Contenido.Controls.Add(_txtNombre);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(0, y)));
            _txtApellido = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtApellido.MaxLength = Huesped.LargoApellido;
            Contenido.Controls.Add(_txtApellido);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(0, y)));
            _txtTelefono = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 22), ancho), Huesped.LargoTelefono);
            Contenido.Controls.Add(_txtTelefono);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(0, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtDireccion.MaxLength = Huesped.LargoDireccion;
            Contenido.Controls.Add(_txtDireccion);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Correo electrónico", new Point(0, y)));
            _txtCorreo = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtCorreo.MaxLength = Huesped.LargoCorreo;
            Contenido.Controls.Add(_txtCorreo);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Método de pago", new Point(0, y)));
            _cmbMetodoPago = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbMetodoPago.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPago.ValueMember = nameof(MetodoPago.IdMetodo);
            _cmbMetodoPago.DataSource = _gestionHospedajes.ObtenerMetodosPago();
            Contenido.Controls.Add(_cmbMetodoPago);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Fecha de salida", new Point(0, y)));
            _dtpFechaSalida = new DateTimePicker { Location = new Point(0, y + 22), Size = new Size(ancho, 30), Format = DateTimePickerFormat.Short, MinDate = DateTime.Today, Value = DateTime.Today.AddDays(1) };
            Contenido.Controls.Add(_dtpFechaSalida);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Hora estimada de salida", new Point(0, y)));
            _dtpHoraSalida = new DateTimePicker { Location = new Point(0, y + 22), Size = new Size(ancho, 30), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(12) };
            Contenido.Controls.Add(_dtpHoraSalida);
            y += 62;

            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 40), Location = new Point(ancho - 120 - 140, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button { Text = "Confirmar", Size = new Size(140, 40), Location = new Point(ancho - 140, y) });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);
        }

        private void BuscarHuespedPorDni()
        {
            string dni = _txtDni.Text.Trim();
            if (string.IsNullOrWhiteSpace(dni))
            {
                _lblEncontrado.Visible = false;
                return;
            }

            Huesped? huesped = _gestionHospedajes.BuscarHuespedPorDni(dni);
            if (huesped != null)
            {
                _txtNombre.Text = huesped.Nombre;
                _txtApellido.Text = huesped.Apellido;
                _txtTelefono.Text = huesped.Telefono;
                _txtDireccion.Text = huesped.Direccion;
                _txtCorreo.Text = huesped.Correo;

                _lblEncontrado.Text = "Huésped encontrado. Si modifica sus datos, se actualizarán al confirmar.";
                _lblEncontrado.ForeColor = Paleta.Exito;
                _lblEncontrado.Visible = true;
            }
            else
            {
                _lblEncontrado.Text = "Nuevo huésped.";
                _lblEncontrado.ForeColor = Paleta.TextoTerciario;
                _lblEncontrado.Visible = true;
            }
        }

        private void Confirmar()
        {
            if (!ValidarCampos(out string mensaje, out Control controlInvalido))
            {
                _lblError.Text = mensaje;
                _lblError.Visible = true;
                controlInvalido.Focus();
                return;
            }

            var huesped = new Huesped
            {
                DniHuesped = _txtDni.Text.Trim(),
                Nombre = _txtNombre.Text.Trim(),
                Apellido = _txtApellido.Text.Trim(),
                Telefono = _txtTelefono.Text.Trim(),
                Direccion = _txtDireccion.Text.Trim(),
                Correo = _txtCorreo.Text.Trim()
            };

            int idMetodo = (int)_cmbMetodoPago.SelectedValue;

            try
            {
                _gestionHospedajes.RegistrarCheckIn(_nroHabitacion, _dniUsuario, huesped, idMetodo, _dtpFechaSalida.Value, _dtpHoraSalida.Value.TimeOfDay);

                NombreCompleto = $"{huesped.Nombre} {huesped.Apellido}";
                HoraIngreso = DateTime.Now;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                // Errores de validación de la capa Lógica: se muestran dentro del formulario.
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al registrar el check-in.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarCampos(out string mensaje, out Control controlInvalido)
        {
            (string valor, string etiqueta, Control control)[] requeridos =
            {
                (_txtDni.Text, "el DNI", _txtDni),
                (_txtNombre.Text, "el nombre", _txtNombre),
                (_txtApellido.Text, "el apellido", _txtApellido),
                (_txtTelefono.Text, "el teléfono", _txtTelefono),
                (_txtDireccion.Text, "la dirección", _txtDireccion),
                (_txtCorreo.Text, "el correo electrónico", _txtCorreo),
            };

            foreach (var (valor, etiqueta, control) in requeridos)
            {
                if (string.IsNullOrWhiteSpace(valor))
                {
                    mensaje = $"Debe ingresar {etiqueta}.";
                    controlInvalido = control;
                    return false;
                }
            }

            if (_cmbMetodoPago.SelectedValue == null)
            {
                mensaje = "Debe seleccionar un método de pago.";
                controlInvalido = _cmbMetodoPago;
                return false;
            }

            mensaje = string.Empty;
            controlInvalido = _txtDni;
            return true;
        }
    }
}
