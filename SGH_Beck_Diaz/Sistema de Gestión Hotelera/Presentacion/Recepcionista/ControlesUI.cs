using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    public enum EstadoOcupacion
    {
        Disponible,
        Ocupada,
        Limpieza,
        Mantenimiento
    }

    internal static class DibujoUtil
    {
        public static GraphicsPath RutaRedondeada(RectangleF area, float radio)
        {
            var ruta = new GraphicsPath();
            float diametro = radio * 2F;

            ruta.AddArc(area.X, area.Y, diametro, diametro, 180, 90);
            ruta.AddArc(area.Right - diametro, area.Y, diametro, diametro, 270, 90);
            ruta.AddArc(area.Right - diametro, area.Bottom - diametro, diametro, diametro, 0, 90);
            ruta.AddArc(area.X, area.Bottom - diametro, diametro, diametro, 90, 90);
            ruta.CloseFigure();

            return ruta;
        }

        public static Color ColorPorEstado(EstadoOcupacion estado) => estado switch
        {
            EstadoOcupacion.Disponible => Color.FromArgb(40, 199, 111),
            EstadoOcupacion.Ocupada => Color.FromArgb(234, 84, 85),
            EstadoOcupacion.Limpieza => Color.FromArgb(255, 193, 7),
            EstadoOcupacion.Mantenimiento => Color.FromArgb(79, 134, 247),
            _ => Color.Gray
        };

        public static string TextoPorEstado(EstadoOcupacion estado) => estado switch
        {
            EstadoOcupacion.Disponible => "Disponible",
            EstadoOcupacion.Ocupada => "Ocupada",
            EstadoOcupacion.Limpieza => "Limpieza",
            EstadoOcupacion.Mantenimiento => "Mantenimiento",
            _ => string.Empty
        };

        /// <summary>
        /// Mapea el nombre de estado tal como está en Estado_habitacion (Disponible/Ocupada/
        /// Limpieza) al enum de UI. La base todavía no tiene un estado "Mantenimiento".
        /// </summary>
        public static EstadoOcupacion EstadoDesdeTexto(string nomEstado) => nomEstado switch
        {
            "Disponible" => EstadoOcupacion.Disponible,
            "Ocupada" => EstadoOcupacion.Ocupada,
            "Limpieza" => EstadoOcupacion.Limpieza,
            "Mantenimiento" => EstadoOcupacion.Mantenimiento,
            _ => throw new ArgumentOutOfRangeException(nameof(nomEstado), nomEstado, "Estado de habitación desconocido.")
        };
    }

    /// <summary>Punto de color circular usado en las tarjetas de resumen (Paso 4).</summary>
    public class IndicadorColor : Panel
    {
        public Color ColorEstado { get; set; } = Color.Gray;

        public IndicadorColor()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(14, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brocha = new SolidBrush(ColorEstado);
            e.Graphics.FillEllipse(brocha, 0, 0, Width - 1, Height - 1);
        }
    }

    /// <summary>Etiqueta tipo "pill" con esquinas redondeadas usada como badge de estado (Paso 5).</summary>
    public class BadgeEstado : Label
    {
        private Color _colorFondo = Color.Gray;

        public BadgeEstado()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            ForeColor = Color.White;
            Height = 20;
        }

        public void FijarEstado(string texto, Color colorFondo)
        {
            Text = texto;
            _colorFondo = colorFondo;

            using (var g = CreateGraphics())
            {
                SizeF medida = g.MeasureString(texto, Font);
                Width = (int)medida.Width + 24;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width, Height), Height / 2F);
            using var brocha = new SolidBrush(_colorFondo);
            e.Graphics.FillPath(brocha, ruta);

            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
