using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Switch on/off dibujado a mano, usado en Configuración de alertas.</summary>
    internal class ToggleSwitch : Control
    {
        private bool _checked;

        public event EventHandler? CheckedChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color colorFondo = _checked ? Paleta.Primario : Color.FromArgb(203, 210, 222);
            var pista = new RectangleF(0, 0, Width - 1, Height - 1);

            using var ruta = Presentacion.Recepcionista.DibujoUtil.RutaRedondeada(pista, Height / 2f);
            using var brochaFondo = new SolidBrush(colorFondo);
            e.Graphics.FillPath(brochaFondo, ruta);

            float diametro = Height - 6;
            float x = _checked ? Width - diametro - 3 : 3;
            using var brochaKnob = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(brochaKnob, x, 3, diametro, diametro);
        }
    }
}
