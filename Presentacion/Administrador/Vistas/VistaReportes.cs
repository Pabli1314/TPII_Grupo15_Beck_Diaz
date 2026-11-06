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

namespace Presentacion.Administrador.Vistas
{
    /// <summary>
    /// Reportes y estadísticas del Administrador, pensado para leerse "de arriba hacia abajo":
    /// 1) qué período y filtros estoy mirando, 2) los números clave con su comparativa,
    /// 3) las conclusiones en lenguaje simple, 4) los gráficos que las respaldan y
    /// 5) las tablas detalladas para quien quiera profundizar.
    /// </summary>
    internal class VistaReportes : UserControl, IVistaAdministrador
    {
        private const int Separacion = 16;
        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        private readonly GestionReportes _gestionReportes = new();
        private readonly Usuario? _usuario;
        private readonly ToolTip _toolTip = EstiloReporte.CrearToolTip();

        // Último reporte mostrado en pantalla y el filtro con que se generó: es lo que se exporta a PDF.
        private ReporteHotel? _reporteActual;
        private PeriodoReporte? _periodoActual;
        private string _filtroActual = string.Empty;
        private bool _catalogosCargados;
        private bool _suspenderEventos;

        // --- Estructura general ---
        private readonly FlowLayoutPanel _raiz;
        private readonly Panel _encabezado;
        private readonly Label _lblPeriodo;
        private readonly Label _lblComparacion;
        private readonly Button _btnPdf;

        // --- Filtros ---
        private readonly Panel _tarjetaFiltros;
        private readonly FlowLayoutPanel _filaPeriodo;
        private readonly FlowLayoutPanel _filaFiltros;
        private readonly SelectorPestanas _selectorPeriodo;
        private readonly Panel _panelRango;
        private readonly DateTimePicker _dtpDesde;
        private readonly DateTimePicker _dtpHasta;
        private readonly ComboBox _cmbTipo;
        private readonly ComboBox _cmbEstado;
        private readonly ComboBox _cmbHuesped;
        private readonly ComboBox _cmbPago;
        private readonly LinkLabel _lnkLimpiar;

        // --- KPIs ---
        private readonly FlowLayoutPanel _filaKpis;
        private readonly TarjetaKpi _kpiOcupacionActual;
        private readonly TarjetaKpi _kpiOcupacionPeriodo;
        private readonly TarjetaKpi _kpiIngresos;
        private readonly TarjetaKpi _kpiEstadias;
        private readonly TarjetaKpi _kpiPromedioEstadia;
        private readonly TarjetaKpi _kpiMantenimiento;

        // --- Conclusiones ---
        private readonly PanelInsights _insights;

        private readonly FlowLayoutPanel _filaOperacion;
        private readonly PanelReporte _panelEstadoActual;
        private readonly PanelReporte _panelRanking;
        private readonly BarraEstadoHabitaciones _barraEstados;
        private readonly BarrasHorizontales _rankingHabitaciones;

        // --- Tablas detalladas ---
        private readonly PanelReporte _panelTablaTipos;
        private readonly PanelReporte _panelTablaHabitaciones;
        private readonly DataGridView _dgvTipos;
        private readonly DataGridView _dgvHabitaciones;

        private readonly List<Control> _titulosSeccion = new();

        public VistaReportes(Usuario? usuario = null)
        {
            _usuario = usuario;
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            _raiz = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 0, 0, 24)
            };

            // ================= Encabezado: qué estoy mirando =================
            _encabezado = new Panel { Height = 58, BackColor = Paleta.FondoApp, Margin = new Padding(0, 0, 0, 10) };
            _lblPeriodo = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 13f), ForeColor = Paleta.TextoPrimario, Location = new Point(0, 2) };
            _lblComparacion = new Label { AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, Location = new Point(1, 32) };
            _btnPdf = EstiloBoton.Primario(new Button { Text = "Exportar PDF", Size = new Size(140, 38) });
            _btnPdf.Click += (s, e) => ExportarPdf();
            _toolTip.SetToolTip(_btnPdf, "Descarga un PDF con los números del período y los filtros que estás viendo.");
            _encabezado.Controls.Add(_lblPeriodo);
            _encabezado.Controls.Add(_lblComparacion);
            _encabezado.Controls.Add(_btnPdf);
            _encabezado.Resize += (s, e) =>
            {
                _btnPdf.Location = new Point(_encabezado.Width - _btnPdf.Width, 8);
                _lblPeriodo.MaximumSize = new Size(Math.Max(100, _btnPdf.Left - 16), 0);
                _lblComparacion.MaximumSize = new Size(Math.Max(100, _btnPdf.Left - 16), 0);
            };

            // ================= Filtros =================
            _selectorPeriodo = new SelectorPestanas(
                ("hoy", "Hoy"), ("semana", "Esta semana"), ("mes", "Este mes"),
                ("trimestre", "Último trimestre"), ("anio", "Año en curso"), ("personalizado", "Personalizado…"))
            {
                Margin = new Padding(0, 0, 12, 6)
            };
            _selectorPeriodo.Seleccionar("mes", notificar: false);
            _selectorPeriodo.PestanaCambiada += _ => CambioDePeriodo();

            _dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 118, Font = Paleta.FuenteBase, Location = new Point(0, 10), Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            var lblA = new Label { Text = "al", AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, Location = new Point(124, 14) };
            _dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 118, Font = Paleta.FuenteBase, Location = new Point(146, 10), Value = DateTime.Today };
            var btnAplicar = EstiloBoton.Secundario(new Button { Text = "Aplicar", Size = new Size(84, 34), Location = new Point(272, 6) });
            btnAplicar.Click += (s, e) => Refrescar();
            _panelRango = new Panel { Size = new Size(360, 44), Margin = new Padding(0, 0, 0, 6), Visible = false, BackColor = Paleta.FondoTarjeta };
            _panelRango.Controls.AddRange(new Control[] { _dtpDesde, lblA, _dtpHasta, btnAplicar });

            _filaPeriodo = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Paleta.FondoTarjeta, Margin = Padding.Empty };
            _filaPeriodo.Controls.Add(CrearEtiquetaFiltro("Período", 76, 14));
            _filaPeriodo.Controls.Add(_selectorPeriodo);
            _filaPeriodo.Controls.Add(_panelRango);

            _cmbTipo = CrearComboFiltro();
            _cmbEstado = CrearComboFiltro();
            _cmbHuesped = CrearComboFiltro();
            _cmbHuesped.Items.AddRange(new object[]
            {
                new OpcionFiltro("Todos los huéspedes", (int)TipoHuespedFiltro.Todos),
                new OpcionFiltro("Nuevos (primera visita)", (int)TipoHuespedFiltro.Nuevos),
                new OpcionFiltro("Recurrentes (ya vinieron)", (int)TipoHuespedFiltro.Recurrentes)
            });
            _cmbHuesped.SelectedIndex = 0;
            _cmbPago = CrearComboFiltro();

            _lnkLimpiar = new LinkLabel
            {
                Text = "✕ Quitar filtros",
                AutoSize = true,
                Font = Paleta.FuenteBaseNegrita,
                LinkColor = Paleta.Primario,
                ActiveLinkColor = Paleta.PrimarioHover,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Margin = new Padding(4, 34, 0, 0),
                Visible = false
            };
            _lnkLimpiar.LinkClicked += (s, e) => LimpiarFiltros();

            _filaFiltros = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Paleta.FondoTarjeta, Margin = new Padding(0, 4, 0, 0) };
            _filaFiltros.Controls.Add(CrearEtiquetaFiltro("Filtrar por", 76, 34));
            _filaFiltros.Controls.Add(EnvolverFiltro("Tipo de habitación", _cmbTipo, "Muestra solo las habitaciones de este tipo (Simple, Doble, Suite…)."));
            _filaFiltros.Controls.Add(EnvolverFiltro("Estado actual de la habitación", _cmbEstado, "Muestra solo las habitaciones que HOY están en este estado. Útil, por ejemplo, para ver cuánto facturaron las que hoy están en mantenimiento."));
            _filaFiltros.Controls.Add(EnvolverFiltro("Tipo de huésped", _cmbHuesped, "Nuevos: es la primera vez que se alojan en el hotel.\nRecurrentes: ya se habían alojado antes."));
            _filaFiltros.Controls.Add(EnvolverFiltro("Canal de cobro (forma de pago)", _cmbPago, "Con qué medio se registró el pago de la estadía (efectivo, tarjeta, transferencia…). Es el canal de ingreso que guarda el sistema."));
            _filaFiltros.Controls.Add(_lnkLimpiar);

            _tarjetaFiltros = new TarjetaFiltros { Padding = new Padding(18, 14, 18, 10), Margin = new Padding(0, 0, 0, Separacion) };
            var contenidoFiltros = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Paleta.FondoTarjeta };
            contenidoFiltros.Controls.Add(_filaPeriodo);
            contenidoFiltros.Controls.Add(_filaFiltros);
            _tarjetaFiltros.Controls.Add(contenidoFiltros);

            // ================= KPIs =================
            _kpiOcupacionActual = new TarjetaKpi(_toolTip);
            _kpiOcupacionPeriodo = new TarjetaKpi(_toolTip);
            _kpiIngresos = new TarjetaKpi(_toolTip);
            _kpiEstadias = new TarjetaKpi(_toolTip);
            _kpiPromedioEstadia = new TarjetaKpi(_toolTip);
            _kpiMantenimiento = new TarjetaKpi(_toolTip);
            _filaKpis = CrearFila(_kpiOcupacionActual, _kpiIngresos, _kpiOcupacionPeriodo, _kpiPromedioEstadia, _kpiEstadias, _kpiMantenimiento);

            // ================= Conclusiones =================
            _insights = new PanelInsights { Margin = new Padding(0, 0, 0, Separacion) };

            // ================= Operación =================
            _barraEstados = new BarraEstadoHabitaciones();
            _rankingHabitaciones = new BarrasHorizontales { ColorBarra = Paleta.Exito, AnchoEtiqueta = 96 };
            _panelEstadoActual = new PanelReporte(_toolTip, "¿Cómo están las habitaciones ahora?",
                "Foto del momento: cuántas habitaciones están disponibles, ocupadas, en limpieza o en mantenimiento. No depende del período elegido.",
                _barraEstados, new Size(600, 360)) { TextoPie = "Estado en tiempo real. Las habitaciones en mantenimiento no se pueden ofrecer." };
            _panelRanking = new PanelReporte(_toolTip, "¿Qué habitaciones facturaron más?",
                "Las 6 habitaciones con más ingresos estimados en el período. Útil para detectar las unidades más rentables.",
                _rankingHabitaciones, new Size(600, 360)) { TextoPie = "Ingresos estimados del período, de mayor a menor." };
            _filaOperacion = CrearFila(_panelEstadoActual, _panelRanking);

            // ================= Tablas detalladas =================
            _dgvTipos = CrearGrilla();
            _dgvTipos.Columns.Add("tipo", "Tipo de habitación");
            _dgvTipos.Columns.Add("habitaciones", "Habitaciones");
            _dgvTipos.Columns.Add("estadias", "Estadías");
            _dgvTipos.Columns.Add("noches", "Noches ocupadas");
            _dgvTipos.Columns.Add("ocupacion", "Ocupación");
            _dgvTipos.Columns.Add("ingresos", "Ingresos estimados");
            _dgvTipos.Columns.Add("promedio", "Promedio por estadía");
            _dgvTipos.Columns.Add("participacion", "% de los ingresos");
            _dgvTipos.Columns["tipo"]!.FillWeight = 160;
            _dgvTipos.CellFormatting += FormatearCeldaOcupacion;

            _dgvHabitaciones = CrearGrilla();
            _dgvHabitaciones.Columns.Add("habitacion", "Habitación");
            _dgvHabitaciones.Columns.Add("tipo", "Tipo");
            _dgvHabitaciones.Columns.Add("estado", "Estado actual");
            _dgvHabitaciones.Columns.Add("estadias", "Estadías");
            _dgvHabitaciones.Columns.Add("noches", "Noches ocupadas");
            _dgvHabitaciones.Columns.Add("ingresos", "Ingresos estimados");
            _dgvHabitaciones.CellFormatting += FormatearCeldaEstado;

            _panelTablaTipos = new PanelReporte(_toolTip, "Detalle por tipo de habitación",
                "Resumen numérico de cada tipo. La ocupación se pinta de verde si alcanza la meta y de ámbar si queda por debajo.",
                _dgvTipos, new Size(1200, 300)) { TextoPie = "Ordenado de mayor a menor ingreso." };
            _panelTablaHabitaciones = new PanelReporte(_toolTip, "Detalle por habitación",
                "Todas las habitaciones incluidas en los filtros, con lo que generaron en el período. Las que no tuvieron estadías aparecen al final.",
                _dgvHabitaciones, new Size(1200, 420)) { TextoPie = "Ordenado de mayor a menor ingreso. Usá la rueda del mouse para recorrer la lista." };

            // ================= Armado =================
            _raiz.Controls.Add(_encabezado);
            _raiz.Controls.Add(_tarjetaFiltros);
            _raiz.Controls.Add(TituloSeccion("Indicadores clave", "Los números más importantes del período, comparados automáticamente."));
            _raiz.Controls.Add(_filaKpis);
            _raiz.Controls.Add(_insights);
            _raiz.Controls.Add(TituloSeccion("Operación", "Situación actual de las habitaciones y las más rentables."));
            _raiz.Controls.Add(_filaOperacion);
            _raiz.Controls.Add(TituloSeccion("Tablas detalladas", "Para quien necesite ver todos los números."));
            _raiz.Controls.Add(_panelTablaTipos);
            _raiz.Controls.Add(_panelTablaHabitaciones);

            Controls.Add(_raiz);
            _raiz.Resize += (s, e) => Acomodar();
        }

        // =====================================================================
        // Carga de datos
        // =====================================================================

        public void Refrescar()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                CargarCatalogos();

                PeriodoReporte periodo = ObtenerPeriodo();
                FiltroReporte filtro = ObtenerFiltro();
                ReporteHotel reporte = _gestionReportes.Generar(periodo, filtro);

                _reporteActual = reporte;
                _periodoActual = periodo;
                _filtroActual = DescribirFiltro(periodo, filtro);

                MostrarEncabezado(reporte, periodo, filtro);
                MostrarKpis(reporte);
                _insights.Cargar(reporte.Insights);
                MostrarOperacion(reporte);
                MostrarTablas(reporte);
                Acomodar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo generar el reporte. Verifique la conexión con la base de datos e intente nuevamente.\n\nDetalle: {ex.Message}",
                    "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void CargarCatalogos()
        {
            if (_catalogosCargados) return;

            var (tipos, estados, formasPago) = _gestionReportes.ObtenerCatalogosFiltros();
            _suspenderEventos = true;

            _cmbTipo.Items.Add(new OpcionFiltro("Todos los tipos", null));
            foreach (TipoHabitacion t in tipos.OrderBy(t => t.Descripcion)) _cmbTipo.Items.Add(new OpcionFiltro(t.Descripcion, t.IdTipoHabitacion));
            _cmbTipo.SelectedIndex = 0;

            _cmbEstado.Items.Add(new OpcionFiltro("Todos los estados", null));
            foreach (EstadoHabitacion e in estados) _cmbEstado.Items.Add(new OpcionFiltro(e.NomEstadoHabitacion, e.IdEstado));
            _cmbEstado.SelectedIndex = 0;

            _cmbPago.Items.Add(new OpcionFiltro("Todas las formas de pago", null));
            foreach (MetodoPago m in formasPago.OrderBy(m => m.NomMetodoPago)) _cmbPago.Items.Add(new OpcionFiltro(m.NomMetodoPago, m.IdMetodo));
            _cmbPago.SelectedIndex = 0;

            _suspenderEventos = false;
            _catalogosCargados = true;
        }

        private PeriodoReporte ObtenerPeriodo()
        {
            RangoPredefinido rango = _selectorPeriodo.Seleccionada switch
            {
                "hoy" => RangoPredefinido.Hoy,
                "semana" => RangoPredefinido.EstaSemana,
                "trimestre" => RangoPredefinido.UltimoTrimestre,
                "anio" => RangoPredefinido.AnioEnCurso,
                "personalizado" => RangoPredefinido.Personalizado,
                _ => RangoPredefinido.EsteMes
            };

            PeriodoReporte periodo = PeriodoReporte.Crear(rango, DateTime.Today, _dtpDesde.Value, _dtpHasta.Value);

            // Si eligió las fechas al revés, se corrigen también en pantalla.
            if (rango == RangoPredefinido.Personalizado && _dtpDesde.Value.Date > _dtpHasta.Value.Date)
            {
                _dtpDesde.Value = periodo.Desde;
                _dtpHasta.Value = periodo.Hasta;
            }

            return periodo;
        }

        private FiltroReporte ObtenerFiltro() => new()
        {
            IdTipoHabitacion = (_cmbTipo.SelectedItem as OpcionFiltro)?.Id,
            IdEstado = (_cmbEstado.SelectedItem as OpcionFiltro)?.Id,
            IdMetodo = (_cmbPago.SelectedItem as OpcionFiltro)?.Id,
            TipoHuesped = (TipoHuespedFiltro)((_cmbHuesped.SelectedItem as OpcionFiltro)?.Id ?? 0)
        };

        private void CambioDePeriodo()
        {
            bool personalizado = _selectorPeriodo.Seleccionada == "personalizado";
            _panelRango.Visible = personalizado;
            Acomodar();
            Refrescar();
        }

        private void LimpiarFiltros()
        {
            _suspenderEventos = true;
            _cmbTipo.SelectedIndex = 0;
            _cmbEstado.SelectedIndex = 0;
            _cmbHuesped.SelectedIndex = 0;
            _cmbPago.SelectedIndex = 0;
            _suspenderEventos = false;
            Refrescar();
        }

        // =====================================================================
        // Pintado de cada bloque
        // =====================================================================

        private void MostrarEncabezado(ReporteHotel r, PeriodoReporte periodo, FiltroReporte filtro)
        {
            _lblPeriodo.Text = $"{periodo.Nombre}  ·  {DescribirRango(r.Desde, r.Hasta)}";

            string comparacion = $"Comparado con {r.EtiquetaComparacion} ({DescribirRango(r.DesdeAnterior, r.HastaAnterior)})";
            int cantidadFiltros = new[] { filtro.IdTipoHabitacion.HasValue, filtro.IdEstado.HasValue, filtro.IdMetodo.HasValue, filtro.TipoHuesped != TipoHuespedFiltro.Todos }.Count(f => f);
            _lblComparacion.Text = cantidadFiltros == 0
                ? $"{comparacion}  ·  Sin filtros: se muestran las {r.TotalHabitaciones} habitaciones del hotel"
                : $"{comparacion}  ·  {cantidadFiltros} {(cantidadFiltros == 1 ? "filtro activo" : "filtros activos")} ({r.TotalHabitaciones} habitaciones incluidas)";

            _lnkLimpiar.Visible = filtro.HayFiltros;
        }

        private void MostrarKpis(ReporteHotel r)
        {
            string comp = r.EtiquetaComparacion;
            string meta = $"la meta ({r.MetaOcupacionPorcentaje:0}%)";

            _kpiOcupacionActual.Configurar(
                "Ocupación actual",
                $"{r.OcupacionActualPorcentaje:0}%",
                "¿Qué significa? Porcentaje de habitaciones que tienen huéspedes en este preciso momento.\nNo depende del período elegido: es la foto de hoy.",
                Icono.Cama, Paleta.Primario,
                Comparativa.EnPuntos(r.OcupacionActualPorcentaje, r.MetaOcupacionPorcentaje, meta, esMeta: true),
                $"{r.HabitacionesOcupadasAhora} de {r.TotalHabitaciones} habitaciones ocupadas ahora");

            _kpiIngresos.Configurar(
                "Ingresos del período",
                Moneda(r.TotalIngresos),
                "¿Qué significa? Lo que facturaron las estadías del período, calculado con la tarifa vigente por noche de cada tipo de habitación.\nNo incluye ventas de productos.",
                Icono.Etiqueta, Paleta.Exito,
                Comparativa.Porcentual((double)r.TotalIngresos, (double)r.TotalIngresosAnterior, comp),
                $"{comp[..1].ToUpper()}{comp[1..]}: {Moneda(r.TotalIngresosAnterior)}");

            _kpiOcupacionPeriodo.Configurar(
                "Ocupación promedio del período",
                $"{r.PromedioOcupacionPorcentaje:0}%",
                $"¿Qué significa? De todas las noches disponibles en el período, qué porcentaje se ocupó.\nEjemplo: 70% = de cada 10 habitaciones, 7 tuvieron huéspedes en promedio. La meta del hotel es {r.MetaOcupacionPorcentaje:0}%.",
                Icono.Grafico, ColoresReporte.Categoricos[3],
                Comparativa.EnPuntos(r.PromedioOcupacionPorcentaje, r.PromedioOcupacionAnteriorPorcentaje, comp),
                ContextoMeta(r.PromedioOcupacionPorcentaje, r.MetaOcupacionPorcentaje));

            _kpiPromedioEstadia.Configurar(
                "Promedio de estadía",
                Noches(r.PromedioDuracionEstadiaDias),
                "¿Qué significa? Cuántas noches se queda, en promedio, cada huésped.\nEstadías más largas suelen significar menos limpieza y más ingresos por check-in.",
                Icono.Reloj, ColoresReporte.Categoricos[1],
                r.TotalOcupaciones == 0 ? null : Comparativa.Porcentual(r.PromedioDuracionEstadiaDias, r.PromedioDuracionEstadiaAnteriorDias, comp),
                r.PromedioLimpiezaMinutos > 0 ? $"Limpieza promedio por habitación: {r.PromedioLimpiezaMinutos:0} min" : "Sin registros de limpieza en el período");

            _kpiEstadias.Configurar(
                "Estadías registradas",
                r.TotalOcupaciones.ToString("N0", CulturaAr),
                "¿Qué significa? Cantidad de check-ins (estadías) que estuvieron activas en algún momento del período.",
                Icono.Personas, ColoresReporte.Categoricos[2],
                Comparativa.Porcentual(r.TotalOcupaciones, r.TotalOcupacionesAnterior, comp),
                r.HuespedesDistintos == 1 ? "1 huésped distinto" : $"{r.HuespedesDistintos} huéspedes distintos");

            bool hayMantenimiento = r.HabitacionesEnMantenimiento > 0;
            _kpiMantenimiento.Configurar(
                "Habitaciones en mantenimiento",
                r.HabitacionesEnMantenimiento.ToString(),
                "¿Qué significa? Habitaciones que hoy están fuera de servicio por reparaciones: no se pueden ofrecer a nuevos huéspedes.\nLo ideal es que este número sea 0.",
                Icono.Engranaje, hayMantenimiento ? EstiloReporte.Advertencia : Paleta.Mantenimiento,
                hayMantenimiento
                    ? new Comparativa("Atención", "reducen la capacidad de venta", TonoEtiqueta.Advertencia)
                    : new Comparativa("✓ Todo en orden", "ninguna fuera de servicio", TonoEtiqueta.Exito),
                r.TotalHabitaciones == 0 ? "Sin habitaciones en el filtro" : $"{r.HabitacionesEnMantenimiento * 100.0 / r.TotalHabitaciones:0}% de las {r.TotalHabitaciones} habitaciones",
                alerta: hayMantenimiento);
        }

        private void MostrarOperacion(ReporteHotel r)
        {
            _barraEstados.Cargar(r.EstadoActualHabitaciones.Select(s => (s.Etiqueta, (int)s.Valor, ColoresReporte.DeEstado(s.Etiqueta))));

            _rankingHabitaciones.Cargar(
                r.IngresosPorHabitacion.Take(6).Select(p => (p.Etiqueta, p.Valor, Moneda((decimal)p.Valor))),
                "No hubo ingresos en el período con los filtros elegidos.");
        }

        private void MostrarTablas(ReporteHotel r)
        {
            _dgvTipos.Rows.Clear();
            foreach (ResumenTipoHabitacion t in r.DetallePorTipo)
            {
                int fila = _dgvTipos.Rows.Add(
                    t.Tipo,
                    t.Habitaciones,
                    t.Estadias,
                    t.NochesOcupadas,
                    $"{t.OcupacionPorcentaje:0}%",
                    Moneda(t.Ingresos),
                    t.Estadias > 0 ? Moneda(t.IngresoPromedioPorEstadia) : "—",
                    $"{t.ParticipacionIngresosPorcentaje:0}%");
                _dgvTipos.Rows[fila].Cells["ocupacion"].Tag = t.OcupacionPorcentaje >= r.MetaOcupacionPorcentaje;
            }

            if (r.DetallePorTipo.Count > 0)
            {
                int fila = _dgvTipos.Rows.Add("Total", r.TotalHabitaciones, r.TotalOcupaciones, r.DetallePorTipo.Sum(t => t.NochesOcupadas),
                    $"{r.PromedioOcupacionPorcentaje:0}%", Moneda(r.TotalIngresos),
                    r.TotalOcupaciones > 0 ? Moneda(r.TotalIngresos / r.TotalOcupaciones) : "—", r.TotalIngresos > 0 ? "100%" : "0%");
                _dgvTipos.Rows[fila].DefaultCellStyle.Font = Paleta.FuenteBaseNegrita;
                _dgvTipos.Rows[fila].DefaultCellStyle.BackColor = Paleta.FondoApp;
                _dgvTipos.Rows[fila].Cells["ocupacion"].Tag = r.PromedioOcupacionPorcentaje >= r.MetaOcupacionPorcentaje;
            }

            _dgvHabitaciones.Rows.Clear();
            foreach (ResumenHabitacion h in r.DetallePorHabitacion)
            {
                _dgvHabitaciones.Rows.Add($"Hab. {h.NroHabitacion}", h.Tipo, h.EstadoActual, h.Estadias, h.NochesOcupadas, Moneda(h.Ingresos));
            }

            _dgvTipos.ClearSelection();
            _dgvHabitaciones.ClearSelection();
        }

        // =====================================================================
        // Layout responsivo
        // =====================================================================

        private void Acomodar()
        {
            if (_raiz == null) return;

            int ancho = Math.Max(480, _raiz.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            _raiz.SuspendLayout();

            _encabezado.Width = ancho;
            foreach (Control titulo in _titulosSeccion) titulo.Width = ancho;

            // Filtros: alto según cuántas líneas ocupan los controles al ancho disponible
            int anchoInterno = ancho - _tarjetaFiltros.Padding.Horizontal;
            _filaPeriodo.MaximumSize = new Size(anchoInterno, 0);
            _filaFiltros.MaximumSize = new Size(anchoInterno, 0);
            int altoFiltros = _filaPeriodo.GetPreferredSize(new Size(anchoInterno, 0)).Height
                            + _filaFiltros.GetPreferredSize(new Size(anchoInterno, 0)).Height + _filaFiltros.Margin.Vertical;
            _tarjetaFiltros.Size = new Size(ancho, altoFiltros + _tarjetaFiltros.Padding.Vertical + 4);

            int columnasKpi = ancho >= 1500 ? 6 : ancho >= 760 ? 3 : 2;
            DistribuirEnFila(_filaKpis, ancho, columnasKpi, 158);

            _insights.Width = ancho;

            DistribuirEnFila(_filaOperacion, ancho, ancho >= 900 ? 2 : 1, 360);

            _panelTablaTipos.Size = new Size(ancho, AltoTabla(_dgvTipos, 8));
            _panelTablaHabitaciones.Size = new Size(ancho, AltoTabla(_dgvHabitaciones, 10));

            _raiz.ResumeLayout(true);
        }

        /// <summary>Reparte los controles de una fila en N columnas iguales con separación fija.</summary>
        private static void DistribuirEnFila(FlowLayoutPanel fila, int ancho, int columnas, int alto)
        {
            int n = fila.Controls.Count;
            int anchoCelda = (ancho - Separacion * (columnas - 1)) / columnas;

            for (int i = 0; i < n; i++)
            {
                Control c = fila.Controls[i];
                bool ultimaColumna = i % columnas == columnas - 1;
                c.Margin = new Padding(0, 0, ultimaColumna ? 0 : Separacion, Separacion);
                c.Size = new Size(anchoCelda, alto);
            }

            int filas = (int)Math.Ceiling(n / (double)columnas);
            fila.Size = new Size(ancho, filas * (alto + Separacion));
        }

        private static int AltoTabla(DataGridView dgv, int filasVisibles)
        {
            int filas = Math.Max(1, Math.Min(filasVisibles, dgv.Rows.Count));
            // 34 encabezado + 48 pie + 26 de márgenes internos del PanelReporte
            return 34 + 48 + 26 + dgv.ColumnHeadersHeight + filas * dgv.RowTemplate.Height + 4;
        }

        // =====================================================================
        // Fábricas de controles
        // =====================================================================

        private static FlowLayoutPanel CrearFila(params Control[] controles)
        {
            var fila = new FlowLayoutPanel { WrapContents = true, BackColor = Paleta.FondoApp, Margin = Padding.Empty };
            fila.Controls.AddRange(controles);
            return fila;
        }

        private Control TituloSeccion(string titulo, string descripcion)
        {
            var panel = new Panel { Height = 46, BackColor = Paleta.FondoApp, Margin = new Padding(0, 6, 0, 4) };
            panel.Controls.Add(new Label { Text = titulo.ToUpper(CulturaAr), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f), ForeColor = Paleta.TextoSecundario, Location = new Point(0, 4) });
            panel.Controls.Add(new Label { Text = descripcion, AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, Location = new Point(0, 24) });
            _titulosSeccion.Add(panel);
            return panel;
        }

        private static Label CrearEtiquetaFiltro(string texto, int ancho, int margenSuperior) => new()
        {
            Text = texto,
            AutoSize = false,
            Size = new Size(ancho, 22),
            Font = Paleta.FuenteBaseNegrita,
            ForeColor = Paleta.TextoPrimario,
            Margin = new Padding(0, margenSuperior, 4, 0)
        };

        private ComboBox CrearComboFiltro()
        {
            ComboBox combo = CamposFormulario.Combo(Point.Empty, 210);
            combo.SelectedIndexChanged += (s, e) =>
            {
                if (!_suspenderEventos && _catalogosCargados) Refrescar();
            };
            return combo;
        }

        /// <summary>Etiqueta arriba + combo abajo, con la «i» de ayuda al lado de la etiqueta.</summary>
        private Control EnvolverFiltro(string titulo, ComboBox combo, string ayuda)
        {
            var panel = new Panel { Size = new Size(combo.Width + 2, 64), Margin = new Padding(0, 0, 14, 6), BackColor = Paleta.FondoTarjeta };
            var lbl = new Label { Text = titulo, AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoSecundario, Location = new Point(0, 6) };
            var icono = new IconoAyuda(_toolTip, ayuda) { Size = new Size(18, 18) };
            panel.Controls.Add(lbl);
            panel.Controls.Add(icono);
            icono.Location = new Point(Math.Min(panel.Width - 18, lbl.PreferredWidth + 4), 4);
            combo.Location = new Point(0, 28);
            panel.Controls.Add(combo);
            return panel;
        }

        private static DataGridView CrearGrilla()
        {
            var dgv = new DataGridView { AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Vertical };
            EstiloGrid.Aplicar(dgv);
            dgv.RowTemplate.Height = 38;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 40;
            return dgv;
        }

        private void FormatearCeldaOcupacion(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _dgvTipos.Columns[e.ColumnIndex].Name != "ocupacion" || e.CellStyle == null) return;
            if (_dgvTipos.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is bool alcanzaMeta)
            {
                e.CellStyle.ForeColor = alcanzaMeta ? Paleta.Exito : EstiloReporte.Advertencia;
                e.CellStyle.Font = Paleta.FuenteBaseNegrita;
            }
        }

        private void FormatearCeldaEstado(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _dgvHabitaciones.Columns[e.ColumnIndex].Name != "estado" || e.CellStyle == null || e.Value is not string estado) return;
            Color color = ColoresReporte.DeEstado(estado);
            e.CellStyle.ForeColor = color == Paleta.Limpieza ? EstiloReporte.Advertencia : color;
            e.CellStyle.Font = Paleta.FuenteBaseNegrita;
        }

        // =====================================================================
        // Formatos de texto
        // =====================================================================

        private static string Moneda(decimal valor) => valor.ToString("C0", CulturaAr);

        private static string Noches(double valor) => valor <= 0
            ? "0 noches"
            : $"{valor.ToString("0.#", CulturaAr)} {(Math.Abs(valor - 1) < 0.05 ? "noche" : "noches")}";

        private static string ContextoMeta(double valor, double meta)
        {
            double diferencia = valor - meta;
            if (Math.Abs(diferencia) < 0.5) return $"Justo en la meta del {meta:0}%";
            return diferencia > 0
                ? $"▲ {diferencia:0} pts por encima de la meta ({meta:0}%)"
                : $"▼ {-diferencia:0} pts por debajo de la meta ({meta:0}%)";
        }

        private static string DescribirRango(DateTime desde, DateTime hasta)
        {
            if (desde == hasta) return desde.ToString("dddd d 'de' MMMM yyyy", CulturaAr);
            if (desde.Year == hasta.Year && desde.Month == hasta.Month) return $"{desde.Day} al {hasta.ToString("d 'de' MMMM yyyy", CulturaAr)}";
            if (desde.Year == hasta.Year) return $"{desde.ToString("d 'de' MMMM", CulturaAr)} al {hasta.ToString("d 'de' MMMM yyyy", CulturaAr)}";
            return $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        }

        private string DescribirFiltro(PeriodoReporte periodo, FiltroReporte filtro)
        {
            var partes = new List<string> { $"{periodo.Nombre} ({periodo.Desde:dd/MM/yyyy} - {periodo.Hasta:dd/MM/yyyy})" };
            if (filtro.IdTipoHabitacion.HasValue) partes.Add($"Tipo: {_cmbTipo.Text}");
            if (filtro.IdEstado.HasValue) partes.Add($"Estado actual: {_cmbEstado.Text}");
            if (filtro.TipoHuesped != TipoHuespedFiltro.Todos) partes.Add($"Huéspedes: {_cmbHuesped.Text}");
            if (filtro.IdMetodo.HasValue) partes.Add($"Pago: {_cmbPago.Text}");
            return string.Join(" · ", partes);
        }

        // =====================================================================
        // Exportación a PDF
        // =====================================================================

        private void ExportarPdf()
        {
            if (!GeneradorReportePdf.TieneDatos(_reporteActual))
            {
                MessageBox.Show(
                    "No hay datos para exportar: el reporte actual no registra estadías en el período y filtros seleccionados.\n\nElija otro período o quite algún filtro e intente nuevamente.",
                    "Sin datos para exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Exportar reporte a PDF",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_Hotel_{_periodoActual?.Desde:yyyy-MM-dd}_a_{_periodoActual?.Hasta:yyyy-MM-dd}.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                OverwritePrompt = true,
                RestoreDirectory = true
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                string? generadoPor = _usuario == null ? null : $"{_usuario.NomUsuario} {_usuario.ApeUsuario}".Trim();
                GeneradorReportePdf.GenerarArchivo(_reporteActual!, sfd.FileName, _filtroActual, generadoPor);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"No se pudo guardar el archivo PDF. Verifique que no esté abierto en otro programa.\n\nDetalle: {ex.Message}",
                    "Error al guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No tiene permisos para guardar en la ubicación seleccionada. Elija otra carpeta e intente nuevamente.",
                    "Error al guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al generar el reporte PDF: {ex.Message}",
                    "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            var respuesta = MessageBox.Show(
                $"¡El reporte se exportó correctamente!\n\nUbicación:\n{sfd.FileName}\n\n¿Desea abrir el archivo ahora?",
                "Reporte exportado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (respuesta == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"El archivo se guardó, pero no se pudo abrir: {ex.Message}", "Abrir PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // =====================================================================
        // Tipos auxiliares
        // =====================================================================

        /// <summary>Opción de un combo de filtro: texto visible + id (null = "Todos").</summary>
        private sealed record OpcionFiltro(string Texto, int? Id)
        {
            public override string ToString() => Texto;
        }

        /// <summary>Contenedor blanco con borde redondeado para la barra de filtros.</summary>
        private sealed class TarjetaFiltros : Panel
        {
            public TarjetaFiltros()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Paleta.FondoApp;
            }

            protected override void OnPaint(PaintEventArgs e) => EstiloReporte.DibujarTarjeta(e.Graphics, Size, Paleta.Borde);
        }
    }
}


