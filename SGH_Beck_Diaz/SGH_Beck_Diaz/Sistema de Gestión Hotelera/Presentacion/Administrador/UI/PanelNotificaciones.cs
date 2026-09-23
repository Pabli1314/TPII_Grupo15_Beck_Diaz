using Entidades;
using Presentacion.Recepcionista;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>Popup desplegable con la lista de notificaciones, mostrado desde la campana del header.</summary>
    internal class PanelNotificaciones : Form
    {
        private readonly FlowLayoutPanel _lista;
        private readonly Label _lblVacio;

        public event EventHandler<string>? NotificacionDescartada;
        public event EventHandler? MarcarTodasComoLeidasClick;

        public PanelNotificaciones()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(360, 420);
            BackColor = Paleta.FondoTarjeta;
            ShowInTaskbar = false;

            var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Paleta.FondoTarjeta };
            var lblTitulo = new Label { Text = "Notificaciones", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(16, 12), BackColor = Color.Transparent };
            var lblMarcarTodas = new Label { Text = "Marcar todas como leídas", Font = Paleta.FuenteChica, ForeColor = Paleta.Primario, AutoSize = true, Cursor = Cursors.Hand, BackColor = Color.Transparent };
            lblMarcarTodas.Location = new Point(Width - lblMarcarTodas.PreferredWidth - 20, 16);
            lblMarcarTodas.Click += (s, e) => MarcarTodasComoLeidasClick?.Invoke(this, EventArgs.Empty);
            barra.Controls.Add(lblTitulo);
            barra.Controls.Add(lblMarcarTodas);

            var separador = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Paleta.Borde };

            _lista = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Paleta.FondoTarjeta };

            _lblVacio = new Label { Text = "No hay notificaciones pendientes.", Font = Paleta.FuenteBase, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(16, 16), Visible = false };

            Controls.Add(_lista);
            Controls.Add(_lblVacio);
            Controls.Add(separador);
            Controls.Add(barra);

            Deactivate += (s, e) => Close();
        }

        public void CargarNotificaciones(List<NotificacionItem> notificaciones)
        {
            _lista.Controls.Clear();
            _lblVacio.Visible = notificaciones.Count == 0;

            foreach (NotificacionItem noti in notificaciones)
            {
                _lista.Controls.Add(CrearFila(noti));
            }
        }

        private Panel CrearFila(NotificacionItem noti)
        {
            var (colorIcono, icono) = noti.Tipo switch
            {
                TipoNotificacion.Exito => (Paleta.Exito, Icono.Check),
                TipoNotificacion.Error => (Paleta.Peligro, Icono.Alerta),
                TipoNotificacion.Advertencia => (Color.FromArgb(217, 119, 6), Icono.Alerta),
                _ => (Paleta.Mantenimiento, Icono.Campana)
            };

            var fila = new Panel { Size = new Size(344, 76), BackColor = Paleta.FondoTarjeta, Margin = new Padding(0) };

            fila.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                IconosUI.Dibujar(e.Graphics, icono, new Rectangle(16, 14, 20, 20), colorIcono, 1.6f);
                using var lapiz = new Pen(Paleta.BordeSuave);
                e.Graphics.DrawLine(lapiz, 0, fila.Height - 1, fila.Width, fila.Height - 1);
            };

            var lblTitulo = new Label { Text = noti.Titulo, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, Location = new Point(48, 10), AutoSize = false, Size = new Size(260, 20), BackColor = Color.Transparent };
            var lblMensaje = new Label { Text = noti.Mensaje, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, Location = new Point(48, 30), AutoSize = false, Size = new Size(280, 34), BackColor = Color.Transparent };
            var lblHora = new Label { Text = noti.Fecha.ToString("HH:mm"), Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, Location = new Point(300, 10), AutoSize = true, BackColor = Color.Transparent };

            fila.Controls.Add(lblTitulo);
            fila.Controls.Add(lblMensaje);
            fila.Controls.Add(lblHora);

            return fila;
        }

        protected override bool ShowWithoutActivation => false;
    }
}
