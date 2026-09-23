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
    internal class VistaDashboard : UserControl, IVistaAdministrador
    {
        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly GestionUsuarios _gestionUsuarios = new();
        private readonly GestionReportes _gestionReportes = new();
        private readonly GestionConfiguracion _gestionConfiguracion = new();

        private readonly FlowLayoutPanel _panelTarjetas;
        private readonly FlowLayoutPanel _panelHabitaciones;
        private readonly Label _lblSubtitulo;

        public VistaDashboard()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Resumen general del hotel", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _lblSubtitulo = new Label { Text = string.Empty, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 26) };

            _panelTarjetas = new FlowLayoutPanel
            {
                Location = new Point(0, 54),
                Size = new Size(944, 250),
                MaximumSize = new Size(944, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            var lblEstado = new Label { Text = "Estado de habitaciones", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 314) };

            _panelHabitaciones = new FlowLayoutPanel
            {
                Location = new Point(0, 348),
                Size = new Size(1080, 900),
                MaximumSize = new Size(1080, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            Controls.Add(lblTitulo);
            Controls.Add(_lblSubtitulo);
            Controls.Add(_panelTarjetas);
            Controls.Add(lblEstado);
            Controls.Add(_panelHabitaciones);

            Resize += (s, e) => CentrarPanelTarjetas();
        }

        private void CentrarPanelTarjetas()
        {
            _panelTarjetas.Left = Math.Max(0, (ClientSize.Width - _panelTarjetas.Width) / 2);
        }

        public void Refrescar()
        {
            SuspendLayout();

            var habitaciones = _gestionHabitaciones.ObtenerResumenHabitaciones();
            int total = Math.Max(1, habitaciones.Count);
            int disponibles = habitaciones.Count(h => h.Estado == "Disponible");
            int ocupadas = habitaciones.Count(h => h.Estado == "Ocupada");
            int limpieza = habitaciones.Count(h => h.Estado == "Limpieza");
            int mantenimiento = habitaciones.Count(h => h.Estado == "Mantenimiento");

            decimal ingresosHoy = _gestionReportes.Generar(DateTime.Today, DateTime.Today).TotalIngresos;
            int usuariosRegistrados = _gestionUsuarios.ObtenerUsuarios().Count;
            double ocupacionActual = ocupadas * 100.0 / total;

            _lblSubtitulo.Text = $"Actualizado {DateTime.Now:dd/MM/yyyy HH:mm}";

            _panelTarjetas.Controls.Clear();
            AgregarTarjeta("Habitaciones totales", total.ToString(), Paleta.Primario, Icono.Home);
            AgregarTarjeta("Disponibles", disponibles.ToString(), Paleta.Disponible, Icono.Check);
            AgregarTarjeta("Ocupadas", ocupadas.ToString(), Paleta.Ocupada, Icono.Cama);
            AgregarTarjeta("En limpieza", limpieza.ToString(), Paleta.Limpieza, Icono.Alerta);
            AgregarTarjeta("En mantenimiento", mantenimiento.ToString(), Paleta.Mantenimiento, Icono.Engranaje);
            AgregarTarjeta("Ingresos del día", ingresosHoy.ToString("C0"), Paleta.Exito, Icono.Etiqueta);
            AgregarTarjeta("Ocupación actual", $"{ocupacionActual:0.#}%", Paleta.Primario, Icono.Grafico);
            AgregarTarjeta("Usuarios registrados", usuariosRegistrados.ToString(), Paleta.Mantenimiento, Icono.Personas);

            int minutosTolerancia = _gestionConfiguracion.Obtener().MinutosTolerancia;
            _panelHabitaciones.Controls.Clear();
            foreach (HabitacionResumen resumen in habitaciones.OrderBy(h => h.NroHabitacion))
            {
                var tarjeta = new TarjetaHabitacionAdmin { Margin = new Padding(0, 0, 16, 16) };
                tarjeta.ConfigurarDesdeResumen(resumen, minutosTolerancia, mostrarAccionLimpieza: false);
                tarjeta.HuespedClick += (s, e) => FModalDatosHuesped.Mostrar(this, resumen.NroHabitacion);
                _panelHabitaciones.Controls.Add(tarjeta);
            }

            CentrarPanelTarjetas();
            ResumeLayout();
        }

        private void AgregarTarjeta(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 16) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelTarjetas.Controls.Add(tarjeta);
        }
    }
}

