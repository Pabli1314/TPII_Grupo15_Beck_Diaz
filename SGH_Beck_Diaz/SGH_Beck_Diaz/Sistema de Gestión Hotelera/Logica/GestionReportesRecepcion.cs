using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Arma el reporte de la pantalla Reportes de Recepción: estado del hotel en este momento,
    /// llegadas y salidas del día y caja del turno abierto del recepcionista. Además redacta las
    /// frases resumen en lenguaje cotidiano que se muestran arriba de cada pestaña y en el PDF.
    /// </summary>
    public class GestionReportesRecepcion
    {
        private const string ESTADO_DISPONIBLE = "Disponible";
        private const string ESTADO_OCUPADA = "Ocupada";
        private const string ESTADO_LIMPIEZA = "Limpieza";
        private const string ESTADO_MANTENIMIENTO = "Mantenimiento";

        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();
        private readonly GestionConfiguracion _gestionConfiguracion = new();

        public ReporteRecepcion Generar(string dniUsuario)
        {
            DateTime ahora = DateTime.Now;
            var reporte = new ReporteRecepcion
            {
                Generado = ahora,
                MinutosTolerancia = _gestionConfiguracion.Obtener().MinutosTolerancia
            };

            List<HabitacionResumen> habitaciones = _gestionHabitaciones.ObtenerResumenHabitaciones();

            // Estadías en curso: se identifican por habitación + momento de entrada, porque
            // HabitacionResumen no trae el id del hospedaje.
            var estadiasEnCurso = new HashSet<(int, DateTime)>(habitaciones
                .Where(h => h.Ocupada && h.HoraEntrada.HasValue)
                .Select(h => (h.NroHabitacion, h.HoraEntrada!.Value)));

            CargarEstadoHabitaciones(reporte, habitaciones);
            CargarLlegadasYSalidas(reporte, habitaciones, estadiasEnCurso, ahora);
            CargarTurno(reporte, dniUsuario, estadiasEnCurso, ahora);

            reporte.ResumenAhora = RedactarResumenAhora(reporte);
            reporte.ResumenTurno = RedactarResumenTurno(reporte);
            return reporte;
        }

        private static void CargarEstadoHabitaciones(ReporteRecepcion reporte, List<HabitacionResumen> habitaciones)
        {
            reporte.TotalHabitaciones = habitaciones.Count;
            reporte.Libres = habitaciones.Count(h => EsEstado(h, ESTADO_DISPONIBLE));
            reporte.Ocupadas = habitaciones.Count(h => EsEstado(h, ESTADO_OCUPADA));
            reporte.EnLimpieza = habitaciones.Count(h => EsEstado(h, ESTADO_LIMPIEZA));
            reporte.FueraDeServicio = habitaciones.Count(h => EsEstado(h, ESTADO_MANTENIMIENTO));
        }

        private void CargarLlegadasYSalidas(ReporteRecepcion reporte, List<HabitacionResumen> habitaciones,
            HashSet<(int, DateTime)> estadiasEnCurso, DateTime ahora)
        {
            DateTime hoy = ahora.Date;
            List<Hospedaje> hospedajesDeHoy = HospedajeDAO.ObtenerEnRango(hoy, hoy);
            var nombres = new Dictionary<string, string>();

            // Llegadas: check-ins registrados hoy.
            reporte.Llegadas = hospedajesDeHoy
                .Where(h => h.FechaEntrada.Date == hoy)
                .Select(h => new MovimientoRecepcion
                {
                    NroHabitacion = h.NroHabitacion,
                    Huesped = NombreHuesped(h.DniHuesped, nombres),
                    Hora = h.FechaEntrada.Date + h.HoraEntrada,
                    Estado = EstadoMovimiento.Ingreso
                })
                .OrderByDescending(m => m.Hora)
                .ToList();

            var salidas = new List<MovimientoRecepcion>();

            // Huéspedes todavía alojados: vencidos (de cualquier día) o con salida prevista para hoy.
            foreach (HabitacionResumen habitacion in habitaciones.Where(h => h.Ocupada && h.HoraSalidaEstimada.HasValue))
            {
                DateTime salida = habitacion.HoraSalidaEstimada!.Value;
                EstadoMovimiento? estado = null;

                if (salida <= ahora)
                {
                    estado = EstadoMovimiento.SalidaVencida;
                }
                else if (salida.Date == hoy)
                {
                    estado = (salida - ahora).TotalMinutes <= reporte.MinutosTolerancia
                        ? EstadoMovimiento.SalidaProxima
                        : EstadoMovimiento.SalidaPendiente;
                }

                if (estado.HasValue)
                {
                    salidas.Add(new MovimientoRecepcion
                    {
                        NroHabitacion = habitacion.NroHabitacion,
                        Huesped = habitacion.Huesped ?? habitacion.DniHuesped ?? "-",
                        Hora = salida,
                        Estado = estado.Value
                    });
                }
            }

            // Check-outs ya hechos hoy: al registrar la salida, fecha/hora de salida pasan a ser las reales.
            foreach (Hospedaje h in hospedajesDeHoy)
            {
                DateTime entrada = h.FechaEntrada.Date + h.HoraEntrada;
                DateTime salida = h.FechaSalida.Date + h.HoraSalida;

                if (salida.Date == hoy && salida <= ahora && !estadiasEnCurso.Contains((h.NroHabitacion, entrada)))
                {
                    salidas.Add(new MovimientoRecepcion
                    {
                        NroHabitacion = h.NroHabitacion,
                        Huesped = NombreHuesped(h.DniHuesped, nombres),
                        Hora = salida,
                        Estado = EstadoMovimiento.SalidaRealizada
                    });
                }
            }

            // Primero lo urgente: vencidas, después las que se van pronto, las pendientes y al final las hechas.
            reporte.Salidas = salidas
                .OrderBy(s => OrdenUrgencia(s.Estado))
                .ThenBy(s => s.Estado == EstadoMovimiento.SalidaRealizada ? -s.Hora.Ticks : s.Hora.Ticks)
                .ToList();
        }

        private void CargarTurno(ReporteRecepcion reporte, string dniUsuario, HashSet<(int, DateTime)> estadiasEnCurso, DateTime ahora)
        {
            TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(dniUsuario);
            reporte.HayTurnoAbierto = turno != null;
            if (turno == null)
            {
                return;
            }

            DateTime inicio = turno.FechaApertura.Date + turno.HoraApertura;
            ResumenTurno resumen = _gestionTurnoCaja.ObtenerResumen(turno.IdTurno);

            reporte.InicioTurno = inicio;
            reporte.CambioInicial = resumen.Turno.MontoInicial;
            reporte.CobradoEfectivo = resumen.TotalEfectivo;
            reporte.CobradoTarjeta = resumen.TotalTarjeta;
            reporte.CobradoTransferencia = resumen.TotalTransferencia;
            reporte.CobradoAlojamiento = resumen.TotalAlojamiento;
            reporte.CobradoVentas = resumen.TotalVentas;

            reporte.CheckInsTurno = HospedajeDAO.ObtenerPorTurno(turno.IdTurno).Count;

            List<Venta> ventas = GestionTurnoCaja.ObtenerVentasDelTurno(turno);
            reporte.CantidadVentas = ventas.Count;
            reporte.VentasMostrador = ventas.Count(v => string.IsNullOrWhiteSpace(v.DniHuesped));

            reporte.CheckOutsTurno = HospedajeDAO.ObtenerEnRango(inicio.Date, ahora.Date).Count(h =>
            {
                DateTime salida = h.FechaSalida.Date + h.HoraSalida;
                return salida >= inicio && salida <= ahora
                    && !estadiasEnCurso.Contains((h.NroHabitacion, h.FechaEntrada.Date + h.HoraEntrada));
            });
        }

        // ------------------------------------------------------------------
        // Semana y Mes
        // ------------------------------------------------------------------

        /// <summary>Últimos 7 días (incluido hoy), comparados con los 7 días anteriores.</summary>
        public ReportePeriodo GenerarSemana()
        {
            DateTime hoy = DateTime.Today;
            DateTime desde = hoy.AddDays(-6);
            ReportePeriodo r = GenerarPeriodo(desde, hoy, "los últimos 7 días", esSemana: true);

            r.NombrePeriodoAnterior = "la semana anterior";
            r.HuespedesPeriodoAnterior = HospedajeDAO.ObtenerEnRango(desde.AddDays(-7), desde.AddDays(-1))
                .Count(h => h.FechaEntrada.Date >= desde.AddDays(-7) && h.FechaEntrada.Date <= desde.AddDays(-1));

            r.Resumen = RedactarResumenSemana(r);
            return r;
        }

        /// <summary>Mes en curso (del 1 a hoy) o el mes anterior completo.</summary>
        public ReportePeriodo GenerarMes(bool mesAnterior)
        {
            DateTime hoy = DateTime.Today;
            DateTime inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime desde = mesAnterior ? inicioMesActual.AddMonths(-1) : inicioMesActual;
            DateTime hasta = mesAnterior ? inicioMesActual.AddDays(-1) : hoy;

            string nombre = desde.ToString("MMMM", CulturaAr);
            if (desde.Year != hoy.Year) nombre += $" {desde.Year}";

            ReportePeriodo r = GenerarPeriodo(desde, hasta, nombre, esSemana: false);
            r.Resumen = RedactarResumenMes(r, mesAnterior);
            return r;
        }

        private ReportePeriodo GenerarPeriodo(DateTime desde, DateTime hasta, string nombrePeriodo, bool esSemana)
        {
            // Ocupación, limpieza, rotación e ingresos estimados: mismos cálculos que el reporte del Administrador.
            ReporteHotel base_ = new GestionReportes().Generar(desde, hasta);
            List<Hospedaje> hospedajes = HospedajeDAO.ObtenerEnRango(desde, hasta);
            List<Hospedaje> entradas = hospedajes.Where(h => h.FechaEntrada.Date >= desde && h.FechaEntrada.Date <= hasta).ToList();

            var r = new ReportePeriodo
            {
                Desde = desde,
                Hasta = hasta,
                NombrePeriodo = nombrePeriodo,
                TotalHabitaciones = HabitacionDAO.ObtenerTodas().Count,
                PorcentajeOcupacion = base_.PromedioOcupacionPorcentaje,
                NochesPorHuesped = base_.PromedioDuracionEstadiaDias,
                MinutosLimpieza = base_.PromedioLimpiezaMinutos,
                CantidadLimpiezas = RegistroLimpiezaDAO.ObtenerEnRango(desde, hasta).Count,
                IngresosEstimados = base_.TotalIngresos,
                HuespedesQueEntraron = entradas.Count
            };

            int dias = (hasta - desde).Days + 1;
            r.OcupacionDiaria = Enumerable.Range(0, dias).Select(i => new PuntoSerie
            {
                Etiqueta = EtiquetaDia(desde.AddDays(i), esSemana),
                Valor = i < base_.OcupacionDiaria.Count ? base_.OcupacionDiaria[i].Valor : 0
            }).ToList();

            r.LlegadasDiarias = Enumerable.Range(0, dias).Select(i => new PuntoSerie
            {
                Etiqueta = EtiquetaDia(desde.AddDays(i), esSemana),
                Valor = entradas.Count(h => h.FechaEntrada.Date == desde.AddDays(i))
            }).ToList();

            double maximo = r.OcupacionDiaria.Count == 0 ? 0 : r.OcupacionDiaria.Max(p => p.Valor);
            r.IndiceDiaMasLleno = maximo > 0 ? r.OcupacionDiaria.FindIndex(p => p.Valor == maximo) : -1;

            r.HabitacionesMasPedidas = base_.RotacionPorHabitacion;
            r.HabitacionesQueMasFacturaron = base_.IngresosPorHabitacion.Where(p => p.Valor > 0).ToList();

            // Huéspedes que vuelven: personas (mismo DNI) que entraron en el período y ya tenían una
            // estadía anterior a su primera entrada del período.
            var primeraEntradaPorDni = entradas
                .Where(h => !string.IsNullOrWhiteSpace(h.DniHuesped))
                .GroupBy(h => h.DniHuesped)
                .ToDictionary(g => g.Key, g => g.Min(h => h.FechaEntrada.Date + h.HoraEntrada));
            r.HuespedesDistintos = primeraEntradaPorDni.Count;

            if (primeraEntradaPorDni.Count > 0)
            {
                List<Hospedaje> historial = HospedajeDAO.ObtenerEnRango(new DateTime(2000, 1, 1), hasta);
                r.HuespedesQueVuelven = primeraEntradaPorDni.Count(par =>
                    historial.Any(h => h.DniHuesped == par.Key && h.FechaEntrada.Date + h.HoraEntrada < par.Value));
            }

            return r;
        }

        private static string EtiquetaDia(DateTime dia, bool esSemana)
        {
            if (!esSemana) return dia.Day.ToString(CulturaAr);
            if (dia == DateTime.Today) return "Hoy";

            string nombre = dia.ToString("ddd", CulturaAr).TrimEnd('.');
            return $"{char.ToUpper(nombre[0], CulturaAr)}{nombre.Substring(1)} {dia.Day}";
        }

        /// <summary>Ocupación promedio en habitaciones: «unas 12 de 20», «menos de 1 de 14» o «0 de 14».</summary>
        public static string HabitacionesPorNoche(ReportePeriodo r, bool corto = false)
        {
            string cantidad = r.PorcentajeOcupacion <= 0 ? "0"
                : r.HabitacionesPromedioPorNoche == 0 ? "menos de 1"
                : $"unas {r.HabitacionesPromedioPorNoche}";
            return $"{cantidad} de {r.TotalHabitaciones}{(corto ? "" : " habitaciones")} por noche";
        }

        /// <summary>Minutos en texto corto: «34 min» o «menos de 1 min».</summary>
        public static string Minutos(double minutos) => minutos < 1 ? "menos de 1 min" : $"{minutos:0} min";

        /// <summary>«sábado 31» para usar dentro de una frase.</summary>
        public static string NombreDia(DateTime dia) =>
            dia == DateTime.Today ? "hoy" : dia.ToString("dddd d", CulturaAr);

        // ------------------------------------------------------------------
        // Frases resumen
        // ------------------------------------------------------------------

        private static string RedactarResumenSemana(ReportePeriodo r)
        {
            if (!r.HayMovimiento)
            {
                return "En los últimos 7 días no hubo huéspedes alojados.";
            }

            int diferencia = r.HuespedesQueEntraron - r.HuespedesPeriodoAnterior;
            string comparacion = diferencia > 0 ? $"{diferencia} más que la semana anterior"
                : diferencia < 0 ? $"{-diferencia} menos que la semana anterior"
                : "igual que la semana anterior";

            string texto = r.HuespedesQueEntraron == 1
                ? $"En los últimos 7 días entró 1 huésped ({comparacion})."
                : $"En los últimos 7 días entraron {r.HuespedesQueEntraron} huéspedes ({comparacion}).";

            if (r.DiaMasLleno.HasValue)
            {
                texto += $" El día más lleno fue {(r.DiaMasLleno.Value == DateTime.Today ? "hoy" : "el " + NombreDia(r.DiaMasLleno.Value))} " +
                         $"({r.OcupadasDiaMasLleno} de {r.TotalHabitaciones} habitaciones).";
            }

            if (r.CantidadLimpiezas > 0)
            {
                texto += r.MinutosLimpieza < 1
                    ? " Las limpiezas registradas tardaron menos de 1 minuto en promedio."
                    : $" Una habitación tardó en promedio {r.MinutosLimpieza:0} minutos en quedar limpia.";
            }

            return texto;
        }

        private static string RedactarResumenMes(ReportePeriodo r, bool mesAnterior)
        {
            if (!r.HayMovimiento)
            {
                return $"En {r.NombrePeriodo} no hubo huéspedes alojados{(mesAnterior ? "" : " hasta hoy")}.";
            }

            string texto = $"En {r.NombrePeriodo}{(mesAnterior ? "" : " (hasta hoy)")} el hotel estuvo ocupado al " +
                           $"{r.PorcentajeOcupacion.ToString("0", CulturaAr)} % en promedio ({HabitacionesPorNoche(r)}).";

            if (r.HuespedesQueVuelven > 0)
            {
                texto += r.HuespedesQueVuelven == 1
                    ? $" 1 de los {r.HuespedesDistintos} huéspedes ya se había alojado antes."
                    : $" {r.HuespedesQueVuelven} de los {r.HuespedesDistintos} huéspedes ya se habían alojado antes.";
            }

            return texto;
        }

        // ------------------------------------------------------------------
        // Frases resumen (Ahora / Mi turno)
        // ------------------------------------------------------------------

        private static string RedactarResumenAhora(ReporteRecepcion r)
        {
            if (r.TotalHabitaciones == 0)
            {
                return "Todavía no hay habitaciones cargadas en el sistema.";
            }

            string texto = r.Libres == 0
                ? "No quedan habitaciones libres para entregar en este momento."
                : $"{Plural(r.Libres, "Hay 1 habitación libre", $"Hay {r.Libres} habitaciones libres")} para entregar ahora.";

            int faltan = r.SalidasQueFaltanHoy;
            texto += faltan == 0
                ? " No quedan salidas pendientes para hoy."
                : $" {Plural(faltan, "Falta 1 salida", $"Faltan {faltan} salidas")} de hoy.";

            List<MovimientoRecepcion> vencidas = r.SalidasVencidas;
            if (vencidas.Count > 0)
            {
                string habitaciones = string.Join(", ", vencidas.Select(v => v.NroHabitacion));
                texto += vencidas.Count == 1
                    ? $" 1 huésped ya pasó su hora de salida (Hab. {habitaciones})."
                    : $" {vencidas.Count} huéspedes ya pasaron su hora de salida (Hab. {habitaciones}).";
            }

            return texto;
        }

        private static string RedactarResumenTurno(ReporteRecepcion r)
        {
            if (!r.HayTurnoAbierto)
            {
                return "Todavía no abriste la caja, así que no hay cobros para mostrar. Abrí tu turno desde Caja.";
            }

            string desde = r.InicioTurno!.Value.Date == r.Generado.Date
                ? $"Desde las {r.InicioTurno.Value:HH:mm}"
                : $"Desde el {r.InicioTurno.Value.ToString("dd/MM", CulturaAr)} a las {r.InicioTurno.Value:HH:mm}";

            string cobrado = r.TotalCobrado == 0
                ? $"{desde} todavía no registraste cobros."
                : $"{desde} cobraste {Moneda(r.TotalCobrado)}.";

            return $"{cobrado} Al cerrar, en la caja debería haber {Moneda(r.EfectivoEsperado)} en efectivo.";
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        public static string Moneda(decimal valor) => valor.ToString("C0", CulturaAr);

        /// <summary>Texto corto del estado de una llegada/salida, igual en pantalla y en el PDF
        /// (ej. «Pasó hace 40 min», «Sale en 25 min», «Salió 11:05»).</summary>
        public static string DescribirEstado(MovimientoRecepcion m, DateTime ahora) => m.Estado switch
        {
            EstadoMovimiento.Ingreso => $"Ingresó {m.Hora:HH:mm}",
            EstadoMovimiento.SalidaRealizada => $"Salió {m.Hora:HH:mm}",
            EstadoMovimiento.SalidaVencida => $"Pasó hace {Duracion(ahora - m.Hora)}",
            EstadoMovimiento.SalidaProxima => $"Sale en {Duracion(m.Hora - ahora)}",
            _ => $"Sale a las {m.Hora:HH:mm}"
        };

        /// <summary>Duración en palabras simples: «40 min», «2 h 05 min», «3 días».</summary>
        public static string Duracion(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = t.Negate();
            if (t.TotalDays >= 1)
            {
                int dias = (int)t.TotalDays;
                return dias == 1 ? "1 día" : $"{dias} días";
            }
            if (t.TotalMinutes < 60) return $"{Math.Max(1, (int)t.TotalMinutes)} min";
            return $"{(int)t.TotalHours} h {t.Minutes:00} min";
        }

        private static string Plural(int cantidad, string singular, string plural) => cantidad == 1 ? singular : plural;

        private static bool EsEstado(HabitacionResumen h, string estado) =>
            string.Equals(h.Estado, estado, StringComparison.OrdinalIgnoreCase);

        private static int OrdenUrgencia(EstadoMovimiento estado) => estado switch
        {
            EstadoMovimiento.SalidaVencida => 0,
            EstadoMovimiento.SalidaProxima => 1,
            EstadoMovimiento.SalidaPendiente => 2,
            _ => 3
        };

        private static string NombreHuesped(string dni, Dictionary<string, string> cache)
        {
            if (string.IsNullOrWhiteSpace(dni)) return "-";
            if (cache.TryGetValue(dni, out string? nombre)) return nombre;

            Huesped? huesped = HuespedDAO.ObtenerPorDni(dni);
            nombre = huesped != null ? $"{huesped.Nombre} {huesped.Apellido}".Trim() : $"DNI {dni}";
            cache[dni] = nombre;
            return nombre;
        }
    }
}
