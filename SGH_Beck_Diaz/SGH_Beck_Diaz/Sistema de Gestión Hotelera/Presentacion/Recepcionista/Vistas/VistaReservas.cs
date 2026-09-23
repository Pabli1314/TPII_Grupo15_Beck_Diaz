using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
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

            var lblTitulo = new Label { Text = "Gestión de Reservas", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            var lblSubtitulo = new Label { Text = "Estadías actuales e históricas (check-ins realizados).", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 28) };

            var panelBusqueda = new FlowLayoutPanel { Location = new Point(0, 56), Size = new Size(1200, 40), FlowDirection = FlowDirection.LeftToRight, AutoSize = true };

            _txtBuscar = CamposFormulario.Texto(Point.Empty, 260);
            _txtBuscar.Margin = new Padding(0, 0, 10, 0);
            var lblPlaceholder = new Label { Text = "DNI o nombre", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Margin = new Padding(0, 6, 12, 0) };

            _numHabitacion = CamposFormulario.Numerico(Point.Empty, 100, 0, 999, 0);
            _numHabitacion.Margin = new Padding(0, 0, 12, 0);
            var lblHabitacion = new Label { Text = "N° habitación (0 = todas)", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Margin = new Padding(0, 6, 12, 0) };

            _chkUsarFecha = new CheckBox { Text = "Filtrar por fecha", Font = Paleta.FuenteBase, AutoSize = true, Margin = new Padding(0, 4, 6, 0) };
            _dtpFecha = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Margin = new Padding(0, 0, 12, 0), Enabled = false };
            _chkUsarFecha.CheckedChanged += (s, e) => _dtpFecha.Enabled = _chkUsarFecha.Checked;

            var btnBuscar = EstiloBoton.Primario(new Button { Text = "Buscar", Size = new Size(110, 34) });
            btnBuscar.Click += (s, e) => Buscar();

            panelBusqueda.Controls.Add(_txtBuscar);
            panelBusqueda.Controls.Add(lblPlaceholder);
            panelBusqueda.Controls.Add(_numHabitacion);
            panelBusqueda.Controls.Add(lblHabitacion);
            panelBusqueda.Controls.Add(_chkUsarFecha);
            panelBusqueda.Controls.Add(_dtpFecha);
            panelBusqueda.Controls.Add(btnBuscar);

            _grilla = new DataGridView
            {
                Location = new Point(0, 106),
                Size = new Size(250, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grilla);
            _grilla.Columns.Add("reserva", "Reserva");
            _grilla.Columns.Add("huesped", "Huésped");
            _grilla.Columns.Add("habitacion", "Habitación");
            _grilla.Columns.Add("entrada", "Entrada");
            _grilla.Columns.Add("salida", "Salida");
            _grilla.Columns.Add("estado", "Estado");

            Controls.Add(lblTitulo);
            Controls.Add(lblSubtitulo);
            Controls.Add(panelBusqueda);
            Controls.Add(_grilla);

            Buscar();
        }

        private void Buscar()
        {
            string? nombre = string.IsNullOrWhiteSpace(_txtBuscar.Text) ? null : _txtBuscar.Text.Trim();
            int? nroHabitacion = _numHabitacion.Value > 0 ? (int)_numHabitacion.Value : null;
            DateTime? fecha = _chkUsarFecha.Checked ? _dtpFecha.Value.Date : null;

            var resultados = _gestionHospedajes.BuscarHospedajes(dni: nombre, nombre: nombre, nroHabitacion: nroHabitacion, fecha: fecha);

            _grilla.Rows.Clear();
            foreach (HospedajeDetalle hospedaje in resultados)
            {
                _grilla.Rows.Add(
                    hospedaje.IdHospedaje,
                    hospedaje.NombreHuesped,
                    hospedaje.NroHabitacion,
                    $"{hospedaje.FechaEntrada:dd/MM/yyyy} {hospedaje.HoraEntrada:hh\\:mm}",
                    $"{hospedaje.FechaSalida:dd/MM/yyyy} {hospedaje.HoraSalida:hh\\:mm}",
                    hospedaje.Estado);
            }
        }
    }
}
