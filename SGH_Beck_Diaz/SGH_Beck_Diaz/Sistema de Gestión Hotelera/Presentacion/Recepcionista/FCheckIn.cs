using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Alta de (huésped + hospedaje + pase de la habitación a Ocupada).
    /// Permite seleccionar un huésped registrado o ingresar/actualizar sus datos manualmente.
    /// Muestra el resumen del costo estimado de la estadía y emite el comprobante de hospedaje.
    /// </summary>
    internal class FCheckIn : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly string _dniUsuario;
        private readonly GestionHospedajes _gestionHospedajes = new GestionHospedajes();
        private readonly GestionHuespedes _gestionHuespedes = new GestionHuespedes();

        private readonly ComboBox _cmbHuespedes;
        private readonly Label _lblDetalleHuesped;
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

        // --- Panel resumen de costo ---
        private readonly Panel _pnlResumen;
        private readonly Label _lblResumenTipo;
        private readonly Label _lblResumenNoches;
        private readonly Label _lblResumenTarifa;
        private readonly Label _lblResumenTotal;

        // --- Datos calculados para el ticket ---
        private decimal _montoTotal;
        private decimal _tarifaDiaria;
        private int _noches;
        private string _tipoHabitacion = string.Empty;

        public string NombreCompleto { get; private set; } = string.Empty;
        public DateTime HoraIngreso { get; private set; }

        public FCheckIn(string nroHabitacion, string dniUsuario)
        {
            if (!int.TryParse(nroHabitacion, out _nroHabitacion))
            {
                _nroHabitacion = 0;
            }

            _dniUsuario = dniUsuario ?? string.Empty;

            Size = new Size(520, 860);
            EstablecerTitulo($"Check-in — Habitación {nroHabitacion}");

            const int ancho = 440;
            int y = 0;

            // Selector de Huéspedes Registrados
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Seleccionar Huésped existente", new Point(0, y)));
            _cmbHuespedes = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbHuespedes.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbHuespedes.SelectedIndexChanged += CmbHuespedes_SelectedIndexChanged;
            Contenido.Controls.Add(_cmbHuespedes);
            y += 54;

            _lblDetalleHuesped = new Label
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 40),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(6),
                Text = "Seleccione un huésped o complete los datos a continuación."
            };
            Contenido.Controls.Add(_lblDetalleHuesped);
            y += 48;

            // Datos del Huésped
            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(0, y)));
            _txtDni = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 22), ancho), Huesped.LargoDni);
            _txtDni.Leave += (s, e) => BuscarHuespedPorDni();
            Contenido.Controls.Add(_txtDni);
            y += 50;

            _lblEncontrado = new Label { Location = new Point(0, y), AutoSize = true, Font = Paleta.FuenteChica, Visible = false };
            Contenido.Controls.Add(_lblEncontrado);
            y += 20;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtNombre.MaxLength = Huesped.LargoNombre;
            Contenido.Controls.Add(_txtNombre);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(0, y)));
            _txtApellido = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtApellido.MaxLength = Huesped.LargoApellido;
            Contenido.Controls.Add(_txtApellido);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(0, y)));
            _txtTelefono = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 22), ancho), Huesped.LargoTelefono);
            Contenido.Controls.Add(_txtTelefono);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(0, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtDireccion.MaxLength = Huesped.LargoDireccion;
            Contenido.Controls.Add(_txtDireccion);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Correo electrónico", new Point(0, y)));
            _txtCorreo = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtCorreo.MaxLength = Huesped.LargoCorreo;
            Contenido.Controls.Add(_txtCorreo);
            y += 50;

            // Método de Pago
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Método de pago", new Point(0, y)));
            _cmbMetodoPago = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbMetodoPago.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPago.ValueMember = nameof(MetodoPago.IdMetodo);
            _cmbMetodoPago.DataSource = _gestionHospedajes.ObtenerMetodosPago() ?? new List<MetodoPago>();
            Contenido.Controls.Add(_cmbMetodoPago);
            y += 50;

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
            _dtpFechaSalida.ValueChanged += (s, e) => ActualizarResumenCosto();
            Contenido.Controls.Add(_dtpFechaSalida);
            y += 50;

            // Hora Estimada de Salida
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Hora estimada de salida", new Point(0, y)));
            _dtpHoraSalida = new DateTimePicker
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 30),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now
            };
            Contenido.Controls.Add(_dtpHoraSalida);
            y += 54;

            // -------------------------------------------------------
            // Panel resumen de costo estimado
            // -------------------------------------------------------
            _pnlResumen = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 110),
                BackColor = Color.FromArgb(224, 234, 253),   // PrimarioSuave
                Padding = new Padding(10)
            };

            var lblResumenTitulo = new Label
            {
                Text = "💰 Resumen de costo estimado",
                Location = new Point(10, 8),
                Size = new Size(ancho - 20, 20),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                BackColor = Color.Transparent
            };

            _lblResumenTipo = new Label
            {
                Location = new Point(10, 30),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblResumenNoches = new Label
            {
                Location = new Point(10, 50),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblResumenTarifa = new Label
            {
                Location = new Point(10, 68),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblResumenTotal = new Label
            {
                Location = new Point(10, 86),
                Size = new Size(ancho - 20, 20),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.Primario,
                BackColor = Color.Transparent
            };

            _pnlResumen.Controls.Add(lblResumenTitulo);
            _pnlResumen.Controls.Add(_lblResumenTipo);
            _pnlResumen.Controls.Add(_lblResumenNoches);
            _pnlResumen.Controls.Add(_lblResumenTarifa);
            _pnlResumen.Controls.Add(_lblResumenTotal);
            Contenido.Controls.Add(_pnlResumen);
            y += 118;

            // Mensajes de Error
            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            // Botones de Acción
            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 40), Location = new Point(0, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button { Text = "Confirmar Check-in", Size = new Size(165, 40), Location = new Point(ancho - 165, y) });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);

            CargarHuespedesDisponibles();
            ActualizarResumenCosto();
        }

        // ---------------------------------------------------------------
        //  Actualización del panel de resumen
        // ---------------------------------------------------------------

        private void ActualizarResumenCosto()
        {
            try
            {
                var (monto, tarifa, noches, tipo) = _gestionHospedajes.CalcularMontoEstadia(
                    _nroHabitacion, _dtpFechaSalida.Value);

                _montoTotal = monto;
                _tarifaDiaria = tarifa;
                _noches = noches;
                _tipoHabitacion = tipo;

                var hab = Datos.HabitacionDAO.ObtenerPorNumero(_nroHabitacion);
                string detalleHab = hab != null ? $"  ·  Piso {hab.Piso}  ·  {hab.CantCamas} {(hab.CantCamas == 1 ? "cama" : "camas")}" : string.Empty;
                _lblResumenTipo.Text = $"Habitación {_nroHabitacion}  —  Tipo: {tipo}{detalleHab}";
                _lblResumenNoches.Text = $"Noches: {noches}  (entrada hoy → salida {_dtpFechaSalida.Value:dd/MM/yyyy})";
                _lblResumenTarifa.Text = $"Tarifa diaria: {tarifa:C}";
                _lblResumenTotal.Text = $"Total estimado: {monto:C}";
                _pnlResumen.BackColor = Color.FromArgb(224, 234, 253);   // PrimarioSuave
            }
            catch
            {
                _lblResumenTipo.Text = string.Empty;
                _lblResumenNoches.Text = string.Empty;
                _lblResumenTarifa.Text = string.Empty;
                _lblResumenTotal.Text = "No se pudo calcular el costo.";
                _pnlResumen.BackColor = Color.FromArgb(255, 243, 205);   // AdvertenciaSuave
            }
        }

        // ---------------------------------------------------------------
        //  Carga del combo de huéspedes
        // ---------------------------------------------------------------

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
                        _txtDni.Text = h.DniHuesped;
                        _txtNombre.Text = h.Nombre;
                        _txtApellido.Text = h.Apellido;
                        _txtTelefono.Text = h.Telefono;
                        _txtDireccion.Text = h.Direccion;
                        _txtCorreo.Text = h.Correo;

                        _lblDetalleHuesped.Text = $"Registrado: {h.Nombre} {h.Apellido} (DNI: {h.DniHuesped})";
                        return;
                    }
                }
                catch
                {
                    // Fallback si no se recupera el objeto
                }
            }

            if (_cmbHuespedes.Enabled)
            {
                _lblDetalleHuesped.Text = "Sin huésped seleccionado.";
            }
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

        // ---------------------------------------------------------------
        //  Confirmación y emisión del ticket
        // ---------------------------------------------------------------

        private void Confirmar()
        {
            _lblError.Visible = false;

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

            int idMetodo = Convert.ToInt32(_cmbMetodoPago.SelectedValue);
            string nombreMetodo = (_cmbMetodoPago.SelectedItem as MetodoPago)?.NomMetodoPago
                ?? _cmbMetodoPago.Text;

            if (new GestionTurnoCaja().ObtenerTurnoAbierto(_dniUsuario) == null)
            {
                MessageBox.Show("La caja se encuentra cerrada. Debe abrir la caja antes de registrar un Check-in y realizar el cobro.", "Caja cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    _dtpHoraSalida.Value.TimeOfDay,
                    _montoTotal
                );

                NombreCompleto = $"{huesped.Nombre} {huesped.Apellido}".Trim();
                HoraIngreso = DateTime.Now;

                // Recalcular para asegurarse de tener los valores actualizados
                ActualizarResumenCosto();

                EmitirTicketCheckIn(huesped, nombreMetodo);

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

        /// <summary>
        /// Genera el PDF del comprobante de hospedaje, lo guarda en la carpeta de Documentos
        /// del usuario y lo abre automáticamente con el visor PDF predeterminado.
        /// </summary>
        private void EmitirTicketCheckIn(Huesped huesped, string nombreMetodo)
        {
            try
            {
                byte[] pdfBytes = GeneradorTicketPdf.GenerarBytesCheckIn(
                    nombreHuesped: $"{huesped.Nombre} {huesped.Apellido}",
                    dniHuesped: huesped.DniHuesped,
                    nroHabitacion: _nroHabitacion,
                    tipoHabitacion: _tipoHabitacion,
                    fechaEntrada: HoraIngreso,
                    fechaSalida: _dtpFechaSalida.Value,
                    horaSalida: _dtpHoraSalida.Value.TimeOfDay,
                    tarifaDiaria: _tarifaDiaria,
                    noches: _noches,
                    montoTotal: _montoTotal,
                    metodoPago: nombreMetodo,
                    emitidoPor: _dniUsuario
                );

                string carpeta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string nombreArchivo = $"CheckIn_{_nroHabitacion}_{huesped.DniHuesped}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

                File.WriteAllBytes(rutaArchivo, pdfBytes);

                // Abrir el PDF con el visor predeterminado del sistema
                Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });

                MessageBox.Show(
                    $"Comprobante de hospedaje generado correctamente.\n\nArchivo: {nombreArchivo}\nUbicación: {carpeta}",
                    "Ticket emitido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"El check-in se registró correctamente, pero no se pudo generar el ticket PDF.\n\n{ex.Message}",
                    "Advertencia",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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