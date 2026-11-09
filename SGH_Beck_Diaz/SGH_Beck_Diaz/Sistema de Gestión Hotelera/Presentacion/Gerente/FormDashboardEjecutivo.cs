using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista;
using Presentacion.Recepcionista.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Presentacion.Gerente.ComponentesGerencia;

namespace Presentacion.Gerente
{
    /// <summary>
    /// Dashboard Ejecutivo: KPIs hoteleros del mes en curso (ocupación, ADR, RevPAR e ingresos) con
    /// comparativas automáticas mes contra mes e interanual, la meta de ocupación frente a lo real y la
    /// tendencia de los últimos 12 meses.
    /// </summary>
    public class FormDashboardEjecutivo : Form
    {
        private static readonly Color ColorVentas = Color.FromArgb(13, 148, 136);
        private static readonly Color ColorOcupacion = Color.FromArgb(217, 119, 6);

        private readonly GestionGerente _gestionGerente = new();
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();
        private readonly List<Control> _anchoCompleto = new();

        private readonly FlowLayoutPanel _raiz;
        private readonly Label _lblPeriodo;
        private readonly Label _lblComparacion;

        private readonly FlowLayoutPanel _filaKpis;
        private readonly TarjetaKpi _kpiOcupacion;
        private readonly TarjetaKpi _kpiAdr;
        private readonly TarjetaKpi _kpiRevPar;
        private readonly TarjetaKpi _kpiIngresos;

        private readonly FlowLayoutPanel _filaComparativas;
        private readonly DataGridView _dgvComparativa;
        private readonly PanelReporte _panelComparativa;
        private readonly MetaVsReal _metaVsReal;
        private readonly PanelReporte _panelMeta;

        private readonly CartesianChart _chartTendencia;
        private readonly PanelReporte _panelTendencia;

        public FormDashboardEjecutivo()
        {
            BackColor = Paleta.FondoApp;
            _raiz = CrearRaiz();

            // ================= Encabezado =================
            (Panel encabezado, _lblPeriodo, _lblComparacion) = CrearEncabezado();
            _anchoCompleto.Add(encabezado);

            // ================= KPIs =================
            _kpiOcupacion = new TarjetaKpi(_toolTip);
            _kpiAdr = new TarjetaKpi(_toolTip);
            _kpiRevPar = new TarjetaKpi(_toolTip);
            _kpiIngresos = new TarjetaKpi(_toolTip);
            _filaKpis = CrearFila(_kpiOcupacion, _kpiAdr, _kpiRevPar, _kpiIngresos);

            // ================= Comparativas y meta =================
            _dgvComparativa = CrearGrilla();
            _dgvComparativa.Columns.Add("indicador", "Indicador");
            _dgvComparativa.Columns.Add("actual", "Actual");
            _dgvComparativa.Columns.Add("mesAnterior", "Mes ant.");
            _dgvComparativa.Columns.Add("varMes", "Var. mes");
            _dgvComparativa.Columns.Add("anioAnterior", "Año ant.");
            _dgvComparativa.Columns.Add("varAnio", "Var. año");
            PesosColumnas(_dgvComparativa, ("indicador", 160));
            AlinearDerecha(_dgvComparativa, "actual", "mesAnterior", "varMes", "anioAnterior", "varAnio");
            _dgvComparativa.CellFormatting += (s, e) => ColorearVariacion(_dgvComparativa, e, "varMes", "varAnio");

            _panelComparativa = new PanelReporte(_toolTip, "Comparativa automática",
                "Cada indicador del mes en curso frente a los mismos días del mes anterior (mensual) y del mismo mes del año pasado (interanual). Verde: mejoró. Rojo: empeoró.",
                _dgvComparativa, new Size(800, 400)) { TextoPie = "Se comparan siempre los mismos días, así un mes recién empezado no queda en desventaja." };

            _metaVsReal = new MetaVsReal();
            _panelMeta = new PanelReporte(_toolTip, "Meta vs. real",
                $"El hotel no tiene un presupuesto cargado: la referencia es la meta de ocupación del {GestionGerente.MetaOcupacionPorcentaje:0}% que usan también los Reportes del Administrador. El ingreso a la meta se calcula con el ADR del mes.",
                _metaVsReal, new Size(400, 400)) { TextoPie = "La barra se llena con la ocupación real. La línea marca la meta." };

            _filaComparativas = CrearFila(_panelComparativa, _panelMeta);

            // ================= Tendencia =================
            _chartTendencia = new CartesianChart { LegendPosition = LegendPosition.Bottom, BackColor = Paleta.FondoTarjeta };
            _panelTendencia = new PanelReporte(_toolTip, "Tendencia de los últimos 12 meses",
                "Columnas: ingresos de alojamiento y de ventas de cada mes (eje izquierdo). Línea: ocupación del mes (eje derecho). El último mes está en curso.",
                _chartTendencia, new Size(1200, 380)) { TextoPie = "Pasá el mouse sobre una columna para ver el detalle del mes." };
            _anchoCompleto.Add(_panelTendencia);

            Panel tituloKpis = TituloSeccion("Indicadores clave", "Ocupación, tarifa promedio, ingreso por habitación disponible e ingresos del mes en curso.");
            Panel tituloComparativas = TituloSeccion("Comparativas", "Mes contra mes, interanual y meta contra lo real.");
            Panel tituloTendencia = TituloSeccion("Evolución", "Cómo vienen los ingresos y la ocupación mes a mes.");
            _anchoCompleto.AddRange(new Control[] { tituloKpis, tituloComparativas, tituloTendencia });

            _raiz.Controls.AddRange(new Control[] { encabezado, tituloKpis, _filaKpis, tituloComparativas, _filaComparativas, tituloTendencia, _panelTendencia });
            Controls.Add(_raiz);

            _raiz.Resize += (s, e) => Acomodar();
            Load += (s, e) => CargarDatos();
        }

        // =====================================================================
        // Datos
        // =====================================================================

        private void CargarDatos()
        {
            try
            {
                IndicadoresGerencia ind = _gestionGerente.ObtenerIndicadoresMes();
                MetricasPeriodo act = ind.Actual, mes = ind.MesAnterior, anio = ind.AnioAnterior;

                _lblPeriodo.Text = $"Mes en curso: {act.Desde:dd/MM} al {act.Hasta:dd/MM/yyyy}";
                _lblComparacion.Text = $"Comparado con los mismos días de {NombreMes(mes.Desde)} (mensual) y de {NombreMes(anio.Desde)} (interanual) · {act.Habitaciones} habitaciones";

                string interanual = $"Interanual ({NombreMes(anio.Desde)}):";

                _kpiOcupacion.Configurar("Ocupación (Occupancy Rate)", Porcentaje(act.Ocupacion),
                    "Noches vendidas sobre noches disponibles (habitaciones × días del período).",
                    Icono.Cama, Paleta.Primario,
                    Comparativa.EnPuntos(act.Ocupacion, mes.Ocupacion, "el mes pasado"),
                    $"{interanual} {VariacionPuntos(act.Ocupacion, anio.Ocupacion).Texto} · Meta {ind.MetaOcupacion:0}%",
                    alerta: act.NochesVendidas > 0 && act.Ocupacion < ind.MetaOcupacion);

                _kpiAdr.Configurar("ADR (tarifa promedio diaria)", Moneda(act.Adr),
                    "Average Daily Rate: ingreso de alojamiento dividido por las noches vendidas. Indica a qué precio promedio se vendió cada noche.",
                    Icono.Etiqueta, Color.FromArgb(124, 58, 237),
                    Comparativa.Porcentual((double)act.Adr, (double)mes.Adr, "el mes pasado"),
                    $"{interanual} {Variacion((double)act.Adr, (double)anio.Adr).Texto} · {act.NochesVendidas} noches vendidas");

                _kpiRevPar.Configurar("RevPAR (ingreso por hab. disponible)", Moneda(act.RevPar),
                    "Revenue Per Available Room: ingreso de alojamiento dividido por las noches disponibles. Es el ADR multiplicado por la ocupación: combina precio y ocupación en un solo número.",
                    Icono.Grafico, ColorVentas,
                    Comparativa.Porcentual((double)act.RevPar, (double)mes.RevPar, "el mes pasado"),
                    $"{interanual} {Variacion((double)act.RevPar, (double)anio.RevPar).Texto}");

                _kpiIngresos.Configurar("Ingresos del mes", Moneda(act.IngresosTotales),
                    "Alojamiento (noches ocupadas × tarifa del tipo de habitación) más las ventas de productos y servicios registradas en el mes.",
                    Icono.Caja, ColorOcupacion,
                    Comparativa.Porcentual((double)act.IngresosTotales, (double)mes.IngresosTotales, "el mes pasado"),
                    $"Alojamiento {Moneda(act.IngresosAlojamiento)} · Ventas {Moneda(act.IngresosVentas)} · {interanual} {Variacion((double)act.IngresosTotales, (double)anio.IngresosTotales).Texto}");

                CargarComparativa(act, mes, anio);
                CargarMeta(act, ind.MetaOcupacion);
                CargarTendencia(ind.Tendencia);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudieron cargar los indicadores: {ex.Message}", "Dashboard Ejecutivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Acomodar();
        }

        private void CargarComparativa(MetricasPeriodo act, MetricasPeriodo mes, MetricasPeriodo anio)
        {
            _dgvComparativa.Rows.Clear();

            void Fila(string nombre, double a, double m, double y, Func<double, string> formato, bool enPuntos = false)
            {
                var vm = enPuntos ? VariacionPuntos(a, m) : Variacion(a, m);
                var vy = enPuntos ? VariacionPuntos(a, y) : Variacion(a, y);
                int i = _dgvComparativa.Rows.Add(nombre, formato(a), formato(m), vm.Texto, formato(y), vy.Texto);
                _dgvComparativa.Rows[i].Cells["varMes"].Tag = vm.Signo;
                _dgvComparativa.Rows[i].Cells["varAnio"].Tag = vy.Signo;
            }

            string M(double v) => Moneda((decimal)v);

            Fila("Ocupación", act.Ocupacion, mes.Ocupacion, anio.Ocupacion, Porcentaje, enPuntos: true);
            Fila("ADR", (double)act.Adr, (double)mes.Adr, (double)anio.Adr, M);
            Fila("RevPAR", (double)act.RevPar, (double)mes.RevPar, (double)anio.RevPar, M);
            Fila("Noches vendidas", act.NochesVendidas, mes.NochesVendidas, anio.NochesVendidas, v => v.ToString("0", CulturaAr));
            Fila("Alojamiento", (double)act.IngresosAlojamiento, (double)mes.IngresosAlojamiento, (double)anio.IngresosAlojamiento, M);
            Fila("Ventas",(double)act.IngresosVentas, (double)mes.IngresosVentas, (double)anio.IngresosVentas, M);
            Fila("Ingresos totales", (double)act.IngresosTotales, (double)mes.IngresosTotales, (double)anio.IngresosTotales, M);

            _dgvComparativa.Rows[^1].DefaultCellStyle.Font = Paleta.FuenteBaseNegrita;
            _dgvComparativa.ClearSelection();
        }

        private void CargarMeta(MetricasPeriodo act, double meta)
        {
            int diasMes = DateTime.DaysInMonth(act.Desde.Year, act.Desde.Month);
            double nochesMeta = Math.Ceiling(meta / 100 * act.Habitaciones * diasMes);
            int faltan = (int)Math.Max(0, nochesMeta - act.NochesVendidas);

            var lineas = new List<string>
            {
                $"Ocupación real: {Porcentaje(act.Ocupacion)} · Meta: {meta:0}%"
            };

            if (act.Habitaciones == 0)
            {
                lineas.Add("No hay habitaciones cargadas para calcular la meta.");
            }
            else if (faltan == 0)
            {
                lineas.Add($"Las noches vendidas del mes ({act.NochesVendidas}) ya cubren la meta del mes completo ({nochesMeta:0}).");
            }
            else
            {
                lineas.Add($"Para cerrar {NombreMes(act.Desde)} en la meta faltan {faltan} noches vendidas de {nochesMeta:0} (quedan {diasMes - act.Dias} días).");
            }

            if (act.Adr > 0)
            {
                decimal ingresoMeta = (decimal)nochesMeta * act.Adr;
                double avance = ingresoMeta > 0 ? (double)(act.IngresosAlojamiento / ingresoMeta * 100) : 0;
                lineas.Add($"Ingreso de alojamiento a la meta: {Moneda(ingresoMeta)}. Real a la fecha: {Moneda(act.IngresosAlojamiento)} ({Porcentaje(avance)} de la meta).");
            }
            else
            {
                lineas.Add("Todavía no hay noches vendidas en el mes para estimar el ingreso a la meta.");
            }

            _metaVsReal.Cargar(act.Ocupacion, meta, lineas);
        }

        private void CargarTendencia(List<PuntoMensual> tendencia)
        {
            var etiquetas = tendencia.Select(p => p.EnCurso ? p.Etiqueta + " *" : p.Etiqueta).ToList();

            _chartTendencia.Series = new ISeries[]
            {
                new StackedColumnSeries<double>
                {
                    Name = "Alojamiento",
                    Values = tendencia.Select(p => (double)p.Alojamiento).ToArray(),
                    Fill = new SolidColorPaint(Skia(Paleta.Primario)),
                    MaxBarWidth = 38,
                    YToolTipLabelFormatter = p => Moneda((decimal)p.Coordinate.PrimaryValue)
                },
                new StackedColumnSeries<double>
                {
                    Name = "Ventas",
                    Values = tendencia.Select(p => (double)p.Ventas).ToArray(),
                    Fill = new SolidColorPaint(Skia(ColorVentas)),
                    MaxBarWidth = 38,
                    YToolTipLabelFormatter = p => Moneda((decimal)p.Coordinate.PrimaryValue)
                },
                new LineSeries<double>
                {
                    Name = "Ocupación",
                    Values = tendencia.Select(p => Math.Round(p.Ocupacion, 1)).ToArray(),
                    ScalesYAt = 1,
                    Fill = null,
                    Stroke = new SolidColorPaint(Skia(ColorOcupacion)) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(Skia(ColorOcupacion)) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(Skia(Color.White)),
                    LineSmoothness = 0,
                    YToolTipLabelFormatter = p => Porcentaje(p.Coordinate.PrimaryValue)
                }
            };

            _chartTendencia.XAxes = new[] { EjeCategorias(etiquetas) };
            Axis ejeOcupacion = EjeValores(v => $"{v:0}%", 100);
            ejeOcupacion.Position = AxisPosition.End;
            ejeOcupacion.SeparatorsPaint = null;
            _chartTendencia.YAxes = new[] { EjeValores(MonedaCorta), ejeOcupacion };

            _panelTendencia.TextoPie = "* Mes en curso (hasta hoy). Pasá el mouse sobre una columna para ver el detalle del mes.";
        }

        // =====================================================================
        // Layout responsivo
        // =====================================================================

        private void Acomodar()
        {
            if (_raiz == null) return;

            int ancho = AnchoUtil(_raiz);
            _raiz.SuspendLayout();

            foreach (Control c in _anchoCompleto) c.Width = ancho;

            DistribuirEnFila(_filaKpis, ancho, ancho >= 1100 ? 4 : 2, 158);

            int altoComparativa = AltoTabla(_dgvComparativa, 8);
            if (ancho >= 1000)
            {
                int anchoMeta = Math.Max(320, ancho * 30 / 100);
                _panelComparativa.Size = new Size(ancho - anchoMeta - Separacion, altoComparativa);
                _panelComparativa.Margin = new Padding(0, 0, Separacion, Separacion);
                _panelMeta.Size = new Size(anchoMeta, altoComparativa);
                _panelMeta.Margin = new Padding(0, 0, 0, Separacion);
                _filaComparativas.Size = new Size(ancho, altoComparativa + Separacion);
            }
            else
            {
                _panelComparativa.Size = new Size(ancho, altoComparativa);
                _panelComparativa.Margin = new Padding(0, 0, 0, Separacion);
                _panelMeta.Size = new Size(ancho, 300);
                _panelMeta.Margin = new Padding(0, 0, 0, Separacion);
                _filaComparativas.Size = new Size(ancho, altoComparativa + 300 + Separacion * 2);
            }

            _raiz.ResumeLayout(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>Barra de ocupación real con la marca de la meta y las conclusiones debajo.</summary>
        private sealed class MetaVsReal : Control
        {
            private double _real;
            private double _meta;
            private List<string> _lineas = new();

            public MetaVsReal()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            public void Cargar(double real, double meta, List<string> lineas)
            {
                _real = Math.Max(0, Math.Min(100, real));
                _meta = meta;
                _lineas = lineas;
                AccessibleName = string.Join(" ", lineas);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                bool alcanza = _real >= _meta;
                Color colorReal = alcanza ? Paleta.Exito : EstiloReporte.Advertencia;

                using var fuenteValor = new Font("Segoe UI Semibold", 26f);
                using var fuenteChica = Paleta.FuenteChica;
                using var fuente = Paleta.FuenteBase;

                TextRenderer.DrawText(g, Porcentaje(_real), fuenteValor, new Point(0, 4), colorReal);
                Size medida = TextRenderer.MeasureText(Porcentaje(_real), fuenteValor);
                TextRenderer.DrawText(g, alcanza ? "en la meta o por encima" : $"{(_meta - _real).ToString("0", CulturaAr)} pts por debajo de la meta",
                    fuente, new Rectangle(medida.Width + 4, 4, Math.Max(10, Width - medida.Width - 4), medida.Height), Paleta.TextoSecundario, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);

                // Barra 0–100 %
                var pista = new RectangleF(0, medida.Height + 22, Width - 1, 18);
                using (var ruta = DibujoUtil.RutaRedondeada(pista, 9))
                using (var fondo = new SolidBrush(Paleta.BordeSuave))
                {
                    g.FillPath(fondo, ruta);
                }

                float anchoReal = (float)(pista.Width * _real / 100);
                if (anchoReal > 0)
                {
                    using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(pista.X, pista.Y, Math.Max(anchoReal, 18), pista.Height), 9);
                    using var brocha = new SolidBrush(colorReal);
                    g.FillPath(brocha, ruta);
                }

                float xMeta = pista.X + (float)(pista.Width * _meta / 100);
                using (var lapiz = new Pen(Paleta.TextoPrimario, 2))
                {
                    g.DrawLine(lapiz, xMeta, pista.Y - 6, xMeta, pista.Bottom + 6);
                }
                string textoMeta = $"Meta {_meta:0}%";
                int anchoTextoMeta = TextRenderer.MeasureText(textoMeta, fuenteChica).Width;
                int xTexto = (int)Math.Min(Math.Max(0, xMeta - anchoTextoMeta / 2f), Width - anchoTextoMeta);
                TextRenderer.DrawText(g, textoMeta, fuenteChica, new Point(xTexto, (int)pista.Bottom + 8), Paleta.TextoSecundario);

                // Conclusiones
                int y = (int)pista.Bottom + 34;
                foreach (string linea in _lineas)
                {
                    int alto = TextRenderer.MeasureText(linea, fuente, new Size(Width, int.MaxValue), TextFormatFlags.WordBreak).Height;
                    TextRenderer.DrawText(g, linea, fuente, new Rectangle(0, y, Width, alto), Paleta.TextoPrimario, TextFormatFlags.WordBreak);
                    y += alto + 8;
                }
            }
        }
    }
}
