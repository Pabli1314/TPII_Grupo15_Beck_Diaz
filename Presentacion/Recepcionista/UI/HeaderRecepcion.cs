using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.UI
{
    /// <summary>Header superior fijo del Recepcionista: título de sección, notificaciones y menú
    /// de usuario. Mismo patrón visual</summary>
    internal class HeaderRecepcion : Panel
    {
        private readonly Label _lblTitulo;
        private readonly BotonCampana _campana;
        private readonly AvatarCircular _avatar;
        private readonly Label _lblUsuario;
        private readonly System.Windows.Forms.Timer _temporizadorNotificaciones;
        private readonly GestionNotificaciones _gestionNotificaciones = new();
        private PanelNotificaciones? _popupNotificaciones;

        public event EventHandler? MiPerfilClick;
        public event EventHandler? CambiarPasswordClick;
        public event EventHandler? CerrarSesionClick;

        public HeaderRecepcion()
        {
            Dock = DockStyle.Top;
            Height = 68;
            BackColor = Paleta.FondoTarjeta;
            Padding = new Padding(28, 0, 28, 0);

            _lblTitulo = new Label { Text = "Dashboard", Font = Paleta.FuenteTitulo, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(28, 18), BackColor = Color.Transparent };
            Controls.Add(_lblTitulo);

            _campana = new BotonCampana();
            _campana.Click += (s, e) => AlternarNotificaciones();
            Controls.Add(_campana);

            var chipPerfil = new Panel { Size = new Size(170, 48), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            _avatar = new AvatarCircular { Size = new Size(36, 36), Location = new Point(0, 6), Iniciales = "RE" };
            _lblUsuario = new Label { Text = "Recepcionista", Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(46, 8), BackColor = Color.Transparent };
            var lblRol = new Label { Text = "Recepcionista", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(46, 26), BackColor = Color.Transparent };
            chipPerfil.Controls.Add(_avatar);
            chipPerfil.Controls.Add(_lblUsuario);
            chipPerfil.Controls.Add(lblRol);
            chipPerfil.Paint += (s, e) => IconosUI.Dibujar(e.Graphics, Icono.Flecha, new Rectangle(148, 16, 16, 16), Paleta.TextoTerciario, 1.6f);

            var menu = new ContextMenuStrip { Font = Paleta.FuenteBase };
            menu.Items.Add("Mi perfil", null, (s, e) => MiPerfilClick?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Cambiar contraseña", null, (s, e) => CambiarPasswordClick?.Invoke(this, EventArgs.Empty));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Cerrar sesión", null, (s, e) => CerrarSesionClick?.Invoke(this, EventArgs.Empty));

            chipPerfil.Click += (s, e) => menu.Show(chipPerfil, new Point(0, chipPerfil.Height));
            foreach (Control hijo in chipPerfil.Controls)
            {
                hijo.Cursor = Cursors.Hand;
                hijo.Click += (s, e) => menu.Show(chipPerfil, new Point(0, chipPerfil.Height));
            }

            Controls.Add(chipPerfil);

            Resize += (s, e) => AcomodarDerecha(chipPerfil);
            AcomodarDerecha(chipPerfil);

            _temporizadorNotificaciones = new System.Windows.Forms.Timer { Interval = 30000 };
            _temporizadorNotificaciones.Tick += (s, e) => ActualizarContadorNotificaciones();
            _temporizadorNotificaciones.Start();
            ActualizarContadorNotificaciones();

            Disposed += (s, e) => _temporizadorNotificaciones.Dispose();
        }

        private void AcomodarDerecha(Panel chipPerfil)
        {
            chipPerfil.Location = new Point(Width - chipPerfil.Width - 28, (Height - chipPerfil.Height) / 2);
            _campana.Location = new Point(chipPerfil.Left - 56, (Height - _campana.Height) / 2);
        }

        public void EstablecerTitulo(string titulo) => _lblTitulo.Text = titulo;

        public void ConfigurarUsuario(string nombreCompleto)
        {
            _lblUsuario.Text = string.IsNullOrWhiteSpace(nombreCompleto) ? "Recepcionista" : nombreCompleto;
        }

        public void ActualizarContadorNotificaciones()
        {
            _campana.Contador = _gestionNotificaciones.ObtenerActivas().Count;
            _campana.Invalidate();
        }

        private void AlternarNotificaciones()
        {
            if (_popupNotificaciones is { IsDisposed: false })
            {
                _popupNotificaciones.Close();
                return;
            }

            _popupNotificaciones = new PanelNotificaciones();
            _popupNotificaciones.CargarNotificaciones(_gestionNotificaciones.ObtenerActivas());
            _popupNotificaciones.MarcarTodasComoLeidasClick += (s, e) =>
            {
                var claves = _gestionNotificaciones.ObtenerActivas().ConvertAll(n => n.Clave);
                _gestionNotificaciones.DescartarTodas(claves);
                ActualizarContadorNotificaciones();
                _popupNotificaciones?.Close();
            };

            Point puntoPantalla = PointToScreen(new Point(_campana.Left, _campana.Bottom + 8));
            _popupNotificaciones.Location = new Point(puntoPantalla.X - _popupNotificaciones.Width + _campana.Width, puntoPantalla.Y);
            _popupNotificaciones.FormClosed += (s, e) => ActualizarContadorNotificaciones();
            _popupNotificaciones.Show(FindForm());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawLine(lapiz, 0, Height - 1, Width, Height - 1);
        }
    }
}
