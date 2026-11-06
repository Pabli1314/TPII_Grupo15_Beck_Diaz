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

            var lblTitulo = new Label { Text = "Historial de operaciones", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };

            var panelFiltros = new FlowLayoutPanel { Location = new Point(0, 36), Size = new Size(1300, 40), FlowDirection = FlowDirection.LeftToRight, AutoSize = true };

            _cmbTipo = CamposFormulario.Combo(Point.Empty, 140);
            _cmbTipo.Items.AddRange(new object[] { "Todas", "Check-in", "Check-out", "Venta" });
            _cmbTipo.SelectedIndex = 0;
            _cmbTipo.Margin = new Padding(0, 0, 12, 0);

            _txtHuesped = CamposFormulario.Texto(Point.Empty, 180);
            _txtHuesped.Margin = new Padding(0, 0, 4, 0);
            var lblHuesped = new Label { Text = "Huésped", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Margin = new Padding(0, 6, 12, 0) };

            _numHabitacion = CamposFormulario.Numerico(Point.Empty, 90, 0, 999, 0);
            _numHabitacion.Margin = new Padding(0, 0, 4, 0);
            var lblHabitacion = new Label { Text = "Habitación", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Margin = new Padding(0, 6, 12, 0) };

            _txtUsuario = CamposFormulario.Texto(Point.Empty, 140);
            _txtUsuario.Margin = new Padding(0, 0, 4, 0);
            var lblUsuario = new Label { Text = "Usuario", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Margin = new Padding(0, 6, 12, 0) };

            _chkUsarFecha = new CheckBox { Text = "Filtrar por fecha", Font = Paleta.FuenteBase, AutoSize = true, Margin = new Padding(0, 4, 6, 0) };
            _dtpFecha = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Margin = new Padding(0, 0, 12, 0), Enabled = false };
            _chkUsarFecha.CheckedChanged += (s, e) => _dtpFecha.Enabled = _chkUsarFecha.Checked;

            var btnBuscar = EstiloBoton.Primario(new Button { Text = "Filtrar", Size = new Size(100, 34) });
            btnBuscar.Click += (s, e) => Refrescar();

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

            _grilla = new DataGridView
            {
                Location = new Point(0, 86),
                Size = new Size(1300, 700),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grilla);
            _grilla.Columns.Add("fecha", "Fecha");
            _grilla.Columns.Add("hora", "Hora");
            _grilla.Columns.Add("tipo", "Tipo");
            _grilla.Columns.Add("descripcion", "Descripción");
            _grilla.Columns.Add("habitacion", "Habitación");
            _grilla.Columns.Add("usuario", "Usuario");
            _grilla.Columns.Add("monto", "Monto");

            Controls.Add(lblTitulo);
            Controls.Add(panelFiltros);
            Controls.Add(_grilla);

            Refrescar();
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
    }
}
