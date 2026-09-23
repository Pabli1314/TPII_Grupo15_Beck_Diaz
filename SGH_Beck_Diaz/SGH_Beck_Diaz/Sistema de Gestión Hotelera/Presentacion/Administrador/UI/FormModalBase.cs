using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Presentacion.Recepcionista;

namespace Presentacion.Administrador.UI
{
    /// <summary>
    /// Form base para todos los modales del Administrador: barra de título propia (arrastrable),
    /// botón de cerrar y esquinas redondeadas. El contenido de cada modal va en <see cref="Contenido"/>.
    /// </summary>
    internal class FormModalBase : Form
    {
        private const int RadioEsquina = 14;
        private const int AltoBarra = 52;

        protected Panel Contenido { get; }
        private readonly Label _lblTitulo;
        private Point _puntoArrastre;
        private bool _arrastrando;

        public FormModalBase()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Paleta.FondoTarjeta;
            Font = Paleta.FuenteBase;

            var barra = new Panel { Dock = DockStyle.Top, Height = AltoBarra, BackColor = Paleta.FondoTarjeta };
            barra.MouseDown += (s, e) => { _arrastrando = true; _puntoArrastre = e.Location; };
            barra.MouseMove += (s, e) => { if (_arrastrando) { Location = new Point(Location.X + e.X - _puntoArrastre.X, Location.Y + e.Y - _puntoArrastre.Y); } };
            barra.MouseUp += (s, e) => _arrastrando = false;

            _lblTitulo = new Label
            {
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(24, 0),
                Size = new Size(360, AltoBarra),
                BackColor = Color.Transparent
            };
            barra.Controls.Add(_lblTitulo);

            var btnCerrar = new Button
            {
                Size = new Size(36, 36),
                Location = new Point(0, (AltoBarra - 36) / 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Paleta.FondoTarjeta,
                Cursor = Cursors.Hand,
                Text = string.Empty
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Paint += (s, e) => IconosUI.Dibujar(e.Graphics, Icono.X, new Rectangle(8, 8, 20, 20), Paleta.TextoSecundario, 1.8f);
            btnCerrar.MouseEnter += (s, e) => btnCerrar.BackColor = Paleta.PeligroSuave;
            btnCerrar.MouseLeave += (s, e) => btnCerrar.BackColor = Paleta.FondoTarjeta;
            btnCerrar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            barra.Controls.Add(btnCerrar);
            barra.Resize += (s, e) => btnCerrar.Location = new Point(barra.Width - 48, (AltoBarra - 36) / 2);

            var separador = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Paleta.Borde };

            var marco = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.FondoTarjeta, Padding = new Padding(24) };
            // AutoScroll: si el contenido no entra verticalmente (pantallas chicas o con escalado
            // de DPI alto), queda accesible haciendo scroll en vez de quedar cortado fuera de vista.
            Contenido = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.FondoTarjeta, AutoScroll = true };
            marco.Controls.Add(Contenido);

            Controls.Add(marco);
            Controls.Add(separador);
            Controls.Add(barra);

            Load += (s, e) => AjustarAlAreaDeTrabajo();
            Load += (s, e) => AplicarEsquinasRedondeadas();
            Resize += (s, e) => AplicarEsquinasRedondeadas();
        }

        /// <summary>
        /// Si el tamaño que definió el modal queda más grande que el área de trabajo de la pantalla
        /// (resolución chica o escalado de DPI alto), lo recorta para que entre entero; lo que no
        /// entre en el modal recortado se alcanza haciendo scroll en <see cref="Contenido"/> en vez
        /// de quedar fuera de la pantalla sin forma de llegar a esos controles.
        /// </summary>
        private void AjustarAlAreaDeTrabajo()
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int anchoMaximo = area.Width - 40;
            int altoMaximo = area.Height - 40;

            if (Width > anchoMaximo || Height > altoMaximo)
            {
                Width = Math.Min(Width, anchoMaximo);
                Height = Math.Min(Height, altoMaximo);
                CenterToScreen();
            }
        }

        protected new string Text
        {
            get => _lblTitulo.Text;
            set { _lblTitulo.Text = value; base.Text = value; }
        }

        protected void EstablecerTitulo(string titulo) => Text = titulo;

        private void AplicarEsquinasRedondeadas()
        {
            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width, Height), RadioEsquina);
            Region = new Region(ruta);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawRectangle(lapiz, 0, 0, Width - 1, Height - 1);
        }
    }
}
