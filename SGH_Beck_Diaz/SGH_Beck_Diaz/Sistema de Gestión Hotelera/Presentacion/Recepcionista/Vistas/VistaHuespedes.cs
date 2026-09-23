using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.Modales;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Búsqueda y alta de huéspedes. Sin acción de eliminar (fuera del alcance del rol Recepcionista).</summary>
    internal class VistaHuespedes : UserControl
    {
        private readonly GestionHuespedes _gestionHuespedes = new();
        private readonly GestionHospedajes _gestionHospedajes = new();

        private readonly TextBox _txtBuscar;
        private readonly DataGridView _grilla;

        public VistaHuespedes()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var lblTitulo = new Label { Text = "Huéspedes", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };

            var panelAcciones = new FlowLayoutPanel { Location = new Point(0, 36), Size = new Size(900, 40), FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            _txtBuscar = CamposFormulario.Texto(Point.Empty, 280);
            _txtBuscar.Margin = new Padding(0, 0, 10, 0);
            var btnBuscar = EstiloBoton.Secundario(new Button { Text = "Buscar", Size = new Size(100, 34), Margin = new Padding(0, 0, 20, 0) });
            btnBuscar.Click += (s, e) => Refrescar();
            var btnRegistrar = EstiloBoton.Primario(new Button { Text = "Registrar huésped", Size = new Size(170, 34) });
            btnRegistrar.Click += (s, e) => RegistrarHuesped();

            panelAcciones.Controls.Add(_txtBuscar);
            panelAcciones.Controls.Add(btnBuscar);
            panelAcciones.Controls.Add(btnRegistrar);

            _grilla = new DataGridView
            {
                Location = new Point(0, 86),
                Size = new Size(130, 700),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grilla);
            _grilla.Columns.Add("dni", "DNI");
            _grilla.Columns.Add("nombre", "Nombre");
            _grilla.Columns.Add("apellido", "Apellido");
            _grilla.Columns.Add("telefono", "Teléfono");
            _grilla.Columns.Add("habitacion", "Habitación actual");
            _grilla.Columns.Add("estado", "Estado");

            Controls.Add(lblTitulo);
            Controls.Add(panelAcciones);
            Controls.Add(_grilla);

            Refrescar();
        }

        public void Refrescar()
        {
            var huespedes = _gestionHuespedes.Buscar(_txtBuscar.Text.Trim());

            _grilla.Rows.Clear();
            foreach (Huesped huesped in huespedes)
            {
                HospedajeDetalle? estadia = _gestionHospedajes.ObtenerEstadiaActual(huesped.DniHuesped);

                _grilla.Rows.Add(
                    huesped.DniHuesped,
                    huesped.Nombre,
                    huesped.Apellido,
                    huesped.Telefono,
                    estadia != null ? estadia.NroHabitacion.ToString() : "-",
                    estadia != null ? "Alojado" : "Sin estadía activa");
            }
        }

        private void RegistrarHuesped()
        {
            using var modal = new FModalRegistrarHuesped();
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Refrescar();
            }
        }
    }
}
