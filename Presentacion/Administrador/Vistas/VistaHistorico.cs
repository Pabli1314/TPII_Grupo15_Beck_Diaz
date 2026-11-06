using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    /// <summary>
    /// Histórico del Administrador centrado en los huéspedes: altas, ingresos (check-in), egresos
    /// (check-out) y consumos, filtrable por huésped. No muestra recaudación ni arqueos de caja
    /// (eso queda en el histórico de Gerencia).
    /// </summary>
    internal class VistaHistorico : UserControl, IVistaAdministrador
    {
        private const string OpcionTodos = "-- Todos --";

        private readonly GestionHistorialAdministrador _gestionHistorial = new();

        private readonly DateTimePicker _dtpDesde;
        private readonly DateTimePicker _dtpHasta;
        private readonly ComboBox _cmbHuesped;
        private readonly ComboBox _cmbTipo;
        private readonly FlowLayoutPanel _panelKpis;
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();
        private readonly GraficoActividadPorTipo _graficoPorTipo;
        private readonly RankingActividadHuespedes _rankingHuespedes;
        private readonly PanelReporte _panelPorTipo;
        private readonly PanelReporte _panelPorHuesped;
        private readonly DataGridView _dgv;
        private bool _huespedesCargados;

        public VistaHistorico()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var barraFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _dtpDesde = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Margin = new Padding(0, 4, 8, 0) };
            _dtpHasta = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 4, 16, 0) };

            _cmbHuesped = CamposFormulario.Combo(Point.Empty, 240);
            _cmbHuesped.Margin = new Padding(0, 2, 8, 0);

            _cmbTipo = CamposFormulario.Combo(Point.Empty, 170);
            _cmbTipo.Margin = new Padding(0, 2, 16, 0);
            _cmbTipo.Items.Add(OpcionTodos);
            _cmbTipo.Items.AddRange(GestionHistorialAdministrador.Tipos);
            _cmbTipo.SelectedIndex = 0;

            var btnFiltrar = EstiloBoton.Primario(new Button { Text = "Filtrar", Size = new Size(110, 34), Margin = new Padding(0, 0, 8, 0) });
            btnFiltrar.Click += (s, e) => CargarHistorial();

            var btnLimpiar = EstiloBoton.Secundario(new Button { Text = "Limpiar filtros", Size = new Size(130, 34), Margin = new Padding(0, 0, 0, 0) });
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            barraFiltros.Controls.Add(CrearEtiqueta("Desde"));
            barraFiltros.Controls.Add(_dtpDesde);
            barraFiltros.Controls.Add(CrearEtiqueta("Hasta"));
            barraFiltros.Controls.Add(_dtpHasta);
            barraFiltros.Controls.Add(CrearEtiqueta("Huésped"));
            barraFiltros.Controls.Add(_cmbHuesped);
            barraFiltros.Controls.Add(CrearEtiqueta("Tipo"));
            barraFiltros.Controls.Add(_cmbTipo);
            barraFiltros.Controls.Add(btnFiltrar);
            barraFiltros.Controls.Add(btnLimpiar);

            _panelKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 132,
                Padding = new Padding(0, 8, 0, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            _graficoPorTipo = new GraficoActividadPorTipo(_toolTip);
            _rankingHuespedes = new RankingActividadHuespedes(_toolTip) { MaximoFilas = 5 };

            _panelPorTipo = new PanelReporte(_toolTip, "Actividad por tipo: ¿qué movimientos hubo?",
                "Cuántas veces ocurrió cada tipo de movimiento de huéspedes en el período:\n" +
                "• Alta de huésped: se registró un huésped nuevo.\n" +
                "• Ingreso (check-in): un huésped ocupó una habitación.\n" +
                "• Egreso (check-out): un huésped dejó la habitación.\n" +
                "• Consumo: se cargó un producto a la cuenta de un huésped.\n" +
                "La barra más larga es el movimiento más frecuente. Pasá el mouse por cada fila para ver el detalle.",
                _graficoPorTipo, new Size(600, 370)) { Dock = DockStyle.Fill, Margin = new Padding(8) };

            _panelPorHuesped = new PanelReporte(_toolTip, "Huéspedes con más actividad",
                "Los 5 huéspedes con más movimientos en el período. La barra muestra de qué se compone su actividad " +
                "(mismos colores que «Actividad por tipo»). Pasá el mouse por un huésped para ver su detalle.",
                _rankingHuespedes, new Size(600, 370)) { Dock = DockStyle.Fill, Margin = new Padding(8) };

            var panelGraficos = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 386,
                ColumnCount = 2,
                RowCount = 1
            };
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelGraficos.Controls.Add(_panelPorTipo, 0, 0);
            panelGraficos.Controls.Add(_panelPorHuesped, 1, 0);

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();

            var contenedorGrilla = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 0) };
            contenedorGrilla.Controls.Add(_dgv);

            Controls.Add(contenedorGrilla);
            Controls.Add(panelGraficos);
            Controls.Add(_panelKpis);
            Controls.Add(barraFiltros);
        }

        private static Label CrearEtiqueta(string texto) => new Label
        {
            Text = texto,
            AutoSize = true,
            Font = Paleta.FuenteBaseNegrita,
            ForeColor = Paleta.TextoSecundario,
            Margin = new Padding(0, 9, 6, 0)
        };

        private void ConfigurarColumnas()
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Fecha), HeaderText = "Fecha", FillWeight = 11, DefaultCellStyle = { Format = "dd/MM/yyyy" } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Hora), HeaderText = "Hora", FillWeight = 7, DefaultCellStyle = { Format = @"hh\:mm" } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Tipo), HeaderText = "Tipo", FillWeight = 14 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Huesped), HeaderText = "Huésped", FillWeight = 16 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.DniHuesped), HeaderText = "DNI", FillWeight = 9 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.NroHabitacion), HeaderText = "Habitación", FillWeight = 9 });
        }

        public void Refrescar()
        {
            if (!_huespedesCargados)
            {
                CargarComboHuespedes();
            }

            CargarHistorial();
        }

        // Vuelve los filtros a su estado inicial (último mes, todos los huéspedes y tipos) y recarga el histórico.
        private void LimpiarFiltros()
        {
            _dtpDesde.Value = DateTime.Today.AddMonths(-1);
            _dtpHasta.Value = DateTime.Today;
            if (_cmbHuesped.Items.Count > 0)
            {
                _cmbHuesped.SelectedIndex = 0;
            }
            _cmbTipo.SelectedIndex = 0;
            CargarHistorial();
        }

        private void CargarComboHuespedes()
        {
            try
            {
                var huespedes = new List<KeyValuePair<string, string>> { new(string.Empty, OpcionTodos) };
                huespedes.AddRange(_gestionHistorial.ObtenerListaHuespedes());

                _cmbHuesped.DataSource = huespedes;
                _cmbHuesped.DisplayMember = "Value";
                _cmbHuesped.ValueMember = "Key";
                _cmbHuesped.SelectedIndex = 0;
                _huespedesCargados = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de huéspedes: {ex.Message}", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CargarHistorial()
        {
            if (_dtpDesde.Value.Date > _dtpHasta.Value.Date)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string? dniHuesped = _cmbHuesped.SelectedValue as string;
                string? tipo = _cmbTipo.SelectedItem as string == OpcionTodos ? null : _cmbTipo.SelectedItem as string;

                HistorialAdministradorResultado datos = _gestionHistorial.Consultar(_dtpDesde.Value, _dtpHasta.Value, dniHuesped, tipo);

                _panelKpis.Controls.Clear();
                AgregarKpi("Eventos registrados", datos.TotalEventos.ToString(), Paleta.Primario, Icono.Reloj);
                AgregarKpi("Huéspedes con actividad", datos.HuespedesDistintos.ToString(), Paleta.Mantenimiento, Icono.Personas);
                // Mismos colores que los tipos en los gráficos, para que se lean como un solo sistema.
                AgregarKpi("Ingresos (check-in)", datos.TotalIngresos.ToString(), TiposActividad.ColorIngreso, Icono.Cama);
                AgregarKpi("Egresos (check-out)", datos.TotalEgresos.ToString(), TiposActividad.ColorEgreso, Icono.Salir);
                AgregarKpi("Unidades consumidas", datos.UnidadesConsumidas.ToString(), TiposActividad.ColorConsumo, Icono.Caja);
                AgregarKpi("Altas de huéspedes", datos.TotalAltasHuespedes.ToString(), TiposActividad.ColorAlta, Icono.Mas);

                MostrarGraficos(datos);

                _dgv.DataSource = null;
                _dgv.DataSource = datos.Lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar el histórico: {ex.Message}", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Carga ambos gráficos y escribe al pie de cada uno una conclusión en lenguaje simple.</summary>
        private void MostrarGraficos(HistorialAdministradorResultado datos)
        {
            // Se muestran siempre los 4 tipos (aunque estén en 0) para que la comparación sea completa.
            _graficoPorTipo.Cargar(GestionHistorialAdministrador.Tipos.Select(t =>
                new KeyValuePair<string, int>(t, datos.EventosPorTipo.TryGetValue(t, out int n) ? n : 0)));

            int total = datos.TotalEventos;
            if (total == 0)
            {
                _panelPorTipo.TextoPie = "Sin movimientos para mostrar. Probá ampliar el rango de fechas o quitar filtros.";
            }
            else
            {
                var masFrecuente = datos.EventosPorTipo.OrderByDescending(kvp => kvp.Value).First();
                string balance = datos.TotalIngresos == datos.TotalEgresos
                    ? "Hubo tantas llegadas como salidas."
                    : datos.TotalIngresos > datos.TotalEgresos
                        ? $"Hubo {datos.TotalIngresos - datos.TotalEgresos} llegada(s) más que salidas: huéspedes que siguen alojados."
                        : $"Hubo {datos.TotalEgresos - datos.TotalIngresos} salida(s) más que llegadas: estadías que habían empezado antes.";
                _panelPorTipo.TextoPie = $"Lo más frecuente fue «{TiposActividad.NombreCorto(masFrecuente.Key)}» ({masFrecuente.Value * 100.0 / total:0}% de {TiposActividad.Plural(total, "movimiento", "movimientos")}). {balance}";
            }

            _rankingHuespedes.Cargar(datos.RankingHuespedes);

            ActividadHuesped? primero = datos.RankingHuespedes.FirstOrDefault();
            if (primero == null)
            {
                _panelPorHuesped.TextoPie = "Ningún huésped tuvo movimientos en el período elegido.";
            }
            else if (datos.RankingHuespedes.Count == 1)
            {
                _panelPorHuesped.TextoPie = $"Solo {primero.Nombre} tuvo actividad en el período ({TiposActividad.Plural(primero.Total, "movimiento", "movimientos")}).";
            }
            else
            {
                string motivo = primero.Consumos >= primero.Ingresos + primero.Egresos + primero.Altas
                    ? "principalmente por consumos"
                    : "principalmente por sus estadías";
                _panelPorHuesped.TextoPie = $"{primero.Nombre} lidera con {TiposActividad.Plural(primero.Total, "movimiento", "movimientos")} ({primero.Total * 100.0 / Math.Max(1, total):0}% del total), {motivo}. " +
                                            $"{datos.RankingHuespedes.Count} huéspedes tuvieron actividad.";
            }
        }

        private void AgregarKpi(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 8) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelKpis.Controls.Add(tarjeta);
        }
    }
}
