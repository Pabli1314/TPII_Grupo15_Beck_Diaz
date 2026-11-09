using Entidades;
using Logica;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    public partial class UC_Habitacion : UserControl
    {
        private const int Radio = 14;

        private readonly GestionConfiguracion _gestionConfiguracion = new();
        private DateTime? _horaSalidaEstimada;
        private bool _vencida;
        private bool _avisoSonoroReproducido;

        public string NumeroHabitacion { get; private set; } = string.Empty;
        public EstadoOcupacion Estado { get; private set; }

        /// <summary>
        /// Se dispara al hacer clic en el botón "Extender estadía" disponible en habitaciones ocupadas.
        /// </summary>
        public event EventHandler? ExtenderEstadiaClick;

        public UC_Habitacion()
        {
            InitializeComponent();

            MinimumSize = MaximumSize = Size;
            Cursor = Cursors.Hand;
            badgeEstado.BackColor = BackColor;

            btnExtender.Click += (s, e) => ExtenderEstadiaClick?.Invoke(this, EventArgs.Empty);

            // Efecto hover sobre el botón extender
            btnExtender.MouseEnter += (s, e) => btnExtender.BackColor = Color.FromArgb(200, 220, 250);
            btnExtender.MouseLeave += (s, e) => btnExtender.BackColor = Color.FromArgb(224, 234, 253);

            Region = new Region(DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width, Height), Radio));

            PropagarClick(this);
        }

        /// <summary>Configura el contenido y el estado visual de la tarjeta de habitación: características
        /// (tipo, camas, piso) y tarifa por día; si está ocupada, el huésped, la fecha de salida y el
        /// costo del hospedaje, más una cuenta regresiva que resalta la tarjeta cuando se acerca o
        /// supera la tolerancia configurada, y el botón "Extender estadía".</summary>
        public void ConfigurarTarjeta(HabitacionInfo habitacion)
        {
            if (habitacion == null) throw new ArgumentNullException(nameof(habitacion));

            NumeroHabitacion = habitacion.NroHabitacion;
            Estado = habitacion.Estado;
            _horaSalidaEstimada = habitacion.HoraSalidaEstimada;
            _vencida = false;
            _avisoSonoroReproducido = false;

            EstadoOcupacion estado = habitacion.Estado;
            string huesped = habitacion.Huesped;
            DateTime? horaSalidaEstimada = habitacion.HoraSalidaEstimada;

            lblNumero.Text = habitacion.NroHabitacion;
            lblTipo.Text = habitacion.Caracteristicas;

            lblSalida.Visible = estado == EstadoOcupacion.Ocupada && horaSalidaEstimada.HasValue;
            lblSalida.Text = horaSalidaEstimada.HasValue ? $"Salida: {horaSalidaEstimada.Value:dd/MM/yyyy HH:mm}" : string.Empty;

            lblPie.Text = estado == EstadoOcupacion.Ocupada && habitacion.CostoHospedaje.HasValue
                ? $"Hospedaje: {habitacion.CostoHospedaje.Value:C0} ({habitacion.Noches} noche{(habitacion.Noches == 1 ? string.Empty : "s")})"
                : $"Tarifa: {habitacion.TarifaPorDia:C0} por día";

            Color colorEstado = DibujoUtil.ColorPorEstado(estado);
            badgeEstado.FijarEstado(DibujoUtil.TextoPorEstado(estado), colorEstado);
            badgeEstado.Location = new Point(Width - badgeEstado.Width - 10, 8);

            bool esOcupada = estado == EstadoOcupacion.Ocupada;

            lblHuesped.Text = huesped ?? string.Empty;
            lblHuesped.Visible = esOcupada && !string.IsNullOrWhiteSpace(huesped);

            lblTemporizador.Visible = esOcupada && horaSalidaEstimada.HasValue;

            // Botón extender estadía visible solo en habitaciones ocupadas
            btnExtender.Visible = esOcupada;

            if (esOcupada && horaSalidaEstimada.HasValue)
            {
                ActualizarTemporizador();
                temporizador.Start();
            }
            else
            {
                temporizador.Stop();
                lblTemporizador.Text = string.Empty;
                Invalidate();
            }
        }

        private void temporizador_Tick(object sender, EventArgs e) => ActualizarTemporizador();

        private void ActualizarTemporizador()
        {
            if (!_horaSalidaEstimada.HasValue)
            {
                return;
            }

            ConfiguracionAlertas configuracion = _gestionConfiguracion.Obtener();
            TimeSpan restante = _horaSalidaEstimada.Value - DateTime.Now;
            bool vencida = restante.TotalSeconds < 0;
            TimeSpan absoluto = vencida ? restante.Negate() : restante;
            bool proximaAVencer = !vencida && restante.TotalMinutes <= configuracion.MinutosTolerancia;

            lblTemporizador.Text = vencida ? $"Vencida hace {absoluto:hh\\:mm\\:ss}" : $"Salida en {absoluto:hh\\:mm\\:ss}";
            lblTemporizador.ForeColor = vencida
                ? DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada)
                : (proximaAVencer ? Color.FromArgb(217, 119, 6) : Color.FromArgb(40, 199, 111));

            bool resaltar = configuracion.AlertasVisualesActivas && (vencida || proximaAVencer);
            if (resaltar != _vencida)
            {
                _vencida = resaltar;
                Invalidate();
            }

            if (vencida && configuracion.AlertasSonorasActivas && !_avisoSonoroReproducido)
            {
                _avisoSonoroReproducido = true;
                SystemSounds.Exclamation.Play();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0.5F, 0.5F, Width - 1, Height - 1), Radio - 1);
            using var lapiz = new Pen(_vencida ? DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada) : Color.FromArgb(220, 224, 230), _vencida ? 2.5f : 1f);
            e.Graphics.DrawPath(lapiz, ruta);
        }

        /// <summary>
        /// Permite hacer click en cualquier parte de la tarjeta para abrir la gestión general,
        /// pero excluye el botón btnExtender para que su acción sea independiente.
        /// </summary>
        private void PropagarClick(Control contenedor)
        {
            foreach (Control hijo in contenedor.Controls)
            {
                if (hijo == btnExtender) continue;

                hijo.Cursor = Cursors.Hand;
                hijo.Click += (s, e) => OnClick(EventArgs.Empty);

                if (hijo.HasChildren) PropagarClick(hijo);
            }
        }
    }
}
