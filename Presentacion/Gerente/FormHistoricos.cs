using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Presentacion.Gerente.ComponentesGerencia;

namespace Presentacion.Gerente
{
    /// <summary>
    /// Auditoría de turnos de caja cerrados: lo cobrado en cada turno (recalculado con el mismo cálculo
    /// que usa el cierre de caja), el efectivo que se contó en el arqueo y la diferencia entre ambos,
    /// con totales por recepcionista y por medio de cobro. Solo lectura.
    /// </summary>
    public class FormHistoricos : Form
    {
        private const int FilasVisiblesDetalle = 10;
        private const string TextoEnCurso = "En curso";

        private readonly GestionGerente _gestionGerente = new();
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();
        private readonly List<Control> _anchoCompleto = new();

        private readonly FlowLayoutPanel _raiz;
        private readonly Label _lblPeriodo;
        private readonly Label _lblDetalle;

        // --- Filtros ---
        private readonly Panel _tarjetaFiltros;
        private readonly DateTimePicker _dtpDesde;
        private readonly DateTimePicker _dtpHasta;
        private readonly ComboBox _cmbRecepcionista;
        private readonly Button _btnBuscar;

        // --- KPIs ---
        private readonly FlowLayoutPanel _filaKpis;
        private readonly TarjetaKpi _kpiTurnos;
        private readonly TarjetaKpi _kpiCobrado;
        private readonly TarjetaKpi _kpiPromedio;
        private readonly TarjetaKpi _kpiDiferencias;

        // --- Gráficos ---
        private readonly FlowLayoutPanel _filaGraficos;
        private readonly BarrasHorizontales _barrasRecepcionistas;
        private readonly PanelReporte _panelRecepcionistas;
        private readonly GraficoDona _donaMedios;
        private readonly PanelReporte _panelMedios;
        private int _cantidadRecepcionistas;

        // --- Detalle ---
        private readonly DataGridView _dgvTurnos;
        private readonly PanelReporte _panelTurnos;

        public FormHistoricos()
        {
            BackColor = Paleta.FondoApp;
            Text = "Auditoría e Históricos Operativos";
            _raiz = CrearRaiz();

            // ================= Encabezado =================
            (Panel encabezado, _lblPeriodo, _lblDetalle) = CrearEncabezado();
            _anchoCompleto.Add(encabezado);

            // ================= Filtros =================
            _dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130, Font = Paleta.FuenteBase, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-2) };
            _dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130, Font = Paleta.FuenteBase, Value = DateTime.Today };
            _cmbRecepcionista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Font = Paleta.FuenteBase };
            _btnBuscar = EstiloBoton.Primario(new Button { Text = "Buscar", Size = new Size(120, 34), Margin = new Padding(8, 22, 0, 0) });
            _btnBuscar.Click += (s, e) => Consultar();
            _toolTip.SetToolTip(_btnBuscar, "Muestra los turnos cerrados entre las dos fechas (según el día de cierre) y los que siguen abiertos.");

            var filaFiltros = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, BackColor = Paleta.FondoTarjeta };
            filaFiltros.Controls.Add(Filtro("Desde", _dtpDesde));
            filaFiltros.Controls.Add(Filtro("Hasta", _dtpHasta));
            filaFiltros.Controls.Add(Filtro("Recepcionista", _cmbRecepcionista));
            filaFiltros.Controls.Add(_btnBuscar);

            _tarjetaFiltros = new Panel { Height = 86, BackColor = Paleta.FondoApp, Padding = new Padding(18, 12, 18, 10), Margin = new Padding(0, 0, 0, Separacion) };
            _tarjetaFiltros.Paint += (s, e) => EstiloReporte.DibujarTarjeta(e.Graphics, _tarjetaFiltros.Size, Paleta.Borde);
            _tarjetaFiltros.Controls.Add(filaFiltros);
            _anchoCompleto.Add(_tarjetaFiltros);

            // ================= KPIs =================
            _kpiTurnos = new TarjetaKpi(_toolTip);
            _kpiCobrado = new TarjetaKpi(_toolTip);
            _kpiPromedio = new TarjetaKpi(_toolTip);
            _kpiDiferencias = new TarjetaKpi(_toolTip);
            _filaKpis = CrearFila(_kpiTurnos, _kpiCobrado, _kpiPromedio, _kpiDiferencias);

            // ================= Gráficos =================
            _barrasRecepcionistas = new BarrasHorizontales { ColorBarra = Paleta.Primario, AnchoEtiqueta = 170 };
            _panelRecepcionistas = new PanelReporte(_toolTip, "¿Cuánto cobró cada recepcionista?",
                "Total cobrado (todos los medios) en los turnos de cada recepcionista dentro del período (cerrados y abiertos).",
                _barrasRecepcionistas, new Size(700, 300));

            _donaMedios = new GraficoDona();
            _panelMedios = new PanelReporte(_toolTip, "¿Con qué medios se cobró?",
                "Reparto de lo cobrado en los turnos del período entre efectivo, tarjeta y transferencia.",
                _donaMedios, new Size(500, 300)) { TextoPie = "Solo el efectivo se controla en el arqueo de caja." };

            _filaGraficos = CrearFila(_panelRecepcionistas, _panelMedios);

            // ================= Detalle =================
            _dgvTurnos = CrearGrilla();
            _dgvTurnos.ShowCellToolTips = true;
            _dgvTurnos.Columns.Add("turno", "N°");
            _dgvTurnos.Columns.Add("usuario", "Recepcionista");
            _dgvTurnos.Columns.Add("apertura", "Apertura");
            _dgvTurnos.Columns.Add("cierre", "Cierre");
            _dgvTurnos.Columns.Add("cobrado", "Cobrado");
            _dgvTurnos.Columns.Add("esperado", "Esperado");
            _dgvTurnos.Columns.Add("contado", "Contado");
            _dgvTurnos.Columns.Add("diferencia", "Diferencia");
            _dgvTurnos.Columns.Add("observaciones", "Observaciones");
            PesosColumnas(_dgvTurnos, ("turno", 56), ("usuario", 118), ("apertura", 140), ("cierre", 140),
                ("cobrado", 96), ("esperado", 96), ("contado", 96), ("diferencia", 102), ("observaciones", 120));
            AlinearDerecha(_dgvTurnos, "cobrado", "esperado", "contado", "diferencia");
            _dgvTurnos.CellFormatting += (s, e) => ColorearVariacion(_dgvTurnos, e, "diferencia");
            _dgvTurnos.CellFormatting += FormatearTurnoAbierto;

            _panelTurnos = new PanelReporte(_toolTip, "Detalle de turnos",
                "Cobrado: check-ins (tarifa × noches) y ventas del turno, en todos los medios. Efectivo esperado: monto inicial + cobros en efectivo. Efectivo contado: lo que se registró en el arqueo al cerrar. Diferencia: contado − esperado (rojo = faltante, verde = sobrante). Pasá el mouse sobre una fila para ver el desglose.",
                _dgvTurnos, new Size(1200, 400));
            _anchoCompleto.Add(_panelTurnos);

            Panel tituloKpis = TituloSeccion("Resumen del período", "Cuántos turnos se cerraron, cuánto se cobró y si la caja cerró bien.");
            Panel tituloGraficos = TituloSeccion("Por recepcionista y medio de cobro", "Quién cobró y cómo se cobró.");
            Panel tituloDetalle = TituloSeccion("Auditoría de turnos", "Cada turno cerrado con su arqueo de caja.");
            _anchoCompleto.AddRange(new Control[] { tituloKpis, tituloGraficos, tituloDetalle });

            _raiz.Controls.AddRange(new Control[]
            {
                encabezado, _tarjetaFiltros,
                tituloKpis, _filaKpis,
                tituloGraficos, _filaGraficos,
                tituloDetalle, _panelTurnos
            });
            Controls.Add(_raiz);

            _raiz.Resize += (s, e) => Acomodar();
            Load += (s, e) =>
            {
                CargarRecepcionistas();
                Consultar();
            };
        }

        // =====================================================================
        // Datos
        // =====================================================================

        private void CargarRecepcionistas()
        {
            var opciones = new List<KeyValuePair<string, string>> { new(string.Empty, "Todos") };
            try
            {
                opciones.AddRange(_gestionGerente.ObtenerListaUsuarios());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar la lista de recepcionistas: {ex.Message}", "Históricos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _cmbRecepcionista.DataSource = opciones;
            _cmbRecepcionista.DisplayMember = "Value";
            _cmbRecepcionista.ValueMember = "Key";
            _cmbRecepcionista.SelectedIndex = 0;
        }

        private void Consultar()
        {
            if (_dtpDesde.Value.Date > _dtpHasta.Value.Date)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.", "Históricos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _dtpDesde.Focus();
                return;
            }

            string? dni = _cmbRecepcionista.SelectedValue as string;
            string nombreFiltro = _cmbRecepcionista.SelectedIndex > 0 ? _cmbRecepcionista.Text : "Todos";

            try
            {
                Cursor = Cursors.WaitCursor;
                HistorialOperativoResultado r = _gestionGerente.ConsultarHistorialOperativo(_dtpDesde.Value.Date, _dtpHasta.Value.Date, dni);

                _lblPeriodo.Text = $"Turnos de caja del {r.Desde:dd/MM/yyyy} al {r.Hasta:dd/MM/yyyy}";
                _lblDetalle.Text = $"Recepcionista: {nombreFiltro} · Cerrados según su fecha de cierre, más los que siguen abiertos";

                CargarKpis(r);
                CargarGraficos(r);
                CargarDetalle(r, _cmbRecepcionista.SelectedIndex > 0 ? _cmbRecepcionista.Text : string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudieron consultar los históricos: {ex.Message}", "Históricos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            Acomodar();
        }

        private void CargarKpis(HistorialOperativoResultado r)
        {
            int checkIns = r.Lista.Sum(t => t.CheckIns);
            int ventas = r.Lista.Sum(t => t.Ventas);

            string abiertos = r.TurnosAbiertos == 0 ? string.Empty : $" · {r.TurnosAbiertos} {(r.TurnosAbiertos == 1 ? "abierto" : "abiertos")} en curso";
            _kpiTurnos.Configurar("Turnos cerrados", r.TotalTurnosCerrados.ToString(CulturaAr),
                "Turnos de caja cuyo cierre cae dentro del período elegido. Los turnos que siguen abiertos se cuentan aparte: todavía no tienen arqueo.",
                Icono.Reloj, Paleta.Primario, null,
                (r.TotalTurnosCerrados == 0
                    ? "No hubo cierres de caja en el período"
                    : $"Duración promedio {Duracion(r.DuracionPromedio)}") + $"{abiertos} · {checkIns} check-ins · {ventas} ventas");

            _kpiCobrado.Configurar("Total cobrado", Moneda(r.TotalCobrado),
                "Todo lo cobrado en los turnos del período: check-ins (tarifa × noches) y ventas, en efectivo, tarjeta y transferencia.",
                Icono.Caja, Color.FromArgb(13, 148, 136), null,
                $"Efectivo {Moneda(r.CobradoEfectivo)} · Tarjeta {Moneda(r.CobradoTarjeta)} · Transferencia {Moneda(r.CobradoTransferencia)}");

            _kpiPromedio.Configurar("Promedio cobrado por turno", Moneda(r.PromedioCobradoTurno),
                "Total cobrado dividido por la cantidad de turnos del período (cerrados y abiertos).",
                Icono.Grafico, Color.FromArgb(124, 58, 237), null,
                $"Efectivo arqueado en el período: {Moneda(r.EfectivoArqueado)} (contado al cierre − monto inicial)");

            bool hayDiferencias = r.TurnosConDiferencia > 0;
            Comparativa? estado = r.TotalTurnosCerrados == 0
                ? null
                : hayDiferencias
                    ? new Comparativa($"{r.TurnosConDiferencia} de {r.TotalTurnosCerrados}", "turnos no cerraron exacto", TonoEtiqueta.Peligro)
                    : new Comparativa("✓ Sin diferencias", "todos los arqueos cerraron exacto", TonoEtiqueta.Exito);
            _kpiDiferencias.Configurar("Diferencias de caja", hayDiferencias ? Moneda(r.Sobrante - r.Faltante) : Moneda(0),
                "Suma neta de las diferencias entre el efectivo contado y el esperado. Negativo: faltó dinero. Positivo: sobró.",
                Icono.Alerta, hayDiferencias ? Paleta.Peligro : Paleta.Exito, estado,
                hayDiferencias ? $"Faltantes {Moneda(r.Faltante)} · Sobrantes {Moneda(r.Sobrante)}" : "El efectivo contado coincidió con el esperado.",
                alerta: hayDiferencias);
        }

        private void CargarGraficos(HistorialOperativoResultado r)
        {
            _cantidadRecepcionistas = r.PorRecepcionista.Count;
            _barrasRecepcionistas.Cargar(
                r.PorRecepcionista.Select(p => (p.Nombre, (double)p.Cobrado, $"{Moneda(p.Cobrado)} · {p.Turnos} {(p.Turnos == 1 ? "turno" : "turnos")}")),
                "No hay turnos de caja en el período.");

            List<ResumenRecepcionista> conDiferencia = r.PorRecepcionista.Where(p => p.TurnosConDiferencia > 0).ToList();
            _panelRecepcionistas.TextoPie = conDiferencia.Count == 0
                ? "Ordenado de mayor a menor. Ningún recepcionista tuvo diferencias de caja en el período."
                : "Con diferencias de caja: " + string.Join(" · ", conDiferencia.Select(p => $"{p.Nombre} ({p.TurnosConDiferencia}, neto {Moneda(p.Diferencia)})"));

            decimal total = r.CobradoEfectivo + r.CobradoTarjeta + r.CobradoTransferencia;
            double Pct(decimal v) => total > 0 ? (double)(v * 100 / total) : 0;
            var segmentos = new List<SegmentoDistribucion>
            {
                new() { Etiqueta = "Efectivo", Valor = (double)r.CobradoEfectivo, Porcentaje = Pct(r.CobradoEfectivo) },
                new() { Etiqueta = "Tarjeta", Valor = (double)r.CobradoTarjeta, Porcentaje = Pct(r.CobradoTarjeta) },
                new() { Etiqueta = "Transferencia", Valor = (double)r.CobradoTransferencia, Porcentaje = Pct(r.CobradoTransferencia) }
            };
            _donaMedios.Cargar(segmentos, ColoresReporte.Categoricos, v => Moneda((decimal)v), "No hay cobros en el período.");
        }

        private void CargarDetalle(HistorialOperativoResultado r, string nombreRecepcionista)
        {
            _dgvTurnos.Rows.Clear();
            foreach (TurnoHistorialItem t in r.Lista)
            {
                string diferencia = t.Abierto ? "Pendiente"
                    : !t.Diferencia.HasValue ? "Sin arqueo"
                    : !t.TieneDiferencia ? "= $ 0"
                    : (t.Diferencia.Value > 0 ? "+" : string.Empty) + Moneda(t.Diferencia.Value);

                int i = _dgvTurnos.Rows.Add(
                    t.IdTurno.ToString(CulturaAr), t.Usuario,
                    t.Apertura.ToString("dd/MM/yy HH:mm"), t.Cierre?.ToString("dd/MM/yy HH:mm") ?? TextoEnCurso,
                    Moneda(t.CobradoTotal), Moneda(t.EfectivoEsperado),
                    t.EfectivoContado.HasValue ? Moneda(t.EfectivoContado.Value) : "—",
                    diferencia, t.Observaciones);

                DataGridViewRow fila = _dgvTurnos.Rows[i];
                fila.Cells["diferencia"].Tag = t.TieneDiferencia ? Math.Sign(t.Diferencia!.Value) : 0;

                string desglose = $"Turno N° {t.IdTurno} · {t.Usuario} · {(t.Abierto ? $"abierto hace {Duracion(t.Duracion)}, todavía sin arqueo" : $"duró {Duracion(t.Duracion)}")}\n" +
                                  $"Monto inicial: {Moneda(t.MontoInicial)}\n" +
                                  $"Check-ins: {t.CheckIns} ({Moneda(t.CobradoAlojamiento)}) · Ventas: {t.Ventas} ({Moneda(t.CobradoVentas)})\n" +
                                  $"Efectivo {Moneda(t.CobradoEfectivo)} · Tarjeta {Moneda(t.CobradoTarjeta)} · Transferencia {Moneda(t.CobradoTransferencia)}";
                foreach (DataGridViewCell celda in fila.Cells) celda.ToolTipText = desglose;
                if (t.Observaciones.Length > 0) fila.Cells["observaciones"].ToolTipText = t.Observaciones;
            }
            _dgvTurnos.ClearSelection();

            string abiertos = r.TurnosAbiertos > 0 ? $"Arriba, {r.TurnosAbiertos} {(r.TurnosAbiertos == 1 ? "turno abierto" : "turnos abiertos")} (todavía sin arqueo); después, los cerrados del más reciente al más antiguo. " : "Del cierre más reciente al más antiguo. ";
            _panelTurnos.TextoPie = r.Lista.Count == 0
                ? $"{(string.IsNullOrEmpty(nombreRecepcionista) ? "No hay turnos de caja" : $"{nombreRecepcionista} no tiene turnos de caja")} en el período. Probá ampliar las fechas o elegir «Todos»."
                : abiertos + "Lo cobrado se recalcula con las tarifas actuales; si un huésped se fue antes de lo planificado, cuentan las noches que efectivamente se quedó.";
        }

        /// <summary>Resalta los turnos que siguen abiertos.</summary>
        private void FormatearTurnoAbierto(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || _dgvTurnos.Columns[e.ColumnIndex].Name != "cierre" || e.Value as string != TextoEnCurso) return;
            e.CellStyle.ForeColor = Paleta.Primario;
            e.CellStyle.Font = Paleta.FuenteBaseNegrita;
        }

        // =====================================================================
        // Layout responsivo
        // =====================================================================

        private void Acomodar()
        {
            if (_raiz == null) return;

            int ancho = AnchoUtil(_raiz);
            _raiz.SuspendLayout();

            foreach (Control c in _anchoCompleto) c.Width = ancho;

            DistribuirEnFila(_filaKpis, ancho, ancho >= 1100 ? 4 : 2, 158);

            // 34 encabezado + 48 pie + 26 márgenes del PanelReporte + 40 px por barra
            int altoGraficos = Math.Max(300, 34 + 48 + 26 + Math.Max(1, _cantidadRecepcionistas) * 40 + 8);
            if (ancho >= 1000)
            {
                int anchoDona = (ancho - Separacion) * 40 / 100;
                _panelRecepcionistas.Size = new Size(ancho - anchoDona - Separacion, altoGraficos);
                _panelRecepcionistas.Margin = new Padding(0, 0, Separacion, Separacion);
                _panelMedios.Size = new Size(anchoDona, altoGraficos);
                _panelMedios.Margin = new Padding(0, 0, 0, Separacion);
                _filaGraficos.Size = new Size(ancho, altoGraficos + Separacion);
            }
            else
            {
                _panelRecepcionistas.Size = new Size(ancho, altoGraficos);
                _panelRecepcionistas.Margin = new Padding(0, 0, 0, Separacion);
                _panelMedios.Size = new Size(ancho, 320);
                _panelMedios.Margin = new Padding(0, 0, 0, Separacion);
                _filaGraficos.Size = new Size(ancho, altoGraficos + 320 + Separacion * 2);
            }

            _panelTurnos.Height = AltoTabla(_dgvTurnos, FilasVisiblesDetalle);

            _raiz.ResumeLayout(true);
        }

        // =====================================================================
        // Utilidades
        // =====================================================================

        private static Control Filtro(string titulo, Control control)
        {
            var panel = new Panel { Size = new Size(control.Width + 16, 60), BackColor = Paleta.FondoTarjeta, Margin = new Padding(0, 0, 8, 0) };
            panel.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, Location = new Point(0, 2) });
            control.Location = new Point(0, 24);
            panel.Controls.Add(control);
            return panel;
        }

        /// <summary>"8 h 05 min" o, para turnos de más de un día, "3 d 4 h".</summary>
        private static string Duracion(TimeSpan d) => d.TotalDays >= 1
            ? $"{(int)d.TotalDays} d {d.Hours} h"
            : $"{d.Hours} h {d.Minutes:00} min";

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
