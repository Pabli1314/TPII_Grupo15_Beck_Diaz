using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Logica;

namespace Presentacion.Gerente
{
    public class FormDashboardEjecutivo : Form
    {
        private readonly GestionGerente _lGerente;
        private Chart chartIngresos;
        private Chart chartHabitaciones;

        public FormDashboardEjecutivo()
        {
            _lGerente = new GestionGerente();
            InitializeComponentes();
            this.Load += (s, e) => CargarDatosGraficos();
        }

        private void InitializeComponentes()
        {
            this.BackColor = Color.FromArgb(240, 243, 246);

            // --- 1. PANEL SUPERIOR (KPIs) ---
            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10)
            };

            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            tlpKpis.Controls.Add(CrearCardKPI("TASA OCUPACIÓN", "78.5%", "⬆ +4.2% vs mes anterior", Color.FromArgb(16, 185, 129)), 0, 0);
            tlpKpis.Controls.Add(CrearCardKPI("REVPAR (Ingreso/Hab)", "$35.400", "Tarifa por Hab. Disponible", Color.FromArgb(59, 130, 246)), 1, 0);
            tlpKpis.Controls.Add(CrearCardKPI("ADR (Tarifa Promed.)", "$45.100", "Promedio diario venta", Color.FromArgb(139, 92, 246)), 2, 0);
            tlpKpis.Controls.Add(CrearCardKPI("INGRESOS MES", "$12.450.000", "Meta proyectada: 85%", Color.FromArgb(245, 158, 11)), 3, 0);

            // --- 2. CONTENEDOR DE GRÁFICOS ---
            TableLayoutPanel tlpGraficos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // A) GRÁFICO DE BARRAS (INGRESOS POR MES)
            chartIngresos = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5)
            };
            ChartArea areaIngresos = new ChartArea("AreaIngresos");
            areaIngresos.AxisX.MajorGrid.LineColor = Color.LightGray;
            areaIngresos.AxisY.MajorGrid.LineColor = Color.LightGray;
            areaIngresos.AxisY.LabelStyle.Format = "C0";
            areaIngresos.AxisX.Interval = 1;
            chartIngresos.ChartAreas.Add(areaIngresos);

            Series serieIngresos = new Series("IngresosMensuales")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(13, 202, 240),
                IsValueShownAsLabel = true,
                LabelFormat = "{0:C0}",
                // X es texto (mes): sin esto todas las barras caen en la misma posición y se superponen.
                IsXValueIndexed = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            chartIngresos.Series.Add(serieIngresos);
            chartIngresos.Titles.Add(new Title("📊 Recaudación Mensual (Cajas)", Docking.Top, new Font("Segoe UI", 11F, FontStyle.Bold), Color.FromArgb(30, 41, 59)));

            // B) GRÁFICO DE TORTA (ESTADO DE HABITACIONES)
            chartHabitaciones = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5)
            };
            ChartArea areaHabitaciones = new ChartArea("AreaHabitaciones");
            chartHabitaciones.ChartAreas.Add(areaHabitaciones);

            Series serieHabitaciones = new Series("EstadoHabitaciones")
            {
                ChartType = SeriesChartType.Pie,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                IsValueShownAsLabel = true,
                Label = "#VALX: #PERCENT{P1}"
            };

            serieHabitaciones["PieLabelStyle"] = "Outside";
            chartHabitaciones.Series.Add(serieHabitaciones);

            Legend leyenda = new Legend("LeyendaHabitaciones")
            {
                Docking = Docking.Bottom,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            chartHabitaciones.Legends.Add(leyenda);
            chartHabitaciones.Titles.Add(new Title("🏨 Estado Actual de Habitaciones", Docking.Top, new Font("Segoe UI", 11F, FontStyle.Bold), Color.FromArgb(30, 41, 59)));

            tlpGraficos.Controls.Add(chartIngresos, 0, 0);
            tlpGraficos.Controls.Add(chartHabitaciones, 1, 0);

            this.Controls.Add(tlpGraficos);
            this.Controls.Add(tlpKpis);
        }

        private void CargarDatosGraficos()
        {
            try
            {
                // 1. Cargar gráfico de Ingresos (corregido _lGer por _lGerente)
                Dictionary<string, decimal> datosIngresos = _lGerente.ObtenerEstadisticasIngresosMensuales();
                chartIngresos.Series["IngresosMensuales"].Points.Clear();

                foreach (var item in datosIngresos)
                {
                    chartIngresos.Series["IngresosMensuales"].Points.AddXY(item.Key, item.Value);
                }

                // 2. Cargar gráfico de Estado de Habitaciones
                Dictionary<string, int> datosHabitaciones = _lGerente.ObtenerEstadisticasHabitacionesDashboard();
                chartHabitaciones.Series["EstadoHabitaciones"].Points.Clear();

                foreach (var item in datosHabitaciones)
                {
                    int index = chartHabitaciones.Series["EstadoHabitaciones"].Points.AddXY(item.Key, item.Value);
                    DataPoint p = chartHabitaciones.Series["EstadoHabitaciones"].Points[index];

                    switch (item.Key.ToLower().Trim())
                    {
                        case "libre":
                        case "disponible":
                            p.Color = Color.FromArgb(25, 135, 84);
                            break;
                        case "ocupada":
                        case "ocupado":
                            p.Color = Color.FromArgb(220, 53, 69);
                            break;
                        case "limpieza":
                            p.Color = Color.FromArgb(255, 193, 7);
                            break;
                        case "mantenimiento":
                            p.Color = Color.FromArgb(13, 110, 253);
                            break;
                        default:
                            p.Color = Color.FromArgb(59, 130, 246);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos estadísticos: {ex.Message}", "Error de Renderizado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CrearCardKPI(string titulo, string valor, string subtitulo, Color colorBorde)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5),
                Padding = new Padding(10)
            };

            Panel pnlAcento = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = colorBorde
            };

            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.Gray,
                Dock = DockStyle.Top,
                Height = 20
            };

            Label lblValue = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 35
            };

            Label lblSub = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill
            };

            card.Controls.Add(lblSub);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            card.Controls.Add(pnlAcento);

            return card;
        }
    }
}