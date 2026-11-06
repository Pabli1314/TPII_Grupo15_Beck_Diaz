using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Modales
{
    /// <summary>
    /// Modal para extender la estadía de una habitación ocupada.
    /// Precarga los datos de la estadía actual (huésped, habitación, fecha de salida actual),
    /// permite seleccionar nueva fecha/hora y método de pago, calcula automáticamente el importe
    /// adicional según la tarifa diaria y emite el ticket comprobante de prórroga.
    /// </summary>
    internal class FModalExtenderEstadia : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly string _dniUsuario;
        private readonly GestionHospedajes _gestionHospedajes = new();

        // Datos precargados
        private Huesped? _huesped;
        private Hospedaje? _hospedaje;
        private string _tipoHabitacion = string.Empty;
        private decimal _tarifaDiaria;
        private DateTime _fechaSalidaActual;
        private TimeSpan _horaSalidaActual;

        // Controles de interfaz
        private readonly Label _lblInfoHuesped;
        private readonly Label _lblInfoHabitacion;
        private readonly Label _lblSalidaActual;
        private readonly DateTimePicker _dtpNuevaFechaSalida;
        private readonly DateTimePicker _dtpNuevaHoraSalida;
        private readonly ComboBox _cmbMetodoPago;
        private readonly Label _lblError;

        // Panel de resumen
        private readonly Panel _pnlResumen;
        private readonly Label _lblResumenNoches;
        private readonly Label _lblResumenTarifa;
        private readonly Label _lblResumenImporteAdicional;
        private readonly Label _lblResumenTotalAcumulado;

        // Cálculos
        private int _diasAdicionales;
        private decimal _montoAdicional;
        private decimal _nuevoMontoTotal;

        public FModalExtenderEstadia(string nroHabitacion, string dniUsuario)
        {
            if (!int.TryParse(nroHabitacion, out _nroHabitacion))
            {
                _nroHabitacion = 0;
            }

            _dniUsuario = dniUsuario ?? string.Empty;

            Size = new Size(500, 780);
            EstablecerTitulo($"Extender Estadía — Habitación {nroHabitacion}");

            const int ancho = 420;
            int y = 0;

            // 1. Tarjeta informativa de la estadía actual
            var pnlInfo = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 115),
                BackColor = Paleta.FondoApp,
                Padding = new Padding(12)
            };

            var lblTituloEstadia = new Label
            {
                Text = "DATOS ACTUALES DE LA ESTADÍA",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                Location = new Point(12, 10),
                AutoSize = true
            };

            _lblInfoHuesped = new Label
            {
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                Location = new Point(12, 32),
                AutoSize = true
            };

            _lblInfoHabitacion = new Label
            {
                Font = Paleta.FuenteBase,
                ForeColor = Paleta.TextoSecundario,
                Location = new Point(12, 58),
                AutoSize = true
            };

            _lblSalidaActual = new Label
            {
                Font = Paleta.FuenteBase,
                ForeColor = Paleta.TextoSecundario,
                Location = new Point(12, 84),
                AutoSize = true
            };

            pnlInfo.Controls.Add(lblTituloEstadia);
            pnlInfo.Controls.Add(_lblInfoHuesped);
            pnlInfo.Controls.Add(_lblInfoHabitacion);
            pnlInfo.Controls.Add(_lblSalidaActual);
            Contenido.Controls.Add(pnlInfo);
            y += 128;

            // 2. Selección de Nueva Fecha de Salida
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nueva fecha de salida", new Point(0, y)));
            _dtpNuevaFechaSalida = new DateTimePicker
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 30),
                Format = DateTimePickerFormat.Short,
                Font = Paleta.FuenteBase
            };
            _dtpNuevaFechaSalida.ValueChanged += (s, e) => RecalcularImporte();
            Contenido.Controls.Add(_dtpNuevaFechaSalida);
            y += 56;

            // 3. Selección de Nueva Hora Estimada de Salida
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nueva hora estimada de salida", new Point(0, y)));
            _dtpNuevaHoraSalida = new DateTimePicker
            {
                Location = new Point(0, y + 22),
                Size = new Size(ancho, 30),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = Paleta.FuenteBase,
                Value = DateTime.Now
            };
            _dtpNuevaHoraSalida.ValueChanged += (s, e) => RecalcularImporte();
            Contenido.Controls.Add(_dtpNuevaHoraSalida);
            y += 56;

            // 4. Método de Pago para los días adicionales
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Método de pago para los días adicionales", new Point(0, y)));
            _cmbMetodoPago = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbMetodoPago.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPago.ValueMember = nameof(MetodoPago.IdMetodo);
            _cmbMetodoPago.DataSource = _gestionHospedajes.ObtenerMetodosPago() ?? new List<MetodoPago>();
            Contenido.Controls.Add(_cmbMetodoPago);
            y += 58;

            // 5. Panel Resumen de Costo de la Prórroga
            _pnlResumen = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 130),
                BackColor = Paleta.PrimarioSuave,
                Padding = new Padding(12)
            };

            var lblResumenTitulo = new Label
            {
                Text = "💰 Resumen de prórroga de estadía",
                Location = new Point(10, 8),
                Size = new Size(ancho - 20, 20),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                BackColor = Color.Transparent
            };

            _lblResumenNoches = new Label
            {
                Location = new Point(10, 32),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblResumenTarifa = new Label
            {
                Location = new Point(10, 52),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _lblResumenImporteAdicional = new Label
            {
                Location = new Point(10, 74),
                Size = new Size(ancho - 20, 22),
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.Primario,
                BackColor = Color.Transparent
            };

            _lblResumenTotalAcumulado = new Label
            {
                Location = new Point(10, 98),
                Size = new Size(ancho - 20, 18),
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent
            };

            _pnlResumen.Controls.Add(lblResumenTitulo);
            _pnlResumen.Controls.Add(_lblResumenNoches);
            _pnlResumen.Controls.Add(_lblResumenTarifa);
            _pnlResumen.Controls.Add(_lblResumenImporteAdicional);
            _pnlResumen.Controls.Add(_lblResumenTotalAcumulado);
            Contenido.Controls.Add(_pnlResumen);
            y += 140;

            // 6. Mensaje de error / validación
            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 28;

            // 7. Botones de acción
            var btnCancelar = EstiloBoton.Secundario(new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(0, y)
            });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button
            {
                Text = "Confirmar prórroga",
                Size = new Size(180, 40),
                Location = new Point(ancho - 180, y)
            });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);

            Load += (s, e) => CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            var activo = _gestionHospedajes.ObtenerHuespedActivoPorHabitacion(_nroHabitacion);
            if (activo == null)
            {
                MessageBox.Show("No se encontró el hospedaje activo de esta habitación.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _huesped = activo.Value.Huesped;
            _hospedaje = activo.Value.Hospedaje;

            _fechaSalidaActual = _hospedaje.FechaSalida;
            _horaSalidaActual = _hospedaje.HoraSalida;

            // Precarga de controles informativos
            _lblInfoHuesped.Text = $"Huésped: {_huesped.Nombre} {_huesped.Apellido} (DNI: {_huesped.DniHuesped})";

            // Obtener tarifa y tipo
            try
            {
                var (_, tarifa, _, tipo, _, _) = _gestionHospedajes.CalcularProrrogaEstadia(_nroHabitacion, _fechaSalidaActual.AddDays(1));
                _tipoHabitacion = tipo;
                _tarifaDiaria = tarifa;
                _lblInfoHabitacion.Text = $"Habitación {_nroHabitacion} — {tipo} | Tarifa diaria: {tarifa:C}";
            }
            catch
            {
                _lblInfoHabitacion.Text = $"Habitación {_nroHabitacion}";
            }

            DateTime salidaActualTotal = _fechaSalidaActual.Date + _horaSalidaActual;
            _lblSalidaActual.Text = $"Salida pactada actual: {salidaActualTotal:dd/MM/yyyy HH:mm}";

            // Configurar fecha mínima y sugerida para la nueva salida
            _dtpNuevaFechaSalida.MinDate = _fechaSalidaActual.Date;
            _dtpNuevaFechaSalida.Value = _fechaSalidaActual.Date.AddDays(1);
            _dtpNuevaHoraSalida.Value = DateTime.Today + _horaSalidaActual;

            RecalcularImporte();
        }

        private void RecalcularImporte()
        {
            if (_hospedaje == null) return;

            DateTime nuevaFecha = _dtpNuevaFechaSalida.Value.Date;
            TimeSpan nuevaHora = _dtpNuevaHoraSalida.Value.TimeOfDay;
            DateTime nuevaSalidaTotal = nuevaFecha + nuevaHora;
            DateTime salidaActualTotal = _fechaSalidaActual.Date + _horaSalidaActual;

            _diasAdicionales = (nuevaFecha - _fechaSalidaActual.Date).Days;

            if (_diasAdicionales < 0) _diasAdicionales = 0;

            _montoAdicional = _diasAdicionales * _tarifaDiaria;
            _nuevoMontoTotal = _hospedaje.MontoTotal + _montoAdicional;

            if (nuevaSalidaTotal <= salidaActualTotal)
            {
                _lblResumenNoches.Text = "La nueva salida debe ser posterior a la salida actual.";
                _lblResumenTarifa.Text = string.Empty;
                _lblResumenImporteAdicional.Text = "Importe adicional: $0,00";
                _lblResumenTotalAcumulado.Text = string.Empty;
                _pnlResumen.BackColor = Paleta.AdvertenciaSuave;
                return;
            }

            if (_diasAdicionales <= 0)
            {
                _lblResumenNoches.Text = "Seleccione al menos 1 día adicional de estadía.";
                _lblResumenTarifa.Text = $"Tarifa diaria: {_tarifaDiaria:C}";
                _lblResumenImporteAdicional.Text = "Importe adicional: $0,00";
                _lblResumenTotalAcumulado.Text = $"Monto actual: {_hospedaje.MontoTotal:C}";
                _pnlResumen.BackColor = Paleta.AdvertenciaSuave;
                return;
            }

            _lblResumenNoches.Text = $"Nuevas noches adicionales: {_diasAdicionales} noche(s)";
            _lblResumenTarifa.Text = $"Tarifa diaria: {_tarifaDiaria:C}";
            _lblResumenImporteAdicional.Text = $"Importe adicional a abonar: {_montoAdicional:C}";
            _lblResumenTotalAcumulado.Text = $"Monto total acumulado: {_nuevoMontoTotal:C} (anterior {_hospedaje.MontoTotal:C} + adicional {_montoAdicional:C})";
            _pnlResumen.BackColor = Paleta.PrimarioSuave;
            _lblError.Visible = false;
        }

        private void Confirmar()
        {
            _lblError.Visible = false;

            if (_huesped == null || _hospedaje == null)
            {
                _lblError.Text = "Datos de estadía no disponibles.";
                _lblError.Visible = true;
                return;
            }

            DateTime nuevaFecha = _dtpNuevaFechaSalida.Value.Date;
            TimeSpan nuevaHora = _dtpNuevaHoraSalida.Value.TimeOfDay;
            DateTime nuevaSalidaTotal = nuevaFecha + nuevaHora;
            DateTime salidaActualTotal = _fechaSalidaActual.Date + _horaSalidaActual;

            if (nuevaSalidaTotal <= salidaActualTotal)
            {
                _lblError.Text = "La nueva fecha y hora de salida deben ser posteriores a la salida actual.";
                _lblError.Visible = true;
                _dtpNuevaFechaSalida.Focus();
                return;
            }

            if (_diasAdicionales <= 0)
            {
                _lblError.Text = "Debe extender la estadía por al menos una (1) noche adicional.";
                _lblError.Visible = true;
                _dtpNuevaFechaSalida.Focus();
                return;
            }

            if (_cmbMetodoPago.SelectedValue == null)
            {
                _lblError.Text = "Debe seleccionar un método de pago para los días adicionales.";
                _lblError.Visible = true;
                _cmbMetodoPago.Focus();
                return;
            }

            if (new GestionTurnoCaja().ObtenerTurnoAbierto(_dniUsuario) == null)
            {
                MessageBox.Show("La caja se encuentra cerrada. Debe abrir la caja antes de registrar la extensión de estadía y realizar el cobro.", "Caja cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idMetodo = Convert.ToInt32(_cmbMetodoPago.SelectedValue);
            string nombreMetodo = (_cmbMetodoPago.SelectedItem as MetodoPago)?.NomMetodoPago ?? _cmbMetodoPago.Text;

            try
            {
                ProrrogaEstadia prorroga = _gestionHospedajes.RegistrarProrrogaEstadia(
                    _nroHabitacion,
                    _dniUsuario,
                    nuevaFecha,
                    nuevaHora,
                    idMetodo,
                    _montoAdicional,
                    _diasAdicionales
                );

                // Emite el ticket comprobante de prórroga
                EmitirTicketProrroga(nombreMetodo);

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
                MessageBox.Show($"Ocurrió un error al registrar la prórroga de estadía.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EmitirTicketProrroga(string nombreMetodo)
        {
            try
            {
                byte[] pdfBytes = GeneradorTicketPdf.GenerarBytesProrrogaEstadia(
                    nombreHuesped: $"{_huesped!.Nombre} {_huesped.Apellido}".Trim(),
                    dniHuesped: _huesped.DniHuesped,
                    nroHabitacion: _nroHabitacion,
                    tipoHabitacion: _tipoHabitacion,
                    fechaSalidaAnterior: _fechaSalidaActual,
                    horaSalidaAnterior: _horaSalidaActual,
                    nuevaFechaSalida: _dtpNuevaFechaSalida.Value.Date,
                    nuevaHoraSalida: _dtpNuevaHoraSalida.Value.TimeOfDay,
                    diasAdicionales: _diasAdicionales,
                    tarifaDiaria: _tarifaDiaria,
                    montoAdicional: _montoAdicional,
                    montoTotalAcumulado: _nuevoMontoTotal,
                    metodoPago: nombreMetodo,
                    emitidoPor: _dniUsuario
                );

                string carpeta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string nombreArchivo = $"Prorroga_{_nroHabitacion}_{_huesped.DniHuesped}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

                File.WriteAllBytes(rutaArchivo, pdfBytes);

                Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });

                MessageBox.Show(
                    $"Prórroga registrada con éxito.\nComprobante de prórroga generado correctamente.\n\nArchivo: {nombreArchivo}\nUbicación: {carpeta}",
                    "Ticket de Prórroga emitido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"La prórroga se registró correctamente en el sistema, pero no se pudo generar el ticket PDF.\n\n{ex.Message}",
                    "Advertencia",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
