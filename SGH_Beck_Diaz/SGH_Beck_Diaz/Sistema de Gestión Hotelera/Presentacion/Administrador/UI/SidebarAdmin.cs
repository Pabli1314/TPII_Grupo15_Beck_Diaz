using Presentacion.Recepcionista;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Ítem de navegación del sidebar: icono + texto, con estado activo/hover propio.</summary>
    internal class ItemNavSidebar : Panel
    {
        public string Clave { get; }
        private readonly Icono _icono;
        private readonly Label _lblTexto;
        private bool _activo;
        private bool _esSalir;

        public bool Activo
        {
            get => _activo;
            set { _activo = value; ActualizarColores(); Invalidate(); }
        }

        public ItemNavSidebar(string clave, string texto, Icono icono, bool esSalir = false)
        {
            Clave = clave;
            _icono = icono;
            _esSalir = esSalir;
            Size = new Size(260, 46);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;

            _lblTexto = new Label
            {
                Text = texto,
                AutoSize = false,
                Location = new Point(58, 0),
                Size = new Size(180, 46),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Paleta.FuenteBaseNegrita,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            Controls.Add(_lblTexto);
            _lblTexto.Click += (s, e) => OnClick(EventArgs.Empty);

            MouseEnter += (s, e) => { if (!_activo) BackColor = Paleta.SidebarHover; };
            MouseLeave += (s, e) => { if (!_activo) BackColor = Paleta.Sidebar; };

            ActualizarColores();
        }

        private void ActualizarColores()
        {
            BackColor = _activo ? Paleta.SidebarHover : Paleta.Sidebar;
            _lblTexto.ForeColor = _activo ? Paleta.SidebarTextoActivo : (_esSalir ? Color.FromArgb(234, 130, 130) : Paleta.SidebarTexto);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_activo)
            {
                using var brochaBarra = new SolidBrush(Paleta.Primario);
                e.Graphics.FillRectangle(brochaBarra, 0, 4, 4, Height - 8);
            }

            Color colorIcono = _activo ? Paleta.SidebarTextoActivo : (_esSalir ? Color.FromArgb(234, 130, 130) : Paleta.SidebarTexto);
            IconosUI.Dibujar(e.Graphics, _icono, new Rectangle(20, 13, 20, 20), colorIcono, 1.7f);
        }
    }

    /// <summary>Sidebar fijo del Administrador: logo, usuario/rol, navegación y cerrar sesión.</summary>
    internal class SidebarAdmin : Panel
    {
        private readonly List<ItemNavSidebar> _items = new();
        private readonly Label _lblNombreUsuario;

        public event Action<string>? NavegacionSeleccionada;
        public event EventHandler? CerrarSesionSolicitado;

        public SidebarAdmin()
        {
            Dock = DockStyle.Left;
            Width = 260;
            BackColor = Paleta.Sidebar;

            var panelLogo = new Panel { Size = new Size(260, 84), Location = new Point(0, 0), BackColor = Color.Transparent };
            var lblSigla = new Label { Text = "SGH", Font = new Font("Segoe UI Semibold", 20f), ForeColor = Color.White, AutoSize = true, Location = new Point(24, 16), BackColor = Color.Transparent };
            var lblNombreSistema = new Label { Text = "Sistema de Gestión Hotelera", Font = Paleta.FuenteChica, ForeColor = Paleta.SidebarTexto, AutoSize = true, Location = new Point(24, 52), BackColor = Color.Transparent };
            panelLogo.Controls.Add(lblSigla);
            panelLogo.Controls.Add(lblNombreSistema);

            var panelUsuario = new Panel { Size = new Size(260, 88), Location = new Point(0, 84), BackColor = Color.Transparent };
            var avatar = new AvatarCircular { Size = new Size(42, 42), Location = new Point(24, 12), Iniciales = "AD" };
            _lblNombreUsuario = new Label { Text = "Administrador", Font = Paleta.FuenteBaseNegrita, ForeColor = Color.White, AutoSize = true, Location = new Point(76, 14), BackColor = Color.Transparent };
            var badgeRol = new BadgeEstado { Location = new Point(76, 36) };
            badgeRol.FijarEstado("ADMINISTRADOR", Paleta.Primario);
            panelUsuario.Controls.Add(avatar);
            panelUsuario.Controls.Add(_lblNombreUsuario);
            panelUsuario.Controls.Add(badgeRol);

            var separador = new Panel { Size = new Size(260, 1), Location = new Point(0, 172), BackColor = Paleta.SidebarHover };

            var panelNav = new FlowLayoutPanel
            {
                Location = new Point(0, 184),
                Size = new Size(260, 380),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            (string clave, string texto, Icono icono)[] navegacion =
            {
                ("dashboard", "Dashboard", Icono.Home),
                ("habitaciones", "Habitaciones", Icono.Cama),
                ("usuarios", "Usuarios", Icono.Personas),
                ("tarifas", "Tarifas", Icono.Etiqueta),
                ("inventario", "Inventario", Icono.Caja),
                ("reportes", "Reportes", Icono.Grafico),
                ("configuracion", "Configuración", Icono.Engranaje),
                ("backups", "Copias de seguridad", Icono.Disco),
            };

            foreach (var (clave, texto, icono) in navegacion)
            {
                var item = new ItemNavSidebar(clave, texto, icono);
                item.Click += (s, e) => SeleccionarInterno(clave);
                _items.Add(item);
                panelNav.Controls.Add(item);
            }

            var itemSalir = new ItemNavSidebar("salir", "Cerrar sesión", Icono.Salir, esSalir: true);
            itemSalir.Click += (s, e) => CerrarSesionSolicitado?.Invoke(this, EventArgs.Empty);
            itemSalir.Location = new Point(0, 0);
            itemSalir.Dock = DockStyle.Bottom;
            itemSalir.Margin = new Padding(0, 0, 0, 16);

            Controls.Add(panelNav);
            Controls.Add(separador);
            Controls.Add(panelUsuario);
            Controls.Add(panelLogo);
            Controls.Add(itemSalir);
        }

        public void ConfigurarUsuario(string nombreCompleto)
        {
            _lblNombreUsuario.Text = string.IsNullOrWhiteSpace(nombreCompleto) ? "Administrador" : nombreCompleto;
        }

        public void EstablecerActivo(string clave)
        {
            foreach (ItemNavSidebar item in _items)
            {
                item.Activo = item.Clave == clave;
            }
        }

        private void SeleccionarInterno(string clave)
        {
            EstablecerActivo(clave);
            NavegacionSeleccionada?.Invoke(clave);
        }
    }

    /// <summary>Círculo con iniciales, usado como avatar simple sin depender de imágenes.</summary>
    internal class AvatarCircular : Control
    {
        public string Iniciales { get; set; } = "AD";
        public Color ColorFondo { get; set; } = Color.FromArgb(37, 99, 235);

        public AvatarCircular()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brocha = new SolidBrush(ColorFondo);
            e.Graphics.FillEllipse(brocha, 0, 0, Width - 1, Height - 1);

            using var fuente = new Font("Segoe UI Semibold", 12f);
            var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(Iniciales, fuente, Brushes.White, new RectangleF(0, 0, Width, Height), formato);
        }
    }
}
