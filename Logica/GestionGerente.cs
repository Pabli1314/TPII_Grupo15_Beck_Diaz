using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>
    /// Estadísticas consumidas por las pantallas de Gerencia (dashboard ejecutivo, panel financiero,
    /// análisis de habitaciones e históricos de caja).
    ///
    /// Criterio de ingresos de alojamiento (devengado por noche): cada estadía genera la tarifa por día
    /// del tipo de habitación (Tipo_habitacion.tarifa, la misma que cobra el check-in) por cada noche
    /// que ocupa. Una noche pertenece al día en que empieza; la estadía de un mismo día cuenta como una
    /// noche. Así ocupación, ADR y RevPAR salen de las mismas noches y un período nunca se lleva los
    /// ingresos de las noches de otro.
    /// </summary>
    public class GestionGerente
    {
        /// <summary>Misma meta de ocupación que usan los Reportes del Administrador.</summary>
        public const double MetaOcupacionPorcentaje = GestionReportes.MetaOcupacionPorcentaje;

        private const string RolRecepcionista = "Recepcionista";
        private const string LineaAlojamiento = "Alojamiento";
        private const string LineaSinCategoria = "Otros productos";

        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        private static readonly string[] ORDEN_ESTADOS = { "Disponible", "Ocupada", "Limpieza", "Mantenimiento" };

        // Mismo orden en que las pantallas de Gerencia muestran las tarjetas por tipo de
        // habitación; no se puede asumir que coincida con el orden por id_tipo_habitacion.
        private static readonly string[] ORDEN_TIPOS = { "Individual", "Estandar", "Doble Matrimonial", "Suite Executive" };

        private readonly GestionTurnoCaja _gestionTurnoCaja = new GestionTurnoCaja();

        // =====================================================================
        // Dashboard ejecutivo
        // =====================================================================

        /// <summary>
        /// KPIs del mes en curso (del día 1 a hoy) comparados con los mismos días del mes anterior y
        /// del mismo mes del año anterior, más la tendencia de los últimos 12 meses.
        /// </summary>
        public IndicadoresGerencia ObtenerIndicadoresMes()
        {
            DateTime hoy = DateTime.Today;
            DateTime inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime inicioTendencia = inicioMes.AddMonths(-11);
            DateTime inicioConsulta = new[] { inicioTendencia, inicioMes.AddYears(-1) }.Min();

            var datos = DatosOperativos.Cargar(inicioConsulta, hoy);

            var indicadores = new IndicadoresGerencia
            {
                MetaOcupacion = MetaOcupacionPorcentaje,
                Actual = datos.Metricas(inicioMes, hoy),
                // AddMonths/AddYears ajustan solos el 31 → 30/28 y el 29/02.
                MesAnterior = datos.Metricas(inicioMes.AddMonths(-1), hoy.AddMonths(-1)),
                AnioAnterior = datos.Metricas(inicioMes.AddYears(-1), hoy.AddYears(-1))
            };

            for (DateTime mes = inicioTendencia; mes <= inicioMes; mes = mes.AddMonths(1))
            {
                DateTime fin = mes == inicioMes ? hoy : mes.AddMonths(1).AddDays(-1);
                MetricasPeriodo m = datos.Metricas(mes, fin);
                indicadores.Tendencia.Add(new PuntoMensual
                {
                    Etiqueta = Capitalizar(mes.ToString("MMM yy", CulturaAr).Replace(".", string.Empty)),
                    Alojamiento = m.IngresosAlojamiento,
                    Ventas = m.IngresosVentas,
                    Ocupacion = m.Ocupacion,
                    EnCurso = mes == inicioMes
                });
            }

            return indicadores;
        }

        // =====================================================================
        // Panel financiero y flujo de caja
        // =====================================================================

        /// <summary>
        /// Ingresos del mes en curso: evolución diaria acumulada contra el mes anterior, proyección al
        /// cierre, desglose por línea de negocio y por medio de pago, y la situación de las cajas.
        /// </summary>
        public PanelFinanciero ObtenerPanelFinanciero()
        {
            DateTime hoy = DateTime.Today;
            DateTime inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);
            DateTime inicioMesAnterior = inicioMes.AddMonths(-1);
            DateTime finMesAnterior = inicioMes.AddDays(-1);

            // Se consulta hasta fin de mes para conocer las noches ya comprometidas por las estadías en curso.
            var datos = DatosOperativos.Cargar(inicioMesAnterior, finMes);

            var panel = new PanelFinanciero
            {
                Desde = inicioMes,
                Hasta = hoy,
                FinMes = finMes,
                Actual = datos.Metricas(inicioMes, hoy),
                MesAnteriorMismosDias = datos.Metricas(inicioMesAnterior, hoy.AddMonths(-1)),
                MesAnteriorCompleto = datos.Metricas(inicioMesAnterior, finMesAnterior).IngresosTotales
            };

            // --- Proyección al cierre: lo ya ganado + noches ya reservadas por estadías en curso + ventas al ritmo actual ---
            int diasTranscurridos = (hoy - inicioMes).Days + 1;
            panel.VentasPromedioDiario = panel.Actual.IngresosVentas / diasTranscurridos;
            panel.Comprometido = datos.AlojamientoComprometido(hoy.AddDays(1), finMes, hoy);
            panel.ProyeccionCierre = panel.Actual.IngresosTotales + panel.Comprometido + panel.VentasPromedioDiario * (finMes - hoy).Days;

            // --- Flujo diario acumulado ---
            decimal realAcumulado = 0, proyectadoAcumulado = 0, anteriorAcumulado = 0;
            int diasMesAnterior = (finMesAnterior - inicioMesAnterior).Days + 1;
            int diasGrafico = Math.Max(finMes.Day, diasMesAnterior);

            for (int dia = 1; dia <= diasGrafico; dia++)
            {
                var punto = new PuntoFlujo { Dia = dia };

                if (dia <= diasMesAnterior)
                {
                    anteriorAcumulado += datos.IngresosDelDia(inicioMesAnterior.AddDays(dia - 1));
                    punto.MesAnterior = anteriorAcumulado;
                }

                if (dia <= finMes.Day)
                {
                    DateTime fecha = inicioMes.AddDays(dia - 1);
                    if (fecha <= hoy)
                    {
                        realAcumulado += datos.IngresosDelDia(fecha);
                        punto.Real = realAcumulado;
                        proyectadoAcumulado = realAcumulado;
                        // La proyección arranca en el último dato real para que las dos líneas se unan.
                        if (fecha == hoy) punto.Proyectado = proyectadoAcumulado;
                    }
                    else
                    {
                        proyectadoAcumulado += datos.AlojamientoComprometido(fecha, fecha, hoy) + panel.VentasPromedioDiario;
                        punto.Proyectado = proyectadoAcumulado;
                    }
                }

                panel.Flujo.Add(punto);
            }

            // --- Líneas de negocio (alojamiento + cada categoría de producto vendida) ---
            Dictionary<string, decimal> lineasActual = datos.IngresosPorLinea(inicioMes, hoy);
            Dictionary<string, decimal> lineasAnterior = datos.IngresosPorLinea(inicioMesAnterior, hoy.AddMonths(-1));
            panel.Lineas = lineasActual.Keys.Union(lineasAnterior.Keys)
                .Select(nombre => new LineaNegocio
                {
                    Nombre = nombre,
                    Actual = lineasActual.GetValueOrDefault(nombre),
                    Anterior = lineasAnterior.GetValueOrDefault(nombre)
                })
                .OrderBy(l => l.Nombre == LineaAlojamiento ? 0 : 1)
                .ThenByDescending(l => l.Actual)
                .ThenBy(l => l.Nombre)
                .ToList();

            panel.MediosDePago = datos.IngresosPorMedio(inicioMes, hoy)
                .Select(kv => new MontoPorMedio { Medio = kv.Key, Monto = kv.Value })
                .OrderByDescending(m => m.Monto)
                .ToList();

            // --- Cajas: turnos abiertos ahora y arqueos del mes ---
            List<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos();
            Dictionary<string, string> nombres = UsuarioDAO.ObtenerTodos()
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .GroupBy(u => u.DniUsuario.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().NomUsuario} {g.First().ApeUsuario}".Trim());

            foreach (TurnoCaja turno in turnos.Where(t => t.FechaCierre == null).OrderBy(t => t.FechaApertura).ThenBy(t => t.HoraApertura))
            {
                ResumenTurno resumen = _gestionTurnoCaja.ObtenerResumen(turno.IdTurno);
                string dni = (turno.DniUsuario ?? string.Empty).Trim();
                panel.CajasAbiertas.Add(new CajaAbierta
                {
                    IdTurno = turno.IdTurno,
                    Usuario = nombres.TryGetValue(dni, out string? nombre) && nombre.Length > 0 ? nombre : $"DNI {dni}",
                    Apertura = turno.FechaApertura.Date + turno.HoraApertura,
                    MontoInicial = turno.MontoInicial,
                    Cobrado = resumen.TotalAlojamiento + resumen.TotalVentas,
                    EfectivoEsperado = resumen.EfectivoEsperado
                });
            }

            // Mismo cálculo que la pantalla de Históricos, para que las dos muestren las mismas diferencias.
            HistorialOperativoResultado cerradosMes = ConsultarHistorialOperativo(inicioMes, hoy, null, soloRecepcionistas: false);
            panel.TurnosCerradosMes = cerradosMes.TotalTurnosCerrados;
            panel.EfectivoArqueadoMes = cerradosMes.EfectivoArqueado;
            panel.TurnosConDiferencia = cerradosMes.TurnosConDiferencia;

            return panel;
        }

        // =====================================================================
        // Análisis de habitaciones e históricos (sin cambios de criterio)
        // =====================================================================

        /// <summary>Vector de 4 posiciones por estado (Disponible, Ocupada, Limpieza, Mantenimiento)
        /// y vector de 4 posiciones por tipo de habitación.</summary>
        public (int[] vectorEstados, int[] vectorTipos) ObtenerEstadisticasHabitaciones()
        {
            List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();
            List<EstadoHabitacion> estados = EstadoHabitacionDAO.ObtenerTodos();
            List<TipoHabitacion> tipos = TipoHabitacionDAO.ObtenerTodos();

            int[] vectorEstados = new int[4];
            for (int i = 0; i < ORDEN_ESTADOS.Length; i++)
            {
                EstadoHabitacion? estado = estados.FirstOrDefault(e => CoincideNombre(e.NomEstadoHabitacion, ORDEN_ESTADOS[i]));
                vectorEstados[i] = estado == null ? 0 : habitaciones.Count(h => h.IdEstado == estado.IdEstado);
            }

            int[] vectorTipos = new int[4];
            for (int i = 0; i < ORDEN_TIPOS.Length; i++)
            {
                TipoHabitacion? tipo = tipos.FirstOrDefault(t => CoincideNombre(t.Descripcion, ORDEN_TIPOS[i]));
                vectorTipos[i] = tipo == null ? 0 : habitaciones.Count(h => h.IdTipoHabitacion == tipo.IdTipoHabitacion);
            }

            return (vectorEstados, vectorTipos);
        }

        public List<Habitacion> ConsultarHabitacionesPorEstado(string estado)
        {
            EstadoHabitacion? estadoEncontrado = EstadoHabitacionDAO.ObtenerTodos()
                .FirstOrDefault(e => CoincideEstado(e.NomEstadoHabitacion, estado));

            if (estadoEncontrado == null)
            {
                return new List<Habitacion>();
            }

            return HabitacionDAO.ObtenerTodas()
                .Where(h => h.IdEstado == estadoEncontrado.IdEstado)
                .ToList();
        }

        /// <summary>Nombre real de cada tipo de habitación (según la tabla Tipo_habitacion) por id, para
        /// no depender de que el id coincida con una posición fija en una lista hardcodeada.</summary>
        public Dictionary<int, string> ObtenerNombresTiposHabitacion()
        {
            return TipoHabitacionDAO.ObtenerTodos()
                .ToDictionary(t => t.IdTipoHabitacion, t => t.Descripcion);
        }

        /// <summary>
        /// Recepcionistas para el filtro de Históricos (DNI → "Apellido Nombre"): todos los activos, aunque
        /// todavía no tengan turnos (recién creados), más los dados de baja que alguna vez operaron una caja.
        /// </summary>
        public List<KeyValuePair<string, string>> ObtenerListaUsuarios()
        {
            HashSet<string> conTurnos = TurnoCajaDAO.ObtenerTodos()
                .Select(t => (t.DniUsuario ?? string.Empty).Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return UsuarioDAO.ObtenerTodos()
                .Where(EsRecepcionista)
                .Where(u => u.Estado || conTurnos.Contains(u.DniUsuario.Trim()))
                .Select(u => new KeyValuePair<string, string>(u.DniUsuario.Trim(),
                    $"{u.ApeUsuario} {u.NomUsuario}".Trim() + (u.Estado ? string.Empty : " (inactivo)")))
                .OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Auditoría de los turnos de caja entre dos fechas: los cerrados en el período (por fecha de cierre)
        /// y los que siguen abiertos (abiertos hasta la fecha 'Hasta'), para que la actividad de un turno en
        /// curso se vea antes del cierre. Para cada turno recalcula lo cobrado (check-ins y ventas, por medio
        /// de pago) con el mismo cálculo que usa el cierre de caja y, si ya cerró, lo compara con el efectivo
        /// que se contó en el arqueo.
        /// </summary>
        /// <param name="soloRecepcionistas">true (Históricos): solo turnos de usuarios con rol Recepcionista,
        /// sin los que haya abierto otro rol (Gerente, Administrador). false: todos los turnos del hotel.</param>
        public HistorialOperativoResultado ConsultarHistorialOperativo(DateTime desde, DateTime hasta, string? dniUsuarioFiltro, bool soloRecepcionistas = true)
        {
            if (desde.Date > hasta.Date)
            {
                throw new ArgumentException("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
            }

            var resultado = new HistorialOperativoResultado { Desde = desde.Date, Hasta = hasta.Date };
            string? dniFiltro = string.IsNullOrWhiteSpace(dniUsuarioFiltro) ? null : dniUsuarioFiltro.Trim();
            HashSet<string>? recepcionistas = soloRecepcionistas ? DnisRecepcionistas() : null;

            List<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos()
                .Where(t => t.FechaCierre.HasValue
                    ? t.FechaCierre.Value.Date >= desde.Date && t.FechaCierre.Value.Date <= hasta.Date
                    : t.FechaApertura.Date <= hasta.Date)
                .Where(t => recepcionistas == null || recepcionistas.Contains((t.DniUsuario ?? string.Empty).Trim()))
                .Where(t => dniFiltro == null || string.Equals((t.DniUsuario ?? string.Empty).Trim(), dniFiltro, StringComparison.OrdinalIgnoreCase))
                // Primero los abiertos; después los cerrados, del cierre más reciente al más antiguo.
                .OrderBy(t => t.FechaCierre.HasValue ? 1 : 0)
                .ThenByDescending(t => t.FechaCierre.HasValue ? t.FechaCierre.Value.Date + (t.HoraCierre ?? TimeSpan.Zero) : t.FechaApertura.Date + t.HoraApertura)
                .ThenByDescending(t => t.IdTurno)
                .ToList();

            if (turnos.Count == 0)
            {
                return resultado;
            }

            // Una consulta por tabla para todo el período (no una por turno). Un turno abierto llega hasta hoy.
            DateTime primeraApertura = turnos.Min(t => t.FechaApertura.Date);
            DateTime ultimoCierre = turnos.Max(t => t.FechaCierre?.Date ?? DateTime.Today);
            HashSet<int> idsTurnos = turnos.Select(t => t.IdTurno).ToHashSet();

            Dictionary<string, string> nombres = NombresDeUsuarios();
            Dictionary<int, string> metodos = MetodoPagoDAO.ObtenerTodos().ToDictionary(m => m.IdMetodo, m => m.NomMetodoPago);
            Dictionary<int, decimal> tarifaPorTipo = TipoHabitacionDAO.ObtenerTodos().ToDictionary(t => t.IdTipoHabitacion, t => t.Tarifa);
            Dictionary<int, decimal> tarifaPorHabitacion = HabitacionDAO.ObtenerTodas()
                .ToDictionary(h => h.NroHabitacion, h => tarifaPorTipo.GetValueOrDefault(h.IdTipoHabitacion));

            // Un check-in cargado en un turno tiene su entrada dentro del turno, así que este rango lo incluye.
            ILookup<int, (Hospedaje, decimal)> hospedajesPorTurno = HospedajeDAO.ObtenerEnRango(primeraApertura, ultimoCierre)
                .Where(h => idsTurnos.Contains(h.IdTurno))
                .ToLookup(h => h.IdTurno, h => (h, tarifaPorHabitacion.GetValueOrDefault(h.NroHabitacion)));
            List<Venta> ventas = VentaDAO.ObtenerEnRango(primeraApertura, ultimoCierre);

            foreach (TurnoCaja turno in turnos)
            {
                List<Venta> ventasTurno = ventas.Where(v => GestionTurnoCaja.VentaPerteneceATurno(v, turno)).ToList();
                List<(Hospedaje, decimal)> checkIns = hospedajesPorTurno[turno.IdTurno].ToList();
                ResumenTurno r = GestionTurnoCaja.CalcularResumen(turno, checkIns, ventasTurno, metodos);

                string dni = (turno.DniUsuario ?? string.Empty).Trim();
                DateTime apertura = turno.FechaApertura.Date + turno.HoraApertura;
                DateTime? cierre = turno.FechaCierre.HasValue ? turno.FechaCierre.Value.Date + (turno.HoraCierre ?? TimeSpan.Zero) : null;

                resultado.Lista.Add(new TurnoHistorialItem
                {
                    IdTurno = turno.IdTurno,
                    DniUsuario = dni,
                    Usuario = nombres.TryGetValue(dni, out string? nombre) ? nombre : $"DNI {dni}",
                    Apertura = apertura,
                    Cierre = cierre,
                    MontoInicial = turno.MontoInicial,
                    CobradoEfectivo = r.TotalEfectivo,
                    CobradoTarjeta = r.TotalTarjeta,
                    CobradoTransferencia = r.TotalTransferencia,
                    CobradoAlojamiento = r.TotalAlojamiento,
                    CobradoVentas = r.TotalVentas,
                    CheckIns = checkIns.Count,
                    Ventas = ventasTurno.Count,
                    EfectivoEsperado = r.EfectivoEsperado,
                    // Un turno abierto todavía no tiene arqueo, aunque monto_final tuviera algún valor.
                    EfectivoContado = turno.FechaCierre.HasValue ? turno.MontoFinal : null,
                    Observaciones = turno.Observaciones ?? string.Empty
                });
            }

            resultado.PorRecepcionista = resultado.Lista
                .GroupBy(t => t.DniUsuario)
                .Select(g => new ResumenRecepcionista
                {
                    DniUsuario = g.Key,
                    Nombre = g.First().Usuario,
                    Turnos = g.Count(),
                    Cobrado = g.Sum(t => t.CobradoTotal),
                    TurnosConDiferencia = g.Count(t => t.TieneDiferencia),
                    Diferencia = g.Sum(t => t.Diferencia ?? 0)
                })
                .OrderByDescending(r => r.Cobrado)
                .ThenBy(r => r.Nombre)
                .ToList();

            return resultado;
        }

        /// <summary>DNI de los usuarios con rol Recepcionista (activos o no: un recepcionista dado de baja
        /// sigue figurando en los turnos que cerró).</summary>
        private static HashSet<string> DnisRecepcionistas() => UsuarioDAO.ObtenerTodos()
            .Where(EsRecepcionista)
            .Select(u => u.DniUsuario.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static bool EsRecepcionista(Usuario u) =>
            !string.IsNullOrWhiteSpace(u.DniUsuario) && string.Equals(u.Rol?.NomRol?.Trim(), RolRecepcionista, StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, string> NombresDeUsuarios() => UsuarioDAO.ObtenerTodos()
            .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
            .GroupBy(u => u.DniUsuario.Trim())
            .ToDictionary(g => g.Key, g => $"{g.First().ApeUsuario} {g.First().NomUsuario}".Trim());

        // =====================================================================
        // Datos operativos en memoria (una consulta por tabla, se filtran por período)
        // =====================================================================

        /// <summary>Hospedajes, ventas y catálogos de un rango ya consultados, con los cálculos por período.</summary>
        private sealed class DatosOperativos
        {
            private List<Hospedaje> _hospedajes = new();
            private List<VentaRealizada> _ventas = new();
            private Dictionary<int, decimal> _tarifaPorHabitacion = new();
            private Dictionary<string, string> _lineaPorProducto = new();
            private Dictionary<int, string> _nombreMetodo = new();
            private int _totalHabitaciones;

            public static DatosOperativos Cargar(DateTime desde, DateTime hasta)
            {
                List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();
                Dictionary<int, decimal> tarifaPorTipo = TipoHabitacionDAO.ObtenerTodos()
                    .ToDictionary(t => t.IdTipoHabitacion, t => t.Tarifa);
                Dictionary<int, string> categorias = CategoriaDAO.ObtenerTodas()
                    .ToDictionary(c => c.IdCategoria, c => c.Descripcion);

                return new DatosOperativos
                {
                    _hospedajes = HospedajeDAO.ObtenerEnRango(desde, hasta),
                    _ventas = VentaDAO.ObtenerRealizadas(desde: desde.Date)
                        .Where(v => v.FechaVenta.Date <= hasta.Date)
                        .ToList(),
                    _totalHabitaciones = habitaciones.Count,
                    _tarifaPorHabitacion = habitaciones.ToDictionary(
                        h => h.NroHabitacion,
                        h => tarifaPorTipo.GetValueOrDefault(h.IdTipoHabitacion)),
                    _lineaPorProducto = ProductoDAO.ObtenerTodos()
                        .GroupBy(p => p.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            g => g.Key,
                            g => categorias.TryGetValue(g.First().IdCategoria, out string? c) && !string.IsNullOrWhiteSpace(c) ? c.Trim() : LineaSinCategoria,
                            StringComparer.OrdinalIgnoreCase),
                    _nombreMetodo = MetodoPagoDAO.ObtenerTodos()
                        .ToDictionary(m => m.IdMetodo, m => string.IsNullOrWhiteSpace(m.NomMetodoPago) ? $"Medio {m.IdMetodo}" : m.NomMetodoPago.Trim())
                };
            }

            public MetricasPeriodo Metricas(DateTime desde, DateTime hasta)
            {
                var noches = Noches(desde, hasta).ToList();
                List<VentaRealizada> ventas = VentasEn(desde, hasta).ToList();

                return new MetricasPeriodo
                {
                    Desde = desde.Date,
                    Hasta = hasta.Date,
                    Habitaciones = _totalHabitaciones,
                    NochesVendidas = noches.Count,
                    Estadias = noches.Select(n => n.Hospedaje.IdHospedaje).Distinct().Count(),
                    IngresosAlojamiento = noches.Sum(n => n.Tarifa),
                    IngresosVentas = ventas.Sum(v => v.Total)
                };
            }

            /// <summary>Alojamiento + ventas de un día (para el flujo diario).</summary>
            public decimal IngresosDelDia(DateTime dia) =>
                Noches(dia, dia).Sum(n => n.Tarifa) + VentasEn(dia, dia).Sum(v => v.Total);

            /// <summary>
            /// Noches entre <paramref name="desde"/> y <paramref name="hasta"/> que ya están reservadas por
            /// estadías que siguen en curso a la fecha de <paramref name="hoy"/> (salida planificada futura).
            /// </summary>
            public decimal AlojamientoComprometido(DateTime desde, DateTime hasta, DateTime hoy) =>
                Noches(desde, hasta)
                    .Where(n => n.Hospedaje.FechaEntrada.Date <= hoy && n.Hospedaje.FechaSalida.Date > hoy)
                    .Sum(n => n.Tarifa);

            public Dictionary<string, decimal> IngresosPorLinea(DateTime desde, DateTime hasta)
            {
                var resultado = new Dictionary<string, decimal>();
                void Sumar(string linea, decimal monto)
                {
                    if (monto != 0) resultado[linea] = resultado.GetValueOrDefault(linea) + monto;
                }

                Sumar(LineaAlojamiento, Noches(desde, hasta).Sum(n => n.Tarifa));

                foreach (VentaRealizada venta in VentasEn(desde, hasta))
                {
                    foreach (ItemConsumido item in venta.Items)
                    {
                        Sumar(_lineaPorProducto.GetValueOrDefault(item.CodProducto.Trim(), LineaSinCategoria), item.Subtotal);
                    }

                    // Si el total de la venta no coincide con su detalle, la diferencia no se pierde.
                    Sumar(LineaSinCategoria, venta.Total - venta.Items.Sum(i => i.Subtotal));
                }

                return resultado;
            }

            /// <summary>Ingresos (alojamiento devengado + ventas) según el medio con que se cobraron.</summary>
            public Dictionary<string, decimal> IngresosPorMedio(DateTime desde, DateTime hasta)
            {
                var resultado = new Dictionary<string, decimal>();
                foreach (var noche in Noches(desde, hasta))
                {
                    string medio = _nombreMetodo.GetValueOrDefault(noche.Hospedaje.IdMetodo, "Sin especificar");
                    resultado[medio] = resultado.GetValueOrDefault(medio) + noche.Tarifa;
                }

                foreach (VentaRealizada venta in VentasEn(desde, hasta))
                {
                    string medio = string.IsNullOrWhiteSpace(venta.MetodoPago) ? "Sin especificar" : venta.MetodoPago.Trim();
                    resultado[medio] = resultado.GetValueOrDefault(medio) + venta.Total;
                }

                return resultado.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            private IEnumerable<VentaRealizada> VentasEn(DateTime desde, DateTime hasta) =>
                _ventas.Where(v => v.FechaVenta.Date >= desde.Date && v.FechaVenta.Date <= hasta.Date);

            /// <summary>
            /// Cada noche ocupada dentro del rango, con la tarifa de su habitación. Una estadía ocupa las
            /// noches desde la entrada hasta el día anterior a la salida; si entra y sale el mismo día,
            /// cuenta una noche (mismo mínimo que GestionHospedajes.CalcularNoches).
            /// </summary>
            private IEnumerable<(Hospedaje Hospedaje, DateTime Noche, decimal Tarifa)> Noches(DateTime desde, DateTime hasta)
            {
                foreach (Hospedaje h in _hospedajes)
                {
                    DateTime entrada = h.FechaEntrada.Date;
                    DateTime ultimaNoche = h.FechaSalida.Date > entrada ? h.FechaSalida.Date.AddDays(-1) : entrada;
                    DateTime inicio = entrada > desde.Date ? entrada : desde.Date;
                    DateTime fin = ultimaNoche < hasta.Date ? ultimaNoche : hasta.Date;
                    decimal tarifa = _tarifaPorHabitacion.GetValueOrDefault(h.NroHabitacion);

                    for (DateTime noche = inicio; noche <= fin; noche = noche.AddDays(1))
                    {
                        yield return (h, noche, tarifa);
                    }
                }
            }
        }

        // =====================================================================
        // Utilidades
        // =====================================================================

        /// <summary>Compara nombres de estado tolerando variaciones de género ("Ocupada"/"Ocupado").</summary>
        private static bool CoincideEstado(string nombreReal, string nombreBuscado)
        {
            static string Normalizar(string s) => QuitarAcentos(s).Trim().ToLowerInvariant().TrimEnd('a', 'o');
            return Normalizar(nombreReal) == Normalizar(nombreBuscado);
        }

        /// <summary>Compara dos nombres ignorando acentos, mayúsculas y espacios extra.</summary>
        private static bool CoincideNombre(string nombreReal, string nombreBuscado)
        {
            static string Normalizar(string s) => QuitarAcentos(s).Trim().ToLowerInvariant();
            return Normalizar(nombreReal) == Normalizar(nombreBuscado);
        }

        private static string QuitarAcentos(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var sinAcentos = new StringBuilder();

            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sinAcentos.Append(c);
                }
            }

            return sinAcentos.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Capitalizar(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], CulturaAr) + texto[1..];
    }

    /// <summary>Métricas de un período: noches vendidas, ingresos y los KPIs hoteleros derivados.</summary>
    public class MetricasPeriodo
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int Dias => (Hasta - Desde).Days + 1;
        public int Habitaciones { get; set; }
        public int NochesVendidas { get; set; }
        public int Estadias { get; set; }
        public decimal IngresosAlojamiento { get; set; }
        public decimal IngresosVentas { get; set; }
        public decimal IngresosTotales => IngresosAlojamiento + IngresosVentas;

        /// <summary>Noches disponibles: habitaciones × días del período.</summary>
        public int NochesDisponibles => Habitaciones * Dias;

        /// <summary>Occupancy rate: noches vendidas / noches disponibles.</summary>
        public double Ocupacion => NochesDisponibles > 0 ? Math.Min(100, NochesVendidas * 100.0 / NochesDisponibles) : 0;

        /// <summary>ADR: ingreso de alojamiento / noches vendidas.</summary>
        public decimal Adr => NochesVendidas > 0 ? IngresosAlojamiento / NochesVendidas : 0m;

        /// <summary>RevPAR: ingreso de alojamiento / noches disponibles (= ADR × ocupación).</summary>
        public decimal RevPar => NochesDisponibles > 0 ? IngresosAlojamiento / NochesDisponibles : 0m;
    }

    public class IndicadoresGerencia
    {
        public MetricasPeriodo Actual { get; set; } = new();
        /// <summary>Mismos días del mes anterior (MoM).</summary>
        public MetricasPeriodo MesAnterior { get; set; } = new();
        /// <summary>Mismos días del mismo mes del año anterior (YoY).</summary>
        public MetricasPeriodo AnioAnterior { get; set; } = new();
        public double MetaOcupacion { get; set; }
        /// <summary>Últimos 12 meses, del más antiguo al actual (el último está en curso).</summary>
        public List<PuntoMensual> Tendencia { get; set; } = new();
    }

    public class PuntoMensual
    {
        public string Etiqueta { get; set; } = string.Empty;
        public decimal Alojamiento { get; set; }
        public decimal Ventas { get; set; }
        public double Ocupacion { get; set; }
        public bool EnCurso { get; set; }
    }

    public class PanelFinanciero
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime FinMes { get; set; }
        public MetricasPeriodo Actual { get; set; } = new();
        public MetricasPeriodo MesAnteriorMismosDias { get; set; } = new();
        public decimal MesAnteriorCompleto { get; set; }

        /// <summary>Noches de lo que resta del mes ya reservadas por estadías en curso.</summary>
        public decimal Comprometido { get; set; }
        public decimal VentasPromedioDiario { get; set; }
        public decimal ProyeccionCierre { get; set; }

        public List<PuntoFlujo> Flujo { get; set; } = new();
        public List<LineaNegocio> Lineas { get; set; } = new();
        public List<MontoPorMedio> MediosDePago { get; set; } = new();

        public List<CajaAbierta> CajasAbiertas { get; set; } = new();
        public int TurnosCerradosMes { get; set; }
        public decimal EfectivoArqueadoMes { get; set; }
        public int TurnosConDiferencia { get; set; }
    }

    /// <summary>Un día del flujo acumulado. null = ese día no aplica a la serie.</summary>
    public class PuntoFlujo
    {
        public int Dia { get; set; }
        public decimal? Real { get; set; }
        public decimal? Proyectado { get; set; }
        public decimal? MesAnterior { get; set; }
    }

    public class LineaNegocio
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Actual { get; set; }
        /// <summary>Mismos días del mes anterior.</summary>
        public decimal Anterior { get; set; }
    }

    public class MontoPorMedio
    {
        public string Medio { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class CajaAbierta
    {
        public int IdTurno { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime Apertura { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal Cobrado { get; set; }
        public decimal EfectivoEsperado { get; set; }
    }

    public class HistorialOperativoResultado
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        /// <summary>Turnos abiertos primero; después los cerrados, del cierre más reciente al más antiguo.</summary>
        public List<TurnoHistorialItem> Lista { get; set; } = new List<TurnoHistorialItem>();
        public List<ResumenRecepcionista> PorRecepcionista { get; set; } = new List<ResumenRecepcionista>();

        public int TotalTurnosCerrados => Lista.Count(t => !t.Abierto);
        public int TurnosAbiertos => Lista.Count(t => t.Abierto);
        public decimal TotalCobrado => Lista.Sum(t => t.CobradoTotal);
        public decimal PromedioCobradoTurno => Lista.Count > 0 ? TotalCobrado / Lista.Count : 0m;
        public decimal CobradoEfectivo => Lista.Sum(t => t.CobradoEfectivo);
        public decimal CobradoTarjeta => Lista.Sum(t => t.CobradoTarjeta);
        public decimal CobradoTransferencia => Lista.Sum(t => t.CobradoTransferencia);

        /// <summary>Efectivo contado al cierre menos el monto inicial, sumado en todos los turnos.</summary>
        public decimal EfectivoArqueado => Lista.Sum(t => t.EfectivoArqueado);

        public int TurnosConDiferencia => Lista.Count(t => t.TieneDiferencia);
        /// <summary>Suma de los faltantes (en positivo).</summary>
        public decimal Faltante => -Lista.Where(t => t.Diferencia < 0).Sum(t => t.Diferencia!.Value);
        public decimal Sobrante => Lista.Where(t => t.Diferencia > 0).Sum(t => t.Diferencia!.Value);

        /// <summary>Duración promedio de los turnos cerrados (los abiertos todavía no terminaron).</summary>
        public TimeSpan DuracionPromedio => TotalTurnosCerrados > 0
            ? TimeSpan.FromMinutes(Lista.Where(t => !t.Abierto).Average(t => t.Duracion.TotalMinutes))
            : TimeSpan.Zero;
    }

    /// <summary>Un turno de caja con lo cobrado recalculado y, si ya cerró, el resultado de su arqueo.</summary>
    public class TurnoHistorialItem
    {
        public int IdTurno { get; set; }
        public string DniUsuario { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime Apertura { get; set; }
        /// <summary>Null = el turno sigue abierto.</summary>
        public DateTime? Cierre { get; set; }
        public bool Abierto => !Cierre.HasValue;
        /// <summary>Hasta el cierre, o hasta ahora si sigue abierto.</summary>
        public TimeSpan Duracion => (Cierre ?? DateTime.Now) > Apertura ? (Cierre ?? DateTime.Now) - Apertura : TimeSpan.Zero;

        public decimal MontoInicial { get; set; }
        public decimal CobradoEfectivo { get; set; }
        public decimal CobradoTarjeta { get; set; }
        public decimal CobradoTransferencia { get; set; }
        public decimal CobradoAlojamiento { get; set; }
        public decimal CobradoVentas { get; set; }
        public decimal CobradoTotal => CobradoAlojamiento + CobradoVentas;
        public int CheckIns { get; set; }
        public int Ventas { get; set; }

        /// <summary>Monto inicial + cobros en efectivo: lo que debía haber en la caja al cerrar.</summary>
        public decimal EfectivoEsperado { get; set; }
        /// <summary>Efectivo contado en el arqueo (Turno_caja.monto_final). Null si no se registró.</summary>
        public decimal? EfectivoContado { get; set; }
        public decimal EfectivoArqueado => EfectivoContado.HasValue ? EfectivoContado.Value - MontoInicial : 0m;

        /// <summary>Contado − esperado: negativo = faltante, positivo = sobrante. Null si no hay arqueo.</summary>
        public decimal? Diferencia => EfectivoContado.HasValue ? EfectivoContado.Value - EfectivoEsperado : null;
        public bool TieneDiferencia => Diferencia.HasValue && Math.Abs(Diferencia.Value) >= 0.01m;

        public string Observaciones { get; set; } = string.Empty;
    }

    public class ResumenRecepcionista
    {
        public string DniUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Turnos { get; set; }
        public decimal Cobrado { get; set; }
        public int TurnosConDiferencia { get; set; }
        /// <summary>Suma neta de diferencias de caja (negativo = faltante).</summary>
        public decimal Diferencia { get; set; }
    }
}
