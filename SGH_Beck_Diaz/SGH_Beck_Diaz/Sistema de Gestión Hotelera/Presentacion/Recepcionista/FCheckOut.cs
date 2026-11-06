using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Check-out de una habitación ocupada con evaluación de vencimiento y liquidación final:
    /// - Evalúa el tiempo transcurrido desde la fecha y hora pactadas de salida.
    /// - Aplica margen de tolerancia de 2 horas (sin recargo ni días extra).
    /// - Si supera las 2 horas, calcula y cobra el/los día(s) extra según la tarifa diaria.
    /// - Muestra el detalle del recargo por días excedidos desglosado antes de confirmar.
    /// - Emite el ticket final de liquidación detallando días pactados originales, días extras
    ///   cobrados por vencimiento, consumos/ventas adicionales y el importe total cancelado.
    /// </summary>
    internal class FCheckOut : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly Usuario? _usuario;
        private readonly GestionHospedajes _gestionHospedajes = new();

        // Datos precargados
        private Huesped? _huesped;
        private Hospedaje? _hospedaje;
        private LiquidacionCheckOut? _liquidacion;
        private string _correoHuesped = string.Empty;

        // Controles de interfaz
        private readonly Label _lblIngresoValor;
        private readonly Label _lblSalidaPactadaValor;
        private readonly Label _lblSalidaRealValor;

        // Panel de vencimiento / tolerancia
        private readonly Panel _pnlVencimiento;
        private readonly Label _lblEstadoVencimiento;
        private readonly Label _lblDetalleVencimiento;

        // Grilla de consumos
        private readonly DataGridView _grillaConsumos;
        private readonly Label _lblTotalConsumos;

        // Selector método de pago recargo
        private readonly Label _lblMetodoPagoTitulo;
        private readonly ComboBox _cmbMetodoPagoRecargo;

        // Panel de liquidación final
        private readonly Panel _pnlLiquidacion;
        private readonly Label _lblLiqDiasOriginales;
        private readonly Label _lblLiqDiasExtras;
        private readonly Label _lblLiqConsumos;
        private readonly Label _lblLiqTotalFinal;
        private readonly Label _lblLiqSaldoAbonar;

        // Datos de huésped
        private readonly TextBox _txtDni;
        private readonly TextBox _txtNombre;
        private readonly TextBox _txtApellido;
        private readonly TextBox _txtTelefono;
        private readonly TextBox _txtDireccion;
        private readonly Label _lblError;

        public string NombreCompleto { get; private set; } = string.Empty;

        public FCheckOut(string nroHabitacion) : this(nroHabitacion, null)
        {
        }

        public FCheckOut(string nroHabitacion, Usuario? usuario)
        {
            _nroHabitacion = int.Parse(nroHabitacion);
            _usuario = usuario;

            Size = new Size(540, 920);
            EstablecerTitulo($"Check-out — Habitación {nroHabitacion}");

            const int ancho = 460;
            int y = 0;

            // 1. Fechas y Horarios de la estadía
            var pnlFechas = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 72),
                BackColor = Paleta.FondoApp,
                Padding = new Padding(10)
            };

            var lblIngresoTit = new Label { Text = "Ingreso:", Location = new Point(10, 8), AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario };
            _lblIngresoValor = new Label { Location = new Point(10, 24), AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario };

            var lblSalidaPactadaTit = new Label { Text = "Salida pactada:", Location = new Point(155, 8), AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario };
            _lblSalidaPactadaValor = new Label { Location = new Point(155, 24), AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario };

            var lblSalidaRealTit = new Label { Text = "Check-out (actual):", Location = new Point(310, 8), AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario };
            _lblSalidaRealValor = new Label { Location = new Point(310, 24), AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.Primario };

            pnlFechas.Controls.Add(lblIngresoTit);
            pnlFechas.Controls.Add(_lblIngresoValor);
            pnlFechas.Controls.Add(lblSalidaPactadaTit);
            pnlFechas.Controls.Add(_lblSalidaPactadaValor);
            pnlFechas.Controls.Add(lblSalidaRealTit);
            pnlFechas.Controls.Add(_lblSalidaRealValor);
            Contenido.Controls.Add(pnlFechas);
            y += 80;

            // 2. Panel de Evaluación de Vencimiento y Tolerancia de 2 horas
            _pnlVencimiento = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 74),
                BackColor = Paleta.ExitoSuave,
                Padding = new Padding(10)
            };

            _lblEstadoVencimiento = new Label
            {
                Location = new Point(10, 8),
                Size = new Size(ancho - 20, 22),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.Exito,
                BackColor = Color.Transparent
            };

            _lblDetalleVencimiento = new Label
            {
                Location = new Point(10, 32),
                Size = new Size(ancho - 20, 36),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _pnlVencimiento.Controls.Add(_lblEstadoVencimiento);
            _pnlVencimiento.Controls.Add(_lblDetalleVencimiento);
            Contenido.Controls.Add(_pnlVencimiento);
            y += 82;

            // 3. Consumos durante la estadía
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Consumos durante la estadía", new Point(0, y)));
            _grillaConsumos = new DataGridView
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 115),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grillaConsumos);
            _grillaConsumos.RowTemplate.Height = 26;
            _grillaConsumos.ColumnHeadersHeight = 28;
            _grillaConsumos.Columns.Add("fecha", "Fecha");
            _grillaConsumos.Columns.Add("productos", "Productos");
            _grillaConsumos.Columns.Add("pago", "Pago");
            _grillaConsumos.Columns.Add("total", "Total");
            _grillaConsumos.Columns["fecha"].FillWeight = 85;
            _grillaConsumos.Columns["productos"].FillWeight = 160;
            _grillaConsumos.Columns["pago"].FillWeight = 75;
            _grillaConsumos.Columns["total"].FillWeight = 75;
            _grillaConsumos.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grillaConsumos.Columns["productos"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _grillaConsumos.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            Contenido.Controls.Add(_grillaConsumos);

            _lblTotalConsumos = new Label
            {
                Location = new Point(0, y + 140),
                AutoSize = true,
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                Text = "Sin consumos registrados."
            };
            Contenido.Controls.Add(_lblTotalConsumos);
            y += 166;

            // 4. Panel Resumen de Liquidación Final
            _pnlLiquidacion = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 138),
                BackColor = Paleta.PrimarioSuave,
                Padding = new Padding(12)
            };

            var lblLiqTitulo = new Label
            {
                Text = "📋 Liquidación final de estadía",
                Location = new Point(10, 8),
                Size = new Size(ancho - 20, 20),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                BackColor = Color.Transparent
            };

            _lblLiqDiasOriginales = new Label
            {
                Location = new Point(10, 32),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblLiqDiasExtras = new Label
            {
                Location = new Point(10, 52),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblLiqConsumos = new Label
            {
                Location = new Point(10, 72),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblLiqTotalFinal = new Label
            {
                Location = new Point(10, 92),
                Size = new Size(ancho - 20, 20),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.Primario,
                BackColor = Color.Transparent
            };

            _lblLiqSaldoAbonar = new Label
            {
                Location = new Point(10, 114),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.Peligro,
                BackColor = Color.Transparent
            };

            _pnlLiquidacion.Controls.Add(lblLiqTitulo);
            _pnlLiquidacion.Controls.Add(_lblLiqDiasOriginales);
            _pnlLiquidacion.Controls.Add(_lblLiqDiasExtras);
            _pnlLiquidacion.Controls.Add(_lblLiqConsumos);
            _pnlLiquidacion.Controls.Add(_lblLiqTotalFinal);
            _pnlLiquidacion.Controls.Add(_lblLiqSaldoAbonar);
            Contenido.Controls.Add(_pnlLiquidacion);
            y += 148;

            // 5. Método de Pago para el recargo (visible si DiasExtras > 0)
            _lblMetodoPagoTitulo = CamposFormulario.Etiqueta("Método de pago para el recargo de días extras:", new Point(0, y));
            _lblMetodoPagoTitulo.Visible = false;
            Contenido.Controls.Add(_lblMetodoPagoTitulo);

            _cmbMetodoPagoRecargo = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbMetodoPagoRecargo.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPagoRecargo.ValueMember = nameof(MetodoPago.IdMetodo);
            _cmbMetodoPagoRecargo.DataSource = _gestionHospedajes.ObtenerMetodosPago() ?? new List<MetodoPago>();
            _cmbMetodoPagoRecargo.Visible = false;
            Contenido.Controls.Add(_cmbMetodoPagoRecargo);
            y += 56;

            // 6. Datos del Huésped (confirmación / edición de contacto)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(0, y)));
            _txtDni = CamposFormulario.Texto(new Point(0, y + 20), ancho);
            _txtDni.ReadOnly = true;
            _txtDni.TabStop = false;
            _txtDni.BackColor = Paleta.FondoApp;
            Contenido.Controls.Add(_txtDni);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 20), ancho);
            Contenido.Controls.Add(_txtNombre);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(0, y)));
            _txtApellido = CamposFormulario.Texto(new Point(0, y + 20), ancho);
            Contenido.Controls.Add(_txtApellido);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(0, y)));
            _txtTelefono = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 20), ancho), Huesped.LargoTelefono);
            Contenido.Controls.Add(_txtTelefono);
            y += 50;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(0, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(0, y + 20), ancho);
            Contenido.Controls.Add(_txtDireccion);
            y += 58;

            // 7. Error y Botones
            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 28;

            var btnCancelar = EstiloBoton.Secundario(new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(ancho - 120 - 200 - 12, y)
            });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button
            {
                Text = "Confirmar check-out",
                Size = new Size(200, 40),
                Location = new Point(ancho - 200, y)
            });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);

            Load += (s, e) => CargarDatos();
        }

        private void CargarDatos()
        {
            var activo = _gestionHospedajes.ObtenerHuespedActivoPorHabitacion(_nroHabitacion);
            if (activo == null)
            {
                _lblError.Text = "No se encontró el hospedaje vigente de esta habitación.";
                _lblError.Visible = true;
                return;
            }

            _huesped = activo.Value.Huesped;
            _hospedaje = activo.Value.Hospedaje;

            _txtDni.Text = _huesped.DniHuesped;
            _txtNombre.Text = _huesped.Nombre;
            _txtApellido.Text = _huesped.Apellido;
            _txtTelefono.Text = _huesped.Telefono;
            _txtDireccion.Text = _huesped.Direccion;
            _correoHuesped = _huesped.Correo;

            DateTime entrada = _hospedaje.FechaEntrada.Date + _hospedaje.HoraEntrada;
            DateTime pactada = _hospedaje.FechaSalida.Date + _hospedaje.HoraSalida;
            DateTime salidaReal = DateTime.Now;

            _lblIngresoValor.Text = entrada.ToString("dd/MM/yy HH:mm");
            _lblSalidaPactadaValor.Text = pactada.ToString("dd/MM/yy HH:mm");
            _lblSalidaRealValor.Text = salidaReal.ToString("dd/MM/yy HH:mm");

            // Evaluar liquidación con regla de tolerancia de 2 horas y cálculo diario
            try
            {
                _liquidacion = _gestionHospedajes.EvaluarLiquidacionCheckOut(_nroHabitacion, salidaReal);
            }
            catch (Exception ex)
            {
                _lblError.Text = $"Error al evaluar la liquidación: {ex.Message}";
                _lblError.Visible = true;
                return;
            }

            // 1. Mostrar estado de vencimiento y tolerancia
            if (_liquidacion.Retraso <= TimeSpan.Zero)
            {
                _pnlVencimiento.BackColor = Paleta.ExitoSuave;
                _lblEstadoVencimiento.Text = "✅ Salida en fecha y hora acordada";
                _lblEstadoVencimiento.ForeColor = Paleta.Exito;
                _lblDetalleVencimiento.Text = "El huésped realiza el check-out puntualmente. Sin recargo.";
            }
            else if (!_liquidacion.SuperaTolerancia)
            {
                _pnlVencimiento.BackColor = Color.FromArgb(230, 246, 255);
                _lblEstadoVencimiento.Text = $"⏱️ Retraso de {_liquidacion.Retraso:hh\\:mm} — Dentro de la tolerancia (<= 2 horas)";
                _lblEstadoVencimiento.ForeColor = Color.FromArgb(14, 116, 144);
                _lblDetalleVencimiento.Text = "El retraso está cubierto por el margen de cortesía de 2 horas. No se cobra penalización ni días extras.";
            }
            else
            {
                int horasTotales = (int)_liquidacion.Retraso.TotalHours;
                int minutos = _liquidacion.Retraso.Minutes;

                _pnlVencimiento.BackColor = Paleta.PeligroSuave;
                _lblEstadoVencimiento.Text = $"⚠️ Estadía vencida por {horasTotales}h {minutos}m (Tolerancia de 2 hs superada)";
                _lblEstadoVencimiento.ForeColor = Paleta.Peligro;
                _lblDetalleVencimiento.Text = $"Días extras cobrados: {_liquidacion.DiasExtras} día(s) × {_liquidacion.TarifaDiaria:C} = {_liquidacion.MontoDiasExtras:C} de recargo.";
            }

            // 2. Cargar grilla de consumos
            _grillaConsumos.Rows.Clear();
            foreach (VentaRealizada venta in _liquidacion.Consumos)
            {
                _grillaConsumos.Rows.Add(venta.Momento.ToString("dd/MM HH:mm"), venta.ResumenProductos, venta.MetodoPago, venta.Total.ToString("C"));
            }

            _lblTotalConsumos.Text = _liquidacion.Consumos.Count == 0
                ? "Sin consumos registrados durante la estadía."
                : $"Total consumido: {_liquidacion.TotalConsumos:C} en {_liquidacion.Consumos.Count} venta(s)";

            // 3. Desglose en Panel de Liquidación
            _lblLiqDiasOriginales.Text = $"• Hospedaje original ({_liquidacion.DiasPactadosOriginales} noches): {_liquidacion.MontoHospedajeOriginal:C}";
            _lblLiqDiasExtras.Text = _liquidacion.DiasExtras > 0
                ? $"• Recargo por días extras ({_liquidacion.DiasExtras} días): {_liquidacion.MontoDiasExtras:C}"
                : "• Recargo por días extras: $0,00 (dentro de tolerancia)";
            _lblLiqConsumos.Text = $"• Consumos adicionales: {_liquidacion.TotalConsumos:C}";
            _lblLiqTotalFinal.Text = $"• TOTAL CANCELADO DE LA ESTADÍA: {_liquidacion.TotalLiquidacion:C}";

            if (_liquidacion.DiasExtras > 0)
            {
                _lblLiqSaldoAbonar.Text = $"⚠️ Saldo a abonar en este acto (recargo días extras): {_liquidacion.MontoDiasExtras:C}";
                _lblLiqSaldoAbonar.Visible = true;

                _lblMetodoPagoTitulo.Visible = true;
                _cmbMetodoPagoRecargo.Visible = true;
            }
            else
            {
                _lblLiqSaldoAbonar.Text = "✅ Sin saldos pendientes a abonar.";
                _lblLiqSaldoAbonar.ForeColor = Paleta.Exito;
                _lblLiqSaldoAbonar.Visible = true;

                _lblMetodoPagoTitulo.Visible = false;
                _cmbMetodoPagoRecargo.Visible = false;
            }
        }

        private void Confirmar()
        {
            if (_huesped == null || _hospedaje == null || _liquidacion == null)
            {
                _lblError.Text = "Datos de estadía no disponibles.";
                _lblError.Visible = true;
                return;
            }

            if (!ValidarCampos(out string mensaje, out Control controlInvalido))
            {
                _lblError.Text = mensaje;
                _lblError.Visible = true;
                controlInvalido.Focus();
                return;
            }

            int? idMetodoExtras = null;
            string? nombreMetodoExtras = null;

            if (_liquidacion.DiasExtras > 0)
            {
                if (_cmbMetodoPagoRecargo.SelectedValue == null)
                {
                    _lblError.Text = "Debe seleccionar un método de pago para el recargo de días extras.";
                    _lblError.Visible = true;
                    _cmbMetodoPagoRecargo.Focus();
                    return;
                }

                idMetodoExtras = Convert.ToInt32(_cmbMetodoPagoRecargo.SelectedValue);
                nombreMetodoExtras = (_cmbMetodoPagoRecargo.SelectedItem as MetodoPago)?.NomMetodoPago ?? _cmbMetodoPagoRecargo.Text;

                string dniTurno = _usuario?.DniUsuario ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(dniTurno) && new GestionTurnoCaja().ObtenerTurnoAbierto(dniTurno) == null)
                {
                    MessageBox.Show("La caja se encuentra cerrada. Debe abrir la caja antes de registrar un Check-out con cobro de recargo por estadía vencida.", "Caja cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var huespedActualizado = new Huesped
            {
                DniHuesped = _txtDni.Text.Trim(),
                Nombre = _txtNombre.Text.Trim(),
                Apellido = _txtApellido.Text.Trim(),
                Telefono = _txtTelefono.Text.Trim(),
                Direccion = _txtDireccion.Text.Trim(),
                Correo = _correoHuesped
            };

            try
            {
                _gestionHospedajes.ActualizarHuesped(huespedActualizado);
                _gestionHospedajes.RegistrarCheckOutConLiquidacion(_nroHabitacion, _usuario?.DniUsuario, _liquidacion, idMetodoExtras);

                NombreCompleto = $"{huespedActualizado.Nombre} {huespedActualizado.Apellido}";

                // Emite el ticket final de liquidación
                EmitirTicketLiquidacion(nombreMetodoExtras);

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
                MessageBox.Show($"Ocurrió un error al registrar el check-out.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EmitirTicketLiquidacion(string? metodoPagoExtras)
        {
            try
            {
                byte[] pdfBytes = GeneradorTicketPdf.GenerarBytesLiquidacionCheckOut(
                    liquidacion: _liquidacion!,
                    emitidoPor: _usuario?.DniUsuario,
                    metodoPagoExtras: metodoPagoExtras
                );

                string carpeta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string nombreArchivo = $"Liquidacion_CheckOut_{_nroHabitacion}_{_liquidacion!.DniHuesped}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

                File.WriteAllBytes(rutaArchivo, pdfBytes);

                Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });

                MessageBox.Show(
                    $"Check-out finalizado con éxito.\nTicket final de liquidación emitido correctamente.\n\nArchivo: {nombreArchivo}\nUbicación: {carpeta}",
                    "Liquidación Check-Out",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"El check-out se registró correctamente en el sistema, pero no se pudo generar el ticket PDF.\n\n{ex.Message}",
                    "Advertencia",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private bool ValidarCampos(out string mensaje, out Control controlInvalido)
        {
            (string valor, string etiqueta, Control control)[] requeridos =
            {
                (_txtNombre.Text, "el nombre", _txtNombre),
                (_txtApellido.Text, "el apellido", _txtApellido),
                (_txtTelefono.Text, "el teléfono", _txtTelefono),
                (_txtDireccion.Text, "la dirección", _txtDireccion),
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

            mensaje = string.Empty;
            controlInvalido = _txtNombre;
            return true;
        }
    }
}
