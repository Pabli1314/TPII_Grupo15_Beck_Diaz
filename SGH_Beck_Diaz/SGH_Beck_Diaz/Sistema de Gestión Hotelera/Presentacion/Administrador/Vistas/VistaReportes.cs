using Entidades;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Logica;
using Presentacion.Administrador.UI;
using SkiaSharp;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaReportes : UserControl, IVistaAdministrador
    {
        private readonly GestionReportes _gestionReportes = new();

        private readonly ComboBox _cmbFiltro;
        private readonly DateTimePicker _dtpDesde;
        private readonly DateTimePicker _dtpHasta;
        private readonly FlowLayoutPanel _panelKpis;
        private readonly CartesianChart _chartOcupacion;
        private readonly CartesianChart _chartIngresosDiarios;
        private readonly CartesianChart _chartIngresosPorHabitacion;
        private readonly CartesianChart _chartRotacion;

        public VistaReportes()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var barraFiltros = new Panel { Location = new Point(0, 0), Size = new Size(1400, 40) };

            _cmbFiltro = CamposFormulario.Combo(new Point(0, 0), 180);
            _cmbFiltro.Items.AddRange(new object[] { "Hoy", "Esta semana", "Este mes", "Rango personalizado" });
            _cmbFiltro.SelectedIndex = 0;
            _cmbFiltro.SelectedIndexChanged += (s, e) => AlternarRango();

            _dtpDesde = new DateTimePicker { Location = new Point(196, 1), Width = 130, Format = DateTimePickerFormat.Short, Enabled = false };
            _dtpHasta = new DateTimePicker { Location = new Point(334, 1), Width = 130, Format = DateTimePickerFormat.Short, Enabled = false };

            var btnGenerar = EstiloBoton.Primario(new Button { Text = "Generar", Size = new Size(110, 34), Location = new Point(478, 0) });
            btnGenerar.Click += (s, e) => Refrescar();

            var btnPdf = EstiloBoton.Secundario(new Button { Text = "Exportar PDF", Size = new Size(130, 34), Location = new Point(1050, 0) });
            var btnExcel = EstiloBoton.Secundario(new Button { Text = "Exportar Excel", Size = new Size(130, 34), Location = new Point(1190, 0) });
            var btnImprimir = EstiloBoton.Secundario(new Button { Text = "Imprimir", Size = new Size(110, 34), Location = new Point(1330, 0) });
            void MostrarAvisoExportacion(object? s, EventArgs e) => MessageBox.Show(
                "Esta acción queda lista para integrarse con una librería real de exportación/impresión.",
                "Función en preparación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnPdf.Click += MostrarAvisoExportacion;
            btnExcel.Click += MostrarAvisoExportacion;
            btnImprimir.Click += MostrarAvisoExportacion;

            barraFiltros.Controls.Add(_cmbFiltro);
            barraFiltros.Controls.Add(_dtpDesde);
            barraFiltros.Controls.Add(_dtpHasta);
            barraFiltros.Controls.Add(btnGenerar);
            barraFiltros.Controls.Add(btnPdf);
            barraFiltros.Controls.Add(btnExcel);
            barraFiltros.Controls.Add(btnImprimir);

            _panelKpis = new FlowLayoutPanel { Location = new Point(0, 52), Size = new Size(1400, 130), FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true };

            _chartOcupacion = CrearGrafico();
            _chartIngresosDiarios = CrearGrafico();
            _chartIngresosPorHabitacion = CrearGrafico();
            _chartRotacion = CrearGrafico();

            var panelGraficos = new TableLayoutPanel
            {
                Location = new Point(0, 196),
                Size = new Size(1400, 700),
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = false
            };
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panelGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            panelGraficos.Controls.Add(EnvolverConTitulo("Ocupación diaria", _chartOcupacion), 0, 0);
            panelGraficos.Controls.Add(EnvolverConTitulo("Ingresos por día", _chartIngresosDiarios), 1, 0);
            panelGraficos.Controls.Add(EnvolverConTitulo("Ranking de ingresos por habitación", _chartIngresosPorHabitacion), 0, 1);
            panelGraficos.Controls.Add(EnvolverConTitulo("Rotación por habitación", _chartRotacion), 1, 1);

            Controls.Add(panelGraficos);
            Controls.Add(_panelKpis);
            Controls.Add(barraFiltros);
        }

        private static CartesianChart CrearGrafico() => new CartesianChart
        {
            Dock = DockStyle.Fill,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden
        };

        private static Panel EnvolverConTitulo(string titulo, Control grafico)
        {
            var contenedor = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Paleta.FondoTarjeta, Padding = new Padding(16) };
            var lblTitulo = new Label { Text = titulo, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, Dock = DockStyle.Top, Height = 26 };
            grafico.Dock = DockStyle.Fill;
            contenedor.Controls.Add(grafico);
            contenedor.Controls.Add(lblTitulo);
            return contenedor;
        }

        private void AlternarRango()
        {
            bool esPersonalizado = _cmbFiltro.SelectedItem as string == "Rango personalizado";
            _dtpDesde.Enabled = esPersonalizado;
            _dtpHasta.Enabled = esPersonalizado;
        }

        public void Refrescar()
        {
            var (desde, hasta) = ObtenerRango();
            ReporteHotel reporte = _gestionReportes.Generar(desde, hasta);

            _panelKpis.Controls.Clear();
            AgregarKpi("Total de ocupaciones", reporte.TotalOcupaciones.ToString(), Paleta.Primario, Icono.Cama);
            AgregarKpi("Total de ingresos", reporte.TotalIngresos.ToString("C0"), Paleta.Exito, Icono.Etiqueta);
            AgregarKpi("Promedio de ocupación", $"{reporte.PromedioOcupacionPorcentaje:0.#}%", Paleta.Mantenimiento, Icono.Grafico);
            AgregarKpi("Promedio de duración", $"{reporte.PromedioDuracionEstadiaDias:0.#} noches", Paleta.Primario, Icono.Reloj);
            AgregarKpi("Promedio de limpieza", $"{reporte.PromedioLimpiezaMinutos:0} min", Paleta.Limpieza, Icono.Alerta);

            CargarGrafico(_chartOcupacion, reporte.OcupacionDiaria, Paleta.Primario);
            CargarGrafico(_chartIngresosDiarios, reporte.IngresosDiarios, Paleta.Exito);
            CargarGrafico(_chartIngresosPorHabitacion, reporte.IngresosPorHabitacion, Paleta.Mantenimiento);
            CargarGrafico(_chartRotacion, reporte.RotacionPorHabitacion, Paleta.Limpieza);
        }

        private static void CargarGrafico(CartesianChart chart, System.Collections.Generic.List<PuntoSerie> puntos, Color color)
        {
            chart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = puntos.Select(p => p.Valor).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(color.R, color.G, color.B)),
                    MaxBarWidth = 32
                }
            };

            chart.XAxes = new[] { new Axis { Labels = puntos.Select(p => p.Etiqueta).ToArray(), LabelsRotation = 0 } };
        }

        private void AgregarKpi(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 16) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelKpis.Controls.Add(tarjeta);
        }

        private (DateTime desde, DateTime hasta) ObtenerRango()
        {
            DateTime hoy = DateTime.Today;

            return _cmbFiltro.SelectedItem as string switch
            {
                "Esta semana" => (hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)), hoy),
                "Este mes" => (new DateTime(hoy.Year, hoy.Month, 1), hoy),
                "Rango personalizado" => (_dtpDesde.Value.Date, _dtpHasta.Value.Date),
                _ => (hoy, hoy)
            };
        }
    }
}
