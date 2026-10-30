using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Entidades;
using Logica;

namespace Presentacion.Gerente
{
    public class FormDashboardEjecutivo : Form
    {
        private readonly LGerente _lGerente;
        private Chart chartIngresos;
        private Chart chartHabitaciones;

        // Referencias de UI para actualizar TODAS las tarjetas KPI en tiempo real
        private Label lblValorTasaOcupacion, lblSubtasaOcupacion;
        private Label lblValorRevPAR, lblSubRevPAR;
        private Label lblValorADR, lblSubADR;
        private Label lblValorIngresosMes, lblSubIngresosMes;

        public FormDashboardEjecutivo()
        {
            _lGerente = new LGerente();
            InitializeComponentes();
            this.Load += (s, e) => CargarDatosGraficos();
        }

        private void InitializeComponentes()
        {
            this.BackColor = Color.FromArgb(240, 243, 246);

            // --- 1. PANEL SUPERIOR (KPIs Dinámicos) ---
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

            // Creación de tarjetas KPI vinculadas a referencias dinámicas
            Panel cardOcupacion = CrearCardKPI("TASA OCUPACIÓN", "0.0%", "Calculando...", Color.FromArgb(16, 185, 129), out lblValorTasaOcupacion, out lblSubtasaOcupacion);
            Panel cardRevPAR = CrearCardKPI("REVPAR (Ingreso/Hab)", "$0", "Ingreso por hab. disponible", Color.FromArgb(59, 130, 246), out lblValorRevPAR, out lblSubRevPAR);
            Panel cardADR = CrearCardKPI("ADR (Tarifa Promed.)", "$0", "Promedio diario de venta", Color.FromArgb(139, 92, 246), out lblValorADR, out lblSubADR);
            Panel cardIngresos = CrearCardKPI("INGRESOS MES", "$0", "Acumulado mes en curso", Color.FromArgb(245, 158, 11), out lblValorIngresosMes, out lblSubIngresosMes);

            tlpKpis.Controls.Add(cardOcupacion, 0, 0);
            tlpKpis.Controls.Add(cardRevPAR, 1, 0);
            tlpKpis.Controls.Add(cardADR, 2, 0);
            tlpKpis.Controls.Add(cardIngresos, 3, 0);

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

            // A) GRÁFICO DE BARRAS
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
                IsXValueIndexed = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            chartIngresos.Series.Add(serieIngresos);
            chartIngresos.Titles.Add(new Title("📊 Recaudación Mensual (Cajas)", Docking.Top, new Font("Segoe UI", 11F, FontStyle.Bold), Color.FromArgb(30, 41, 59)));

            // B) GRÁFICO DE TORTA
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
                // 1. Recaudación histórica y actualización del gráfico de barras
                Dictionary<string, decimal> datosIngresos = _lGerente.ObtenerEstadisticasIngresosMensuales();
                chartIngresos.Series["IngresosMensuales"].Points.Clear();

                foreach (var item in datosIngresos)
                {
                    chartIngresos.Series["IngresosMensuales"].Points.AddXY(item.Key, item.Value);
                }

                // 2. Estado de Habitaciones y cálculo dinámico de Tasa de Ocupación
                Dictionary<string, int> datosHabitaciones = _lGerente.ObtenerEstadisticasHabitacionesDashboard();
                chartHabitaciones.Series["EstadoHabitaciones"].Points.Clear();

                int totalHabitaciones = 0;
                int habitacionesOcupadas = 0;

                foreach (var item in datosHabitaciones)
                {
                    int cantidad = item.Value;
                    totalHabitaciones += cantidad;

                    string clave = item.Key.ToLower().Trim();
                    if (clave.StartsWith("ocupad"))
                    {
                        habitacionesOcupadas += cantidad;
                    }

                    int index = chartHabitaciones.Series["EstadoHabitaciones"].Points.AddXY(item.Key, cantidad);
                    DataPoint p = chartHabitaciones.Series["EstadoHabitaciones"].Points[index];

                    if (clave.StartsWith("disponib") || clave.StartsWith("libre"))
                        p.Color = Color.FromArgb(25, 135, 84);
                    else if (clave.StartsWith("ocupad"))
                        p.Color = Color.FromArgb(220, 53, 69);
                    else if (clave.StartsWith("limpiez"))
                        p.Color = Color.FromArgb(255, 193, 7);
                    else if (clave.StartsWith("manten"))
                        p.Color = Color.FromArgb(13, 110, 253);
                    else
                        p.Color = Color.FromArgb(59, 130, 246);
                }

                // KPI 1: Tasa de Ocupación Real
                if (totalHabitaciones > 0)
                {
                    double porcentaje = ((double)habitacionesOcupadas / totalHabitaciones) * 100.0;
                    lblValorTasaOcupacion.Text = $"{porcentaje:F1}%";
                    lblSubtasaOcupacion.Text = $"{habitacionesOcupadas} de {totalHabitaciones} habs. ocupadas";
                }
                else
                {
                    lblValorTasaOcupacion.Text = "0.0%";
                    lblSubtasaOcupacion.Text = "Sin datos de habitaciones";
                }

                // KPIs 2, 3 y 4: REVPAR, ADR e INGRESOS MES desde BBDD
                var (adr, revPar, ingresoMesActual) = _lGerente.ObtenerMetricasKpiDashboard();

                lblValorRevPAR.Text = revPar.ToString("C0");
                lblSubRevPAR.Text = "Recaudación / Hab. totales";

                lblValorADR.Text = adr.ToString("C0");
                lblSubADR.Text = "Recaudación / Hab. ocupadas";

                lblValorIngresosMes.Text = ingresoMesActual.ToString("C0");
                lblSubIngresosMes.Text = $"Mes {DateTime.Now:MMMM yyyy}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos estadísticos: {ex.Message}", "Error de Renderizado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CrearCardKPI(string titulo, string valor, string subtitulo, Color colorBorde, out Label lblValueOut, out Label lblSubOut)
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

            lblValueOut = lblValue;
            lblSubOut = lblSub;

            return card;
        }
    }
}