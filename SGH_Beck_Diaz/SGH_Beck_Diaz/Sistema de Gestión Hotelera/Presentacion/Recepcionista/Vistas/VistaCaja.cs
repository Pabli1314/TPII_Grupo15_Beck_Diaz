using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.Modales;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>
    /// Gestión de Caja del recepcionista: apertura, resumen y cierre manual del turno.
    ///
    /// Política de cierre manual obligatorio: esta vista no tiene timers ni cierra turnos por su cuenta.
    /// Muestra uno de tres estados (ver <see cref="EstadoCaja"/>):
    ///  - Cerrada: solo "Abrir caja".
    ///  - Abierta (turno de hoy): resumen, "Registrar venta adicional" y "Cerrar caja".
    ///  - Pendiente de cierre (turno de un día anterior sin cerrar): banner de alerta, resumen del turno
    ///    vencido, "Cerrar turno pendiente" como única acción y "Abrir caja" inhabilitado.
    /// </summary>
    internal class VistaCaja : UserControl
    {
        // Grilla fija de tarjetas: siempre 4 por fila, sin depender del ancho del contenedor
        // (a diferencia de un FlowLayoutPanel, que solo envuelve cuando el ancho real se lo permite).
        private const int ColumnasPorFila = 4;
        private const int AnchoTarjeta = 280;
        private const int AltoTarjeta = 182;
        private const int SeparacionTarjeta = 8;
        private const int AnchoContenido = ColumnasPorFila * AnchoTarjeta + (ColumnasPorFila - 1) * SeparacionTarjeta;

        private const int TopContenido = 60;
        private const int AltoBanner = 78;
        private const int MargenBloques = 20;

        private static readonly Color TextoAdvertencia = Color.FromArgb(133, 77, 14);
        private static readonly Color BordeAdvertencia = Color.FromArgb(234, 179, 8);

        private readonly Usuario _usuario;
        private readonly Action _navegarAVentas;
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();

        private readonly Label _lblSubtitulo;
        private readonly Panel _panelAlertaPendiente;
        private readonly Label _lblAlertaTitulo;
        private readonly Label _lblAlertaDetalle;
        private readonly Panel _panelTarjetas;
        private readonly FlowLayoutPanel _panelAcciones;
        private readonly Panel _panelSinTurno;

        public VistaCaja(Usuario usuario, Action navegarAVentas)
        {
            _usuario = usuario;
            _navegarAVentas = navegarAVentas;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Estado del turno", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _lblSubtitulo = new Label { Text = string.Empty, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 26) };

            // ── Banner de turno vencido ──
            _panelAlertaPendiente = new Panel
            {
                Location = new Point(0, TopContenido),
                Size = new Size(AnchoContenido, AltoBanner),
                BackColor = Paleta.AdvertenciaSuave,
                Visible = false
            };
            _panelAlertaPendiente.Paint += DibujarBordeAlerta;

            _lblAlertaTitulo = new Label
            {
                Text = "⚠  Turno pendiente de cierre",
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = TextoAdvertencia,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            _lblAlertaDetalle = new Label
            {
                Text = string.Empty,
                Font = Paleta.FuenteBase,
                ForeColor = TextoAdvertencia,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(18, 38),
                Size = new Size(AnchoContenido - 36, 36)
            };
            _panelAlertaPendiente.Controls.Add(_lblAlertaTitulo);
            _panelAlertaPendiente.Controls.Add(_lblAlertaDetalle);

            _panelTarjetas = new Panel { Location = new Point(0, TopContenido + 5) };

            _panelAcciones = new FlowLayoutPanel
            {
                Location = new Point(0, 260), // Se reposiciona dinámicamente en Refrescar()
                Size = new Size(1400, 60),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            // ── Caja cerrada ──
            _panelSinTurno = new Panel { Location = new Point(0, TopContenido), Size = new Size(700, 160), Visible = false };
            var lblSinTurno = new Label { Text = "No hay un turno de caja abierto.", Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, AutoSize = true, Location = new Point(0, 0) };
            var lblIndicacion = new Label
            {
                Text = "Abra la caja ingresando el monto inicial para operar en la jornada de hoy.",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Location = new Point(0, 26)
            };
            var btnAbrirGrande = EstiloBoton.Primario(new Button { Text = "Abrir caja", Size = new Size(200, 44), Location = new Point(0, 75) });
            btnAbrirGrande.Click += (s, e) => AbrirTurno();
            _panelSinTurno.Controls.Add(lblSinTurno);
            _panelSinTurno.Controls.Add(lblIndicacion);
            _panelSinTurno.Controls.Add(btnAbrirGrande);

            Controls.Add(lblTitulo);
            Controls.Add(_lblSubtitulo);
            Controls.Add(_panelAlertaPendiente);
            Controls.Add(_panelTarjetas);
            Controls.Add(_panelAcciones);
            Controls.Add(_panelSinTurno);

            Refrescar();
        }

        /// <summary>Consulta el estado de la caja y redibuja la vista. No cierra ni abre turnos.</summary>
        public void Refrescar()
        {
            EstadoTurnoCaja estado = _gestionTurnoCaja.ConsultarEstadoCaja(_usuario.DniUsuario);

            switch (estado.Estado)
            {
                case EstadoCaja.Abierta:
                    MostrarTurnoAbierto(estado.Turno!);
                    break;
                case EstadoCaja.PendienteCierre:
                    MostrarTurnoPendiente(estado.Turno!);
                    break;
                default:
                    MostrarCajaCerrada();
                    break;
            }
        }

        private void MostrarCajaCerrada()
        {
            _lblSubtitulo.Text = "Caja cerrada";
            _panelAlertaPendiente.Visible = false;
            _panelTarjetas.Visible = false;
            _panelAcciones.Visible = false;
            _panelSinTurno.Visible = true;
        }

        private void MostrarTurnoAbierto(TurnoCaja turno)
        {
            _lblSubtitulo.Text = $"Turno #{turno.IdTurno} · Abierto {turno.FechaApertura:dd/MM/yyyy} {turno.HoraApertura:hh\\:mm}";
            _panelAlertaPendiente.Visible = false;
            _panelSinTurno.Visible = false;

            MostrarResumen(turno, TopContenido + 5);

            var btnVenta = EstiloBoton.Secundario(new Button { Text = "Registrar venta adicional", Size = new Size(220, 40), Margin = new Padding(0, 0, 12, 0) });
            btnVenta.Click += (s, e) => _navegarAVentas();

            var btnCerrar = EstiloBoton.Primario(new Button { Text = "Cerrar caja", Size = new Size(160, 40) });
            btnCerrar.Click += (s, e) => CerrarTurno(turno.IdTurno);

            CargarAcciones(btnVenta, btnCerrar);
        }

        /// <summary>
        /// Turno vencido: alerta prominente, resumen del turno a arquear y, como única acción operativa,
        /// "Cerrar turno pendiente". "Abrir caja" se muestra inhabilitado hasta que se liquide el turno.
        /// </summary>
        private void MostrarTurnoPendiente(TurnoCaja turno)
        {
            _lblSubtitulo.Text = $"Pendiente de cierre · Turno #{turno.IdTurno} abierto el {turno.FechaApertura:dd/MM/yyyy} {turno.HoraApertura:hh\\:mm}";
            _panelSinTurno.Visible = false;

            _lblAlertaDetalle.Text =
                $"Existe un turno abierto del día {turno.FechaApertura:dd/MM/yyyy} sin cerrar. " +
                "Debe realizar el cierre y arqueo de dicho turno antes de abrir la jornada actual.";
            _panelAlertaPendiente.Visible = true;

            MostrarResumen(turno, _panelAlertaPendiente.Bottom + MargenBloques);

            var btnCerrarPendiente = EstiloBoton.Primario(new Button { Text = "Cerrar turno pendiente", Size = new Size(220, 40), Margin = new Padding(0, 0, 12, 0) });
            btnCerrarPendiente.Click += (s, e) => CerrarTurno(turno.IdTurno);

            // Visible pero inhabilitado: se habilita solo cuando el turno pendiente queda liquidado.
            var btnAbrirDeshabilitado = EstiloBoton.Secundario(new Button { Text = "Abrir caja", Size = new Size(160, 40), Enabled = false });

            CargarAcciones(btnCerrarPendiente, btnAbrirDeshabilitado);
        }

        private void MostrarResumen(TurnoCaja turno, int top)
        {
            ResumenTurno resumen = _gestionTurnoCaja.ObtenerResumen(turno.IdTurno);

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

            _panelTarjetas.Location = new Point(0, top);
            PosicionarTarjetas(tarjetas);
            _panelTarjetas.Visible = true;
        }

        private void CargarAcciones(params Button[] botones)
        {
            // Reposicionar el panel de acciones debajo de las tarjetas con un margen fijo
            _panelAcciones.Location = new Point(0, _panelTarjetas.Bottom + MargenBloques);
            _panelAcciones.Controls.Clear();
            _panelAcciones.Controls.AddRange(botones);
            _panelAcciones.Visible = true;
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

        private static void DibujarBordeAlerta(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;

            using var pen = new Pen(BordeAdvertencia, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);

            // Franja lateral para reforzar visualmente la alerta
            using var franja = new SolidBrush(BordeAdvertencia);
            e.Graphics.FillRectangle(franja, 0, 0, 5, panel.Height);
        }

        private void AbrirTurno()
        {
            using var modal = new FModalAperturaTurno(_usuario);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Refrescar();
            }
        }

        /// <summary>Cierre manual: el usuario arquea, declara el efectivo y confirma en el modal.
        /// Al volver, la vista pasa a "Caja cerrada" y habilita "Abrir caja".</summary>
        private void CerrarTurno(int idTurno)
        {
            using var modal = new FModalCierreTurno(idTurno);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Refrescar();
            }
        }
    }
}
