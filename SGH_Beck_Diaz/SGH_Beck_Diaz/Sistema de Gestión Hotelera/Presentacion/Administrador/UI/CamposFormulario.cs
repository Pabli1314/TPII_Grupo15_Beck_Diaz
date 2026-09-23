using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Fábricas de controles de formulario con el estilo del Administrador, para no repetir
    /// la configuración de fuente/color/borde en cada modal.</summary>
    internal static class CamposFormulario
    {
        public static Label Etiqueta(string texto, Point ubicacion) => new Label
        {
            Text = texto,
            Location = ubicacion,
            AutoSize = true,
            Font = Paleta.FuenteBaseNegrita,
            ForeColor = Paleta.TextoPrimario
        };

        public static TextBox Texto(Point ubicacion, int ancho, bool esPassword = false) => new TextBox
        {
            Location = ubicacion,
            Size = new Size(ancho, 30),
            Font = Paleta.FuenteBase,
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = esPassword
        };

        /// <summary>Limita el TextBox a dígitos y a <paramref name="largoMaximo"/> caracteres (DNI, teléfono).
        /// Lo pegado desde el portapapeles igual se valida en la capa Lógica.</summary>
        public static TextBox SoloNumeros(TextBox txt, int largoMaximo)
        {
            txt.MaxLength = largoMaximo;
            txt.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            return txt;
        }

        public static ComboBox Combo(Point ubicacion, int ancho) => new ComboBox
        {
            Location = ubicacion,
            Size = new Size(ancho, 30),
            Font = Paleta.FuenteBase,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };

        public static NumericUpDown Numerico(Point ubicacion, int ancho, decimal min, decimal max, int decimales = 0) => new NumericUpDown
        {
            Location = ubicacion,
            Size = new Size(ancho, 30),
            Font = Paleta.FuenteBase,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimales,
            BorderStyle = BorderStyle.FixedSingle
        };

        public static Label Error(Point ubicacion, int ancho) => new Label
        {
            Location = ubicacion,
            Size = new Size(ancho, 18),
            Font = Paleta.FuenteChica,
            ForeColor = Paleta.Peligro,
            Visible = false
        };

        /// <summary>Marca/desmarca visualmente un campo como inválido (borde rojo).</summary>
        public static void MarcarInvalido(Control control, Label? error, string? mensaje)
        {
            if (control is TextBox txt)
            {
                txt.BackColor = mensaje != null ? Paleta.PeligroSuave : Color.White;
            }

            if (error != null)
            {
                error.Text = mensaje ?? string.Empty;
                error.Visible = mensaje != null;
            }
        }
    }
}
