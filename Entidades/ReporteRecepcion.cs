using System;
using System.Collections.Generic;
using System.Linq;

namespace Entidades
{
    /// <summary>Situación de una llegada o salida del día, tal como la ve el recepcionista.</summary>
    public enum EstadoMovimiento
    {
        /// <summary>El huésped ya hizo el check-in hoy.</summary>
        Ingreso,
        /// <summary>El huésped ya hizo el check-out.</summary>
        SalidaRealizada,
        /// <summary>Se va hoy y todavía falta para su hora de salida.</summary>
        SalidaPendiente,
        /// <summary>Se va hoy y su hora de salida está dentro de los minutos de tolerancia.</summary>
        SalidaProxima,
        /// <summary>Ya pasó su hora de salida y sigue en la habitación.</summary>
        SalidaVencida
    }

    /// <summary>Una fila de las listas «Llegadas de hoy» y «Salidas de hoy».</summary>
    public class MovimientoRecepcion
    {
        public int NroHabitacion { get; set; }
        public string Huesped { get; set; } = string.Empty;
        public DateTime Hora { get; set; }
        public EstadoMovimiento Estado { get; set; }
    }

    /// <summary>
    /// Datos de la pantalla Reportes de Recepción. Lo arma Logica.GestionReportesRecepcion a partir de
    /// datos reales (habitaciones, hospedajes, turno de caja y ventas). No persiste.
    /// </summary>
    public class ReporteRecepcion
    {
        public DateTime Generado { get; set; } = DateTime.Now;
        public int MinutosTolerancia { get; set; }

        // ---------- Ahora ----------
        public int TotalHabitaciones { get; set; }
        public int Libres { get; set; }
        public int Ocupadas { get; set; }
        public int EnLimpieza { get; set; }
        public int FueraDeServicio { get; set; }

        public List<MovimientoRecepcion> Llegadas { get; set; } = new();
        public List<MovimientoRecepcion> Salidas { get; set; } = new();

        public int SalidasRealizadasHoy => Salidas.Count(s => s.Estado == EstadoMovimiento.SalidaRealizada);
        public int SalidasQueFaltanHoy => Salidas.Count(s => s.Estado is EstadoMovimiento.SalidaPendiente or EstadoMovimiento.SalidaProxima);
        public List<MovimientoRecepcion> SalidasVencidas => Salidas.Where(s => s.Estado == EstadoMovimiento.SalidaVencida).ToList();

        public double PorcentajeOcupacion => TotalHabitaciones > 0 ? Ocupadas * 100.0 / TotalHabitaciones : 0;

        // ---------- Mi turno ----------
        public bool HayTurnoAbierto { get; set; }
        public DateTime? InicioTurno { get; set; }
        public decimal CambioInicial { get; set; }
        public decimal CobradoEfectivo { get; set; }
        public decimal CobradoTarjeta { get; set; }
        public decimal CobradoTransferencia { get; set; }
        public decimal CobradoAlojamiento { get; set; }
        public decimal CobradoVentas { get; set; }
        public int CheckInsTurno { get; set; }
        public int CheckOutsTurno { get; set; }
        public int CantidadVentas { get; set; }
        public int VentasMostrador { get; set; }

        public decimal TotalCobrado => CobradoEfectivo + CobradoTarjeta + CobradoTransferencia;
        public decimal EfectivoEsperado => CambioInicial + CobradoEfectivo;

        // ---------- Frases resumen (las arma la lógica) ----------
        public string ResumenAhora { get; set; } = string.Empty;
        public string ResumenTurno { get; set; } = string.Empty;
    }

    /// <summary>
    /// Datos de las pestañas «Semana» y «Mes» de Reportes de Recepción. Lo arma
    /// Logica.GestionReportesRecepcion a partir de hospedajes y limpiezas reales. No persiste.
    /// </summary>
    public class ReportePeriodo
    {
        public DateTime Generado { get; set; } = DateTime.Now;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        /// <summary>Nombre del período en palabras: «los últimos 7 días», «noviembre», «octubre 2026».</summary>
        public string NombrePeriodo { get; set; } = string.Empty;
        public int TotalHabitaciones { get; set; }

        /// <summary>Habitaciones con huésped en cada día del período (etiqueta: «Sáb 31», «Hoy» o el número de día).</summary>
        public List<PuntoSerie> OcupacionDiaria { get; set; } = new();
        /// <summary>Check-ins hechos en cada día del período.</summary>
        public List<PuntoSerie> LlegadasDiarias { get; set; } = new();
        /// <summary>Veces que se alojó alguien en cada habitación, de mayor a menor.</summary>
        public List<PuntoSerie> HabitacionesMasPedidas { get; set; } = new();
        /// <summary>Ingresos estimados (tarifa × noches) por habitación, de mayor a menor.</summary>
        public List<PuntoSerie> HabitacionesQueMasFacturaron { get; set; } = new();

        public int HuespedesQueEntraron { get; set; }
        public int HuespedesPeriodoAnterior { get; set; }
        public string NombrePeriodoAnterior { get; set; } = string.Empty;
        public int HuespedesDistintos { get; set; }
        public int HuespedesQueVuelven { get; set; }

        public double PorcentajeOcupacion { get; set; }
        public double NochesPorHuesped { get; set; }
        public double MinutosLimpieza { get; set; }
        public int CantidadLimpiezas { get; set; }
        public decimal IngresosEstimados { get; set; }

        /// <summary>Índice del día con más habitaciones ocupadas (-1 si no hubo ocupación).</summary>
        public int IndiceDiaMasLleno { get; set; } = -1;
        public DateTime? DiaMasLleno => IndiceDiaMasLleno >= 0 ? Desde.AddDays(IndiceDiaMasLleno) : null;
        public int OcupadasDiaMasLleno => IndiceDiaMasLleno >= 0 ? (int)OcupacionDiaria[IndiceDiaMasLleno].Valor : 0;

        /// <summary>Promedio de habitaciones ocupadas por noche (redondeado), para leer el porcentaje como «≈ N de T».</summary>
        public int HabitacionesPromedioPorNoche => (int)Math.Round(PorcentajeOcupacion * TotalHabitaciones / 100.0);

        public bool HayMovimiento => HuespedesQueEntraron > 0 || OcupacionDiaria.Any(p => p.Valor > 0);

        public string Resumen { get; set; } = string.Empty;
    }
}
