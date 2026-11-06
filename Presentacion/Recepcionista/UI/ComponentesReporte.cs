using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.UI
{
    /// <summary>Colores de las etiquetas tipo semáforo de los reportes.</summary>
    internal enum TonoEtiqueta
    {
        Neutro,
        Exito,
        Advertencia,
        Peligro
    }

    internal static class EstiloReporte
    {
        public static readonly Color Advertencia = Color.FromArgb(180, 83, 9);

        public static (Color Fondo, Color Texto) ColoresTono(TonoEtiqueta tono) => tono switch
        {
            TonoEtiqueta.Exito => (Paleta.ExitoSuave, Paleta.Exito),
            TonoEtiqueta.Advertencia => (Paleta.AdvertenciaSuave, Advertencia),
            TonoEtiqueta.Peligro => (Paleta.PeligroSuave, Paleta.Peligro),
            _ => (Paleta.PrimarioSuave, Paleta.PrimarioHover)
        };

        /// <summary>ToolTip compartido por todas las ayudas de la pantalla.</summary>
        public static ToolTip CrearToolTip() => new ToolTip
        {
            InitialDelay = 300,
            AutoPopDelay = 15000,
            ReshowDelay = 100,
            ShowAlways = true
        };

        public static void DibujarTarjeta(Graphics g, Size tamano, Color borde, float grosorBorde = 1f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0.5f, 0.5f, tamano.Width - 1.5f, tamano.Height - 1.5f), 12);
            using var fondo = new SolidBrush(Paleta.FondoTarjeta);
            g.FillPath(fondo, ruta);
            using var lapiz = new Pen(borde, grosorBorde);
            g.DrawPath(lapiz, ruta);
        }
    }

    /// <summary>
    /// Círculo con una «i» que muestra qué significa un número o cómo leer un gráfico. Se abre al
    /// pasar el mouse y también con clic o con Enter/Espacio (para quien navega con teclado).
    /// </summary>
    internal class IconoAyuda : Control
    {
        private readonly ToolTip _toolTip;
        private string _texto;
        private bool _resaltado;

        public IconoAyuda(ToolTip toolTip, string texto)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            _toolTip = toolTip;
            _texto = texto;
            Size = new Size(22, 22);
            BackColor = Color.Transparent;
            Cursor = Cursors.Help;
            TabStop = true;
            AccessibleRole = AccessibleRole.HelpBalloon;
            AccessibleName = "Ayuda";
            AccessibleDescription = texto;
            _toolTip.SetToolTip(this, texto);
        }

        public void CambiarTexto(string texto)
        {
            _texto = texto;
            AccessibleDescription = texto;
            _toolTip.SetToolTip(this, texto);
        }

        private void MostrarAyuda() => _toolTip.Show(_texto, this, 0, Height + 4, 12000);

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            MostrarAyuda();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                MostrarAyuda();
                e.Handled = true;
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _resaltado = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _resaltado = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _toolTip.Hide(this); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool activo = _resaltado || Focused;
            var circulo = new RectangleF(1, 1, Width - 3, Height - 3);

            using var fondo = new SolidBrush(activo ? Paleta.PrimarioSuave : Paleta.FondoTarjeta);
            e.Graphics.FillEllipse(fondo, circulo);
            using var borde = new Pen(activo ? Paleta.Primario : Paleta.Borde, Focused ? 2f : 1f);
            e.Graphics.DrawEllipse(borde, circulo);

            using var fuente = new Font("Segoe UI Semibold", 8.5f);
            TextRenderer.DrawText(e.Graphics, "i", fuente, ClientRectangle, activo ? Paleta.Primario : Paleta.TextoSecundario,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>
    /// Tarjeta de indicador para recepción: título en lenguaje cotidiano, número grande, una línea de
    /// contexto («de 20 habitaciones») y el ícono de ayuda. En modo alerta el número y el borde se pintan de rojo.
    /// </summary>
    internal class TarjetaIndicador : Panel
    {
        private readonly Label _lblTitulo;
        private readonly Label _lblValor;
        private readonly Label _lblContexto;
        private readonly IconoAyuda _ayuda;
        private Color? _colorPunto;
        private bool _alerta;

        public TarjetaIndicador(ToolTip toolTip)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoApp;
            Size = new Size(250, 124);
            Margin = new Padding(0, 0, 16, 16);

            _lblTitulo = new Label { AutoSize = true, MaximumSize = new Size(196, 0), Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, BackColor = Color.Transparent, Location = new Point(18, 15) };
            _lblValor = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 22f), ForeColor = Paleta.TextoPrimario, BackColor = Color.Transparent, Location = new Point(16, 40) };
            _lblContexto = new Label { AutoSize = true, MaximumSize = new Size(220, 0), Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, BackColor = Color.Transparent, Location = new Point(18, 92) };
            _ayuda = new IconoAyuda(toolTip, string.Empty) { Location = new Point(Width - 38, 13) };

            Controls.Add(_lblTitulo);
            Controls.Add(_lblValor);
            Controls.Add(_lblContexto);
            Controls.Add(_ayuda);
        }

        public void Configurar(string titulo, string valor, string contexto, string ayuda, Color? colorPunto = null, bool alerta = false)
        {
            _lblTitulo.Text = titulo;
            _lblValor.Text = valor;
            _lblContexto.Text = contexto;
            _ayuda.CambiarTexto(ayuda);
            _colorPunto = colorPunto;
            _alerta = alerta;

            _lblValor.ForeColor = alerta ? Paleta.Peligro : Paleta.TextoPrimario;
            _lblValor.Left = colorPunto.HasValue ? 34 : 16;
            AccessibleName = $"{titulo}: {valor}, {contexto}";
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_ayuda == null) return;

            _ayuda.Location = new Point(Width - _ayuda.Width - 16, 13);
            _lblTitulo.MaximumSize = new Size(Math.Max(60, _ayuda.Left - 26), 0);
            _lblContexto.MaximumSize = new Size(Math.Max(60, Width - 34), 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            EstiloReporte.DibujarTarjeta(e.Graphics, Size, _alerta ? Paleta.Peligro : Paleta.Borde, _alerta ? 1.6f : 1f);

            if (_colorPunto.HasValue)
            {
                using var punto = new SolidBrush(_colorPunto.Value);
                float centroY = _lblValor.Top + _lblValor.Height / 2f;
                e.Graphics.FillEllipse(punto, 19, centroY - 5, 10, 10);
            }
        }
    }

    /// <summary>
    /// Tarjeta contenedora de un gráfico o lista: título en forma de pregunta, ayuda, un enlace de
    /// acción opcional («Ir a Check-out») y una línea gris al pie que explica cómo leerlo.
    /// </summary>
    internal class PanelReporte : Panel
    {
        private readonly Label _lblPie;
        private readonly LinkLabel _lnkAccion;
        private readonly Panel _pie;
        private bool _accionVisible;

        public event EventHandler? AccionClick;

        public PanelReporte(ToolTip toolTip, string titulo, string ayuda, Control contenido, Size tamano, string? textoAccion = null)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoApp;
            Size = tamano;
            Margin = new Padding(0, 0, 16, 16);
            Padding = new Padding(18, 14, 18, 12);

            var encabezado = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
            var lblTitulo = new Label { Text = titulo, AutoSize = false, AutoEllipsis = true, Font = new Font("Segoe UI Semibold", 11f), ForeColor = Paleta.TextoPrimario, BackColor = Color.Transparent, Location = new Point(0, 2), Height = 26 };
            var icono = new IconoAyuda(toolTip, ayuda);
            encabezado.Controls.Add(lblTitulo);
            encabezado.Controls.Add(icono);
            encabezado.Resize += (s, e) =>
            {
                icono.Location = new Point(encabezado.Width - icono.Width, 2);
                lblTitulo.Width = Math.Max(40, icono.Left - 8);
            };

            // Pie: explicación de cómo leer el panel y, a la derecha, la acción opcional («Ir a Check-out»).
            var pie = _pie = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };
            _accionVisible = textoAccion != null;
            _lnkAccion = new LinkLabel
            {
                Text = textoAccion != null ? textoAccion + " →" : string.Empty,
                AutoSize = true,
                Font = Paleta.FuenteBaseNegrita,
                LinkColor = Paleta.Primario,
                ActiveLinkColor = Paleta.PrimarioHover,
                LinkBehavior = LinkBehavior.HoverUnderline,
                BackColor = Color.Transparent,
                Visible = textoAccion != null
            };
            _lnkAccion.LinkClicked += (s, e) => AccionClick?.Invoke(this, EventArgs.Empty);

            _lblPie = new Label
            {
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.BottomLeft,
                Location = new Point(0, 0)
            };
            pie.Controls.Add(_lblPie);
            pie.Controls.Add(_lnkAccion);
            pie.Resize += (s, e) => AcomodarPie();

            contenido.Dock = DockStyle.Fill;
            Controls.Add(contenido);
            Controls.Add(pie);
            Controls.Add(encabezado);
        }

        /// <summary>Si es true, el panel ocupa todo el ancho de la pestaña (para gráficos de muchos días).</summary>
        public bool AnchoCompleto { get; set; }

        public string TextoPie
        {
            get => _lblPie.Text;
            set => _lblPie.Text = value;
        }

        public void MostrarAccion(bool visible)
        {
            // Se guarda aparte porque Control.Visible da false mientras el panel no está en pantalla.
            _accionVisible = visible && _lnkAccion.Text.Length > 0;
            _lnkAccion.Visible = _accionVisible;
            AcomodarPie();
        }

        private void AcomodarPie()
        {
            Panel pie = _pie;
            int anchoAccion = _accionVisible ? _lnkAccion.PreferredWidth + 12 : 0;
            _lnkAccion.Location = new Point(pie.Width - _lnkAccion.PreferredWidth, pie.Height - _lnkAccion.PreferredHeight);
            _lblPie.Bounds = new Rectangle(0, 0, Math.Max(40, pie.Width - anchoAccion), pie.Height);
        }

        protected override void OnPaint(PaintEventArgs e) => EstiloReporte.DibujarTarjeta(e.Graphics, Size, Paleta.Borde);
    }

    /// <summary>
    /// Barra apilada al 100 % con el estado de todas las habitaciones y su leyenda con cantidades.
    /// Usa los mismos colores que el mapa de habitaciones.
    /// </summary>
    internal class BarraEstadoHabitaciones : Control
    {
        private List<(string Texto, int Cantidad, Color Color)> _partes = new();

        public BarraEstadoHabitaciones()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void Cargar(IEnumerable<(string Texto, int Cantidad, Color Color)> partes)
        {
            _partes = partes.ToList();
            AccessibleName = string.Join(", ", _partes.Select(p => $"{p.Cantidad} {p.Texto.ToLowerInvariant()}"));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int total = _partes.Sum(p => p.Cantidad);

            if (total == 0)
            {
                using var fuenteVacio = Paleta.FuenteBase;
                TextRenderer.DrawText(g, "No hay habitaciones cargadas.", fuenteVacio, new Point(0, 8), Paleta.TextoSecundario);
                return;
            }

            // Barra
            var areaBarra = new RectangleF(0, 6, Width - 1, 30);
            using (var clip = DibujoUtil.RutaRedondeada(areaBarra, 7))
            {
                g.SetClip(clip);
                float x = areaBarra.X;
                foreach (var parte in _partes.Where(p => p.Cantidad > 0))
                {
                    float ancho = areaBarra.Width * parte.Cantidad / total;
                    using var brocha = new SolidBrush(parte.Color);
                    g.FillRectangle(brocha, x, areaBarra.Y, Math.Max(0, ancho - 2), areaBarra.Height);
                    x += ancho;
                }
                g.ResetClip();
            }

            // Leyenda en dos columnas: punto, nombre y cantidad alineada a la derecha.
            using var fuente = Paleta.FuenteBase;
            using var fuenteNumero = new Font("Segoe UI Semibold", 10f);
            // Dos columnas si el nombre más largo y su número entran; si no, una sola.
            int anchoNecesario = _partes.Max(p => TextRenderer.MeasureText(p.Texto, fuente).Width) + 18 + 36;
            int columnas = (Width - 24) / 2 >= anchoNecesario ? 2 : 1;
            int anchoColumna = columnas == 2 ? (Width - 24) / 2 : Width;
            for (int i = 0; i < _partes.Count; i++)
            {
                int x = (i % columnas) * (anchoColumna + 24);
                int y = 52 + (i / columnas) * 28;
                using var brocha = new SolidBrush(_partes[i].Color);
                g.FillEllipse(brocha, x, y + 5, 11, 11);
                TextRenderer.DrawText(g, _partes[i].Texto, fuente, new Point(x + 18, y), Paleta.TextoPrimario);
                TextRenderer.DrawText(g, _partes[i].Cantidad.ToString(), fuenteNumero, new Rectangle(x, y - 1, anchoColumna, 22),
                    Paleta.TextoPrimario, TextFormatFlags.Right | TextFormatFlags.Top);
            }
        }
    }

    /// <summary>
    /// Barras horizontales para rankings y montos: nombre a la izquierda, barra proporcional al mayor
    /// valor y el valor escrito dentro de la barra (o a su derecha si no entra).
    /// </summary>
    internal class BarrasHorizontales : Control
    {
        private List<(string Etiqueta, double Valor, string TextoValor)> _items = new();
        private string _textoVacio = "Sin datos para mostrar.";

        public Color ColorBarra { get; set; } = Paleta.Primario;
        public int AnchoEtiqueta { get; set; } = 120;

        public BarrasHorizontales()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void Cargar(IEnumerable<(string Etiqueta, double Valor, string TextoValor)> items, string textoVacio)
        {
            _items = items.ToList();
            _textoVacio = textoVacio;
            AccessibleName = string.Join(", ", _items.Select(i => $"{i.Etiqueta}: {i.TextoValor}"));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            double maximo = _items.Count == 0 ? 0 : _items.Max(i => i.Valor);

            using var fuente = Paleta.FuenteBase;
            if (maximo <= 0)
            {
                TextRenderer.DrawText(g, _textoVacio, fuente, new Rectangle(0, 4, Width, 60), Paleta.TextoSecundario, TextFormatFlags.WordBreak);
                return;
            }

            using var fuenteValor = new Font("Segoe UI Semibold", 9.5f);
            const int altoBarra = 28;
            const int paso = 40;
            int anchoMaximo = Math.Max(10, Width - AnchoEtiqueta - 4);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                int y = 4 + i * paso;

                TextRenderer.DrawText(g, item.Etiqueta, fuente, new Rectangle(0, y, AnchoEtiqueta - 8, altoBarra), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                float ancho = (float)(item.Valor / maximo * anchoMaximo);
                Size medida = TextRenderer.MeasureText(item.TextoValor, fuenteValor);

                if (ancho > 0)
                {
                    using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(AnchoEtiqueta, y, Math.Max(ancho, 8), altoBarra), 5);
                    using var brocha = new SolidBrush(ColorBarra);
                    g.FillPath(brocha, ruta);
                }

                bool entraAdentro = medida.Width + 16 <= ancho;
                var areaTexto = entraAdentro
                    ? new Rectangle(AnchoEtiqueta, y, (int)ancho - 10, altoBarra)
                    : new Rectangle(AnchoEtiqueta + (int)ancho + 8, y, medida.Width + 4, altoBarra);
                TextRenderer.DrawText(g, item.TextoValor, fuenteValor, areaTexto, entraAdentro ? Color.White : Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | (entraAdentro ? TextFormatFlags.Right : TextFormatFlags.Left));
            }
        }
    }

    /// <summary>
    /// Lista ordenada por urgencia (habitación, huésped y una etiqueta de color con la situación).
    /// Si no entran todas las filas, la última avisa cuántas quedan sin mostrar.
    /// </summary>
    internal class ListaMovimientos : Control
    {
        private List<(string Habitacion, string Huesped, string Etiqueta, TonoEtiqueta Tono)> _filas = new();
        private string _textoVacio = string.Empty;
        private const int AltoFila = 36;

        public ListaMovimientos()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void Cargar(IEnumerable<(string Habitacion, string Huesped, string Etiqueta, TonoEtiqueta Tono)> filas, string textoVacio)
        {
            _filas = filas.ToList();
            _textoVacio = textoVacio;
            AccessibleName = _filas.Count == 0
                ? textoVacio
                : string.Join("; ", _filas.Select(f => $"Habitación {f.Habitacion}, {f.Huesped}, {f.Etiqueta}"));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fuente = Paleta.FuenteBase;

            if (_filas.Count == 0)
            {
                TextRenderer.DrawText(g, _textoVacio, fuente, new Rectangle(0, 6, Width, Height - 6), Paleta.TextoSecundario, TextFormatFlags.WordBreak);
                return;
            }

            using var fuenteHab = new Font("Segoe UI Semibold", 10f);
            using var fuenteEtiqueta = new Font("Segoe UI Semibold", 8.5f);
            using var lineaSeparadora = new Pen(Paleta.BordeSuave);

            int capacidad = Math.Max(1, Height / AltoFila);
            bool sobran = _filas.Count > capacidad;
            int visibles = sobran ? capacidad - 1 : _filas.Count;

            for (int i = 0; i < visibles; i++)
            {
                var fila = _filas[i];
                int y = i * AltoFila;

                Size medida = TextRenderer.MeasureText(fila.Etiqueta, fuenteEtiqueta);
                int anchoEtiqueta = medida.Width + 14;
                var etiqueta = new RectangleF(Width - anchoEtiqueta - 1, y + 7, anchoEtiqueta, 22);
                var (fondo, texto) = EstiloReporte.ColoresTono(fila.Tono);
                using (var ruta = DibujoUtil.RutaRedondeada(etiqueta, 11))
                using (var brocha = new SolidBrush(fondo))
                {
                    g.FillPath(brocha, ruta);
                }
                TextRenderer.DrawText(g, fila.Etiqueta, fuenteEtiqueta, Rectangle.Round(etiqueta), texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, fila.Habitacion, fuenteHab, new Rectangle(0, y, 52, AltoFila), Paleta.TextoPrimario, TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, fila.Huesped, fuente, new Rectangle(56, y, (int)etiqueta.X - 64, AltoFila), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                if (i < visibles - 1 || sobran)
                {
                    g.DrawLine(lineaSeparadora, 0, y + AltoFila - 1, Width, y + AltoFila - 1);
                }
            }

            if (sobran)
            {
                int restantes = _filas.Count - visibles;
                TextRenderer.DrawText(g, restantes == 1 ? "y 1 más…" : $"y {restantes} más…", fuente,
                    new Rectangle(0, visibles * AltoFila, Width, AltoFila), Paleta.TextoSecundario, TextFormatFlags.VerticalCenter);
            }
        }
    }

    /// <summary>
    /// Columnas verticales, una por día: el valor va escrito en la columna, el eje arranca en cero y una
    /// línea punteada opcional marca el total del hotel («si la columna la toca, el hotel estuvo lleno»).
    /// La columna resaltada (por ejemplo «Hoy») se pinta más oscura.
    /// </summary>
    internal class GraficoColumnas : Control
    {
        private List<(string Etiqueta, double Valor)> _puntos = new();
        private double? _capacidad;
        private string _textoCapacidad = string.Empty;
        private int _resaltado = -1;
        private string _textoVacio = "Sin datos para mostrar.";

        public GraficoColumnas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void Cargar(IEnumerable<(string Etiqueta, double Valor)> puntos, double? capacidad, string textoCapacidad, int resaltado, string textoVacio)
        {
            _puntos = puntos.ToList();
            _capacidad = capacidad > 0 ? capacidad : null;
            _textoCapacidad = textoCapacidad;
            _resaltado = resaltado;
            _textoVacio = textoVacio;
            AccessibleName = string.Join(", ", _puntos.Select(p => $"{p.Etiqueta}: {p.Valor:0}"));
            Invalidate();
        }

        /// <summary>Paso «redondo» del eje (1, 2, 5, 10, 20…) para unas 4 divisiones.</summary>
        public static double PasoEje(double maximo)
        {
            double bruto = Math.Max(1, maximo / 4);
            double magnitud = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
            double normal = bruto / magnitud;
            double paso = normal <= 1 ? 1 : normal <= 2 ? 2 : normal <= 5 ? 5 : 10;
            return paso * magnitud;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fuente = Paleta.FuenteChica;

            double maximoDatos = _puntos.Count == 0 ? 0 : _puntos.Max(p => p.Valor);
            if (maximoDatos <= 0)
            {
                using var fuenteVacio = Paleta.FuenteBase;
                TextRenderer.DrawText(g, _textoVacio, fuenteVacio, new Rectangle(0, 6, Width, Height - 6), Paleta.TextoSecundario, TextFormatFlags.WordBreak);
                return;
            }

            double maximo = Math.Max(maximoDatos, _capacidad ?? 0);
            double paso = PasoEje(maximo);
            double tope = Math.Ceiling(maximo / paso) * paso;

            var area = new RectangleF(30, 22, Width - 34, Height - 22 - 24);
            float Y(double v) => area.Bottom - (float)(v / tope) * area.Height;

            // Grilla y números del eje
            using (var grilla = new Pen(Paleta.BordeSuave))
            {
                for (double v = 0; v <= tope + 0.001; v += paso)
                {
                    float y = Y(v);
                    g.DrawLine(grilla, area.Left, y, area.Right, y);
                    TextRenderer.DrawText(g, v.ToString("0"), fuente, new Rectangle(0, (int)y - 8, (int)area.Left - 6, 16),
                        Paleta.TextoTerciario, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
            }

            // Columnas
            int n = _puntos.Count;
            float ranura = area.Width / n;
            float anchoColumna = Math.Max(3, Math.Min(44, ranura * 0.62f));
            using var fuenteValor = new Font("Segoe UI Semibold", ranura >= 26 ? 8.5f : 7f);

            for (int i = 0; i < n; i++)
            {
                double valor = _puntos[i].Valor;
                float x = area.Left + i * ranura + (ranura - anchoColumna) / 2;
                float y = Y(valor);
                float alto = area.Bottom - y;
                if (alto <= 0) continue;

                Color color = i == _resaltado ? Color.FromArgb(29, 64, 175) : Paleta.Primario;
                using (var ruta = DibujoUtil.RutaRedondeada(new RectangleF(x, y, anchoColumna, Math.Max(alto, 4)), Math.Min(4, anchoColumna / 2 - 0.5f)))
                using (var brocha = new SolidBrush(color))
                {
                    g.FillPath(brocha, ruta);
                }

                // Valor escrito: dentro de la columna si entra, si no arriba.
                string texto = valor.ToString("0");
                Size medida = TextRenderer.MeasureText(texto, fuenteValor);
                if (medida.Width <= ranura)
                {
                    bool adentro = alto >= medida.Height + 6 && medida.Width <= anchoColumna + 2;
                    var r = adentro
                        ? new Rectangle((int)(x + anchoColumna / 2 - ranura / 2), (int)y + 3, (int)ranura, medida.Height)
                        : new Rectangle((int)(x + anchoColumna / 2 - ranura / 2), (int)y - medida.Height - 1, (int)ranura, medida.Height);
                    TextRenderer.DrawText(g, texto, fuenteValor, r, adentro ? Color.White : Paleta.TextoPrimario, TextFormatFlags.HorizontalCenter);
                }
            }

            // Línea de capacidad del hotel (se dibuja después para que quede por encima de las columnas)
            if (_capacidad.HasValue)
            {
                float yCap = Y(_capacidad.Value);
                using var punteada = new Pen(Paleta.TextoSecundario) { DashStyle = DashStyle.Dash };
                g.DrawLine(punteada, area.Left, yCap, area.Right, yCap);
                TextRenderer.DrawText(g, _textoCapacidad, fuente, new Rectangle((int)area.Left, (int)yCap - 18, (int)area.Width, 16),
                    Paleta.TextoSecundario, TextFormatFlags.Right | TextFormatFlags.Bottom);
            }

            // Etiquetas de los días: si no entran todas, se muestra una cada tantas (siempre la resaltada).
            int anchoEtiqueta = _puntos.Max(p => TextRenderer.MeasureText(p.Etiqueta, fuente).Width) + 6;
            int cada = Math.Max(1, (int)Math.Ceiling(anchoEtiqueta / ranura));
            using var fuenteResaltada = new Font("Segoe UI Semibold", 8.5f);
            for (int i = 0; i < n; i++)
            {
                if (i % cada != 0 && i != _resaltado) continue;
                if (i != _resaltado && _resaltado >= 0 && Math.Abs(i - _resaltado) < cada) continue;

                var r = new Rectangle((int)(area.Left + i * ranura + ranura / 2 - anchoEtiqueta), (int)area.Bottom + 4, anchoEtiqueta * 2, 18);
                bool resaltada = i == _resaltado;
                TextRenderer.DrawText(g, _puntos[i].Etiqueta, resaltada ? fuenteResaltada : fuente, r,
                    resaltada ? Paleta.TextoPrimario : Paleta.TextoSecundario, TextFormatFlags.HorizontalCenter);
            }
        }
    }

    /// <summary>Desglose del efectivo esperado en caja: cambio inicial + efectivo cobrado = total.</summary>
    internal class DesgloseCaja : Control
    {
        private decimal _cambioInicial;
        private decimal _efectivo;
        private Func<decimal, string> _formato = v => v.ToString("C0");

        public DesgloseCaja()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void Cargar(decimal cambioInicial, decimal efectivo, Func<decimal, string> formato)
        {
            _cambioInicial = cambioInicial;
            _efectivo = efectivo;
            _formato = formato;
            AccessibleName = $"Cambio inicial {formato(cambioInicial)} más efectivo cobrado {formato(efectivo)} igual a {formato(cambioInicial + efectivo)}";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using var fuente = Paleta.FuenteBase;
            using var fuenteTotal = new Font("Segoe UI Semibold", 20f);
            using var fuenteEtiquetaTotal = Paleta.FuenteBaseNegrita;
            var derecha = TextFormatFlags.Right | TextFormatFlags.VerticalCenter;
            var izquierda = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

            TextRenderer.DrawText(g, "Cambio inicial", fuente, new Rectangle(0, 4, Width, 26), Paleta.TextoSecundario, izquierda);
            TextRenderer.DrawText(g, _formato(_cambioInicial), fuente, new Rectangle(0, 4, Width, 26), Paleta.TextoPrimario, derecha);
            TextRenderer.DrawText(g, "+ Cobrado en efectivo", fuente, new Rectangle(0, 34, Width, 26), Paleta.TextoSecundario, izquierda);
            TextRenderer.DrawText(g, _formato(_efectivo), fuente, new Rectangle(0, 34, Width, 26), Paleta.TextoPrimario, derecha);

            using var lapiz = new Pen(Paleta.Borde) { DashStyle = DashStyle.Dash };
            g.DrawLine(lapiz, 0, 70, Width, 70);

            TextRenderer.DrawText(g, "Total esperado", fuenteEtiquetaTotal, new Rectangle(0, 78, Width, 40), Paleta.TextoPrimario, izquierda);
            TextRenderer.DrawText(g, _formato(_cambioInicial + _efectivo), fuenteTotal, new Rectangle(0, 78, Width, 40), Paleta.Exito, derecha);
        }
    }

    /// <summary>Selector de pestañas segmentado (Ahora / Mi turno).</summary>
    internal class SelectorPestanas : Panel
    {
        private readonly List<Button> _botones = new();
        public string Seleccionada { get; private set; } = string.Empty;
        public event Action<string>? PestanaCambiada;

        public SelectorPestanas(params (string Clave, string Texto)[] pestanas)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoApp;
            Height = 42;

            int x = 4;
            foreach (var (clave, texto) in pestanas)
            {
                var boton = new Button
                {
                    Text = texto,
                    Tag = clave,
                    FlatStyle = FlatStyle.Flat,
                    Font = Paleta.FuenteBaseNegrita,
                    Cursor = Cursors.Hand,
                    Location = new Point(x, 4),
                    Size = new Size(TextRenderer.MeasureText(texto, Paleta.FuenteBaseNegrita).Width + 34, 34),
                    UseVisualStyleBackColor = false
                };
                boton.FlatAppearance.BorderSize = 0;
                boton.Click += (s, e) => Seleccionar(clave);
                _botones.Add(boton);
                Controls.Add(boton);
                x += boton.Width + 4;
            }

            Width = x;
            if (pestanas.Length > 0) Seleccionar(pestanas[0].Clave, notificar: false);
        }

        public void Seleccionar(string clave, bool notificar = true)
        {
            Seleccionada = clave;
            foreach (Button boton in _botones)
            {
                bool activo = (string)boton.Tag! == clave;
                boton.BackColor = activo ? Paleta.Primario : Paleta.FondoTarjeta;
                boton.ForeColor = activo ? Color.White : Paleta.TextoSecundario;
                boton.FlatAppearance.MouseOverBackColor = activo ? Paleta.PrimarioHover : Paleta.BordeSuave;
                boton.AccessibleDescription = activo ? "Seleccionada" : string.Empty;
            }

            if (notificar) PestanaCambiada?.Invoke(clave);
        }

        protected override void OnPaint(PaintEventArgs e) => EstiloReporte.DibujarTarjeta(e.Graphics, Size, Paleta.Borde);
    }
}
