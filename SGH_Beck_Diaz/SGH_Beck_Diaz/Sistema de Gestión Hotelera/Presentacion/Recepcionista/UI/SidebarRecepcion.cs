using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.UI
{
    /// <summary>Sidebar fijo del Recepcionista: logo, usuario/rol, navegación y cerrar sesión.
    /// Mismo patrón visual que SidebarAdmin, con las secciones propias de Recepción.</summary>
    internal class SidebarRecepcion : Panel
    {
        private readonly List<ItemNavSidebar> _items = new();
        private readonly Label _lblNombreUsuario;

        public event Action<string>? NavegacionSeleccionada;
        public event EventHandler? CerrarSesionSolicitado;

        public SidebarRecepcion()
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
            var avatar = new AvatarCircular { Size = new Size(42, 42), Location = new Point(24, 12), Iniciales = "RE" };
            _lblNombreUsuario = new Label { Text = "Recepcionista", Font = Paleta.FuenteBaseNegrita, ForeColor = Color.White, AutoSize = true, Location = new Point(76, 14), BackColor = Color.Transparent };
            var badgeRol = new BadgeEstado { Location = new Point(76, 36) };
            badgeRol.FijarEstado("RECEPCIONISTA", Paleta.Primario);
            panelUsuario.Controls.Add(avatar);
            panelUsuario.Controls.Add(_lblNombreUsuario);
            panelUsuario.Controls.Add(badgeRol);

            var separador = new Panel { Size = new Size(260, 1), Location = new Point(0, 172), BackColor = Paleta.SidebarHover };

            var panelNav = new FlowLayoutPanel
            {
                Location = new Point(0, 184),
                Size = new Size(260, 430),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            (string clave, string texto, Icono icono)[] navegacion =
            {
                ("dashboard", "Inicio", Icono.Home),
                ("habitaciones", "Habitaciones", Icono.Cama),
                ("reservas", "Reservas", Icono.Etiqueta),
                ("check-in", "Check-in", Icono.Check),
                ("check-out", "Check-out", Icono.Salir),
                ("caja", "Caja", Icono.Caja),
                ("ventas", "Ventas adicionales", Icono.Grafico),
                ("huespedes", "Huéspedes", Icono.Personas),
                ("historial", "Historial", Icono.Reloj),
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
            _lblNombreUsuario.Text = string.IsNullOrWhiteSpace(nombreCompleto) ? "Recepcionista" : nombreCompleto;
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
}
