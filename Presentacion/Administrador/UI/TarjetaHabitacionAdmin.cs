using Entidades;
using Presentacion.Recepcionista;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    /// <summary>
    /// Tarjeta de habitación "ampliada" para Dashboard y Supervisión del Administrador: agrega
    /// huésped, hora de entrada, hora de salida estimada y cuenta regresiva sobre la tarjeta
    /// base de Recepción (misma paleta de estados, misma forma redondeada).
    /// </summary>
    internal class TarjetaHabitacionAdmin : Panel
    {
        private const int Radio = 14;

        private readonly Label _lblNumero;
        private readonly BadgeEstado _badgeEstado;
        private readonly Label _lblTipo;
        private readonly Label _lblHuesped;
        private readonly Label _lblHorario;
        private readonly Label _lblRestante;
        private readonly Label _lblLimpieza;
        private readonly Button _btnAccion;
        private readonly System.Windows.Forms.Timer _temporizador;

        private DateTime? _horaSalidaEstimada;
        private int _minutosTolerancia = 30;
        private bool _ocupada;

        public int NroHabitacion { get; private set; }

        public event EventHandler? AccionClick;

        /// <summary>Click sobre cualquier parte de la tarjeta (menos el botón de acción) cuando la
        /// habitación está Ocupada; se usa para abrir los datos del huésped alojado.</summary>
        public event EventHandler? HuespedClick;

        public TarjetaHabitacionAdmin()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(252, 196);
            BackColor = Paleta.FondoTarjeta;
            Padding = new Padding(16);

            _lblNumero = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 15f), ForeColor = Paleta.TextoPrimario, Location = new Point(16, 14), BackColor = Color.Transparent };
            _badgeEstado = new BadgeEstado { Location = new Point(16, 46) };
            _lblTipo = new Label { AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, Location = new Point(16, 74), BackColor = Color.Transparent };

            _lblHuesped = new Label { AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, Location = new Point(16, 98), BackColor = Color.Transparent, MaximumSize = new Size(220, 0) };
            _lblHorario = new Label { AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, Location = new Point(16, 120), BackColor = Color.Transparent };
            _lblRestante = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 13f), Location = new Point(16, 142), BackColor = Color.Transparent };
            _lblLimpieza = new Label { AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, Location = new Point(16, 108), BackColor = Color.Transparent, MaximumSize = new Size(220, 0) };

            _btnAccion = EstiloBoton.Primario(new Button { Text = "Marcar como limpia", Size = new Size(220, 34), Location = new Point(16, 148), Visible = false });
            _btnAccion.Click += (s, e) => AccionClick?.Invoke(this, EventArgs.Empty);

            Controls.Add(_lblNumero);
            Controls.Add(_badgeEstado);
            Controls.Add(_lblTipo);
            Controls.Add(_lblHuesped);
            Controls.Add(_lblHorario);
            Controls.Add(_lblRestante);
            Controls.Add(_lblLimpieza);
            Controls.Add(_btnAccion);

            Click += (s, e) => NotificarClickHuesped();
            foreach (Control control in Controls)
            {
                if (control != _btnAccion)
                {
                    control.Click += (s, e) => NotificarClickHuesped();
                }
            }

            _temporizador = new System.Windows.Forms.Timer { Interval = 1000 };
            _temporizador.Tick += (s, e) => ActualizarRestante();
            _temporizador.Start();

            Disposed += (s, e) => _temporizador.Dispose();
        }

        public void ConfigurarDesdeResumen(HabitacionResumen resumen, int minutosTolerancia = 30, bool mostrarAccionLimpieza = false)
        {
            _minutosTolerancia = minutosTolerancia;
            _horaSalidaEstimada = resumen.HoraSalidaEstimada;

            _lblNumero.Text = $"Hab. {resumen.NroHabitacion}";
            _lblTipo.Text = $"{resumen.TipoHabitacion} · Piso {resumen.Piso}";

            EstadoOcupacion estado = SafeEstadoDesdeTexto(resumen.Estado);
            _badgeEstado.FijarEstado(DibujoUtil.TextoPorEstado(estado), DibujoUtil.ColorPorEstado(estado));

            NroHabitacion = resumen.NroHabitacion;
            bool ocupada = resumen.Ocupada;
            _ocupada = ocupada;
            AplicarCursor(ocupada ? Cursors.Hand : Cursors.Default);
            _lblHuesped.Visible = ocupada;
            _lblHorario.Visible = ocupada;
            _lblRestante.Visible = ocupada;
            _lblLimpieza.Visible = !ocupada;

            if (ocupada)
            {
                _lblHuesped.Text = resumen.Huesped ?? "-";
                _lblHorario.Text = resumen.HoraEntrada.HasValue && resumen.HoraSalidaEstimada.HasValue
                    ? $"Entrada {resumen.HoraEntrada:dd/MM HH:mm}  →  Salida {resumen.HoraSalidaEstimada:dd/MM HH:mm}"
                    : string.Empty;
                ActualizarRestante();
            }
            else
            {
                _lblLimpieza.Text = resumen.UltimaLimpieza.HasValue
                    ? $"Última limpieza: {resumen.UltimaLimpieza:dd/MM HH:mm}"
                    : "Sin registro de limpieza.";
            }

            _btnAccion.Visible = mostrarAccionLimpieza && resumen.EnLimpieza;
            _btnAccion.Location = new Point(16, resumen.EnLimpieza ? 148 : Height - 50);

            Invalidate();
        }

        private void NotificarClickHuesped()
        {
            if (_ocupada)
            {
                HuespedClick?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AplicarCursor(Cursor cursor)
        {
            Cursor = cursor;
            foreach (Control control in Controls)
            {
                if (control != _btnAccion)
                {
                    control.Cursor = cursor;
                }
            }
        }

        private static EstadoOcupacion SafeEstadoDesdeTexto(string nombre)
        {
            try
            {
                return DibujoUtil.EstadoDesdeTexto(nombre);
            }
            catch (ArgumentOutOfRangeException)
            {
                return EstadoOcupacion.Disponible;
            }
        }

        private void ActualizarRestante()
        {
            if (!_horaSalidaEstimada.HasValue || !_lblRestante.Visible)
            {
                return;
            }

            TimeSpan restante = _horaSalidaEstimada.Value - DateTime.Now;
            bool vencida = restante.TotalSeconds < 0;
            TimeSpan absoluto = vencida ? restante.Negate() : restante;

            _lblRestante.Text = vencida ? $"Vencida hace {absoluto:hh\\:mm\\:ss}" : $"Salida en {absoluto:hh\\:mm\\:ss}";
            _lblRestante.ForeColor = vencida
                ? Paleta.Ocupada
                : (restante.TotalMinutes <= _minutosTolerancia ? Color.FromArgb(217, 119, 6) : Paleta.Exito);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Radio);
            using var brochaFondo = new SolidBrush(Paleta.FondoTarjeta);
            e.Graphics.FillPath(brochaFondo, ruta);
            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawPath(lapiz, ruta);
        }
    }
}
