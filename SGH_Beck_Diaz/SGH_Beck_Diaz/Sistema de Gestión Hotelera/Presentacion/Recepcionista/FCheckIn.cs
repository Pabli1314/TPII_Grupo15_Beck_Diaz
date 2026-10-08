using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Modal de Check-In mediante selección de un huésped no alojado actualmente.
    /// </summary>
    internal class FCheckIn : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly string _dniUsuario;
        private readonly GestionHospedajes _gestionHospedajes = new GestionHospedajes();
        private readonly GestionHuespedes _gestionHuespedes = new GestionHuespedes();

        private readonly ComboBox _cmbHuespedes;
        private readonly Label _lblDetalleHuesped;
        private readonly ComboBox _cmbMetodoPago;
        private readonly DateTimePicker _dtpFechaSalida;
        private readonly DateTimePicker _dtpHoraSalida;
        private readonly Label _lblError;

        public string NombreCompleto { get; private set; } = string.Empty;
        public DateTime HoraIngreso { get; private set; }

        public FCheckIn(string nroHabitacion, string dniUsuario)
        {
            if (!int.TryParse(nroHabitacion, out _nroHabitacion))
            {
                _nroHabitacion = 0;
            }

            _dniUsuario = dniUsuario ?? string.Empty;

            Size = new Size(520, 520);
            EstablecerTitulo($"Check-in — Habitación {nroHabitacion}");

            const int ancho = 440;
            int y = 0;

            // Selección de Huésped (filtrado)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Seleccionar Huésped", new Point(0, y)));
            _cmbHuespedes = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbHuespedes.DropDownStyle = ComboBoxStyle.DropDownList;

            // Suscribir el evento ANTES de cargar para garantizar la actualización inicial limpia
            _cmbHuespedes.SelectedIndexChanged += CmbHuespedes_SelectedIndexChanged;
            Contenido.Controls.Add(_cmbHuespedes);
            y += 54;

            // Panel informativo del Huésped
            _lblDetalleHuesped = new Label
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 60),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(8),
                Text = "Seleccione un huésped para ver su información."
            };
            Contenido.Controls.Add(_lblDetalleHuesped);
            y += 68;

            // Método de Pago
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Método de pago", new Point(0, y)));
            _cmbMetodoPago = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbMetodoPago.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPago.ValueMember = nameof(MetodoPago.IdMetodo);

            var metodosPago = _gestionHospedajes.ObtenerMetodosPago();
            _cmbMetodoPago.DataSource = metodosPago ?? new List<MetodoPago>();
            Contenido.Controls.Add(_cmbMetodoPago);
            y += 54;

            // Fecha de Salida
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Fecha de salida", new Point(0, y)));
            _dtpFechaSalida = new DateTimePicker
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 30),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1)
            };
            Contenido.Controls.Add(_dtpFechaSalida);
            y += 54;

            // Hora Estimada de Salida
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Hora estimada de salida", new Point(0, y)));
            _dtpHoraSalida = new DateTimePicker
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 30),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(12)
            };
            Contenido.Controls.Add(_dtpHoraSalida);
            y += 62;

            // Mensajes de Error
            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            // Botones de Acción
            var btnCancelar = EstiloBoton.Secundario(new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(ancho - 120 - 140, y)
            });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button
            {
                Text = "Confirmar",
                Size = new Size(140, 40),
                Location = new Point(ancho - 140, y)
            });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);

            // Cargar datos a los controles
            CargarHuespedesDisponibles();
        }

        private void CargarHuespedesDisponibles()
        {
            List<Huesped>? lista = _gestionHuespedes.ObtenerDisponiblesParaCheckIn();

            if (lista == null || lista.Count == 0)
            {
                _cmbHuespedes.DataSource = null;
                _cmbHuespedes.Items.Clear();
                _cmbHuespedes.Enabled = false;
                _lblDetalleHuesped.Text = "No hay huéspedes sin alojar registrados en el sistema.";
                return;
            }

            var listaCombo = lista.ConvertAll(h => new
            {
                Dni = h?.DniHuesped ?? string.Empty,
                NombreCompleto = $"{h?.Apellido ?? string.Empty}, {h?.Nombre ?? string.Empty} (DNI: {h?.DniHuesped ?? "S/D"})",
                ObjetoHuesped = h
            });

            _cmbHuespedes.DisplayMember = "NombreCompleto";
            _cmbHuespedes.ValueMember = "Dni";
            _cmbHuespedes.DataSource = listaCombo;
            _cmbHuespedes.Enabled = true;

            // Seleccionar el primer elemento por defecto si hay items disponibles
            if (_cmbHuespedes.Items.Count > 0)
            {
                _cmbHuespedes.SelectedIndex = 0;
            }
        }

        private void CmbHuespedes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cmbHuespedes.SelectedItem != null)
            {
                try
                {
                    dynamic seleccionado = _cmbHuespedes.SelectedItem;
                    Huesped? h = seleccionado?.ObjetoHuesped;

                    if (h != null)
                    {
                        _lblDetalleHuesped.Text = $"Teléfono: {h.Telefono ?? "-"}\n" +
                                                  $"Correo: {h.Correo ?? "-"}\n" +
                                                  $"Dirección: {h.Direccion ?? "-"}";
                        return;
                    }
                }
                catch
                {
                    // Fallback en caso de que falle la evaluación de la propiedad dinámica
                }
            }

            if (_cmbHuespedes.Enabled)
            {
                _lblDetalleHuesped.Text = "Sin huésped seleccionado.";
            }
        }

        private void Confirmar()
        {
            _lblError.Visible = false;

            if (!_cmbHuespedes.Enabled || _cmbHuespedes.SelectedItem == null)
            {
                _lblError.Text = "Debe seleccionar un huésped válido.";
                _lblError.Visible = true;
                return;
            }

            if (_cmbMetodoPago.SelectedValue == null)
            {
                _lblError.Text = "Debe seleccionar un método de pago.";
                _lblError.Visible = true;
                _cmbMetodoPago.Focus();
                return;
            }

            Huesped? huesped = null;
            try
            {
                dynamic seleccionado = _cmbHuespedes.SelectedItem;
                huesped = seleccionado?.ObjetoHuesped;
            }
            catch
            {
                huesped = null;
            }

            if (huesped == null)
            {
                _lblError.Text = "Error al recuperar los datos del huésped seleccionado.";
                _lblError.Visible = true;
                return;
            }

            if (!int.TryParse(_cmbMetodoPago.SelectedValue.ToString(), out int idMetodo))
            {
                _lblError.Text = "Método de pago no válido.";
                _lblError.Visible = true;
                return;
            }

            try
            {
                _gestionHospedajes.RegistrarCheckIn(
                    _nroHabitacion,
                    _dniUsuario,
                    huesped,
                    idMetodo,
                    _dtpFechaSalida.Value,
                    _dtpHoraSalida.Value.TimeOfDay
                );

                NombreCompleto = $"{huesped.Nombre} {huesped.Apellido}".Trim();
                HoraIngreso = DateTime.Now;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al registrar el check-in.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}