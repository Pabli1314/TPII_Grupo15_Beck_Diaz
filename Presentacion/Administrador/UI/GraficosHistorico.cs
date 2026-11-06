using Logica;
using Presentacion.Recepcionista;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Color, nombre corto y explicación de cada tipo de evento del histórico (uno solo para toda la pantalla).</summary>
    internal static class TiposActividad
    {
        public static readonly Color ColorAlta = Paleta.Exito;
        public static readonly Color ColorIngreso = Paleta.Primario;
        public static readonly Color ColorEgreso = Color.FromArgb(217, 119, 6);
        public static readonly Color ColorConsumo = Color.FromArgb(124, 58, 237);

        public static Color ColorDe(string tipo) => tipo switch
        {
            GestionHistorialAdministrador.TipoAltaHuesped => ColorAlta,
            GestionHistorialAdministrador.TipoIngreso => ColorIngreso,
            GestionHistorialAdministrador.TipoEgreso => ColorEgreso,
            GestionHistorialAdministrador.TipoConsumo => ColorConsumo,
            _ => Paleta.TextoTerciario
        };

        public static string NombreCorto(string tipo) => tipo switch
        {
            GestionHistorialAdministrador.TipoAltaHuesped => "Altas",
            GestionHistorialAdministrador.TipoIngreso => "Check-in",
            GestionHistorialAdministrador.TipoEgreso => "Check-out",
            GestionHistorialAdministrador.TipoConsumo => "Consumos",
            _ => tipo
        };

        public static string Descripcion(string tipo) => tipo switch
        {
            GestionHistorialAdministrador.TipoAltaHuesped => "Huéspedes nuevos registrados en el sistema",
            GestionHistorialAdministrador.TipoIngreso => "Llegadas: el huésped ocupó una habitación",
            GestionHistorialAdministrador.TipoEgreso => "Salidas: el huésped dejó la habitación",
            GestionHistorialAdministrador.TipoConsumo => "Productos cargados a la cuenta del huésped",
            _ => string.Empty
        };

        public static string Plural(int n, string singular, string plural) => $"{n} {(n == 1 ? singular : plural)}";
    }

    /// <summary>
    /// «Actividad por tipo»: una fila por tipo de evento con su nombre, qué significa, una barra
    /// proporcional al total, la cantidad y el porcentaje. Al pasar el mouse se explica la fila.
    /// </summary>
    internal class GraficoActividadPorTipo : Control
    {
        private const int AltoFila = 58;
        private readonly ToolTip _toolTip;
        private List<(string Tipo, int Cantidad)> _datos = new();
        private int _filaResaltada = -1;

        public GraficoActividadPorTipo(ToolTip toolTip)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = System.Drawing.Color.Transparent;
            _toolTip = toolTip;
        }

        public void Cargar(IEnumerable<KeyValuePair<string, int>> datos)
        {
            _datos = datos.Select(d => (d.Key, d.Value)).ToList();
            int total = _datos.Sum(d => d.Cantidad);
            AccessibleName = "Actividad por tipo: " + string.Join(", ", _datos.Select(d => $"{TiposActividad.NombreCorto(d.Tipo)} {d.Cantidad}"));
            _filaResaltada = -1;
            Invalidate();
        }

        private int Y0 => Math.Max(0, (Height - _datos.Count * AltoFila) / 2);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int fila = (e.Y - Y0) / AltoFila;
            if (e.Y < Y0 || fila >= _datos.Count) fila = -1;
            if (fila == _filaResaltada) return;

            _filaResaltada = fila;
            Invalidate();

            if (fila < 0)
            {
                _toolTip.Hide(this);
                return;
            }

            var (tipo, cantidad) = _datos[fila];
            int total = Math.Max(1, _datos.Sum(d => d.Cantidad));
            _toolTip.Show($"{tipo}\n{TiposActividad.Descripcion(tipo)}.\n{TiposActividad.Plural(cantidad, "evento", "eventos")} = {cantidad * 100.0 / total:0}% de toda la actividad del período.",
                this, e.X + 14, e.Y + 18, 6000);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _filaResaltada = -1;
            _toolTip.Hide(this);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fuenteNombre = new Font("Segoe UI Semibold", 10f);
            using var fuenteDescripcion = Paleta.FuenteChica;
            using var fuenteNumero = new Font("Segoe UI Semibold", 11f);
            using var fuentePorcentaje = Paleta.FuenteChica;

            int total = _datos.Sum(d => d.Cantidad);
            if (total == 0)
            {
                using var fuenteVacio = Paleta.FuenteBase;
                TextRenderer.DrawText(g, "No hubo actividad de huéspedes en el período y filtros elegidos.", fuenteVacio,
                    ClientRectangle, Paleta.TextoSecundario, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            int maximo = _datos.Max(d => d.Cantidad);
            int anchoTexto = Math.Min(270, Math.Max(150, Width * 45 / 100));
            const int anchoNumero = 92;
            int xBarra = anchoTexto + 8;
            int anchoBarraMax = Math.Max(20, Width - xBarra - anchoNumero);
            int y0 = Y0;

            for (int i = 0; i < _datos.Count; i++)
            {
                var (tipo, cantidad) = _datos[i];
                Color color = TiposActividad.ColorDe(tipo);
                int y = y0 + i * AltoFila;

                if (i == _filaResaltada)
                {
                    using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, y + 2, Width - 1, AltoFila - 4), 8);
                    using var fondo = new SolidBrush(Paleta.FondoApp);
                    g.FillPath(fondo, ruta);
                }

                // Punto de color + nombre + explicación
                using (var brocha = new SolidBrush(color))
                {
                    g.FillEllipse(brocha, 8, y + 14, 12, 12);
                }
                TextRenderer.DrawText(g, tipo, fuenteNombre, new Rectangle(28, y + 7, anchoTexto - 28, 24), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, TiposActividad.Descripcion(tipo), fuenteDescripcion, new Rectangle(28, y + 30, anchoTexto - 28, 20), Paleta.TextoSecundario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Pista gris y barra proporcional al tipo con más eventos
                var pista = new RectangleF(xBarra, y + 20, anchoBarraMax, 18);
                using (var ruta = DibujoUtil.RutaRedondeada(pista, 9))
                using (var fondo = new SolidBrush(Paleta.BordeSuave))
                {
                    g.FillPath(fondo, ruta);
                }
                if (cantidad > 0)
                {
                    float ancho = Math.Max(18, anchoBarraMax * cantidad / (float)maximo);
                    using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(xBarra, y + 20, ancho, 18), 9);
                    using var brocha = new SolidBrush(color);
                    g.FillPath(brocha, ruta);
                }

                // Cantidad y porcentaje a la derecha
                var areaNumero = new Rectangle(Width - anchoNumero + 8, y + 8, anchoNumero - 8, 24);
                TextRenderer.DrawText(g, cantidad.ToString("N0"), fuenteNumero, areaNumero, Paleta.TextoPrimario, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, $"{cantidad * 100.0 / total:0}% del total", fuentePorcentaje, new Rectangle(areaNumero.X - 20, y + 31, areaNumero.Width + 20, 18),
                    Paleta.TextoTerciario, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
        }
    }

    /// <summary>
    /// «Huéspedes con más actividad»: ranking con medalla de posición, nombre, DNI y última actividad,
    /// una barra apilada con el desglose por tipo (mismos colores que «Actividad por tipo») y el total.
    /// </summary>
    internal class RankingActividadHuespedes : Control
    {
        private const int AltoFila = 46;
        private const int AltoLeyenda = 26;
        private readonly ToolTip _toolTip;
        private List<ActividadHuesped> _ranking = new();
        private int _filaResaltada = -1;

        public int MaximoFilas { get; set; } = 5;

        public RankingActividadHuespedes(ToolTip toolTip)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = System.Drawing.Color.Transparent;
            _toolTip = toolTip;
        }

        private IEnumerable<ActividadHuesped> Visibles => _ranking.Take(Math.Max(1, Math.Min(MaximoFilas, (Height - AltoLeyenda) / AltoFila)));

        public void Cargar(IEnumerable<ActividadHuesped> ranking)
        {
            _ranking = ranking.ToList();
            AccessibleName = "Huéspedes con más actividad: " + string.Join("; ", _ranking.Take(MaximoFilas).Select((h, i) => $"{i + 1}. {h.Nombre}, {h.Total} eventos"));
            _filaResaltada = -1;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            List<ActividadHuesped> visibles = Visibles.ToList();
            int fila = e.Y / AltoFila;
            if (fila >= visibles.Count) fila = -1;
            if (fila == _filaResaltada) return;

            _filaResaltada = fila;
            Invalidate();

            if (fila < 0)
            {
                _toolTip.Hide(this);
                return;
            }

            ActividadHuesped h = visibles[fila];
            var lineas = new List<string> { $"{h.Nombre} (DNI {h.DniHuesped})" };
            if (h.Altas > 0) lineas.Add("• Registrado como huésped nuevo en el período");
            if (h.Ingresos > 0) lineas.Add($"• {TiposActividad.Plural(h.Ingresos, "check-in", "check-ins")}");
            if (h.Egresos > 0) lineas.Add($"• {TiposActividad.Plural(h.Egresos, "check-out", "check-outs")}");
            if (h.Consumos > 0) lineas.Add($"• {TiposActividad.Plural(h.Consumos, "consumo", "consumos")} ({TiposActividad.Plural(h.UnidadesConsumidas, "unidad", "unidades")})");
            lineas.Add($"Última actividad: {h.UltimaActividad:dd/MM/yyyy HH:mm}");
            _toolTip.Show(string.Join("\n", lineas), this, e.X + 14, e.Y + 18, 8000);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _filaResaltada = -1;
            _toolTip.Hide(this);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            List<ActividadHuesped> visibles = Visibles.ToList();
            if (visibles.Count == 0)
            {
                using var fuenteVacio = Paleta.FuenteBase;
                TextRenderer.DrawText(g, "No hubo huéspedes con actividad en el período y filtros elegidos.", fuenteVacio,
                    ClientRectangle, Paleta.TextoSecundario, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            using var fuentePosicion = new Font("Segoe UI Semibold", 9f);
            using var fuenteNombre = new Font("Segoe UI Semibold", 9.75f);
            using var fuenteDetalle = Paleta.FuenteChica;
            using var fuenteTotal = new Font("Segoe UI Semibold", 10.5f);

            int maximo = Math.Max(1, visibles.Max(h => h.Total));
            int anchoTexto = Math.Min(260, Math.Max(150, Width * 42 / 100));
            const int anchoTotal = 84;
            int xBarra = anchoTexto + 6;
            int anchoBarraMax = Math.Max(20, Width - xBarra - anchoTotal);

            Color[] medallas = { Color.FromArgb(234, 179, 8), Color.FromArgb(148, 163, 184), Color.FromArgb(180, 110, 60) };

            for (int i = 0; i < visibles.Count; i++)
            {
                ActividadHuesped h = visibles[i];
                int y = i * AltoFila;

                if (i == _filaResaltada)
                {
                    using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, y + 1, Width - 1, AltoFila - 2), 8);
                    using var fondo = new SolidBrush(Paleta.FondoApp);
                    g.FillPath(fondo, ruta);
                }

                // Posición: medalla para el podio, círculo gris para el resto
                var circulo = new Rectangle(4, y + 11, 24, 24);
                bool podio = i < 3;
                using (var brocha = new SolidBrush(podio ? Color.FromArgb(40, medallas[i]) : Paleta.BordeSuave))
                {
                    g.FillEllipse(brocha, circulo);
                }
                TextRenderer.DrawText(g, (i + 1).ToString(), fuentePosicion, circulo, podio ? Color.FromArgb(Math.Max(0, medallas[i].R - 60), Math.Max(0, medallas[i].G - 60), Math.Max(0, medallas[i].B - 40)) : Paleta.TextoSecundario,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                TextRenderer.DrawText(g, h.Nombre, fuenteNombre, new Rectangle(36, y + 4, anchoTexto - 36, 20), Paleta.TextoPrimario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, $"DNI {h.DniHuesped} · última: {h.UltimaActividad:dd/MM HH:mm}", fuenteDetalle, new Rectangle(36, y + 23, anchoTexto - 36, 18), Paleta.TextoTerciario,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Barra apilada por tipo
                float x = xBarra;
                float anchoTotalBarra = anchoBarraMax * h.Total / (float)maximo;
                var areaBarra = new RectangleF(xBarra, y + 14, Math.Max(10, anchoTotalBarra), 18);
                using (var clip = DibujoUtil.RutaRedondeada(areaBarra, 9))
                {
                    g.SetClip(clip);
                    foreach (string tipo in GestionHistorialAdministrador.Tipos)
                    {
                        int cantidad = h.Cantidad(tipo);
                        if (cantidad == 0) continue;
                        float ancho = areaBarra.Width * cantidad / h.Total;
                        using var brocha = new SolidBrush(TiposActividad.ColorDe(tipo));
                        g.FillRectangle(brocha, x, areaBarra.Y, ancho + 0.5f, areaBarra.Height);
                        x += ancho;
                    }
                    g.ResetClip();
                }

                TextRenderer.DrawText(g, TiposActividad.Plural(h.Total, "evento", "eventos"), fuenteTotal, new Rectangle(Width - anchoTotal, y, anchoTotal, AltoFila),
                    Paleta.TextoPrimario, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            // Leyenda de colores al pie
            int yLeyenda = visibles.Count * AltoFila + 6;
            int xLeyenda = 4;
            foreach (string tipo in GestionHistorialAdministrador.Tipos)
            {
                string texto = TiposActividad.NombreCorto(tipo);
                using (var brocha = new SolidBrush(TiposActividad.ColorDe(tipo)))
                {
                    g.FillEllipse(brocha, xLeyenda, yLeyenda + 5, 10, 10);
                }
                Size medida = TextRenderer.MeasureText(texto, fuenteDetalle);
                TextRenderer.DrawText(g, texto, fuenteDetalle, new Point(xLeyenda + 14, yLeyenda + 2), Paleta.TextoSecundario);
                xLeyenda += 14 + medida.Width + 14;
            }
        }
    }
}
