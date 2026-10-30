using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Historial unificado de check-in/check-out y ventas adicionales, con filtros.</summary>
    internal class VistaHistorial : UserControl
    {
        private readonly GestionHistorialRecepcion _gestionHistorial = new();

        private readonly ComboBox _cmbTipo;
        private readonly TextBox _txtHuesped;
        private readonly NumericUpDown _numHabitacion;
        private readonly TextBox _txtUsuario;
        private readonly CheckBox _chkUsarFecha;
        private readonly DateTimePicker _dtpFecha;
        private readonly DataGridView _grilla;

        public VistaHistorial()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            // Panel superior con título y barra de filtros responsive
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 0, 0, 14)
            };

            var lblTitulo = new Label
            {
                Text = "Historial de operaciones",
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var panelFiltros = new FlowLayoutPanel
            {
                Location = new Point(0, 36),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                BackColor = Paleta.FondoApp
            };

            // 1. Selector de Tipo de Operación
            _cmbTipo = CamposFormulario.Combo(Point.Empty, 130);
            _cmbTipo.Items.AddRange(new object[] { "Todas", "Check-in", "Check-out", "Venta" });
            _cmbTipo.SelectedIndex = 0;
            _cmbTipo.Margin = new Padding(0, 0, 10, 0);

            // 2. Filtro por Huésped
            _txtHuesped = CamposFormulario.Texto(Point.Empty, 170);
            _txtHuesped.Margin = new Padding(0, 0, 4, 0);
            var lblHuesped = new Label
            {
                Text = "Huésped",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 3. Filtro por Habitación
            _numHabitacion = CamposFormulario.Numerico(Point.Empty, 80, 0, 999, 0);
            _numHabitacion.Margin = new Padding(0, 0, 4, 0);
            var lblHabitacion = new Label
            {
                Text = "Habitación",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 4. Filtro por Usuario
            _txtUsuario = CamposFormulario.Texto(Point.Empty, 130);
            _txtUsuario.Margin = new Padding(0, 0, 4, 0);
            var lblUsuario = new Label
            {
                Text = "Usuario",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 5. Filtro por Fecha
            _chkUsarFecha = new CheckBox
            {
                Text = "Filtrar por fecha",
                Font = Paleta.FuenteBase,
                AutoSize = true,
                Margin = new Padding(0, 4, 6, 0)
            };
            _dtpFecha = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Margin = new Padding(0, 0, 12, 0),
                Enabled = false
            };
            _chkUsarFecha.CheckedChanged += (s, e) => _dtpFecha.Enabled = _chkUsarFecha.Checked;

            // 6. Botones de Acción
            var btnBuscar = EstiloBoton.Primario(new Button
            {
                Text = "Filtrar",
                Size = new Size(90, 34),
                Margin = new Padding(0, 0, 6, 0)
            });
            btnBuscar.Click += (s, e) => Refrescar();

            var btnLimpiar = EstiloBoton.Secundario(new Button
            {
                Text = "Limpiar filtros",
                Size = new Size(110, 34)
            });
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            // Eventos para auto-filtrar de forma inmediata
            _txtHuesped.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Refrescar(); };
            _txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Refrescar(); };
            _cmbTipo.SelectedIndexChanged += (s, e) => Refrescar();
            _numHabitacion.ValueChanged += (s, e) => Refrescar();
            _dtpFecha.ValueChanged += (s, e) => { if (_chkUsarFecha.Checked) Refrescar(); };

            panelFiltros.Controls.Add(_cmbTipo);
            panelFiltros.Controls.Add(_txtHuesped);
            panelFiltros.Controls.Add(lblHuesped);
            panelFiltros.Controls.Add(_numHabitacion);
            panelFiltros.Controls.Add(lblHabitacion);
            panelFiltros.Controls.Add(_txtUsuario);
            panelFiltros.Controls.Add(lblUsuario);
            panelFiltros.Controls.Add(_chkUsarFecha);
            panelFiltros.Controls.Add(_dtpFecha);
            panelFiltros.Controls.Add(btnBuscar);
            panelFiltros.Controls.Add(btnLimpiar);

            panelSuperior.Controls.Add(lblTitulo);
            panelSuperior.Controls.Add(panelFiltros);

            // Contenedor principal de la grilla (Dock = Fill)
            var panelGrilla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 6, 0, 0)
            };

            _grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both,
                AutoGenerateColumns = false
            };
            EstiloGrid.Aplicar(_grilla);

            ConfigurarColumnas();

            panelGrilla.Controls.Add(_grilla);

            Controls.Add(panelGrilla);
            Controls.Add(panelSuperior);

            Refrescar();
        }

        private void ConfigurarColumnas()
        {
            _grilla.Columns.Clear();

            // 1. Fecha
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "fecha",
                HeaderText = "Fecha",
                FillWeight = 10,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 6, 0, 6) }
            });

            // 2. Hora
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "hora",
                HeaderText = "Hora",
                FillWeight = 8,
                MinimumWidth = 75,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 6, 0, 6) }
            });

            // 3. Tipo
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "tipo",
                HeaderText = "Tipo",
                FillWeight = 11,
                MinimumWidth = 100,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 6, 0, 6) }
            });

            // 4. Descripción
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "descripcion",
                HeaderText = "Descripción",
                FillWeight = 36,
                MinimumWidth = 220,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) } },
                DefaultCellStyle =
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 6, 10, 6),
                    WrapMode = DataGridViewTriState.False
                }
            });

            // 5. Habitación
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "habitacion",
                HeaderText = "Habitación",
                FillWeight = 9,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 6, 0, 6) }
            });

            // 6. Usuario
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "usuario",
                HeaderText = "Usuario",
                FillWeight = 13,
                MinimumWidth = 105,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(10, 6, 0, 6) }
            });

            // 7. Monto
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "monto",
                HeaderText = "Monto",
                FillWeight = 13,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight, Padding = new Padding(0, 0, 12, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Padding = new Padding(0, 6, 12, 6) }
            });
        }

        public void Refrescar()
        {
            DateTime? fecha = _chkUsarFecha.Checked ? _dtpFecha.Value.Date : null;
            int? nroHabitacion = _numHabitacion.Value > 0 ? (int)_numHabitacion.Value : null;

            var operaciones = _gestionHistorial.Obtener(
                fecha: fecha,
                huesped: string.IsNullOrWhiteSpace(_txtHuesped.Text) ? null : _txtHuesped.Text.Trim(),
                nroHabitacion: nroHabitacion,
                usuario: string.IsNullOrWhiteSpace(_txtUsuario.Text) ? null : _txtUsuario.Text.Trim(),
                tipo: _cmbTipo.SelectedItem?.ToString());

            _grilla.Rows.Clear();
            foreach (OperacionHistorial operacion in operaciones)
            {
                _grilla.Rows.Add(
                    operacion.Fecha.ToString("dd/MM/yyyy"),
                    operacion.Hora.ToString(@"hh\:mm"),
                    operacion.Tipo,
                    operacion.Descripcion,
                    operacion.NroHabitacion?.ToString() ?? "-",
                    operacion.Usuario,
                    operacion.Monto.HasValue ? operacion.Monto.Value.ToString("C") : "-");
            }
        }

        private void LimpiarFiltros()
        {
            _cmbTipo.SelectedIndex = 0;
            _txtHuesped.Clear();
            _numHabitacion.Value = 0;
            _txtUsuario.Clear();
            _chkUsarFecha.Checked = false;
            _dtpFecha.Value = DateTime.Now;
            _dtpFecha.Enabled = false;

            Refrescar();
        }
    }
}
