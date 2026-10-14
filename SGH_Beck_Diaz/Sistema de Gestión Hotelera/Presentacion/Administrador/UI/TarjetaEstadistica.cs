using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Presentacion.Recepcionista;

namespace Presentacion.Administrador.UI
{
    /// <summary>Stat card del dashboard: icono + valor grande + título + subtítulo opcional.</summary>
    internal class TarjetaEstadistica : Panel
    {
        private readonly Label _lblValor;
        private readonly Label _lblTitulo;
        private readonly Label _lblSubtitulo;
        private Color _colorAcento = Paleta.Primario;
        private Icono _icono = Icono.Grafico;

        public TarjetaEstadistica()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Paleta.FondoTarjeta;
            Size = new Size(220, 112);
            Padding = new Padding(18, 16, 16, 14);

            _lblValor = new Label { AutoSize = true, Font = Paleta.FuenteValor, ForeColor = Paleta.TextoPrimario, BackColor = Color.Transparent, Location = new Point(18, 44) };
            _lblTitulo = new Label { AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, BackColor = Color.Transparent, Location = new Point(18, 82) };
            _lblSubtitulo = new Label { AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, BackColor = Color.Transparent, Location = new Point(18, 82), Visible = false };

            Controls.Add(_lblValor);
            Controls.Add(_lblTitulo);
            Controls.Add(_lblSubtitulo);
        }

        public void Configurar(string titulo, string valor, Color colorAcento, Icono icono, string? subtitulo = null)
        {
            _lblTitulo.Text = titulo;
            _lblValor.Text = valor;
            _colorAcento = colorAcento;
            _icono = icono;

            if (!string.IsNullOrWhiteSpace(subtitulo))
            {
                _lblSubtitulo.Text = subtitulo;
                _lblSubtitulo.Visible = true;
                _lblTitulo.Location = new Point(18, 78);
                _lblSubtitulo.Location = new Point(18, 96);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width - 1, Height - 1), 12);
            using var brochaFondo = new SolidBrush(Paleta.FondoTarjeta);
            e.Graphics.FillPath(brochaFondo, ruta);

            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawPath(lapiz, ruta);

            var badge = new RectangleF(Width - 52, 16, 36, 36);
            using var rutaBadge = DibujoUtil.RutaRedondeada(badge, 9);
            using var brochaBadge = new SolidBrush(Color.FromArgb(28, _colorAcento));
            e.Graphics.FillPath(brochaBadge, rutaBadge);

            var areaIcono = new Rectangle((int)badge.X + 7, (int)badge.Y + 7, 22, 22);
            IconosUI.Dibujar(e.Graphics, _icono, areaIcono, _colorAcento, 1.7f);
        }
    }
}
