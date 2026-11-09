using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>
    /// Reportes de Recepción, en cuatro pestañas de lo urgente a lo histórico:
    /// «Ahora» (estado del hotel, llegadas y salidas del día; se actualiza sola cada minuto),
    /// «Mi turno» (lo cobrado y el efectivo que debería haber en caja), «Semana» (últimos 7 días)
    /// y «Mes» (mes en curso o anterior). Cada número trae una línea de contexto y un ícono de ayuda,
    /// y arriba hay una frase resumen en lenguaje cotidiano.
    /// </summary>
    internal class VistaReportes : UserControl
    {
        private const string PESTANA_AHORA = "ahora";
        private const string PESTANA_TURNO = "turno";
        private const string PESTANA_SEMANA = "semana";
        private const string PESTANA_MES = "mes";
        private const string MES_ACTUAL = "actual";
        private const string MES_ANTERIOR = "anterior";
        private const int AltoPanel = 300;

        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        private readonly Usuario _usuario;
        private readonly Action _navegarACheckOut;
        private readonly Action _navegarACaja;
        private readonly GestionReportesRecepcion _gestionReportes = new();
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();
        private readonly Timer _timerActualizacion;

        private ReporteRecepcion? _reporte;
        private ReportePeriodo? _reporteSemana;
        private ReportePeriodo? _reporteMes;
        private Control? _bloqueActivo;

        // Estructura general
        private readonly Panel _contenido;
        private readonly SelectorPestanas _selector;
        private readonly SelectorPestanas _selectorMes;
        private readonly Label _lblPeriodo;
        private readonly Button _btnPdf;
        private readonly Label _lblResumen;
        private readonly BloquePestana _pestanaAhora = new();
        private readonly BloquePestana _pestanaTurno = new();
        private readonly BloquePestana _pestanaSemana = new();
        private readonly BloquePestana _pestanaMes = new();
        private readonly Panel _panelSinTurno;

        // Pestaña «Ahora»
        private readonly TarjetaIndicador _kLibres;
        private readonly TarjetaIndicador _kLlegadas;
        private readonly TarjetaIndicador _kSalidas;
        private readonly TarjetaIndicador _kVencidas;
        private readonly BarraEstadoHabitaciones _barraEstados = new();
        private readonly PanelReporte _panelEstados;
        private readonly ListaMovimientos _listaSalidas = new();
        private readonly PanelReporte _panelSalidas;
        private readonly ListaMovimientos _listaLlegadas = new();

        // Pestaña «Mi turno»
        private readonly TarjetaIndicador _kCobrado;
        private readonly TarjetaIndicador _kCheckIns;
        private readonly TarjetaIndicador _kVentas;
        private readonly BarrasHorizontales _barrasMedios = new();
        private readonly BarrasHorizontales _barrasConceptos = new() { AnchoEtiqueta = 140 };
        private readonly DesgloseCaja _desgloseCaja = new();

        // Pestaña «Semana»
        private readonly TarjetaIndicador _kSemEntraron;
        private readonly TarjetaIndicador _kSemOcupacion;
        private readonly TarjetaIndicador _kSemNoches;
        private readonly TarjetaIndicador _kSemLimpieza;
        private readonly GraficoColumnas _grafSemOcupacion = new();
        private readonly PanelReporte _panelSemOcupacion;
        private readonly GraficoColumnas _grafSemLlegadas = new();
        private readonly BarrasHorizontales _barrasSemPedidas = new() { AnchoEtiqueta = 90 };

        // Pestaña «Mes»
        private readonly TarjetaIndicador _kMesOcupacion;
        private readonly TarjetaIndicador _kMesNoches;
        private readonly TarjetaIndicador _kMesVuelven;
        private readonly TarjetaIndicador _kMesIngresos;
        private readonly GraficoColumnas _grafMesOcupacion = new();
        private readonly PanelReporte _panelMesOcupacion;
        private readonly BarrasHorizontales _barrasMesPedidas = new() { AnchoEtiqueta = 90 };
        private readonly BarrasHorizontales _barrasMesFacturaron = new() { AnchoEtiqueta = 90, ColorBarra = Paleta.Exito };

        public VistaReportes(Usuario usuario, Action navegarACheckOut, Action navegarACaja)
        {
            _usuario = usuario;
            _navegarACheckOut = navegarACheckOut;
            _navegarACaja = navegarACaja;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;
            AutoScroll = true;

            _contenido = new Panel { BackColor = Paleta.FondoApp, Location = new Point(0, 0) };
            Controls.Add(_contenido);

            // ---------- Barra superior ----------
            _selector = new SelectorPestanas((PESTANA_AHORA, "Ahora"), (PESTANA_TURNO, "Mi turno"), (PESTANA_SEMANA, "Semana"), (PESTANA_MES, "Mes"))
            {
                Location = new Point(0, 0)
            };
            _selector.PestanaCambiada += _ => Refrescar();

            _selectorMes = new SelectorPestanas((MES_ACTUAL, "Este mes"), (MES_ANTERIOR, "Mes anterior")) { Visible = false };
            _selectorMes.PestanaCambiada += _ => Refrescar();

            _lblPeriodo = new Label { AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, BackColor = Color.Transparent };

            _btnPdf = EstiloBoton.Primario(new Button { Size = new Size(230, 40) });
            _btnPdf.Click += (s, e) => ExportarPdf();
            _toolTip.SetToolTip(_btnPdf, "Guarda en un archivo PDF exactamente lo que ves en esta pestaña, con el resumen y las explicaciones.");

            _lblResumen = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Paleta.TextoPrimario,
                BackColor = Paleta.PrimarioSuave,
                Padding = new Padding(14, 10, 14, 10)
            };

            _contenido.Controls.Add(_selector);
            _contenido.Controls.Add(_selectorMes);
            _contenido.Controls.Add(_lblPeriodo);
            _contenido.Controls.Add(_btnPdf);
            _contenido.Controls.Add(_lblResumen);

            // ---------- Pestaña «Ahora» ----------
            _kLibres = new TarjetaIndicador(_toolTip);
            _kLlegadas = new TarjetaIndicador(_toolTip);
            _kSalidas = new TarjetaIndicador(_toolTip);
            _kVencidas = new TarjetaIndicador(_toolTip);

            _panelEstados = CrearPanel("¿Cómo están las habitaciones?",
                "La barra completa es el hotel entero. Cada color ocupa la parte que le corresponde y usa el mismo color que el mapa de habitaciones.",
                _barraEstados);

            _panelSalidas = CrearPanel("Salidas de hoy",
                "Huéspedes que se van hoy, ordenados por urgencia: primero los que ya pasaron su hora (rojo), después los que salen pronto (amarillo) y al final los que ya se fueron (verde).",
                _listaSalidas, "Rojo: pasó su hora de salida. Amarillo: sale pronto. Verde: ya se fue.", "Ir a Check-out");
            _panelSalidas.AccionClick += (s, e) => _navegarACheckOut();

            var panelLlegadas = CrearPanel("Llegadas de hoy",
                "Huéspedes que hicieron el check-in hoy, del más reciente al más antiguo.",
                _listaLlegadas, "Del check-in más reciente al más antiguo.");

            _pestanaAhora.Agregar(new Control[] { _kLibres, _kLlegadas, _kSalidas, _kVencidas }, _panelEstados, _panelSalidas, panelLlegadas);
            _contenido.Controls.Add(_pestanaAhora);

            // ---------- Pestaña «Mi turno» ----------
            _kCobrado = new TarjetaIndicador(_toolTip);
            _kCheckIns = new TarjetaIndicador(_toolTip);
            _kVentas = new TarjetaIndicador(_toolTip);

            var panelMedios = CrearPanel("¿Cómo te pagaron?",
                "Cada barra es un medio de pago. La más larga es la que más dinero trajo. El monto está escrito en la barra.",
                _barrasMedios, "Ordenado del medio de pago que más dinero trajo al que menos.", alto: AltoPanel - 60);

            var panelConceptos = CrearPanel("¿Qué cobraste?",
                "Alojamiento: lo cobrado en los check-ins. Ventas adicionales: frigobar y productos vendidos en el mostrador.",
                _barrasConceptos, "Alojamiento comparado con ventas adicionales.", alto: AltoPanel - 60);

            var panelCaja = CrearPanel("Efectivo que debería haber en la caja",
                "Cambio con el que abriste el turno + todo lo que cobraste en efectivo. Contá la caja y comparalo con este número antes de cerrar el turno.",
                _desgloseCaja, "Si al contar no coincide, la diferencia se registra al cerrar el turno.", "Ir a Caja", AltoPanel - 60);
            panelCaja.AccionClick += (s, e) => _navegarACaja();

            _pestanaTurno.Agregar(new Control[] { _kCobrado, _kCheckIns, _kVentas }, panelMedios, panelConceptos, panelCaja);
            _contenido.Controls.Add(_pestanaTurno);

            _panelSinTurno = CrearPanelSinTurno();
            _contenido.Controls.Add(_panelSinTurno);

            // ---------- Pestaña «Semana» ----------
            _kSemEntraron = new TarjetaIndicador(_toolTip);
            _kSemOcupacion = new TarjetaIndicador(_toolTip);
            _kSemNoches = new TarjetaIndicador(_toolTip);
            _kSemLimpieza = new TarjetaIndicador(_toolTip);

            _panelSemOcupacion = CrearPanel("¿Qué días estuvo más lleno el hotel?",
                "Cada columna es un día y el número es cuántas habitaciones tuvieron huésped. La línea punteada marca el total del hotel: si la columna la toca, el hotel estuvo lleno.",
                _grafSemOcupacion, "Habitaciones ocupadas por día. La columna más oscura es hoy.");
            _panelSemOcupacion.AnchoCompleto = true;

            var panelSemLlegadas = CrearPanel("¿Cuántos huéspedes entraron cada día?",
                "Cantidad de check-ins hechos en cada día de la semana.",
                _grafSemLlegadas, "Check-ins por día.");

            var panelSemPedidas = CrearPanel("Habitaciones más pedidas",
                "Cuántas veces se alojó alguien en cada habitación durante la semana. Sirve para repartir el uso y planificar limpieza y mantenimiento.",
                _barrasSemPedidas, "Las 5 habitaciones que más se usaron en la semana.");

            _pestanaSemana.Agregar(new Control[] { _kSemEntraron, _kSemOcupacion, _kSemNoches, _kSemLimpieza }, _panelSemOcupacion, panelSemLlegadas, panelSemPedidas);
            _contenido.Controls.Add(_pestanaSemana);

            // ---------- Pestaña «Mes» ----------
            _kMesOcupacion = new TarjetaIndicador(_toolTip);
            _kMesNoches = new TarjetaIndicador(_toolTip);
            _kMesVuelven = new TarjetaIndicador(_toolTip);
            _kMesIngresos = new TarjetaIndicador(_toolTip);

            _panelMesOcupacion = CrearPanel("Habitaciones ocupadas por día",
                "Cada columna es un día del mes y el número es cuántas habitaciones tuvieron huésped. La línea punteada marca el total del hotel.",
                _grafMesOcupacion, "Debajo de cada columna está el número de día.");
            _panelMesOcupacion.AnchoCompleto = true;

            var panelMesPedidas = CrearPanel("Habitaciones más pedidas",
                "Cuántas veces se alojó alguien en cada habitación durante el mes.",
                _barrasMesPedidas, "Las 6 habitaciones que más se usaron.");

            var panelMesFacturaron = CrearPanel("Habitaciones que más facturaron (estimado)",
                "Es una estimación: tarifa de la habitación × noches vendidas. No es el dinero cobrado; eso está en «Mi turno».",
                _barrasMesFacturaron, "Estimado con la tarifa vigente de cada habitación.");

            _pestanaMes.Agregar(new Control[] { _kMesOcupacion, _kMesNoches, _kMesVuelven, _kMesIngresos }, _panelMesOcupacion, panelMesPedidas, panelMesFacturaron);
            _contenido.Controls.Add(_pestanaMes);

            // La pestaña «Ahora» se actualiza sola mientras está a la vista.
            _timerActualizacion = new Timer { Interval = 60_000 };
            _timerActualizacion.Tick += (s, e) =>
            {
                if (Visible && Parent != null && _selector.Seleccionada == PESTANA_AHORA) Refrescar();
            };
            _timerActualizacion.Start();

            Refrescar();
        }

        private PanelReporte CrearPanel(string titulo, string ayuda, Control contenido, string? pie = null, string? accion = null, int alto = AltoPanel)
        {
            var panel = new PanelReporte(_toolTip, titulo, ayuda, contenido, new Size(400, alto), accion);
            if (pie != null) panel.TextoPie = pie;
            return panel;
        }

        private static FlowLayoutPanel CrearFlujo() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Paleta.FondoApp,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        private Panel CrearPanelSinTurno()
        {
            var panel = new Panel { Size = new Size(560, 120), BackColor = Paleta.FondoApp };
            var lbl = new Label
            {
                Text = "Cuando abras tu turno de caja, acá vas a ver cuánto cobraste, con qué medios de pago y cuánto efectivo debería haber en la caja.",
                Font = Paleta.FuenteBase,
                ForeColor = Paleta.TextoSecundario,
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                Location = new Point(0, 0)
            };
            var btn = EstiloBoton.Primario(new Button { Text = "Ir a Caja", Size = new Size(160, 40), Location = new Point(0, 56) });
            btn.Click += (s, e) => _navegarACaja();
            panel.Controls.Add(lbl);
            panel.Controls.Add(btn);
            return panel;
        }

        // ------------------------------------------------------------------
        // Carga de datos
        // ------------------------------------------------------------------

        /// <summary>Vuelve a leer los datos de la pestaña que está a la vista.</summary>
        public void Refrescar()
        {
            try
            {
                switch (_selector.Seleccionada)
                {
                    case PESTANA_SEMANA:
                        _reporteSemana = _gestionReportes.GenerarSemana();
                        CargarSemana(_reporteSemana);
                        break;
                    case PESTANA_MES:
                        _reporteMes = _gestionReportes.GenerarMes(_selectorMes.Seleccionada == MES_ANTERIOR);
                        CargarMes(_reporteMes);
                        break;
                    default:
                        _reporte = _gestionReportes.Generar(_usuario.DniUsuario);
                        CargarAhora(_reporte);
                        CargarTurno(_reporte);
                        break;
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                return;
            }

            MostrarPestana();
        }

        private void MostrarError(Exception ex)
        {
            _lblResumen.Text = $"No se pudieron cargar los datos del reporte. Revisá la conexión con la base de datos y volvé a entrar a esta sección.\nDetalle: {ex.Message}";
            _lblResumen.BackColor = Paleta.PeligroSuave;
            _bloqueActivo = null;
            foreach (Control bloque in Bloques) bloque.Visible = false;
            Reacomodar();
        }

        private IEnumerable<Control> Bloques => new Control[] { _pestanaAhora, _pestanaTurno, _panelSinTurno, _pestanaSemana, _pestanaMes };

        private void CargarAhora(ReporteRecepcion r)
        {
            _kLibres.Configurar("Habitaciones libres ahora", r.Libres.ToString(),
                $"de {r.TotalHabitaciones} habitaciones",
                "Habitaciones limpias y sin huésped, listas para entregar. No incluye las que están en limpieza ni las que están fuera de servicio.",
                DibujoUtil.ColorPorEstado(EstadoOcupacion.Disponible));

            _kLlegadas.Configurar("Llegadas de hoy", r.Llegadas.Count.ToString(),
                r.Llegadas.Count == 0 ? "todavía no hubo check-ins" : "check-ins hechos hoy",
                "Huéspedes que hicieron el check-in hoy. El detalle está en la lista «Llegadas de hoy».");

            int realizadas = r.SalidasRealizadasHoy;
            _kSalidas.Configurar("Salidas que faltan hoy", r.SalidasQueFaltanHoy.ToString(),
                realizadas == 1 ? "1 ya se fue hoy" : $"{realizadas} ya se fueron hoy",
                $"Huéspedes que se van hoy y todavía no hicieron el check-out. En la lista se marcan en amarillo cuando faltan {r.MinutosTolerancia} minutos o menos.");

            var vencidas = r.SalidasVencidas;
            _kVencidas.Configurar("Pasaron su hora de salida", vencidas.Count.ToString(),
                vencidas.Count == 0 ? "ninguna demorada" : "Hab. " + string.Join(", ", vencidas.Select(v => v.NroHabitacion)),
                "Huéspedes que siguen en la habitación después de su hora de salida. Conviene llamar a la habitación o hacer el check-out.",
                alerta: vencidas.Count > 0);

            _barraEstados.Cargar(new[]
            {
                ("Ocupadas", r.Ocupadas, DibujoUtil.ColorPorEstado(EstadoOcupacion.Ocupada)),
                ("Libres", r.Libres, DibujoUtil.ColorPorEstado(EstadoOcupacion.Disponible)),
                ("En limpieza", r.EnLimpieza, DibujoUtil.ColorPorEstado(EstadoOcupacion.Limpieza)),
                ("Fuera de servicio", r.FueraDeServicio, DibujoUtil.ColorPorEstado(EstadoOcupacion.Mantenimiento))
            });
            _panelEstados.TextoPie = r.TotalHabitaciones == 0
                ? string.Empty
                : $"El hotel está ocupado al {r.PorcentajeOcupacion.ToString("0", CulturaAr)} % ({r.Ocupadas} de {r.TotalHabitaciones} habitaciones).";

            _listaSalidas.Cargar(r.Salidas.Select(m => (m.NroHabitacion.ToString(), m.Huesped,
                    GestionReportesRecepcion.DescribirEstado(m, r.Generado), TonoDe(m.Estado))),
                "No hay salidas previstas para hoy.");
            _panelSalidas.MostrarAccion(r.SalidasQueFaltanHoy + vencidas.Count > 0);

            _listaLlegadas.Cargar(r.Llegadas.Select(m => (m.NroHabitacion.ToString(), m.Huesped,
                    GestionReportesRecepcion.DescribirEstado(m, r.Generado), TonoEtiqueta.Exito)),
                "Todavía no hubo check-ins hoy.");
        }

        private void CargarTurno(ReporteRecepcion r)
        {
            if (!r.HayTurnoAbierto)
            {
                return;
            }

            string desde = r.InicioTurno!.Value.Date == r.Generado.Date
                ? $"desde las {r.InicioTurno.Value:HH:mm}"
                : $"desde el {r.InicioTurno.Value.ToString("dd/MM", CulturaAr)} a las {r.InicioTurno.Value:HH:mm}";

            _kCobrado.Configurar("Cobrado en tu turno", Moneda(r.TotalCobrado), desde,
                "Dinero que recibiste desde que abriste la caja: alojamientos y ventas adicionales, con todos los medios de pago.");

            _kCheckIns.Configurar("Check-ins que hiciste", r.CheckInsTurno.ToString(),
                r.CheckOutsTurno == 1 ? "y 1 check-out desde que abriste" : $"y {r.CheckOutsTurno} check-outs desde que abriste",
                "Huéspedes a los que les hiciste el check-in en este turno, y salidas registradas desde que abriste la caja.");

            _kVentas.Configurar("Ventas adicionales", Moneda(r.CobradoVentas),
                $"{r.CantidadVentas} {(r.CantidadVentas == 1 ? "venta" : "ventas")} · {r.VentasMostrador} de mostrador",
                "Productos vendidos a huéspedes (frigobar) o a clientes de paso (mostrador). Ya están sumados en «Cobrado en tu turno».");

            _barrasMedios.Cargar(new[]
                {
                    ("Tarjeta", (double)r.CobradoTarjeta, Moneda(r.CobradoTarjeta)),
                    ("Efectivo", (double)r.CobradoEfectivo, Moneda(r.CobradoEfectivo)),
                    ("Transferencia", (double)r.CobradoTransferencia, Moneda(r.CobradoTransferencia))
                }.OrderByDescending(i => i.Item2),
                "Todavía no cobraste nada en este turno. Cuando hagas un check-in o una venta, vas a ver acá cómo te pagaron.");

            _barrasConceptos.Cargar(new[]
                {
                    ("Alojamiento", (double)r.CobradoAlojamiento, Moneda(r.CobradoAlojamiento)),
                    ("Ventas adicionales", (double)r.CobradoVentas, Moneda(r.CobradoVentas))
                },
                "Todavía no hay cobros de alojamiento ni ventas en este turno.");

            _desgloseCaja.Cargar(r.CambioInicial, r.CobradoEfectivo, Moneda);
        }

        private void CargarSemana(ReportePeriodo r)
        {
            int diferencia = r.HuespedesQueEntraron - r.HuespedesPeriodoAnterior;
            string comparacion = diferencia > 0 ? $"{diferencia} más que la semana anterior"
                : diferencia < 0 ? $"{-diferencia} menos que la semana anterior"
                : "igual que la semana anterior";

            _kSemEntraron.Configurar("Huéspedes que entraron", r.HuespedesQueEntraron.ToString(), comparacion,
                $"Check-ins hechos en los últimos 7 días. Se compara con los 7 días anteriores, en los que entraron {r.HuespedesPeriodoAnterior}.");

            _kSemOcupacion.Configurar("Hotel ocupado", $"{r.PorcentajeOcupacion.ToString("0", CulturaAr)} %",
                GestionReportesRecepcion.HabitacionesPorNoche(r),
                "Promedio de la semana: de cada 100 habitaciones disponibles por noche, cuántas tuvieron huésped.");

            _kSemNoches.Configurar("Noches por huésped", r.NochesPorHuesped > 0 ? r.NochesPorHuesped.ToString("0.#", CulturaAr) : "-",
                "noches en promedio",
                "Cuántas noches se quedó, en promedio, cada huésped alojado esta semana.");

            _kSemLimpieza.Configurar("Tiempo de limpieza", r.CantidadLimpiezas > 0 ? GestionReportesRecepcion.Minutos(r.MinutosLimpieza) : "-",
                r.CantidadLimpiezas > 0 ? $"promedio de {r.CantidadLimpiezas} {(r.CantidadLimpiezas == 1 ? "limpieza" : "limpiezas")}" : "sin limpiezas registradas",
                "Desde que empieza la limpieza hasta que la habitación queda lista para un nuevo huésped, en promedio.");

            int indiceHoy = r.OcupacionDiaria.Count - 1;
            _grafSemOcupacion.Cargar(r.OcupacionDiaria.Select(p => (p.Etiqueta, p.Valor)), r.TotalHabitaciones,
                $"Total del hotel: {r.TotalHabitaciones}", indiceHoy, "No hubo habitaciones ocupadas en estos 7 días.");

            _grafSemLlegadas.Cargar(r.LlegadasDiarias.Select(p => (p.Etiqueta, p.Valor)), null, string.Empty, indiceHoy,
                "No hubo check-ins en estos 7 días.");

            _barrasSemPedidas.Cargar(r.HabitacionesMasPedidas.Take(5).Select(p => (p.Etiqueta, p.Valor, Veces(p.Valor))),
                "Ninguna habitación se usó en estos 7 días.");
        }

        private void CargarMes(ReportePeriodo r)
        {
            _kMesOcupacion.Configurar("Hotel ocupado", $"{r.PorcentajeOcupacion.ToString("0", CulturaAr)} %",
                GestionReportesRecepcion.HabitacionesPorNoche(r),
                "Promedio del mes: de cada 100 habitaciones disponibles por noche, cuántas tuvieron huésped.");

            _kMesNoches.Configurar("Noches por huésped", r.NochesPorHuesped > 0 ? r.NochesPorHuesped.ToString("0.#", CulturaAr) : "-",
                "noches en promedio",
                "Cuántas noches se quedó, en promedio, cada huésped alojado este mes.");

            _kMesVuelven.Configurar("Huéspedes que vuelven", r.HuespedesQueVuelven.ToString(),
                r.HuespedesDistintos == 0 ? "nadie entró en el mes" : r.HuespedesDistintos == 1 ? "de 1 huésped en el mes" : $"de {r.HuespedesDistintos} huéspedes en el mes",
                "Personas (mismo DNI) que entraron este mes y ya se habían alojado antes en el hotel. Son clientes frecuentes.");

            _kMesIngresos.Configurar("Ingresos estimados", MonedaCompacta(r.IngresosEstimados),
                "por alojamiento, sin ventas",
                "Es una estimación: tarifa de cada habitación × noches vendidas. El dinero cobrado de verdad está en «Mi turno».");

            int indiceHoy = r.Hasta == DateTime.Today ? r.OcupacionDiaria.Count - 1 : -1;
            _grafMesOcupacion.Cargar(r.OcupacionDiaria.Select(p => (p.Etiqueta, p.Valor)), r.TotalHabitaciones,
                $"Total del hotel: {r.TotalHabitaciones}", indiceHoy, $"No hubo habitaciones ocupadas en {r.NombrePeriodo}.");
            _panelMesOcupacion.TextoPie = indiceHoy >= 0
                ? "Debajo de cada columna está el número de día. La columna más oscura es hoy."
                : "Debajo de cada columna está el número de día.";

            _barrasMesPedidas.Cargar(r.HabitacionesMasPedidas.Take(6).Select(p => (p.Etiqueta, p.Valor, Veces(p.Valor))),
                $"Ninguna habitación se usó en {r.NombrePeriodo}.");

            _barrasMesFacturaron.Cargar(r.HabitacionesQueMasFacturaron.Take(6).Select(p => (p.Etiqueta, p.Valor, Moneda((decimal)p.Valor))),
                $"No hay ingresos estimados para {r.NombrePeriodo}.");
        }

        private void MostrarPestana()
        {
            string pestana = _selector.Seleccionada;
            _lblResumen.BackColor = Paleta.PrimarioSuave;
            _selectorMes.Visible = pestana == PESTANA_MES;

            string resumen;
            switch (pestana)
            {
                case PESTANA_SEMANA when _reporteSemana != null:
                    _bloqueActivo = _pestanaSemana;
                    resumen = _reporteSemana.Resumen;
                    _lblPeriodo.Text = $"Del {_reporteSemana.Desde.ToString("dd/MM", CulturaAr)} al {_reporteSemana.Hasta.ToString("dd/MM", CulturaAr)} (últimos 7 días)";
                    break;

                case PESTANA_MES when _reporteMes != null:
                    _bloqueActivo = _pestanaMes;
                    resumen = _reporteMes.Resumen;
                    _lblPeriodo.Text = $"Del {_reporteMes.Desde.ToString("dd/MM", CulturaAr)} al {_reporteMes.Hasta.ToString("dd/MM/yyyy", CulturaAr)}";
                    break;

                case PESTANA_TURNO when _reporte != null:
                    _bloqueActivo = _reporte.HayTurnoAbierto ? _pestanaTurno : _panelSinTurno;
                    resumen = _reporte.ResumenTurno;
                    _lblPeriodo.Text = _reporte.HayTurnoAbierto
                        ? $"Tu turno de caja · abierto {_reporte.InicioTurno!.Value.ToString("dd/MM HH:mm", CulturaAr)}"
                        : "Sin turno de caja abierto";
                    break;

                case PESTANA_AHORA when _reporte != null:
                    _bloqueActivo = _pestanaAhora;
                    resumen = _reporte.ResumenAhora;
                    string fecha = _reporte.Generado.ToString("dddd d 'de' MMMM", CulturaAr);
                    _lblPeriodo.Text = $"{char.ToUpper(fecha[0], CulturaAr)}{fecha.Substring(1)} · actualizado {_reporte.Generado:HH:mm} (se actualiza solo cada minuto)";
                    break;

                default:
                    return;
            }

            foreach (Control bloque in Bloques) bloque.Visible = bloque == _bloqueActivo;
            _lblResumen.Text = "Resumen: " + resumen;
            _btnPdf.Text = $"Guardar «{NombrePestana()}» en PDF";
            Reacomodar();
        }

        private string NombrePestana() => _selector.Seleccionada switch
        {
            PESTANA_TURNO => "Mi turno",
            PESTANA_SEMANA => "Semana",
            PESTANA_MES => "Mes",
            _ => "Ahora"
        };

        private static TonoEtiqueta TonoDe(EstadoMovimiento estado) => estado switch
        {
            EstadoMovimiento.SalidaVencida => TonoEtiqueta.Peligro,
            EstadoMovimiento.SalidaProxima => TonoEtiqueta.Advertencia,
            EstadoMovimiento.SalidaRealizada => TonoEtiqueta.Exito,
            _ => TonoEtiqueta.Neutro
        };

        private static string Moneda(decimal valor) => GestionReportesRecepcion.Moneda(valor);

        private static string MonedaCompacta(decimal valor)
        {
            if (valor >= 1_000_000) return $"$ {(valor / 1_000_000).ToString("0.#", CulturaAr)} M";
            return Moneda(valor);
        }

        private static string Veces(double valor) => valor == 1 ? "1 vez" : $"{valor:0} veces";

        // ------------------------------------------------------------------
        // Disposición en pantalla
        // ------------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Reacomodar();
        }

        private void Reacomodar()
        {
            if (_contenido == null) return;

            int ancho = Math.Max(480, ClientSize.Width);

            _btnPdf.Location = new Point(ancho - _btnPdf.Width, 1);
            int x = _selector.Right + 12;
            if (_selectorMes.Visible || _selector.Seleccionada == PESTANA_MES)
            {
                _selectorMes.Location = new Point(x, 0);
                x = _selectorMes.Right + 12;
            }
            _lblPeriodo.Location = new Point(x + 4, (_selector.Height - _lblPeriodo.Height) / 2);

            int y = _selector.Bottom + 16;
            if (_lblPeriodo.Right > _btnPdf.Left - 12)
            {
                // Pantalla angosta: la fecha baja a su propio renglón.
                _lblPeriodo.Location = new Point(0, y);
                y = _lblPeriodo.Bottom + 12;
            }

            _lblResumen.MaximumSize = new Size(ancho, 0);
            _lblResumen.MinimumSize = new Size(ancho, 0);
            _lblResumen.Location = new Point(0, y);
            y = _lblResumen.Bottom + 18;

            foreach (Control bloque in Bloques)
            {
                if (bloque is BloquePestana pestana) pestana.Acomodar(ancho);
                bloque.Location = new Point(0, y);
            }

            int alto = y + (_bloqueActivo?.Height ?? 0) + 8;
            _contenido.Bounds = new Rectangle(AutoScrollPosition.X, AutoScrollPosition.Y, ancho, alto);
        }

        // ------------------------------------------------------------------
        // Exportar
        // ------------------------------------------------------------------

        private void ExportarPdf()
        {
            string pestana = _selector.Seleccionada;
            var seccion = pestana switch
            {
                PESTANA_TURNO => SeccionReporteRecepcion.MiTurno,
                PESTANA_SEMANA => SeccionReporteRecepcion.Semana,
                PESTANA_MES => SeccionReporteRecepcion.Mes,
                _ => SeccionReporteRecepcion.Ahora
            };
            string nombre = NombrePestana();

            bool hayDatos = seccion switch
            {
                SeccionReporteRecepcion.Semana => _reporteSemana != null,
                SeccionReporteRecepcion.Mes => _reporteMes != null,
                _ => _reporte != null
            };
            if (!hayDatos)
            {
                MessageBox.Show("Todavía no se pudieron cargar los datos, así que no hay nada para guardar. Volvé a entrar a Reportes e intentá de nuevo.",
                    "Guardar en PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (seccion == SeccionReporteRecepcion.MiTurno && !_reporte!.HayTurnoAbierto)
            {
                MessageBox.Show("Todavía no abriste la caja, así que no hay cobros para guardar.\n\nAbrí tu turno desde «Caja» o guardá otra pestaña.",
                    "Guardar en PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string detalle = seccion == SeccionReporteRecepcion.Mes ? $"{nombre} {_reporteMes!.Desde:MM-yyyy}" : nombre;
            using var dialogo = new SaveFileDialog
            {
                Title = $"Guardar «{nombre}» en PDF",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Recepción - {detalle} - {DateTime.Now:dd-MM-yyyy HH.mm}.pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                DefaultExt = "pdf",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                string generadoPor = $"{_usuario.NomUsuario} {_usuario.ApeUsuario}".Trim();
                switch (seccion)
                {
                    case SeccionReporteRecepcion.Semana:
                        GeneradorReportePdf.GenerarArchivoPeriodo(_reporteSemana!, dialogo.FileName, seccion, generadoPor);
                        break;
                    case SeccionReporteRecepcion.Mes:
                        GeneradorReportePdf.GenerarArchivoPeriodo(_reporteMes!, dialogo.FileName, seccion, generadoPor);
                        break;
                    default:
                        GeneradorReportePdf.GenerarArchivoRecepcion(_reporte!, dialogo.FileName, seccion, generadoPor);
                        break;
                }
            }
            catch (IOException)
            {
                MessageBox.Show("No se pudo guardar el PDF porque el archivo está abierto en otro programa. Cerralo e intentá de nuevo, o elegí otro nombre.",
                    "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No tenés permiso para guardar en esa carpeta. Elegí otra, por ejemplo «Documentos».",
                    "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo generar el PDF: {ex.Message}", "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            DialogResult abrir = MessageBox.Show(
                $"Se guardó el reporte «{nombre}» en:\n{dialogo.FileName}\n\n¿Querés abrirlo ahora?",
                "Reporte guardado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (abrir == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialogo.FileName, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"El PDF se guardó, pero no se pudo abrir: {ex.Message}", "Abrir PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Contenido de una pestaña: una fila de indicadores y debajo los paneles. Las tarjetas se reparten
        /// el ancho disponible (todas del mismo ancho) y bajan de fila solo si no entran con su ancho mínimo.
        /// Los paneles marcados como de ancho completo ocupan una fila entera.
        /// </summary>
        private sealed class BloquePestana : Panel
        {
            private const int Separacion = 16;

            public FlowLayoutPanel Indicadores { get; } = CrearFlujo();
            public FlowLayoutPanel Paneles { get; } = CrearFlujo();

            public BloquePestana()
            {
                BackColor = Paleta.FondoApp;
                Controls.Add(Indicadores);
                Controls.Add(Paneles);
            }

            public void Agregar(Control[] indicadores, params Control[] paneles)
            {
                Indicadores.Controls.AddRange(indicadores);
                Paneles.Controls.AddRange(paneles);
            }

            public void Acomodar(int ancho)
            {
                // Cada tarjeta lleva 16 px de margen a la derecha: el flujo ocupa también ese margen.
                var limite = new Size(ancho + Separacion, 0);

                Repartir(Indicadores.Controls.Cast<Control>().ToList(), ancho, anchoMinimo: 220, maximoPorFila: Indicadores.Controls.Count);

                var paneles = Paneles.Controls.Cast<Control>().ToList();
                foreach (Control completo in paneles.Where(EsAnchoCompleto))
                {
                    completo.Width = ancho;
                }
                Repartir(paneles.Where(p => !EsAnchoCompleto(p)).ToList(), ancho, anchoMinimo: 320, maximoPorFila: 3);

                Indicadores.MaximumSize = limite;
                Paneles.MaximumSize = limite;

                // Alto por tamaño preferido: Bottom/Visible no son confiables mientras la vista no está en pantalla.
                int altoIndicadores = Indicadores.GetPreferredSize(limite).Height;
                Indicadores.Location = Point.Empty;
                Paneles.Location = new Point(0, altoIndicadores);
                Size = new Size(ancho + Separacion, altoIndicadores + Paneles.GetPreferredSize(limite).Height);
            }

            private static bool EsAnchoCompleto(Control control) => control is PanelReporte { AnchoCompleto: true };

            private static void Repartir(List<Control> controles, int ancho, int anchoMinimo, int maximoPorFila)
            {
                if (controles.Count == 0) return;
                int porFila = Math.Max(1, Math.Min(Math.Max(1, maximoPorFila), (ancho + Separacion) / (anchoMinimo + Separacion)));
                int anchoTarjeta = (ancho - (porFila - 1) * Separacion) / porFila;
                foreach (Control control in controles)
                {
                    control.Width = anchoTarjeta;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerActualizacion.Dispose();
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
