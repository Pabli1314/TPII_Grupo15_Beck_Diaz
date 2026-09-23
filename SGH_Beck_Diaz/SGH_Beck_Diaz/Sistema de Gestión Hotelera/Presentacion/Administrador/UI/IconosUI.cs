using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Presentacion.Administrador.UI
{
    public enum Icono
    {
        Home, Cama, Personas, Etiqueta, Caja, Grafico, Engranaje, Disco,
        Campana, Salir, Lapiz, Basura, Mas, Candado, Check, X, Reloj, Alerta, Flecha, Descargar, Restaurar
    }

    /// <summary>
    /// Set propio de iconos dibujados con GDI+ (no depende de fuentes de iconos instaladas).
    /// Todas las formas están normalizadas a un cuadro lógico de 24x24 y se escalan al área pedida.
    /// </summary>
    internal static class IconosUI
    {
        public static void Dibujar(Graphics g, Icono icono, Rectangle area, Color color, float grosor = 1.8f)
        {
            var estadoAnterior = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var pluma = new Pen(color, grosor) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var pincel = new SolidBrush(color);

            float escalaX = area.Width / 24f;
            float escalaY = area.Height / 24f;
            PointF P(float x, float y) => new PointF(area.X + x * escalaX, area.Y + y * escalaY);
            RectangleF R(float x, float y, float w, float h) => new RectangleF(area.X + x * escalaX, area.Y + y * escalaY, w * escalaX, h * escalaY);

            switch (icono)
            {
                case Icono.Home:
                    g.DrawLines(pluma, new[] { P(4, 12), P(12, 5), P(20, 12) });
                    g.DrawRectangle(pluma, Rectangle.Round(R(7, 12, 10, 8)));
                    g.DrawRectangle(pluma, Rectangle.Round(R(11, 15, 3, 5)));
                    break;

                case Icono.Cama:
                    g.DrawRectangle(pluma, Rectangle.Round(R(3, 13, 18, 7)));
                    g.DrawRectangle(pluma, Rectangle.Round(R(4, 13, 5, 4)));
                    g.DrawLine(pluma, P(4, 20), P(4, 22));
                    g.DrawLine(pluma, P(20, 20), P(20, 22));
                    break;

                case Icono.Personas:
                    g.DrawEllipse(pluma, R(5.8f, 4.8f, 6.4f, 6.4f));
                    g.DrawArc(pluma, R(3, 11, 12, 10), 180, 180);
                    g.DrawEllipse(pluma, R(14.4f, 6.4f, 5.2f, 5.2f));
                    g.DrawArc(pluma, R(12, 12, 10, 8), 180, 180);
                    break;

                case Icono.Etiqueta:
                    g.DrawRectangle(pluma, Rectangle.Round(R(4, 8, 12, 10)));
                    g.DrawEllipse(pluma, R(7, 11, 3, 3));
                    g.DrawLines(pluma, new[] { P(16, 10), P(21, 5), P(19, 4) });
                    break;

                case Icono.Caja:
                    g.DrawRectangle(pluma, Rectangle.Round(R(4, 8, 16, 12)));
                    g.DrawLine(pluma, P(4, 13), P(20, 13));
                    g.DrawLine(pluma, P(12, 8), P(12, 20));
                    break;

                case Icono.Grafico:
                    g.FillRectangle(pincel, R(5, 14, 4, 7));
                    g.FillRectangle(pincel, R(11, 9, 4, 12));
                    g.FillRectangle(pincel, R(17, 12, 4, 9));
                    g.DrawLine(pluma, P(3, 21), P(21, 21));
                    break;

                case Icono.Engranaje:
                    g.DrawEllipse(pluma, R(6, 6, 12, 12));
                    g.DrawEllipse(pluma, R(10, 10, 4, 4));
                    for (int i = 0; i < 8; i++)
                    {
                        double angulo = i * Math.PI / 4;
                        float cx = 12, cy = 12;
                        var interior = new PointF(cx + (float)Math.Cos(angulo) * 6.5f, cy + (float)Math.Sin(angulo) * 6.5f);
                        var exterior = new PointF(cx + (float)Math.Cos(angulo) * 9.5f, cy + (float)Math.Sin(angulo) * 9.5f);
                        g.DrawLine(pluma, P(interior.X, interior.Y), P(exterior.X, exterior.Y));
                    }
                    break;

                case Icono.Disco:
                    g.DrawEllipse(pluma, R(4, 12, 8, 7));
                    g.DrawEllipse(pluma, R(9, 9, 9, 8));
                    g.DrawEllipse(pluma, R(15, 12, 7, 7));
                    g.DrawLine(pluma, P(7, 17), P(19, 17));
                    g.DrawLines(pluma, new[] { P(9, 14), P(12, 11), P(15, 14) });
                    g.DrawLine(pluma, P(12, 11), P(12, 18));
                    break;

                case Icono.Campana:
                    g.DrawArc(pluma, R(7, 5, 10, 12), 180, 180);
                    g.DrawLine(pluma, P(7, 11), P(7.5f, 15));
                    g.DrawLine(pluma, P(17, 11), P(16.5f, 15));
                    g.DrawLine(pluma, P(7.5f, 15), P(16.5f, 15));
                    g.FillEllipse(pincel, R(10.7f, 16, 2.6f, 2.6f));
                    g.DrawLine(pluma, P(12, 3), P(12, 5));
                    break;

                case Icono.Salir:
                    g.DrawRectangle(pluma, Rectangle.Round(R(5, 4, 8, 16)));
                    g.DrawLine(pluma, P(11, 12), P(21, 12));
                    g.DrawLines(pluma, new[] { P(17, 8), P(21, 12), P(17, 16) });
                    break;

                case Icono.Lapiz:
                    g.DrawLine(pluma, P(5, 19), P(15, 9));
                    g.DrawLines(pluma, new[] { P(15, 9), P(18, 6), P(20, 8), P(17, 11) });
                    g.DrawLine(pluma, P(4, 20), P(5, 19));
                    break;

                case Icono.Basura:
                    g.DrawRectangle(pluma, Rectangle.Round(R(7, 9, 10, 12)));
                    g.DrawLine(pluma, P(5, 9), P(19, 9));
                    g.DrawLine(pluma, P(10, 5), P(10, 9));
                    g.DrawLine(pluma, P(14, 5), P(14, 9));
                    g.DrawLine(pluma, P(10, 5), P(14, 5));
                    g.DrawLine(pluma, P(10, 12), P(10, 18));
                    g.DrawLine(pluma, P(14, 12), P(14, 18));
                    break;

                case Icono.Mas:
                    g.DrawLine(pluma, P(12, 5), P(12, 19));
                    g.DrawLine(pluma, P(5, 12), P(19, 12));
                    break;

                case Icono.Candado:
                    g.DrawRectangle(pluma, Rectangle.Round(R(6, 11, 12, 10)));
                    g.DrawArc(pluma, R(8, 4, 8, 10), 180, 180);
                    g.FillEllipse(pincel, R(10.7f, 14.5f, 2.6f, 2.6f));
                    break;

                case Icono.Check:
                    g.DrawLines(pluma, new[] { P(4, 12), P(10, 18), P(20, 6) });
                    break;

                case Icono.X:
                    g.DrawLine(pluma, P(6, 6), P(18, 18));
                    g.DrawLine(pluma, P(18, 6), P(6, 18));
                    break;

                case Icono.Reloj:
                    g.DrawEllipse(pluma, R(4, 4, 16, 16));
                    g.DrawLine(pluma, P(12, 12), P(12, 7));
                    g.DrawLine(pluma, P(12, 12), P(16, 14));
                    break;

                case Icono.Alerta:
                    g.DrawLines(pluma, new[] { P(12, 4), P(21, 20), P(3, 20), P(12, 4) });
                    g.DrawLine(pluma, P(12, 10), P(12, 15));
                    g.FillEllipse(pincel, R(11, 17, 2, 2));
                    break;

                case Icono.Flecha:
                    g.DrawLines(pluma, new[] { P(8, 10), P(12, 14), P(16, 10) });
                    break;

                case Icono.Descargar:
                    g.DrawLine(pluma, P(12, 4), P(12, 15));
                    g.DrawLines(pluma, new[] { P(7, 11), P(12, 16), P(17, 11) });
                    g.DrawLine(pluma, P(5, 19), P(19, 19));
                    break;

                case Icono.Restaurar:
                    g.DrawArc(pluma, R(4, 4, 16, 16), -20, 280);
                    g.DrawLines(pluma, new[] { P(19, 4), P(19, 9), P(14, 9) });
                    break;
            }

            g.SmoothingMode = estadoAnterior;
        }
    }
}
