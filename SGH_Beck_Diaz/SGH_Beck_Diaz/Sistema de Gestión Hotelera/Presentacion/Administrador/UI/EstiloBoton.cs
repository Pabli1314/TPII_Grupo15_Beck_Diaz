using System;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    internal static class EstiloBoton
    {
        public static Button Primario(Button b)
        {
            Base(b);
            b.BackColor = Paleta.Primario;
            b.ForeColor = System.Drawing.Color.White;
            AplicarHover(b, Paleta.Primario, Paleta.PrimarioHover);
            return b;
        }

        public static Button Secundario(Button b)
        {
            Base(b);
            b.BackColor = Paleta.FondoTarjeta;
            b.ForeColor = Paleta.TextoPrimario;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Paleta.Borde;
            AplicarHover(b, Paleta.FondoTarjeta, Paleta.BordeSuave);
            return b;
        }

        public static Button Peligro(Button b)
        {
            Base(b);
            b.BackColor = Paleta.Peligro;
            b.ForeColor = System.Drawing.Color.White;
            AplicarHover(b, Paleta.Peligro, System.Drawing.Color.FromArgb(189, 43, 58));
            return b;
        }

        public static Button Fantasma(Button b)
        {
            Base(b);
            b.BackColor = System.Drawing.Color.Transparent;
            b.ForeColor = Paleta.TextoSecundario;
            AplicarHover(b, Paleta.FondoTarjeta, Paleta.BordeSuave);
            return b;
        }

        private static void Base(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = Paleta.FuenteBaseNegrita;
            b.Cursor = Cursors.Hand;
            b.Height = Math.Max(b.Height, 36);
            b.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            b.UseVisualStyleBackColor = false;
        }

        private static void AplicarHover(Button b, System.Drawing.Color normal, System.Drawing.Color hover)
        {
            b.MouseEnter += (s, e) => b.BackColor = hover;
            b.MouseLeave += (s, e) => b.BackColor = normal;
        }
    }
}
