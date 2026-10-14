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

        public string NumeroHabitacion { get; private set; }
        public EstadoOcupacion Estado { get; private set; }

        public UC_Habitacion()
        {
            InitializeComponent();

            MinimumSize = MaximumSize = Size;
            Cursor = Cursors.Hand;
            badgeEstado.BackColor = BackColor;

            Region = new Region(DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width, Height), Radio));

            PropagarClick(this);
        }

        /// <summary>Configura el contenido y el estado visual de la tarjeta de habitación. Si la
        /// habitación está ocupada y se conoce la hora estimada de salida, muestra una cuenta
        /// regresiva y resalta la tarjeta cuando se acerca o supera la tolerancia configurada.</summary>
        public void ConfigurarTarjeta(string numero, string tipo, EstadoOcupacion estado, string huesped = null, DateTime? horaIngreso = null, DateTime? horaSalidaEstimada = null)
        {
            NumeroHabitacion = numero;
            Estado = estado;
            _horaSalidaEstimada = horaSalidaEstimada;
            _vencida = false;
            _avisoSonoroReproducido = false;

            lblNumero.Text = numero;
            lblTipo.Text = tipo;

            Color colorEstado = DibujoUtil.ColorPorEstado(estado);
            badgeEstado.FijarEstado(DibujoUtil.TextoPorEstado(estado), colorEstado);
            badgeEstado.Location = new Point(Width - badgeEstado.Width - 10, 10);

            bool esOcupada = estado == EstadoOcupacion.Ocupada;

            lblHuesped.Text = huesped ?? string.Empty;
            lblHuesped.Visible = esOcupada && !string.IsNullOrWhiteSpace(huesped);

            lblTemporizador.Visible = esOcupada && horaSalidaEstimada.HasValue;

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
            using var lapiz = new Pen(_vencida ? DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada) : Color.FromArgb(33, 37, 41), _vencida ? 2.5f : 1f);
            e.Graphics.DrawPath(lapiz, ruta);
        }

        /// <summary>Permite hacer click en cualquier parte de la tarjeta, no solo en el fondo.</summary>
        private void PropagarClick(Control contenedor)
        {
            foreach (Control hijo in contenedor.Controls)
            {
                hijo.Cursor = Cursors.Hand;
                hijo.Click += (s, e) => OnClick(EventArgs.Empty);

                if (hijo.HasChildren) PropagarClick(hijo);
            }
        }
    }
}
