using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>
    /// Mapa de habitaciones: matriz de tarjetas con el estado real de cada habitación
    /// (Logica.GestionHabitaciones.ObtenerResumenHabitaciones — huésped, horarios y estado ya
    /// resueltos). Al hacer click en una habitación se abre FGestionHabitacion, que decide la
    /// acción disponible (check-in, check-out, limpieza, mantenimiento) según su estado.
    /// Con <see cref="_filtro"/> se reutiliza esta misma grilla para las secciones "Check-in"
    /// (solo Disponibles) y "Check-out" (solo Ocupadas) del sidebar.
    /// </summary>
    internal class VistaHabitaciones : UserControl
    {
        private const int AnchoTarjeta = 180;
        private const int AltoTarjeta = 130;
        private const int Separacion = 20;

        private readonly Usuario _usuario;
        private readonly EstadoOcupacion? _filtro;
        private readonly GestionHabitaciones _gestionHabitaciones = new();

        private readonly Label _lblSubtitulo;
        private readonly FlowLayoutPanel _panelResumen;
        private readonly Panel _panelGrilla;
        private List<HabitacionInfo> _habitaciones = new();

        public VistaHabitaciones(Usuario usuario, EstadoOcupacion? filtro = null)
        {
            _usuario = usuario;
            _filtro = filtro;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label { Text = TituloSegunFiltro(), Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _lblSubtitulo = new Label { Text = string.Empty, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 26) };

            _panelResumen = new FlowLayoutPanel
            {
                Location = new Point(0, 54),
                Size = new Size(1400, 130),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Visible = filtro == null
            };

            _panelGrilla = new Panel { Location = new Point(0, filtro == null ? 200 : 54), Size = new Size(1400, 900) };

            Controls.Add(lblTitulo);
            Controls.Add(_lblSubtitulo);
            Controls.Add(_panelResumen);
            Controls.Add(_panelGrilla);

            Resize += (s, e) => CargarGrilla();

            CargarDesdeBaseDeDatos();
        }

        private string TituloSegunFiltro() => _filtro switch
        {
            EstadoOcupacion.Disponible => "Check-in — habitaciones disponibles",
            EstadoOcupacion.Ocupada => "Check-out — habitaciones ocupadas",
            _ => "Mapa de Habitaciones"
        };

        public void Refrescar() => CargarDesdeBaseDeDatos();

        private void CargarDesdeBaseDeDatos()
        {
            try
            {
                List<HabitacionResumen> resumen = _gestionHabitaciones.ObtenerResumenHabitaciones();

                _habitaciones = resumen
                    .Select(r => new HabitacionInfo
                    {
                        NroHabitacion = r.NroHabitacion.ToString(),
                        Tipo = r.TipoHabitacion,
                        Estado = DibujoUtil.EstadoDesdeTexto(r.Estado),
                        Huesped = r.Huesped ?? string.Empty,
                        HoraIngreso = r.HoraEntrada,
                        HoraSalidaEstimada = r.HoraSalidaEstimada
                    })
                    .Where(h => _filtro == null || h.Estado == _filtro)
                    .OrderBy(h => h.NroHabitacion)
                    .ToList();
            }
            catch (Exception ex)
            {
                _habitaciones = new List<HabitacionInfo>();
                MessageBox.Show($"No se pudieron cargar las habitaciones.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _lblSubtitulo.Text = $"{_habitaciones.Count} habitación(es)";

            if (_filtro == null)
            {
                CargarResumen();
            }

            CargarGrilla();
        }

        private void CargarResumen()
        {
            _panelResumen.Controls.Clear();

            int disponibles = _habitaciones.Count(h => h.Estado == EstadoOcupacion.Disponible);
            int ocupadas = _habitaciones.Count(h => h.Estado == EstadoOcupacion.Ocupada);
            int limpieza = _habitaciones.Count(h => h.Estado == EstadoOcupacion.Limpieza);
            int mantenimiento = _habitaciones.Count(h => h.Estado == EstadoOcupacion.Mantenimiento);

            _panelResumen.Controls.Add(CrearTarjetaResumen("Disponibles", disponibles, DibujoUtil.ColorPorEstado(EstadoOcupacion.Disponible)));
            _panelResumen.Controls.Add(CrearTarjetaResumen("Ocupadas", ocupadas, DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada)));
            _panelResumen.Controls.Add(CrearTarjetaResumen("Limpieza", limpieza, DibujoUtil.ColorPorEstado(EstadoOcupacion.Limpieza)));
            _panelResumen.Controls.Add(CrearTarjetaResumen("Mantenimiento", mantenimiento, DibujoUtil.ColorPorEstado(EstadoOcupacion.Mantenimiento)));
        }

        private static Control CrearTarjetaResumen(string titulo, int cantidad, Color colorEstado)
        {
            var tarjeta = new Panel { Size = new Size(220, 100), BackColor = Paleta.FondoTarjeta, Margin = new Padding(0, 0, 16, 16) };
            var indicador = new IndicadorColor { ColorEstado = colorEstado, Location = new Point(16, 18) };
            var lblTitulo = new Label { Text = titulo, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, AutoSize = true, Location = new Point(36, 14) };
            var lblCantidad = new Label { Text = cantidad.ToString(), Font = new Font("Segoe UI Semibold", 20f), ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(16, 40) };
            tarjeta.Controls.Add(indicador);
            tarjeta.Controls.Add(lblTitulo);
            tarjeta.Controls.Add(lblCantidad);
            return tarjeta;
        }

        private void CargarGrilla()
        {
            _panelGrilla.Controls.Clear();

            int anchoDisponible = Math.Max(AnchoTarjeta, Width - 40);
            int porFila = Math.Max(1, anchoDisponible / (AnchoTarjeta + Separacion));
            int top = 0;

            for (int inicio = 0; inicio < _habitaciones.Count; inicio += porFila)
            {
                List<HabitacionInfo> fila = _habitaciones.Skip(inicio).Take(porFila).ToList();
                int x = 0;

                foreach (HabitacionInfo habitacion in fila)
                {
                    var tarjeta = new UC_Habitacion { Location = new Point(x, top) };
                    tarjeta.ConfigurarTarjeta(habitacion.NroHabitacion, habitacion.Tipo, habitacion.Estado, habitacion.Huesped, habitacion.HoraIngreso, habitacion.HoraSalidaEstimada);
                    tarjeta.Click += (s, e) => GestionarHabitacion(habitacion);
                    _panelGrilla.Controls.Add(tarjeta);
                    x += AnchoTarjeta + Separacion;
                }

                top += AltoTarjeta + Separacion;
            }

            _panelGrilla.Height = Math.Max(200, top);
        }

        private void GestionarHabitacion(HabitacionInfo habitacion)
        {
            using (var fGestion = new FGestionHabitacion(habitacion, _usuario))
            {
                fGestion.ShowDialog(FindForm());
            }

            CargarDesdeBaseDeDatos();
        }
    }
}
