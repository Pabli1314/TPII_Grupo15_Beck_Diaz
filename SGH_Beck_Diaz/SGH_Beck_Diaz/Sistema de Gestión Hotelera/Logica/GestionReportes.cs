using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Reportes y estadísticas del Administrador. Ocupación y rotación salen de Hospedaje real;
    /// los ingresos se estiman según la tarifa vigente asociada al tipo de habitación de cada unidad.
    /// Además de los números arma comparativas contra el período anterior y conclusiones en
    /// lenguaje natural para que cualquier perfil administrativo pueda leer el reporte.
    /// </summary>
    public class GestionReportes
    {
        /// <summary>Ocupación promedio que el hotel se propone alcanzar (referencia de las comparativas).</summary>
        public const double MetaOcupacionPorcentaje = 70;

        /// <summary>Hasta este largo la tendencia se muestra día por día; más largo, se agrupa por mes.</summary>
        private const int DiasMaximosTendenciaDiaria = 62;

        private const string EstadoOcupada = "Ocupada";
        private const string EstadoMantenimiento = "Mantenimiento";
        private static readonly string[] OrdenEstados = { "Disponible", "Ocupada", "Limpieza", "Mantenimiento" };

        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        private readonly GestionTarifas _gestionTarifas = new();

        /// <summary>Opciones para los combos de filtro de la pantalla.</summary>
        public (List<TipoHabitacion> Tipos, List<EstadoHabitacion> Estados, List<MetodoPago> FormasPago) ObtenerCatalogosFiltros()
            => (TipoHabitacionDAO.ObtenerTodos(), EstadoHabitacionDAO.ObtenerTodos(), MetodoPagoDAO.ObtenerTodos());

        /// <summary>Compatibilidad: reporte de un rango sin filtros, comparado con los días anteriores.</summary>
        public ReporteHotel Generar(DateTime desde, DateTime hasta)
            => Generar(PeriodoReporte.Crear(RangoPredefinido.Personalizado, DateTime.Today, desde, hasta), new FiltroReporte());

        public ReporteHotel Generar(PeriodoReporte periodo, FiltroReporte filtro)
        {
            DateTime desde = periodo.Desde;
            DateTime hasta = periodo.Hasta;

            // Un período corto (Hoy) se acompaña con la última semana para que la tendencia diga algo.
            DateTime desdeTendencia = periodo.Dias < 7 ? hasta.AddDays(-6) : desde;
            DateTime desdeConsulta = new[] { desde, periodo.DesdeAnterior, desdeTendencia }.Min();

            // Una sola consulta de hospedajes cubre período actual, anterior y tendencia; se separan en memoria.
            List<Hospedaje> hospedajes = HospedajeDAO.ObtenerEnRango(desdeConsulta, hasta);
            List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();
            List<TipoHabitacion> tipos = TipoHabitacionDAO.ObtenerTodos();
            List<EstadoHabitacion> estados = EstadoHabitacionDAO.ObtenerTodos();
            List<MetodoPago> formasPago = MetodoPagoDAO.ObtenerTodos();
            List<RegistroLimpieza> limpiezas = RegistroLimpiezaDAO.ObtenerEnRango(desde, hasta);
            Dictionary<string, int> primerHospedaje = HospedajeDAO.ObtenerPrimerHospedajePorHuesped();

            // Tarifa vigente por tipo de habitación → por número de habitación
            Dictionary<int, decimal> tarifaPorTipo = _gestionTarifas.ObtenerTarifas()
                .GroupBy(t => t.IdTipoHabitacion)
                .ToDictionary(g => g.Key, g => g.First().PrecioPorHora); // Precio base por noche
            Dictionary<int, decimal> tarifaPorHabitacion = habitaciones.ToDictionary(
                h => h.NroHabitacion,
                h => tarifaPorTipo.TryGetValue(h.IdTipoHabitacion, out decimal precio) ? precio : 0m);

            Dictionary<int, string> nombreTipo = tipos.ToDictionary(t => t.IdTipoHabitacion, t => t.Descripcion ?? $"Tipo {t.IdTipoHabitacion}");
            Dictionary<int, string> nombreEstado = estados.ToDictionary(e => e.IdEstado, e => e.NomEstadoHabitacion ?? $"Estado {e.IdEstado}");
            Dictionary<int, string> nombreFormaPago = formasPago.ToDictionary(m => m.IdMetodo, m => m.NomMetodoPago ?? $"Forma {m.IdMetodo}");
            Dictionary<int, Habitacion> habitacionPorNro = habitaciones.ToDictionary(h => h.NroHabitacion);

            string TipoDe(int nroHabitacion) => habitacionPorNro.TryGetValue(nroHabitacion, out Habitacion? h) && nombreTipo.TryGetValue(h.IdTipoHabitacion, out string? n) ? n : "Sin tipo";
            decimal Monto(Hospedaje h) => CalcularMontoEstadia(h, tarifaPorHabitacion);
            bool EsRecurrente(Hospedaje h) => primerHospedaje.TryGetValue((h.DniHuesped ?? string.Empty).Trim(), out int primero) && primero != h.IdHospedaje;

            // ---------------- Filtros ----------------
            List<Habitacion> habitacionesFiltradas = habitaciones
                .Where(h => !filtro.IdTipoHabitacion.HasValue || h.IdTipoHabitacion == filtro.IdTipoHabitacion.Value)
                .Where(h => !filtro.IdEstado.HasValue || h.IdEstado == filtro.IdEstado.Value)
                .ToList();
            HashSet<int> nrosFiltrados = habitacionesFiltradas.Select(h => h.NroHabitacion).ToHashSet();
            int totalHabitaciones = habitacionesFiltradas.Count;

            List<Hospedaje> filtrados = hospedajes
                .Where(h => nrosFiltrados.Contains(h.NroHabitacion))
                .Where(h => !filtro.IdMetodo.HasValue || h.IdMetodo == filtro.IdMetodo.Value)
                .Where(h => filtro.TipoHuesped switch
                {
                    TipoHuespedFiltro.Nuevos => !EsRecurrente(h),
                    TipoHuespedFiltro.Recurrentes => EsRecurrente(h),
                    _ => true
                })
                .ToList();

            List<Hospedaje> EnRango(DateTime d, DateTime hs) => filtrados.Where(h => h.FechaEntrada.Date <= hs && h.FechaSalida.Date >= d).ToList();

            List<Hospedaje> actuales = EnRango(desde, hasta);
            List<Hospedaje> anteriores = EnRango(periodo.DesdeAnterior, periodo.HastaAnterior);

            var reporte = new ReporteHotel
            {
                Desde = desde,
                Hasta = hasta,
                DesdeAnterior = periodo.DesdeAnterior,
                HastaAnterior = periodo.HastaAnterior,
                EtiquetaComparacion = periodo.EtiquetaComparacion,
                MetaOcupacionPorcentaje = MetaOcupacionPorcentaje,
                TotalHabitaciones = totalHabitaciones
            };

            // ---------------- Series diarias (pantalla + PDF) ----------------
            // Habitaciones ocupadas en cada día del rango: cuenta toda estadía que abarque ese día,
            // aunque haya entrado antes del rango (si no, "Hoy" queda vacío con huéspedes alojados).
            reporte.OcupacionDiaria = Enumerable.Range(0, periodo.Dias)
                .Select(i => desde.AddDays(i))
                .Select(dia => new PuntoSerie
                {
                    Etiqueta = dia.ToString("dd/MM"),
                    Valor = actuales.Count(h => h.FechaEntrada.Date <= dia && h.FechaSalida.Date >= dia)
                })
                .ToList();

            reporte.IngresosDiarios = actuales
                .GroupBy(h => h.FechaEntrada.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PuntoSerie { Etiqueta = g.Key.ToString("dd/MM"), Valor = (double)g.Sum(Monto) })
                .ToList();

            reporte.IngresosPorHabitacion = actuales
                .GroupBy(h => h.NroHabitacion)
                .Select(g => new PuntoSerie { Etiqueta = $"Hab. {g.Key}", Valor = (double)g.Sum(Monto) })
                .OrderByDescending(p => p.Valor)
                .Take(10)
                .ToList();

            reporte.RotacionPorHabitacion = actuales
                .GroupBy(h => h.NroHabitacion)
                .Select(g => new PuntoSerie { Etiqueta = $"Hab. {g.Key}", Valor = g.Count() })
                .OrderByDescending(p => p.Valor)
                .ToList();

            // ---------------- Totales y comparativa ----------------
            reporte.TotalOcupaciones = actuales.Count;
            reporte.TotalIngresos = actuales.Sum(Monto);
            reporte.PromedioOcupacionPorcentaje = PorcentajeOcupacion(actuales, desde, hasta, totalHabitaciones);
            reporte.PromedioDuracionEstadiaDias = PromedioEstadia(actuales);

            reporte.TotalOcupacionesAnterior = anteriores.Count;
            reporte.TotalIngresosAnterior = anteriores.Sum(Monto);
            reporte.PromedioOcupacionAnteriorPorcentaje = PorcentajeOcupacion(anteriores, periodo.DesdeAnterior, periodo.HastaAnterior, totalHabitaciones);
            reporte.PromedioDuracionEstadiaAnteriorDias = PromedioEstadia(anteriores);

            List<RegistroLimpieza> limpiezasFiltradas = limpiezas.Where(l => nrosFiltrados.Contains(l.NroHabitacion)).ToList();
            reporte.PromedioLimpiezaMinutos = limpiezasFiltradas.Count > 0
                ? limpiezasFiltradas.Average(l => (l.HoraFin - l.HoraInicio).TotalMinutes)
                : 0;

            // ---------------- Foto actual del hotel ----------------
            int? idOcupada = estados.FirstOrDefault(e => string.Equals(e.NomEstadoHabitacion, EstadoOcupada, StringComparison.OrdinalIgnoreCase))?.IdEstado;
            int? idMantenimiento = estados.FirstOrDefault(e => string.Equals(e.NomEstadoHabitacion, EstadoMantenimiento, StringComparison.OrdinalIgnoreCase))?.IdEstado;

            reporte.HabitacionesOcupadasAhora = habitacionesFiltradas.Count(h => h.IdEstado == idOcupada);
            reporte.OcupacionActualPorcentaje = totalHabitaciones == 0 ? 0 : reporte.HabitacionesOcupadasAhora * 100.0 / totalHabitaciones;
            reporte.HabitacionesEnMantenimiento = habitacionesFiltradas.Count(h => h.IdEstado == idMantenimiento);
            reporte.EstadoActualHabitaciones = Distribucion(
                estados
                    .OrderBy(e => { int i = Array.FindIndex(OrdenEstados, o => string.Equals(o, e.NomEstadoHabitacion, StringComparison.OrdinalIgnoreCase)); return i < 0 ? int.MaxValue : i; })
                    .Select(e => (nombreEstado[e.IdEstado], (double)habitacionesFiltradas.Count(h => h.IdEstado == e.IdEstado))),
                ordenar: false);

            // ---------------- Tendencias ----------------
            List<Hospedaje> paraTendencia = EnRango(desdeTendencia, hasta);
            int diasTendencia = (hasta - desdeTendencia).Days + 1;

            if (diasTendencia <= DiasMaximosTendenciaDiaria)
            {
                reporte.GranularidadTendencia = "día";
                for (DateTime dia = desdeTendencia; dia <= hasta; dia = dia.AddDays(1))
                {
                    string etiqueta = dia.ToString("dd/MM");
                    reporte.TendenciaOcupacionPorcentaje.Add(new PuntoSerie { Etiqueta = etiqueta, Valor = PorcentajeOcupacion(paraTendencia, dia, dia, totalHabitaciones) });
                    reporte.TendenciaIngresos.Add(new PuntoSerie { Etiqueta = etiqueta, Valor = (double)paraTendencia.Where(h => h.FechaEntrada.Date == dia).Sum(Monto) });
                }
            }
            else
            {
                reporte.GranularidadTendencia = "mes";
                for (var mes = new DateTime(desdeTendencia.Year, desdeTendencia.Month, 1); mes <= hasta; mes = mes.AddMonths(1))
                {
                    DateTime ini = mes < desdeTendencia ? desdeTendencia : mes;
                    DateTime fin = mes.AddMonths(1).AddDays(-1) > hasta ? hasta : mes.AddMonths(1).AddDays(-1);
                    string etiqueta = Capitalizar(mes.ToString("MMM yy", CulturaAr).Replace(".", string.Empty));
                    reporte.TendenciaOcupacionPorcentaje.Add(new PuntoSerie { Etiqueta = etiqueta, Valor = PorcentajeOcupacion(paraTendencia, ini, fin, totalHabitaciones) });
                    reporte.TendenciaIngresos.Add(new PuntoSerie { Etiqueta = etiqueta, Valor = (double)paraTendencia.Where(h => h.FechaEntrada.Date >= ini && h.FechaEntrada.Date <= fin).Sum(Monto) });
                }
            }

            // ---------------- Distribuciones ----------------
            reporte.EstadiasPorTipoHabitacion = Distribucion(actuales.GroupBy(h => TipoDe(h.NroHabitacion)).Select(g => (g.Key, (double)g.Count())));
            reporte.EstadiasPorFormaPago = Distribucion(actuales
                .GroupBy(h => nombreFormaPago.TryGetValue(h.IdMetodo, out string? n) ? n : "Sin especificar")
                .Select(g => (g.Key, (double)g.Count())));
            int recurrentes = actuales.Count(EsRecurrente);
            reporte.EstadiasPorTipoHuesped = Distribucion(new[]
            {
                ("Nuevos (primera visita)", (double)(actuales.Count - recurrentes)),
                ("Recurrentes (ya vinieron)", (double)recurrentes)
            }, ordenar: false);
            reporte.HuespedesDistintos = actuales.Select(h => (h.DniHuesped ?? string.Empty).Trim()).Where(d => d.Length > 0).Distinct().Count();

            // ---------------- Detalle por tipo y por habitación ----------------
            reporte.DetallePorTipo = habitacionesFiltradas
                .GroupBy(h => h.IdTipoHabitacion)
                .Select(g =>
                {
                    HashSet<int> nros = g.Select(h => h.NroHabitacion).ToHashSet();
                    List<Hospedaje> estadias = actuales.Where(h => nros.Contains(h.NroHabitacion)).ToList();
                    decimal ingresos = estadias.Sum(Monto);
                    return new ResumenTipoHabitacion
                    {
                        Tipo = nombreTipo.TryGetValue(g.Key, out string? n) ? n : "Sin tipo",
                        Habitaciones = nros.Count,
                        Estadias = estadias.Count,
                        NochesOcupadas = estadias.Sum(h => NochesEnRango(h, desde, hasta)),
                        OcupacionPorcentaje = PorcentajeOcupacion(estadias, desde, hasta, nros.Count),
                        Ingresos = ingresos,
                        IngresoPromedioPorEstadia = estadias.Count > 0 ? ingresos / estadias.Count : 0,
                        ParticipacionIngresosPorcentaje = reporte.TotalIngresos > 0 ? (double)(ingresos * 100 / reporte.TotalIngresos) : 0
                    };
                })
                .OrderByDescending(t => t.Ingresos)
                .ThenByDescending(t => t.Estadias)
                .ToList();

            reporte.DetallePorHabitacion = habitacionesFiltradas
                .Select(hab =>
                {
                    List<Hospedaje> estadias = actuales.Where(h => h.NroHabitacion == hab.NroHabitacion).ToList();
                    return new ResumenHabitacion
                    {
                        NroHabitacion = hab.NroHabitacion,
                        Tipo = TipoDe(hab.NroHabitacion),
                        EstadoActual = nombreEstado.TryGetValue(hab.IdEstado, out string? e) ? e : "—",
                        Estadias = estadias.Count,
                        NochesOcupadas = estadias.Sum(h => NochesEnRango(h, desde, hasta)),
                        Ingresos = estadias.Sum(Monto)
                    };
                })
                .OrderByDescending(h => h.Ingresos)
                .ThenByDescending(h => h.Estadias)
                .ThenBy(h => h.NroHabitacion)
                .ToList();

            reporte.Insights = GenerarInsights(reporte, periodo);
            return reporte;
        }

        // ------------------------------------------------------------------
        // Conclusiones en lenguaje natural
        // ------------------------------------------------------------------

        private static List<InsightReporte> GenerarInsights(ReporteHotel r, PeriodoReporte periodo)
        {
            var lista = new List<InsightReporte>();
            string comp = r.EtiquetaComparacion;

            void Agregar(TonoInsight tono, string texto) => lista.Add(new InsightReporte { Tono = tono, Texto = texto });

            if (r.TotalHabitaciones == 0)
            {
                Agregar(TonoInsight.Atencion, "Ninguna habitación coincide con los filtros elegidos. Quite algún filtro para ver resultados.");
                return lista;
            }

            if (r.TotalOcupaciones == 0)
            {
                Agregar(TonoInsight.Atencion, $"No se registraron estadías en el período ({DescribirRango(r.Desde, r.Hasta)}) con los filtros elegidos.");
                if (r.TotalOcupacionesAnterior > 0)
                {
                    Agregar(TonoInsight.Neutro, $"Como referencia, {comp} hubo {Plural(r.TotalOcupacionesAnterior, "estadía", "estadías")}.");
                }
                AgregarMantenimiento();
                return lista;
            }

            // 1. Ocupación frente a la meta
            double ocupacion = r.PromedioOcupacionPorcentaje;
            double diferenciaMeta = ocupacion - r.MetaOcupacionPorcentaje;
            if (Math.Abs(diferenciaMeta) < 0.5)
                Agregar(TonoInsight.Positivo, $"La ocupación promedio fue del {Pct(ocupacion)}, justo en la meta del {Pct(r.MetaOcupacionPorcentaje)}.");
            else if (diferenciaMeta > 0)
                Agregar(TonoInsight.Positivo, $"La ocupación promedio fue del {Pct(ocupacion)}: {Puntos(diferenciaMeta)} por encima de la meta del {Pct(r.MetaOcupacionPorcentaje)}.");
            else
                Agregar(TonoInsight.Atencion, $"La ocupación promedio fue del {Pct(ocupacion)}: {Puntos(-diferenciaMeta)} por debajo de la meta del {Pct(r.MetaOcupacionPorcentaje)}.");

            // 2. Ingresos frente al período anterior
            string ingresos = Moneda(r.TotalIngresos);
            if (r.TotalIngresosAnterior > 0)
            {
                double variacion = (double)((r.TotalIngresos - r.TotalIngresosAnterior) / r.TotalIngresosAnterior * 100);
                if (variacion >= 0.5)
                    Agregar(TonoInsight.Positivo, $"Los ingresos estimados sumaron {ingresos}, un {variacion:0}% más que {comp}.");
                else if (variacion <= -0.5)
                    Agregar(TonoInsight.Atencion, $"Los ingresos estimados sumaron {ingresos}, un {-variacion:0}% menos que {comp}.");
                else
                    Agregar(TonoInsight.Neutro, $"Los ingresos estimados sumaron {ingresos}, prácticamente lo mismo que {comp}.");
            }
            else
            {
                Agregar(TonoInsight.Neutro, $"Los ingresos estimados sumaron {ingresos} ({comp} no hubo ingresos para comparar).");
            }

            // 3. Tipo de habitación más demandado y el de menor ocupación
            var tiposConHabitaciones = r.DetallePorTipo.Where(t => t.Habitaciones > 0).ToList();
            ResumenTipoHabitacion? masDemandado = tiposConHabitaciones
                .Where(t => t.Estadias > 0)
                .OrderByDescending(t => t.OcupacionPorcentaje)
                .ThenByDescending(t => t.Estadias)
                .FirstOrDefault();

            if (masDemandado != null && tiposConHabitaciones.Count > 1)
            {
                Agregar(TonoInsight.Neutro, $"El tipo de habitación más demandado fue «{masDemandado.Tipo}», con un {Pct(masDemandado.OcupacionPorcentaje)} de ocupación ({Plural(masDemandado.Estadias, "estadía", "estadías")}).");

                ResumenTipoHabitacion menosOcupado = tiposConHabitaciones.OrderBy(t => t.OcupacionPorcentaje).First();
                if (menosOcupado != masDemandado && menosOcupado.OcupacionPorcentaje < r.MetaOcupacionPorcentaje)
                {
                    Agregar(TonoInsight.Atencion, $"«{menosOcupado.Tipo}» fue el tipo con menor ocupación ({Pct(menosOcupado.OcupacionPorcentaje)}): puede ser una buena oportunidad para una promoción.");
                }
            }

            // 4. Habitaciones en mantenimiento (afecta lo que se puede vender hoy)
            AgregarMantenimiento();

            // 5. Huéspedes que vuelven
            double recurrentes = r.EstadiasPorTipoHuesped.Skip(1).FirstOrDefault()?.Valor ?? 0;
            double pctRecurrentes = recurrentes * 100 / r.TotalOcupaciones;
            if (recurrentes == 0)
                Agregar(TonoInsight.Neutro, "Todas las estadías fueron de huéspedes que visitaban el hotel por primera vez.");
            else
                Agregar(pctRecurrentes >= 30 ? TonoInsight.Positivo : TonoInsight.Neutro,
                    $"El {Pct(pctRecurrentes)} de las estadías fue de huéspedes que ya se habían alojado antes (clientes que vuelven).");

            // 6. Día de mayor ocupación
            if (r.OcupacionDiaria.Count > 1)
            {
                double maximo = r.OcupacionDiaria.Max(p => p.Valor);
                int indice = r.OcupacionDiaria.FindIndex(p => p.Valor == maximo);
                if (maximo > 0)
                {
                    DateTime dia = r.Desde.AddDays(indice);
                    Agregar(TonoInsight.Neutro, $"El día de mayor movimiento fue el {dia.ToString("dddd d 'de' MMMM", CulturaAr)}, con {(int)maximo} de {r.TotalHabitaciones} habitaciones ocupadas.");
                }
            }

            // 7. Habitación que más facturó
            PuntoSerie? estrella = r.IngresosPorHabitacion.FirstOrDefault();
            if (estrella != null && estrella.Valor > 0 && r.TotalIngresos > 0)
            {
                double participacion = estrella.Valor * 100 / (double)r.TotalIngresos;
                Agregar(TonoInsight.Neutro, $"La {estrella.Etiqueta.Replace("Hab.", "habitación")} fue la que más ingresos generó: {Moneda((decimal)estrella.Valor)} ({Pct(participacion)} del total).");
            }

            // 8. Forma de pago preferida
            SegmentoDistribucion? pago = r.EstadiasPorFormaPago.FirstOrDefault();
            if (pago != null && r.EstadiasPorFormaPago.Count > 1)
            {
                Agregar(TonoInsight.Neutro, $"La forma de pago más usada fue «{pago.Etiqueta}»: {Pct(pago.Porcentaje)} de las estadías.");
            }

            // 9. Duración de la estadía frente al período anterior
            if (r.PromedioDuracionEstadiaAnteriorDias > 0 && Math.Abs(r.PromedioDuracionEstadiaDias - r.PromedioDuracionEstadiaAnteriorDias) >= 0.1)
            {
                bool mas = r.PromedioDuracionEstadiaDias > r.PromedioDuracionEstadiaAnteriorDias;
                Agregar(TonoInsight.Neutro, $"Los huéspedes se quedaron en promedio {Noches(r.PromedioDuracionEstadiaDias)}, {(mas ? "más" : "menos")} que {comp} ({Noches(r.PromedioDuracionEstadiaAnteriorDias)}).");
            }

            return lista;

            void AgregarMantenimiento()
            {
                if (r.HabitacionesEnMantenimiento > 0)
                {
                    Agregar(TonoInsight.Atencion, r.HabitacionesEnMantenimiento == 1
                        ? "Hay 1 habitación en mantenimiento en este momento: no se puede ofrecer a nuevos huéspedes."
                        : $"Hay {r.HabitacionesEnMantenimiento} habitaciones en mantenimiento en este momento: no se pueden ofrecer a nuevos huéspedes.");
                }
            }
        }

        // ------------------------------------------------------------------
        // Cálculos auxiliares
        // ------------------------------------------------------------------

        /// <summary>Estimación del monto de una estadía según la tarifa asignada a la habitación por cada noche.</summary>
        private static decimal CalcularMontoEstadia(Hospedaje hospedaje, Dictionary<int, decimal> tarifaPorHabitacion)
        {
            decimal tarifa = tarifaPorHabitacion.TryGetValue(hospedaje.NroHabitacion, out decimal valor) ? valor : 0m;
            int noches = Math.Max(1, (hospedaje.FechaSalida.Date - hospedaje.FechaEntrada.Date).Days);
            return tarifa * noches;
        }

        /// <summary>Días del rango en que la estadía tuvo la habitación ocupada (mismo criterio que OcupacionDiaria).</summary>
        private static int NochesEnRango(Hospedaje h, DateTime desde, DateTime hasta)
        {
            DateTime inicio = h.FechaEntrada.Date > desde ? h.FechaEntrada.Date : desde;
            DateTime fin = h.FechaSalida.Date < hasta ? h.FechaSalida.Date : hasta;
            return Math.Max(0, (fin - inicio).Days + 1);
        }

        private static double PorcentajeOcupacion(IEnumerable<Hospedaje> estadias, DateTime desde, DateTime hasta, int totalHabitaciones)
        {
            if (totalHabitaciones <= 0) return 0;
            int dias = Math.Max(1, (hasta - desde).Days + 1);
            double ocupadas = estadias.Sum(h => NochesEnRango(h, desde, hasta));
            return Math.Min(100, ocupadas * 100.0 / (totalHabitaciones * dias));
        }

        private static double PromedioEstadia(List<Hospedaje> estadias) => estadias.Count > 0
            ? estadias.Average(h => Math.Max(1, (h.FechaSalida.Date - h.FechaEntrada.Date).Days))
            : 0;

        private static List<SegmentoDistribucion> Distribucion(IEnumerable<(string Etiqueta, double Valor)> items, bool ordenar = true)
        {
            var lista = items.ToList();
            double total = lista.Sum(i => i.Valor);
            var segmentos = lista.Select(i => new SegmentoDistribucion
            {
                Etiqueta = i.Etiqueta,
                Valor = i.Valor,
                Porcentaje = total > 0 ? i.Valor * 100 / total : 0
            });
            return (ordenar ? segmentos.OrderByDescending(s => s.Valor) : segmentos).ToList();
        }

        private static string Moneda(decimal valor) => valor.ToString("C0", CulturaAr);
        private static string Pct(double valor) => $"{valor.ToString("0", CulturaAr)}%";
        private static string Puntos(double valor) => Math.Round(valor) == 1 ? "1 punto" : $"{valor.ToString("0", CulturaAr)} puntos";
        private static string Noches(double valor) => $"{valor.ToString("0.#", CulturaAr)} {(Math.Abs(valor - 1) < 0.05 ? "noche" : "noches")}";
        private static string Plural(int n, string singular, string plural) => $"{n} {(n == 1 ? singular : plural)}";
        private static string Capitalizar(string texto) => string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], CulturaAr) + texto[1..];

        private static string DescribirRango(DateTime desde, DateTime hasta) => desde == hasta
            ? desde.ToString("dd/MM/yyyy")
            : $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
    }
}
