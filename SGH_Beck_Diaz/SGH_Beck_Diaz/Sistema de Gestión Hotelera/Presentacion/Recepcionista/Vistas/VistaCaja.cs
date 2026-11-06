using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.Modales;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Datos;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Administración del turno de caja propio del recepcionista: apertura, resumen y cierre.</summary>
    internal class VistaCaja : UserControl
    {
        // Grilla fija de tarjetas: siempre 4 por fila, sin depender del ancho del contenedor
        // (a diferencia de un FlowLayoutPanel, que solo envuelve cuando el ancho real se lo permite).
        private const int ColumnasPorFila = 4;
        private const int AnchoTarjeta = 280;
        private const int AltoTarjeta = 182;
        private const int SeparacionTarjeta = 8;

        private readonly Usuario _usuario;
        private readonly Action _navegarAVentas;
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();

        private readonly Label _lblSubtitulo;
        private readonly Panel _panelTarjetas;
        private readonly FlowLayoutPanel _panelAcciones;
        private readonly Panel _panelSinTurno;
        private readonly Label _lblSinTurno;
        private readonly Label _lblInfoCierreAuto;

        // Timer silencioso para verificación periódica en segundo plano
        private System.Windows.Forms.Timer? _timerVerificacion;
        private bool _verificandoCierre = false;

        public VistaCaja(Usuario usuario, Action navegarAVentas)
        {
            _usuario = usuario;
            _navegarAVentas = navegarAVentas;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Estado del turno", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _lblSubtitulo = new Label { Text = string.Empty, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 26) };

            _panelTarjetas = new Panel { Location = new Point(0, 65) };

            _panelAcciones = new FlowLayoutPanel
            {
                Location = new Point(0, 260), // Se reposicionará dinámicamente en Refrescar()
                Size = new Size(1400, 60),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            _panelSinTurno = new Panel { Location = new Point(0, 60), Size = new Size(700, 160), Visible = false };
            _lblSinTurno = new Label { Text = "No hay un turno de caja abierto.", Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, AutoSize = true, Location = new Point(0, 0) };
            _lblInfoCierreAuto = new Label
            {
                Text = string.Empty,
                Font = Paleta.FuenteChica,
                ForeColor = Color.FromArgb(217, 119, 6), // Color de alerta/atención informativo
                AutoSize = true,
                Location = new Point(0, 26),
                Visible = false
            };
            var btnAbrirGrande = EstiloBoton.Primario(new Button { Text = "Abrir turno", Size = new Size(200, 44), Location = new Point(0, 75) });
            btnAbrirGrande.Click += (s, e) => AbrirTurno();
            _panelSinTurno.Controls.Add(_lblSinTurno);
            _panelSinTurno.Controls.Add(_lblInfoCierreAuto);
            _panelSinTurno.Controls.Add(btnAbrirGrande);

            Controls.Add(lblTitulo);
            Controls.Add(_lblSubtitulo);
            Controls.Add(_panelTarjetas);
            Controls.Add(_panelAcciones);
            Controls.Add(_panelSinTurno);

            // Inicializar timer de verificación periódica en segundo plano (cada 30 segundos)
            ConfigurarTimerSegundoPlano();

            Refrescar();
        }

        private void ConfigurarTimerSegundoPlano()
        {
            _timerVerificacion = new System.Windows.Forms.Timer
            {
                Interval = 30000 // 30 segundos
            };
            _timerVerificacion.Tick += TimerVerificacion_Tick;
            _timerVerificacion.Start();
        }

        private async void TimerVerificacion_Tick(object? sender, EventArgs e)
        {
            if (_verificandoCierre)
                return;

            _verificandoCierre = true;

            try
            {
                // Procesamiento asíncrono en segundo plano para no bloquear el hilo de la UI
                int turnosCerrados = await Task.Run(() => _gestionTurnoCaja.VerificarYCerrarTurnosVencidos());

                // Si se detectó y ejecutó un cierre automático, refrescar el estado visual de la vista
                if (turnosCerrados > 0 && !IsDisposed && IsHandleCreated)
                {
                    Refrescar();
                }
            }
            catch
            {
                // Verificación silenciosa para mantener la resiliencia de la interfaz
            }
            finally
            {
                _verificandoCierre = false;
            }
        }

        public void Refrescar()
        {
            // Verificación reactiva complementaria al momento de consultar el estado de caja
            _gestionTurnoCaja.VerificarYCerrarTurnosVencidos();

            TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(_usuario.DniUsuario, autoAbrirSiExpiro: false);

            _panelTarjetas.Visible = turno != null;
            _panelAcciones.Visible = turno != null;
            _panelSinTurno.Visible = turno == null;

            if (turno == null)
            {
                TurnoCaja? ultimoCerrado = TurnoCajaDAO.ObtenerUltimoCerrado(_usuario.DniUsuario);
                if (ultimoCerrado != null &&
                    !string.IsNullOrEmpty(ultimoCerrado.Observaciones) &&
                    ultimoCerrado.Observaciones.Contains(GestionTurnoCaja.OBSERVACION_CIERRE_AUTOMATICO))
                {
                    _lblSubtitulo.Text = "Caja cerrada · Fin de jornada";
                    _lblSinTurno.Text = "El turno de la jornada anterior fue cerrado automáticamente por el sistema (23:59).";
                    _lblInfoCierreAuto.Text = $"Turno #{ultimoCerrado.IdTurno} liquidado el {ultimoCerrado.FechaCierre:dd/MM/yyyy} a las {ultimoCerrado.HoraCierre:hh\\:mm\\:ss} · Total final: {ultimoCerrado.MontoFinal:C}\nInicie un nuevo turno para operar en la jornada de hoy ingresando el monto inicial correspondiente.";
                    _lblInfoCierreAuto.Visible = true;
                }
                else
                {
                    _lblSubtitulo.Text = "Caja cerrada";
                    _lblSinTurno.Text = "No hay un turno de caja abierto.";
                    _lblInfoCierreAuto.Visible = false;
                }

                return;
            }

            ResumenTurno resumen = _gestionTurnoCaja.ObtenerResumen(turno.IdTurno);
            _lblSubtitulo.Text = $"Turno #{turno.IdTurno} · Abierto {turno.FechaApertura:dd/MM/yyyy} {turno.HoraApertura:hh\\:mm}";

            var tarjetas = new List<Control>
            {
                CrearTarjeta("Monto inicial", resumen.Turno.MontoInicial, Paleta.Primario, Icono.Caja),
                CrearTarjeta("Cobrado en efectivo", resumen.TotalEfectivo, Paleta.Exito, Icono.Caja),
                CrearTarjeta("Cobrado con tarjeta", resumen.TotalTarjeta, Paleta.Primario, Icono.Etiqueta),
                CrearTarjeta("Transferencias", resumen.TotalTransferencia, Paleta.Primario, Icono.Etiqueta),
                // A partir de aquí (5ta tarjeta) pasan automáticamente a la siguiente fila:
                CrearTarjeta("Ventas adicionales", resumen.TotalVentas, Color.FromArgb(217, 119, 6), Icono.Grafico),
                CrearTarjeta("Efectivo esperado", resumen.EfectivoEsperado, Paleta.Exito, Icono.Check)
            };

            PosicionarTarjetas(tarjetas);

            // Reposicionar dinámicamente el panel de acciones debajo de las tarjetas con un margen de 20px
            _panelAcciones.Location = new Point(0, _panelTarjetas.Bottom + 20);

            _panelAcciones.Controls.Clear();

            var btnVenta = EstiloBoton.Secundario(new Button { Text = "Registrar venta adicional", Size = new Size(220, 40), Margin = new Padding(0, 0, 12, 0) });
            btnVenta.Click += (s, e) => _navegarAVentas();

            var btnCerrar = EstiloBoton.Primario(new Button { Text = "Cerrar turno", Size = new Size(160, 40) });
            btnCerrar.Click += (s, e) => CerrarTurno(turno.IdTurno);

            _panelAcciones.Controls.Add(btnVenta);
            _panelAcciones.Controls.Add(btnCerrar);
        }

        private static Control CrearTarjeta(string titulo, decimal valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica();
            tarjeta.Configurar(titulo, valor.ToString("C"), color, icono);
            return tarjeta;
        }

        /// <summary>Ubica las tarjetas en una grilla fija de <see cref="ColumnasPorFila"/> columnas:
        /// al llegar a la quinta tarjeta (índice 4) pasa automáticamente a la fila siguiente, y así
        /// con cada grupo de 4, sin importar el ancho disponible del contenedor.</summary>
        private void PosicionarTarjetas(List<Control> tarjetas)
        {
            _panelTarjetas.Controls.Clear();

            int x = 0;
            int y = 0;

            for (int i = 0; i < tarjetas.Count; i++)
            {
                if (i > 0 && i % ColumnasPorFila == 0)
                {
                    x = 0;
                    y += AltoTarjeta + SeparacionTarjeta;
                }

                tarjetas[i].Location = new Point(x, y);
                _panelTarjetas.Controls.Add(tarjetas[i]);
                x += AnchoTarjeta + SeparacionTarjeta;
            }

            int columnas = Math.Min(ColumnasPorFila, tarjetas.Count);
            int filas = (tarjetas.Count + ColumnasPorFila - 1) / ColumnasPorFila;
            _panelTarjetas.Size = new Size(
                columnas * AnchoTarjeta + Math.Max(0, columnas - 1) * SeparacionTarjeta,
                filas * AltoTarjeta + Math.Max(0, filas - 1) * SeparacionTarjeta);
        }

        private void AbrirTurno()
        {
            using var modal = new FModalAperturaTurno(_usuario);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Refrescar();
            }
        }

        private void CerrarTurno(int idTurno)
        {
            using var modal = new FModalCierreTurno(idTurno);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Refrescar();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timerVerificacion != null)
                {
                    _timerVerificacion.Stop();
                    _timerVerificacion.Tick -= TimerVerificacion_Tick;
                    _timerVerificacion.Dispose();
                    _timerVerificacion = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
