using Entidades;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Presentacion.Recepcionista;
using Presentacion.Recepcionista.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Comparativa lista para mostrar: chip de color ("▲ 12%") + frase ("más que el mes pasado").</summary>
    internal readonly record struct Comparativa(string Chip, string Texto, TonoEtiqueta Tono)
    {
        /// <summary>Variación relativa (%) entre dos valores. masEsMejor define si subir es verde o rojo.</summary>
        public static Comparativa Porcentual(double actual, double anterior, string contraQue, bool masEsMejor = true)
        {
            if (anterior <= 0)
            {
                return actual > 0
                    ? new Comparativa("Nuevo", $"{contraQue} no hubo datos para comparar", TonoEtiqueta.Neutro)
                    : new Comparativa("= 0%", $"sin cambios respecto a {contraQue}", TonoEtiqueta.Neutro);
            }

            double variacion = (actual - anterior) / anterior * 100;
            return Construir(variacion, $"{Math.Abs(variacion):0}%", contraQue, masEsMejor);
        }

        /// <summary>Diferencia en puntos porcentuales (para métricas que ya son %, como la ocupación).</summary>
        /// <param name="esMeta">true: "por encima/debajo de la meta"; false: "más/menos que el mes pasado".</param>
        public static Comparativa EnPuntos(double actual, double referencia, string contraQue, bool esMeta = false, bool masEsMejor = true)
        {
            double diferencia = actual - referencia;
            string chip = Math.Abs(diferencia) < 0.5 ? "0 pts" : $"{Math.Abs(diferencia):0} pts";
            return Construir(diferencia, chip, contraQue, masEsMejor, respectoA: esMeta);
        }

        private static Comparativa Construir(double variacion, string magnitud, string contraQue, bool masEsMejor, bool respectoA = false)
        {
            if (Math.Abs(variacion) < 0.5)
            {
                return new Comparativa("= " + magnitud, $"igual que {contraQue}", TonoEtiqueta.Neutro);
            }

            bool sube = variacion > 0;
            bool bueno = sube == masEsMejor;
            string texto = respectoA
                ? $"{(sube ? "por encima de" : "por debajo de")} {contraQue}"
                : $"{(sube ? "más" : "menos")} que {contraQue}";
            return new Comparativa($"{(sube ? "▲" : "▼")} {magnitud}", texto, bueno ? TonoEtiqueta.Exito : TonoEtiqueta.Peligro);
        }
    }

    /// <summary>
    /// Tarjeta de KPI destacada: ícono, título, ayuda «¿Qué significa?», valor grande, comparativa
    /// automática en un chip de color y una línea de contexto. Todo se dibuja a mano para que
    /// se vea igual en cualquier ancho.
    /// </summary>
    internal class TarjetaKpi : Control
    {
        private readonly IconoAyuda _ayuda;
        private string _titulo = string.Empty;
        private string _valor = "—";
        private string _contexto = string.Empty;
        private Comparativa? _comparativa;
        private Color _acento = Paleta.Primario;
        private Icono _icono = Icono.Grafico;
        private bool _alerta;

        public TarjetaKpi(ToolTip toolTip)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoApp;
            Size = new Size(260, 158);
            _ayuda = new IconoAyuda(toolTip, string.Empty);
            Controls.Add(_ayuda);
        }

        public void Configurar(string titulo, string valor, string ayuda, Icono icono, Color acento, Comparativa? comparativa, string contexto, bool alerta = false)
        {
            _titulo = titulo;
            _valor = valor;
            _contexto = contexto;
            _comparativa = comparativa;
            _icono = icono;
            _acento = acento;
            _alerta = alerta;
            _ayuda.CambiarTexto(ayuda);
            AccessibleName = $"{titulo}: {valor}. {(comparativa is { } c ? $"{c.Chip} {c.Texto}. " : string.Empty)}{contexto}";
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // El constructor base puede disparar OnResize antes de crear el ícono.
            if (_ayuda != null) _ayuda.Location = new Point(Width - _ayuda.Width - 14, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            EstiloReporte.DibujarTarjeta(g, Size, _alerta ? EstiloReporte.Advertencia : Paleta.Borde, _alerta ? 1.6f : 1f);

            // Ícono con fondo tenue del color de acento
            var badge = new RectangleF(18, 14, 34, 34);
            using (var ruta = DibujoUtil.RutaRedondeada(badge, 9))
            using (var fondo = new SolidBrush(Color.FromArgb(30, _acento)))
            {
                g.FillPath(fondo, ruta);
            }
            IconosUI.Dibujar(g, _icono, new Rectangle(25, 21, 20, 20), _acento, 1.7f);

            using var fuenteTitulo = Paleta.FuenteBaseNegrita;
            TextRenderer.DrawText(g, _titulo, fuenteTitulo, new Rectangle(60, 14, Math.Max(20, _ayuda.Left - 66), 34), Paleta.TextoSecundario,
                TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            using var fuenteValor = new Font("Segoe UI Semibold", 22f);
            TextRenderer.DrawText(g, _valor, fuenteValor, new Rectangle(14, 54, Width - 28, 44), _alerta ? EstiloReporte.Advertencia : Paleta.TextoPrimario,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int y = 104;
            if (_comparativa is { } c)
            {
                using var fuenteChip = new Font("Segoe UI Semibold", 8.75f);
                Size medida = TextRenderer.MeasureText(c.Chip, fuenteChip);
                var chip = new RectangleF(18, y, medida.Width + 12, 22);
                var (fondoChip, textoChip) = EstiloReporte.ColoresTono(c.Tono);
                if (c.Tono == TonoEtiqueta.Neutro) (fondoChip, textoChip) = (Paleta.BordeSuave, Paleta.TextoSecundario);

                using (var ruta = DibujoUtil.RutaRedondeada(chip, 11))
                using (var brocha = new SolidBrush(fondoChip))
                {
                    g.FillPath(brocha, ruta);
                }
                TextRenderer.DrawText(g, c.Chip, fuenteChip, Rectangle.Round(chip), textoChip, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                using var fuenteChica = Paleta.FuenteChica;
                int xTexto = (int)chip.Right + 6;
                TextRenderer.DrawText(g, c.Texto, fuenteChica, new Rectangle(xTexto, y, Math.Max(10, Width - xTexto - 12), 22), Paleta.TextoSecundario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                y += 28;
            }

            using var fuenteContexto = Paleta.FuenteChica;
            TextRenderer.DrawText(g, _contexto, fuenteContexto, new Rectangle(18, y, Width - 30, Height - y - 6), Paleta.TextoTerciario,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Bloque "Conclusiones rápidas": lista de frases simples con un ícono de color según si la
    /// noticia es buena (✓ verde), requiere atención (! ámbar) o es informativa (i azul).
    /// Calcula su propio alto según el ancho disponible.
    /// </summary>
    internal class PanelInsights : Control
    {
        private const int Margen = 22;
        private const int AltoEncabezado = 70;
        private const int SeparacionFilas = 12;
        private const int AnchoIcono = 34;

        private List<InsightReporte> _insights = new();
        private readonly Font _fuente = new("Segoe UI", 10.25f);

        public PanelInsights()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoApp;
            Height = 160;
        }

        public void Cargar(IEnumerable<InsightReporte> insights)
        {
            _insights = insights.ToList();
            AccessibleName = "Conclusiones rápidas: " + string.Join(" ", _insights.Select(i => i.Texto));
            Height = CalcularAlto(Width);
            Invalidate();
        }

        public int CalcularAlto(int ancho)
        {
            int anchoTexto = Math.Max(100, ancho - Margen * 2 - AnchoIcono);
            int alto = AltoEncabezado;
            foreach (InsightReporte insight in _insights)
            {
                alto += Math.Max(24, MedirTexto(insight.Texto, anchoTexto)) + SeparacionFilas;
            }
            return alto + Margen - SeparacionFilas + 4;
        }

        private int MedirTexto(string texto, int ancho) =>
            TextRenderer.MeasureText(texto, _fuente, new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak).Height;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int alto = CalcularAlto(Width);
            if (alto != Height) Height = alto;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            EstiloReporte.DibujarTarjeta(g, Size, Paleta.Borde);

            // Franja de acento a la izquierda para que el bloque se distinga del resto
            using (var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0.5f, 0.5f, 6, Height - 1.5f), 3))
            using (var acento = new SolidBrush(Paleta.Primario))
            {
                g.FillPath(acento, ruta);
            }

            using var fuenteTitulo = new Font("Segoe UI Semibold", 12.5f);
            using var fuenteSub = Paleta.FuenteChica;
            IconosUI.Dibujar(g, Icono.Grafico, new Rectangle(Margen, 18, 22, 22), Paleta.Primario, 1.8f);
            TextRenderer.DrawText(g, "Conclusiones rápidas", fuenteTitulo, new Point(Margen + 30, 15), Paleta.TextoPrimario);
            TextRenderer.DrawText(g, "Lo más importante del período, explicado en pocas palabras. Se actualiza solo al cambiar el período o los filtros.",
                fuenteSub, new Rectangle(Margen + 30, 42, Width - Margen * 2 - 30, 20), Paleta.TextoSecundario, TextFormatFlags.EndEllipsis);

            using var fuenteIcono = new Font("Segoe UI", 9f, FontStyle.Bold);
            int anchoTexto = Math.Max(100, Width - Margen * 2 - AnchoIcono);
            int y = AltoEncabezado;

            foreach (InsightReporte insight in _insights)
            {
                (Color fondo, Color color, string simbolo) = insight.Tono switch
                {
                    TonoInsight.Positivo => (Paleta.ExitoSuave, Paleta.Exito, "✓"),
                    TonoInsight.Atencion => (Paleta.AdvertenciaSuave, EstiloReporte.Advertencia, "!"),
                    _ => (Paleta.PrimarioSuave, Paleta.Primario, "i")
                };

                var circulo = new Rectangle(Margen, y, 24, 24);
                using (var brocha = new SolidBrush(fondo))
                {
                    g.FillEllipse(brocha, circulo);
                }
                TextRenderer.DrawText(g, simbolo, fuenteIcono, circulo, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int alto = Math.Max(24, MedirTexto(insight.Texto, anchoTexto));
                TextRenderer.DrawText(g, insight.Texto, _fuente, new Rectangle(Margen + AnchoIcono, y + 2, anchoTexto, alto), Paleta.TextoPrimario, TextFormatFlags.WordBreak);
                y += alto + SeparacionFilas;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fuente.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>Leyenda de un gráfico de distribución: punto de color, nombre, cantidad y porcentaje.</summary>
    internal class LeyendaDistribucion : Control
    {
        public const int AltoFila = 30;
        private List<(string Etiqueta, string Valor, double Porcentaje, Color Color)> _items = new();

        public LeyendaDistribucion()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public int Filas => _items.Count;

        public void Cargar(IEnumerable<(string Etiqueta, string Valor, double Porcentaje, Color Color)> items)
        {
            _items = items.ToList();
            AccessibleName = string.Join(", ", _items.Select(i => $"{i.Etiqueta}: {i.Valor}, {i.Porcentaje:0}%"));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fuente = Paleta.FuenteBase;
            using var fuenteNumero = new Font("Segoe UI Semibold", 9.75f);

            int altoTotal = _items.Count * AltoFila;
            int y0 = Math.Max(0, (Height - altoTotal) / 2);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                int y = y0 + i * AltoFila;
                using (var brocha = new SolidBrush(item.Color))
                {
                    g.FillEllipse(brocha, 4, y + 9, 11, 11);
                }

                string numero = $"{item.Porcentaje:0}%";
                Size medidaNumero = TextRenderer.MeasureText(numero, fuenteNumero);
                Size medidaValor = TextRenderer.MeasureText(item.Valor, fuente);
                int anchoDerecha = medidaNumero.Width + medidaValor.Width + 6;

                TextRenderer.DrawText(g, item.Etiqueta, fuente, new Rectangle(22, y, Math.Max(20, Width - 22 - anchoDerecha), AltoFila), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, numero, fuenteNumero, new Rectangle(0, y, Width, AltoFila), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                TextRenderer.DrawText(g, item.Valor, fuente, new Rectangle(0, y, Width - medidaNumero.Width - 4, AltoFila), Paleta.TextoTerciario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }
    }

    /// <summary>
    /// Gráfico de dona con su leyenda (a la derecha si hay lugar, si no debajo) y un mensaje
    /// amable cuando no hay datos en el período.
    /// </summary>
    internal class GraficoDona : UserControl
    {
        private readonly PieChart _chart;
        private readonly LeyendaDistribucion _leyenda;
        private readonly Label _lblVacio;

        public GraficoDona()
        {
            BackColor = Paleta.FondoTarjeta;
            _chart = new PieChart
            {
                Dock = DockStyle.Fill,
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden,
                InitialRotation = -90
            };
            _leyenda = new LeyendaDistribucion();
            _lblVacio = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Paleta.FuenteBase,
                ForeColor = Paleta.TextoSecundario,
                Visible = false
            };

            Controls.Add(_chart);
            Controls.Add(_leyenda);
            Controls.Add(_lblVacio);
        }

        public void Cargar(IReadOnlyList<SegmentoDistribucion> segmentos, IReadOnlyList<Color> colores, Func<double, string> formatoValor, string textoVacio)
        {
            bool hayDatos = segmentos.Any(s => s.Valor > 0);
            _lblVacio.Text = textoVacio;
            _lblVacio.Visible = !hayDatos;
            _chart.Visible = hayDatos;
            _leyenda.Visible = hayDatos;
            if (!hayDatos)
            {
                _chart.Series = Array.Empty<ISeries>();
                return;
            }

            var visibles = segmentos.Select((s, i) => (Segmento: s, Color: colores[i % colores.Count])).ToList();

            _chart.Series = visibles
                .Where(v => v.Segmento.Valor > 0)
                .Select(v => (ISeries)new PieSeries<double>
                {
                    Values = new[] { v.Segmento.Valor },
                    Name = v.Segmento.Etiqueta,
                    Fill = new SolidColorPaint(Skia(v.Color)),
                    Stroke = new SolidColorPaint(SKColors.White, 3),
                    InnerRadius = 40,
                    HoverPushout = 6,
                    ToolTipLabelFormatter = p => $"{formatoValor(p.Coordinate.PrimaryValue)} ({v.Segmento.Porcentaje:0}%)"
                })
                .ToArray();

            _leyenda.Cargar(visibles.Select(v => (v.Segmento.Etiqueta, formatoValor(v.Segmento.Valor), v.Segmento.Porcentaje, v.Color)));
            AcomodarLeyenda();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AcomodarLeyenda();
        }

        private void AcomodarLeyenda()
        {
            if (_leyenda == null) return;

            if (Width >= 420)
            {
                _leyenda.Dock = DockStyle.Right;
                _leyenda.Width = Width / 2;
            }
            else
            {
                _leyenda.Dock = DockStyle.Bottom;
                _leyenda.Height = Math.Min(Height / 2, _leyenda.Filas * LeyendaDistribucion.AltoFila + 6);
            }

            // El agujero de la dona se ajusta al tamaño real para que nunca "se coma" el anillo.
            int anchoTorta = _leyenda.Dock == DockStyle.Right ? Width - _leyenda.Width : Width;
            int altoTorta = _leyenda.Dock == DockStyle.Bottom ? Height - _leyenda.Height : Height;
            double radioInterno = Math.Max(0, (Math.Min(anchoTorta, altoTorta) - 24) * 0.28);
            foreach (ISeries serie in _chart.Series ?? Array.Empty<ISeries>())
            {
                if (serie is PieSeries<double> torta) torta.InnerRadius = radioInterno;
            }
        }

        public static SKColor Skia(Color c) => new(c.R, c.G, c.B, c.A);
    }

    /// <summary>Colores de los gráficos de reportes (categóricos y de estado).</summary>
    internal static class ColoresReporte
    {
        /// <summary>Serie categórica: distinguibles entre sí y legibles sobre blanco.</summary>
        public static readonly Color[] Categoricos =
        {
            Color.FromArgb(37, 99, 235),   // azul
            Color.FromArgb(13, 148, 136),  // verde azulado
            Color.FromArgb(217, 119, 6),   // ámbar
            Color.FromArgb(124, 58, 237),  // violeta
            Color.FromArgb(219, 39, 119),  // rosa
            Color.FromArgb(100, 116, 139), // gris pizarra
            Color.FromArgb(22, 163, 74)    // verde
        };

        public static Color DeEstado(string estado) => estado.Trim().ToLowerInvariant() switch
        {
            "disponible" => Paleta.Disponible,
            "ocupada" => Paleta.Ocupada,
            "limpieza" => Paleta.Limpieza,
            "mantenimiento" => Paleta.Mantenimiento,
            _ => Paleta.TextoTerciario
        };
    }
}
