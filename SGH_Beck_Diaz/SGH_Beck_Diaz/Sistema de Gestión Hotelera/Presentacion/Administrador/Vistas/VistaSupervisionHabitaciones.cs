using Entidades;
using Logica;
using Presentacion.Administrador.Modales;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaSupervisionHabitaciones : UserControl, IVistaAdministrador
    {
        private readonly Usuario _usuario;
        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly GestionConfiguracion _gestionConfiguracion = new();
        private readonly FlowLayoutPanel _panelHabitaciones;
        private readonly Button _btnNuevaHabitacion;

        public VistaSupervisionHabitaciones(Usuario usuario)
        {
            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label
            {
                Text = "Todas las habitaciones",
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            _btnNuevaHabitacion = EstiloBoton.Primario(new Button
            {
                Text = "+  Nueva habitación",
                Size = new Size(190, 38),
                Location = new Point(0, 0)
            });
            _btnNuevaHabitacion.Click += (s, e) => AbrirModalNuevaHabitacion();

            _panelHabitaciones = new FlowLayoutPanel
            {
                Location = new Point(0, 36),
                Size = new Size(1080, 900),
                MaximumSize = new Size(1080, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            Controls.Add(lblTitulo);
            Controls.Add(_btnNuevaHabitacion);
            Controls.Add(_panelHabitaciones);

            Resize += (s, e) => PosicionarBotonNueva();
            PosicionarBotonNueva();
        }

        private void PosicionarBotonNueva()
        {
            _btnNuevaHabitacion.Location = new Point(Math.Max(0, ClientSize.Width - _btnNuevaHabitacion.Width), 0);
        }

        private void AbrirModalNuevaHabitacion()
        {
            using var modal = new FModalHabitacion();
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                Refrescar();
            }
        }

        public void Refrescar()
        {
            int minutosTolerancia = _gestionConfiguracion.Obtener().MinutosTolerancia;

            var habitaciones = _gestionHabitaciones.ObtenerResumenHabitaciones()
                                                    .OrderBy(h => h.NroHabitacion)
                                                    .ToList();

            _panelHabitaciones.Controls.Clear();
            foreach (HabitacionResumen resumen in habitaciones)
            {
                var tarjeta = new TarjetaHabitacionAdmin { Margin = new Padding(0, 0, 16, 16) };
                tarjeta.ConfigurarDesdeResumen(resumen, minutosTolerancia, mostrarAccionLimpieza: true);

                // Clic general en la tarjeta para abrir el detalle
                tarjeta.Click += (s, e) => AbrirDetalleHabitacion(resumen);

                // Clic específico en la acción de marcar como limpia
                tarjeta.AccionClick += (s, e) => MarcarComoLimpia(resumen.NroHabitacion);

                _panelHabitaciones.Controls.Add(tarjeta);
            }
        }

        private void AbrirDetalleHabitacion(HabitacionResumen resumen)
        {
            using var modal = new FModalDetalleHabitacion(resumen);
            modal.ShowDialog(this);
        }

        private void MarcarComoLimpia(int nroHabitacion)
        {
            DialogResult confirmacion = MessageBox.Show(
                $"¿Confirma que la habitación {nroHabitacion} fue limpiada y está lista para volver a Disponible?",
                "Confirmar limpieza",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes) return;

            try
            {
                // Se utiliza _usuario.Dni (o _usuario.DniUsuario si coincide con el nombre en tu entidad Usuario)
                string dniUsuario = _usuario.DniUsuario ?? string.Empty;
                _gestionHabitaciones.MarcarComoDisponible(nroHabitacion, dniUsuario);
                Refrescar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo actualizar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeComponent()
        {
        }
    }
}