using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Centro de operaciones: estado de habitaciones y de caja de un vistazo.</summary>
    internal class VistaDashboard : UserControl
    {
        private readonly Usuario _usuario;
        private readonly Action _navegarACaja;
        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();

        private readonly FlowLayoutPanel _panelHabitaciones;
        private readonly Label _lblCajaTitulo;
        private readonly FlowLayoutPanel _panelCaja;
        private readonly Panel _panelSinTurno;

        public VistaDashboard(Usuario usuario, Action navegarACaja)
        {
            _usuario = usuario;
            _navegarACaja = navegarACaja;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Estado de habitaciones", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _panelHabitaciones = new FlowLayoutPanel { Location = new Point(0, 34), Size = new Size(1400, 130), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            _lblCajaTitulo = new Label { Text = "Estado de caja", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 180) };
            _panelCaja = new FlowLayoutPanel { Location = new Point(0, 214), Size = new Size(1400, 130), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            _panelSinTurno = new Panel { Location = new Point(0, 214), Size = new Size(500, 90), Visible = false };
            var lblSinTurno = new Label { Text = "No hay un turno de caja abierto.", Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, AutoSize = true, Location = new Point(0, 0) };
            var btnIrACaja = EstiloBoton.Primario(new Button { Text = "Ir a Caja", Size = new Size(160, 38), Location = new Point(0, 30) });
            btnIrACaja.Click += (s, e) => _navegarACaja();
            _panelSinTurno.Controls.Add(lblSinTurno);
            _panelSinTurno.Controls.Add(btnIrACaja);

            Controls.Add(lblTitulo);
            Controls.Add(_panelHabitaciones);
            Controls.Add(_lblCajaTitulo);
            Controls.Add(_panelCaja);
            Controls.Add(_panelSinTurno);

            Refrescar();
        }

        public void Refrescar()
        {
            CargarHabitaciones();
            CargarCaja();
        }

        private void CargarHabitaciones()
        {
            _panelHabitaciones.Controls.Clear();

            var resumen = _gestionHabitaciones.ObtenerResumenHabitaciones();
            int disponibles = resumen.Count(r => string.Equals(r.Estado, "Disponible", StringComparison.OrdinalIgnoreCase));
            int ocupadas = resumen.Count(r => r.Ocupada);
            int limpieza = resumen.Count(r => r.EnLimpieza);
            int mantenimiento = resumen.Count(r => string.Equals(r.Estado, "Mantenimiento", StringComparison.OrdinalIgnoreCase));
            int checkInsHoy = resumen.Count(r => r.HoraEntrada?.Date == DateTime.Today);
            int checkOutsPendientesHoy = resumen.Count(r => r.Ocupada && r.HoraSalidaEstimada?.Date == DateTime.Today);

            _panelHabitaciones.Controls.Add(Tarjeta("Disponibles", disponibles.ToString(), DibujoUtil.ColorPorEstado(EstadoOcupacion.Disponible), Icono.Cama));
            _panelHabitaciones.Controls.Add(Tarjeta("Ocupadas", ocupadas.ToString(), DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada), Icono.Cama));
            _panelHabitaciones.Controls.Add(Tarjeta("En limpieza", limpieza.ToString(), DibujoUtil.ColorPorEstado(EstadoOcupacion.Limpieza), Icono.Cama));
            _panelHabitaciones.Controls.Add(Tarjeta("En mantenimiento", mantenimiento.ToString(), DibujoUtil.ColorPorEstado(EstadoOcupacion.Mantenimiento), Icono.Cama));
            _panelHabitaciones.Controls.Add(Tarjeta("Check-ins de hoy", checkInsHoy.ToString(), Paleta.Primario, Icono.Check));
            _panelHabitaciones.Controls.Add(Tarjeta("Check-outs pendientes hoy", checkOutsPendientesHoy.ToString(), Color.FromArgb(217, 119, 6), Icono.Salir));
        }

        private void CargarCaja()
        {
            TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(_usuario.DniUsuario);

            _panelCaja.Visible = turno != null;
            _panelSinTurno.Visible = turno == null;

            if (turno == null)
            {
                return;
            }

            ResumenTurno resumen = _gestionTurnoCaja.ObtenerResumen(turno.IdTurno);

            _panelCaja.Controls.Clear();
            _panelCaja.Controls.Add(Tarjeta("Turno actual", $"#{turno.IdTurno}", Paleta.Primario, Icono.Caja));
            _panelCaja.Controls.Add(Tarjeta("Monto inicial", resumen.Turno.MontoInicial.ToString("C"), Paleta.Primario, Icono.Caja));
            _panelCaja.Controls.Add(Tarjeta("Total cobrado", (resumen.TotalEfectivo + resumen.TotalTarjeta + resumen.TotalTransferencia).ToString("C"), Paleta.Exito, Icono.Caja));
            _panelCaja.Controls.Add(Tarjeta("Ventas adicionales", resumen.TotalVentas.ToString("C"), Color.FromArgb(217, 119, 6), Icono.Grafico));
            _panelCaja.Controls.Add(Tarjeta("Efectivo esperado", resumen.EfectivoEsperado.ToString("C"), Paleta.Exito, Icono.Check));
        }

        private static Control Tarjeta(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 16) };
            tarjeta.Configurar(titulo, valor, color, icono);
            return tarjeta;
        }
    }
}
