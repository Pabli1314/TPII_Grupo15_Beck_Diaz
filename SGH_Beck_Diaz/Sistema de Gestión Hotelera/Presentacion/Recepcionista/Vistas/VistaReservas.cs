using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Consulta de estadías (hospedaje) actuales e históricas, con búsqueda por
    /// DNI, nombre, número de habitación y fecha.</summary>
    internal class VistaReservas : UserControl
    {
        private readonly GestionHospedajes _gestionHospedajes = new();

        private readonly TextBox _txtBuscar;
        private readonly NumericUpDown _numHabitacion;
        private readonly DateTimePicker _dtpFecha;
        private readonly CheckBox _chkUsarFecha;
        private readonly DataGridView _grilla;

        public VistaReservas()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var lblTitulo = new Label
            {
                Text = "Gestión de Reservas",
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblSubtitulo = new Label
            {
                Text = "Estadías actuales e históricas (check-ins realizados).",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Location = new Point(0, 28)
            };

            var panelBusqueda = new FlowLayoutPanel
            {
                Location = new Point(0, 56),
                Size = new Size(1200, 42),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };

            _txtBuscar = CamposFormulario.Texto(Point.Empty, 240);
            _txtBuscar.Margin = new Padding(0, 0, 8, 0);
            _txtBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Buscar();
                }
            };

            var lblPlaceholder = new Label
            {
                Text = "DNI o nombre",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 8, 12, 0)
            };

            _numHabitacion = CamposFormulario.Numerico(Point.Empty, 90, 0, 999, 0);
            _numHabitacion.Margin = new Padding(0, 0, 8, 0);
            _numHabitacion.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Buscar();
                }
            };

            var lblHabitacion = new Label
            {
                Text = "N° hab. (0 = todas)",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 8, 12, 0)
            };

            _chkUsarFecha = new CheckBox
            {
                Text = "Filtrar por fecha",
                Font = Paleta.FuenteBase,
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            _dtpFecha = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Margin = new Padding(0, 2, 12, 0),
                Enabled = false
            };
            _chkUsarFecha.CheckedChanged += (s, e) => _dtpFecha.Enabled = _chkUsarFecha.Checked;

            var btnBuscar = EstiloBoton.Primario(new Button { Text = "Buscar", Size = new Size(100, 34) });
            btnBuscar.Click += (s, e) => Buscar();

            var btnLimpiar = EstiloBoton.Secundario(new Button { Text = "Limpiar", Size = new Size(90, 34), Margin = new Padding(8, 0, 0, 0) });
            btnLimpiar.Click += (s, e) => Limpiar();

            panelBusqueda.Controls.Add(_txtBuscar);
            panelBusqueda.Controls.Add(lblPlaceholder);
            panelBusqueda.Controls.Add(_numHabitacion);
            panelBusqueda.Controls.Add(lblHabitacion);
            panelBusqueda.Controls.Add(_chkUsarFecha);
            panelBusqueda.Controls.Add(_dtpFecha);
            panelBusqueda.Controls.Add(btnBuscar);
            panelBusqueda.Controls.Add(btnLimpiar);

            _grilla = new DataGridView
            {
                Location = new Point(0, 106),
                Size = new Size(1200, 600),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grilla);

            _grilla.Columns.Add("reserva", "Reserva");
            _grilla.Columns.Add("dni", "DNI Huésped");
            _grilla.Columns.Add("habitacion", "Habitación");
            _grilla.Columns.Add("entrada", "Entrada");
            _grilla.Columns.Add("salida", "Salida");
            _grilla.Columns.Add("metodo", "Método Pago");

            _grilla.Columns["reserva"].FillWeight = 60;
            _grilla.Columns["dni"].FillWeight = 90;
            _grilla.Columns["habitacion"].FillWeight = 70;
            _grilla.Columns["entrada"].FillWeight = 120;
            _grilla.Columns["salida"].FillWeight = 120;
            _grilla.Columns["metodo"].FillWeight = 90;

            Controls.Add(lblTitulo);
            Controls.Add(lblSubtitulo);
            Controls.Add(panelBusqueda);
            Controls.Add(_grilla);

            Buscar();
        }

        public void Refrescar()
        {
            Buscar();
        }

        private void Limpiar()
        {
            _txtBuscar.Clear();
            _numHabitacion.Value = 0;
            _chkUsarFecha.Checked = false;
            _dtpFecha.Value = DateTime.Today;
            Buscar();
        }

        private void Buscar()
        {
            try
            {
                string? criterio = string.IsNullOrWhiteSpace(_txtBuscar.Text) ? null : _txtBuscar.Text.Trim();
                int? nroHabitacion = _numHabitacion.Value > 0 ? (int)_numHabitacion.Value : null;
                DateTime? fecha = _chkUsarFecha.Checked ? _dtpFecha.Value.Date : null;

                List<Hospedaje> resultados = _gestionHospedajes.BuscarHospedajes(
                    termino: criterio,
                    nroHabitacion: nroHabitacion,
                    fecha: fecha);

                _grilla.Rows.Clear();
                foreach (Hospedaje hospedaje in resultados)
                {
                    _grilla.Rows.Add(
                        hospedaje.IdHospedaje,
                        hospedaje.DniHuesped,
                        hospedaje.NroHabitacion,
                        $"{hospedaje.FechaEntrada:dd/MM/yyyy} {hospedaje.HoraEntrada:hh\\:mm}",
                        $"{hospedaje.FechaSalida:dd/MM/yyyy} {hospedaje.HoraSalida:hh\\:mm}",
                        hospedaje.IdMetodo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al consultar las reservas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}