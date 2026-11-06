using Entidades;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WinForms;
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
    /// Panel Financiero y Flujo de Caja del mes en curso: ingresos reales acumulados contra la
    /// proyección al cierre y el mes anterior, desglose por línea de negocio y por medio de pago, y la
    /// situación de las cajas (turnos abiertos y arqueos del mes). Solo lectura.
    /// </summary>
    public class FormAnalisisFinanciero : Form
    {
        private static readonly Color ColorMesAnterior = Color.FromArgb(148, 158, 176);

        private readonly GestionGerente _gestionGerente = new();
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();
        private readonly List<Control> _anchoCompleto = new();

        private readonly FlowLayoutPanel _raiz;
        private readonly Label _lblPeriodo;
        private readonly Label _lblComparacion;

        private readonly FlowLayoutPanel _filaKpis;
        private readonly TarjetaKpi _kpiIngresos;
        private readonly TarjetaKpi _kpiProyeccion;
        private readonly TarjetaKpi _kpiComprometido;
        private readonly TarjetaKpi _kpiCaja;

        private readonly CartesianChart _chartFlujo;
        private readonly PanelReporte _panelFlujo;

        private readonly FlowLayoutPanel _filaLineas;
        private readonly GraficoDona _donaLineas;
        private readonly PanelReporte _panelDona;
        private readonly DataGridView _dgvLineas;
        private readonly PanelReporte _panelLineas;

        private readonly FlowLayoutPanel _filaCaja;
        private readonly BarrasHorizontales _barrasMedios;
        private readonly PanelReporte _panelMedios;
        private readonly DataGridView _dgvCajas;
        private readonly PanelReporte _panelCajas;
        private int _cantidadMedios;

        public FormAnalisisFinanciero()
        {
            BackColor = Paleta.FondoApp;
            Text = "Panel Financiero y Flujo de Caja";
            _raiz = CrearRaiz();

            // ================= Encabezado =================
            (Panel encabezado, _lblPeriodo, _lblComparacion) = CrearEncabezado();
            var nota = new Label
            {
                AutoSize = false,
                Height = 22,
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                Margin = new Padding(0, 0, 0, 6),
                Text = "El sistema registra ingresos (alojamiento y ventas) y la caja. No registra egresos, cuentas por pagar ni eventos, por eso este panel no calcula GOP ni utilidad."
            };
            _anchoCompleto.Add(encabezado);
            _anchoCompleto.Add(nota);

            // ================= KPIs =================
            _kpiIngresos = new TarjetaKpi(_toolTip);
            _kpiProyeccion = new TarjetaKpi(_toolTip);
            _kpiComprometido = new TarjetaKpi(_toolTip);
            _kpiCaja = new TarjetaKpi(_toolTip);
            _filaKpis = CrearFila(_kpiIngresos, _kpiProyeccion, _kpiComprometido, _kpiCaja);

            // ================= Flujo =================
            _chartFlujo = new CartesianChart { LegendPosition = LegendPosition.Bottom, BackColor = Paleta.FondoTarjeta };
            _panelFlujo = new PanelReporte(_toolTip, "Ingresos acumulados: real vs. proyección",
                "Línea azul: lo ingresado día a día, acumulado. Línea punteada: proyección hasta fin de mes (noches ya reservadas por huéspedes alojados + ventas al ritmo promedio del mes). Línea gris: el mes anterior completo, para comparar el ritmo.",
                _chartFlujo, new Size(1200, 380)) { TextoPie = "La proyección es conservadora: no incluye check-ins que todavía no ocurrieron." };
            _anchoCompleto.Add(_panelFlujo);

            // ================= Líneas de negocio =================
            _donaLineas = new GraficoDona();
            _panelDona = new PanelReporte(_toolTip, "¿De dónde vienen los ingresos?",
                "Participación de cada línea de negocio en los ingresos del mes: alojamiento y cada categoría de productos vendidos (minibar, snacks, servicios…), según las categorías cargadas en Inventario.",
                _donaLineas, new Size(500, 340)) { TextoPie = "Mes en curso, hasta hoy." };

            _dgvLineas = CrearGrilla();
            _dgvLineas.Columns.Add("linea", "Línea de negocio");
            _dgvLineas.Columns.Add("actual", "Actual");
            _dgvLineas.Columns.Add("participacion", "% total");
            _dgvLineas.Columns.Add("anterior", "Mes ant.");
            _dgvLineas.Columns.Add("variacion", "Variación");
            PesosColumnas(_dgvLineas, ("linea", 180));
            AlinearDerecha(_dgvLineas, "actual", "participacion", "anterior", "variacion");
            _dgvLineas.CellFormatting += (s, e) => ColorearVariacion(_dgvLineas, e, "variacion");
            _panelLineas = new PanelReporte(_toolTip, "Detalle por línea de negocio",
                "Ingresos de cada línea en el mes en curso frente a los mismos días del mes anterior.",
                _dgvLineas, new Size(700, 340)) { TextoPie = "Alojamiento: noches ocupadas × tarifa del tipo de habitación. Productos: ventas registradas." };

            _filaLineas = CrearFila(_panelDona, _panelLineas);

            // ================= Caja y cobros =================
            _barrasMedios = new BarrasHorizontales { ColorBarra = Paleta.Primario, AnchoEtiqueta = 130 };
            _panelMedios = new PanelReporte(_toolTip, "Ingresos por medio de pago",
                "Con qué medio se cobraron los ingresos del mes: la forma de pago registrada en el check-in de cada estadía y en cada venta.",
                _barrasMedios, new Size(500, 300)) { TextoPie = "Mes en curso, hasta hoy." };

            _dgvCajas = CrearGrilla();
            _dgvCajas.Columns.Add("turno", "Turno");
            _dgvCajas.Columns.Add("usuario", "Recepcionista");
            _dgvCajas.Columns.Add("apertura", "Apertura");
            _dgvCajas.Columns.Add("inicial", "Inicial");
            _dgvCajas.Columns.Add("cobrado", "Cobrado");
            _dgvCajas.Columns.Add("esperado", "Efectivo esp.");
            PesosColumnas(_dgvCajas, ("turno", 70), ("usuario", 130), ("apertura", 115), ("inicial", 80), ("cobrado", 100), ("esperado", 120));
            AlinearDerecha(_dgvCajas, "inicial", "cobrado", "esperado");
            _panelCajas = new PanelReporte(_toolTip, "Cajas abiertas ahora",
                "Turnos de caja que siguen abiertos. Cobrado: todo lo registrado en el turno (efectivo, tarjeta y transferencia). Efectivo esperado: monto inicial + cobros en efectivo; es lo que debería haber en el cajón.",
                _dgvCajas, new Size(700, 300));

            _filaCaja = CrearFila(_panelMedios, _panelCajas);

            Panel tituloKpis = TituloSeccion("Resumen del mes", "Lo ingresado, lo que se proyecta al cierre y el efectivo en caja.");
            Panel tituloFlujo = TituloSeccion("Flujo de ingresos", "Evolución diaria del mes comparada con la proyección y con el mes anterior.");
            Panel tituloLineas = TituloSeccion("Líneas de negocio", "Desglose de los ingresos por alojamiento y por cada categoría de productos.");
            Panel tituloCaja = TituloSeccion("Caja y cobros", "Medios de pago y turnos de caja abiertos.");
            _anchoCompleto.AddRange(new Control[] { tituloKpis, tituloFlujo, tituloLineas, tituloCaja });

            _raiz.Controls.AddRange(new Control[]
            {
                encabezado, nota,
                tituloKpis, _filaKpis,
                tituloFlujo, _panelFlujo,
                tituloLineas, _filaLineas,
                tituloCaja, _filaCaja
            });
            Controls.Add(_raiz);

            _raiz.Resize += (s, e) => Acomodar();
            Load += (s, e) => CargarDatos();
        }

        // =====================================================================
        // Datos
        // =====================================================================

        private void CargarDatos()
        {
            try
            {
                PanelFinanciero p = _gestionGerente.ObtenerPanelFinanciero();
                MetricasPeriodo act = p.Actual;
                string mesAnterior = NombreMes(p.MesAnteriorMismosDias.Desde);

                _lblPeriodo.Text = $"Mes en curso: {p.Desde:dd/MM} al {p.Hasta:dd/MM/yyyy}";
                _lblComparacion.Text = $"Cierre del mes: {p.FinMes:dd/MM} · Comparado con los mismos días de {mesAnterior}";

                _kpiIngresos.Configurar("Ingresos a la fecha", Moneda(act.IngresosTotales),
                    "Alojamiento devengado (noches ocupadas × tarifa) más ventas de productos y servicios, del día 1 a hoy.",
                    Icono.Caja, Paleta.Primario,
                    Comparativa.Porcentual((double)act.IngresosTotales, (double)p.MesAnteriorMismosDias.IngresosTotales, "el mes pasado"),
                    $"Alojamiento {Moneda(act.IngresosAlojamiento)} · Ventas {Moneda(act.IngresosVentas)}");

                decimal ventasEstimadas = p.VentasPromedioDiario * (p.FinMes - p.Hasta).Days;
                _kpiProyeccion.Configurar("Proyección al cierre", Moneda(p.ProyeccionCierre),
                    "Ingresos a la fecha + noches ya reservadas por los huéspedes alojados + ventas de los días que faltan al promedio diario del mes.",
                    Icono.Grafico, Color.FromArgb(124, 58, 237),
                    Comparativa.Porcentual((double)p.ProyeccionCierre, (double)p.MesAnteriorCompleto, $"todo {mesAnterior}"),
                    $"A la fecha {Moneda(act.IngresosTotales)} + comprometido {Moneda(p.Comprometido)} + ventas estimadas {Moneda(ventasEstimadas)}");

                _kpiComprometido.Configurar("Comprometido por estadías en curso", Moneda(p.Comprometido),
                    "Noches que los huéspedes alojados hoy ya tienen reservadas hasta su salida planificada, dentro de este mes. Es ingreso que todavía no se devengó pero ya está asegurado por la reserva.",
                    Icono.Cama, Color.FromArgb(13, 148, 136), null,
                    $"Noches reservadas desde mañana hasta el {p.FinMes:dd/MM} por huéspedes ya alojados.");

                int abiertas = p.CajasAbiertas.Count;
                Comparativa? alertaCaja = p.TurnosConDiferencia > 0
                    ? new Comparativa($"{p.TurnosConDiferencia} con diferencia", "turnos cerrados este mes", TonoEtiqueta.Peligro)
                    : null;
                _kpiCaja.Configurar("Efectivo en cajas abiertas", Moneda(p.CajasAbiertas.Sum(c => c.EfectivoEsperado)),
                    "Efectivo que debería haber ahora en las cajas abiertas (monto inicial + cobros en efectivo). Debajo: turnos cerrados del mes y el efectivo que se contó en sus arqueos.",
                    Icono.Candado, Color.FromArgb(217, 119, 6), alertaCaja,
                    $"{abiertas} {(abiertas == 1 ? "caja abierta" : "cajas abiertas")} · {p.TurnosCerradosMes} turnos cerrados en el mes ({Moneda(p.EfectivoArqueadoMes)} arqueado)",
                    alerta: p.TurnosConDiferencia > 0);

                CargarFlujo(p);
                CargarLineas(p);
                CargarCaja(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar el panel financiero: {ex.Message}", "Panel Financiero", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Acomodar();
        }

        private void CargarFlujo(PanelFinanciero p)
        {
            SolidColorPaint Trazo(Color c, bool punteado = false) => new(Skia(c))
            {
                StrokeThickness = 3,
                PathEffect = punteado ? new DashEffect(new float[] { 8, 6 }) : null
            };

            LineSeries<double?> Serie(string nombre, IEnumerable<decimal?> valores, Color color, bool punteado = false) => new()
            {
                Name = nombre,
                Values = valores.Select(v => v.HasValue ? (double?)v.Value : null).ToArray(),
                Fill = null,
                Stroke = Trazo(color, punteado),
                GeometrySize = 0,
                LineSmoothness = 0,
                YToolTipLabelFormatter = pt => Moneda((decimal)pt.Coordinate.PrimaryValue)
            };

            _chartFlujo.Series = new ISeries[]
            {
                Serie($"Mes anterior ({NombreMes(p.MesAnteriorMismosDias.Desde)})", p.Flujo.Select(f => f.MesAnterior), ColorMesAnterior),
                Serie("Proyección", p.Flujo.Select(f => f.Proyectado), Paleta.Primario, punteado: true),
                Serie("Real", p.Flujo.Select(f => f.Real), Paleta.Primario)
            };
            _chartFlujo.XAxes = new[] { EjeCategorias(p.Flujo.Select(f => f.Dia.ToString())) };
            _chartFlujo.YAxes = new[] { EjeValores(MonedaCorta) };

            _panelFlujo.TextoPie = $"Real a hoy: {Moneda(p.Actual.IngresosTotales)} · Proyección al {p.FinMes:dd/MM}: {Moneda(p.ProyeccionCierre)} · Mes anterior completo: {Moneda(p.MesAnteriorCompleto)}. La proyección no incluye check-ins futuros.";
        }

        private void CargarLineas(PanelFinanciero p)
        {
            decimal total = p.Lineas.Sum(l => l.Actual);
            List<LineaNegocio> conIngresos = p.Lineas.Where(l => l.Actual > 0).ToList();

            List<SegmentoDistribucion> segmentos = conIngresos
                .Select(l => new SegmentoDistribucion
                {
                    Etiqueta = l.Nombre,
                    Valor = (double)l.Actual,
                    Porcentaje = total > 0 ? (double)(l.Actual * 100 / total) : 0
                })
                .ToList();
            _donaLineas.Cargar(segmentos, ColoresReporte.Categoricos, v => Moneda((decimal)v), "Todavía no hay ingresos registrados este mes.");

            _dgvLineas.Rows.Clear();
            foreach (LineaNegocio linea in p.Lineas)
            {
                var variacion = Variacion((double)linea.Actual, (double)linea.Anterior);
                double participacion = total > 0 ? (double)(linea.Actual * 100 / total) : 0;
                int i = _dgvLineas.Rows.Add(linea.Nombre, Moneda(linea.Actual), Porcentaje(participacion), Moneda(linea.Anterior), variacion.Texto);
                _dgvLineas.Rows[i].Cells["variacion"].Tag = variacion.Signo;
            }

            var totalAnterior = p.Lineas.Sum(l => l.Anterior);
            var variacionTotal = Variacion((double)total, (double)totalAnterior);
            int t = _dgvLineas.Rows.Add("Total", Moneda(total), total > 0 ? "100%" : "—", Moneda(totalAnterior), variacionTotal.Texto);
            _dgvLineas.Rows[t].Cells["variacion"].Tag = variacionTotal.Signo;
            _dgvLineas.Rows[t].DefaultCellStyle.Font = Paleta.FuenteBaseNegrita;
            _dgvLineas.ClearSelection();
        }

        private void CargarCaja(PanelFinanciero p)
        {
            _cantidadMedios = p.MediosDePago.Count;
            _barrasMedios.Cargar(
                p.MediosDePago.Select(m => (m.Medio, (double)m.Monto, Moneda(m.Monto))),
                "Todavía no hay cobros registrados este mes.");

            _dgvCajas.Rows.Clear();
            foreach (CajaAbierta caja in p.CajasAbiertas)
            {
                _dgvCajas.Rows.Add($"N° {caja.IdTurno}", caja.Usuario, caja.Apertura.ToString("dd/MM HH:mm"),
                    Moneda(caja.MontoInicial), Moneda(caja.Cobrado), Moneda(caja.EfectivoEsperado));
            }
            _dgvCajas.ClearSelection();

            string arqueos = $"En el mes: {p.TurnosCerradosMes} turnos cerrados, {Moneda(p.EfectivoArqueadoMes)} de efectivo arqueado";
            arqueos += p.TurnosConDiferencia > 0
                ? $", {p.TurnosConDiferencia} con diferencia de caja (ver Históricos)."
                : ", sin diferencias de caja.";
            _panelCajas.TextoPie = p.CajasAbiertas.Count == 0 ? "No hay cajas abiertas en este momento. " + arqueos : arqueos;
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

            int altoLineas = Math.Max(320, AltoTabla(_dgvLineas, 7));
            DistribuirEnDos(_filaLineas, _panelDona, _panelLineas, ancho, 40, altoLineas, altoLineas);

            int altoCajas = Math.Max(300, AltoTabla(_dgvCajas, 5));
            // 34 encabezado + 48 pie + 26 márgenes del PanelReporte + 40 px por barra
            int altoMedios = Math.Max(240, 34 + 48 + 26 + Math.Max(1, _cantidadMedios) * 40 + 8);
            DistribuirEnDos(_filaCaja, _panelMedios, _panelCajas, ancho, 33, Math.Max(altoCajas, altoMedios), altoCajas);

            _raiz.ResumeLayout(true);
        }

        /// <summary>Dos paneles lado a lado (el primero ocupa <paramref name="porcentajeIzquierda"/> %) o apilados si no entran.</summary>
        private static void DistribuirEnDos(FlowLayoutPanel fila, Control izquierda, Control derecha, int ancho, int porcentajeIzquierda, int altoLadoALado, int altoApiladoDerecha)
        {
            if (ancho >= 1000)
            {
                int anchoIzq = (ancho - Separacion) * porcentajeIzquierda / 100;
                izquierda.Size = new Size(anchoIzq, altoLadoALado);
                izquierda.Margin = new Padding(0, 0, Separacion, Separacion);
                derecha.Size = new Size(ancho - anchoIzq - Separacion, altoLadoALado);
                derecha.Margin = new Padding(0, 0, 0, Separacion);
                fila.Size = new Size(ancho, altoLadoALado + Separacion);
            }
            else
            {
                izquierda.Size = new Size(ancho, 320);
                izquierda.Margin = new Padding(0, 0, 0, Separacion);
                derecha.Size = new Size(ancho, altoApiladoDerecha);
                derecha.Margin = new Padding(0, 0, 0, Separacion);
                fila.Size = new Size(ancho, 320 + altoApiladoDerecha + Separacion * 2);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
