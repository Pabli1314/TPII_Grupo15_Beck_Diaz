using System;
using System.Collections.Generic;

namespace Entidades
{
    /// <summary>Un punto de una serie de reporte (gráfico de barras/líneas).</summary>
    public class PuntoSerie
    {
        public string Etiqueta { get; set; } = string.Empty;
        public double Valor { get; set; }
    }

    /// <summary>Una porción de un gráfico de distribución (dona): cantidad y su peso sobre el total.</summary>
    public class SegmentoDistribucion
    {
        public string Etiqueta { get; set; } = string.Empty;
        public double Valor { get; set; }
        public double Porcentaje { get; set; }
    }

    /// <summary>Tipo de huésped según su historial en el hotel.</summary>
    public enum TipoHuespedFiltro
    {
        Todos,
        /// <summary>Estadías que son la primera visita del huésped al hotel.</summary>
        Nuevos,
        /// <summary>Estadías de huéspedes que ya se habían alojado antes.</summary>
        Recurrentes
    }

    /// <summary>Filtros opcionales y combinables de la pantalla de Reportes. null = sin filtrar.</summary>
    public class FiltroReporte
    {
        public int? IdTipoHabitacion { get; set; }
        /// <summary>Estado ACTUAL de la habitación (Disponible, Ocupada, Limpieza, Mantenimiento…).</summary>
        public int? IdEstado { get; set; }
        public TipoHuespedFiltro TipoHuesped { get; set; } = TipoHuespedFiltro.Todos;
        /// <summary>Forma de pago con que ingresó la estadía (único "canal" que registra la base).</summary>
        public int? IdMetodo { get; set; }

        public bool HayFiltros => IdTipoHabitacion.HasValue || IdEstado.HasValue || IdMetodo.HasValue || TipoHuesped != TipoHuespedFiltro.Todos;
    }

    /// <summary>Tono de una conclusión: define el color con que se la destaca en pantalla.</summary>
    public enum TonoInsight
    {
        Neutro,
        Positivo,
        Atencion
    }

    /// <summary>Conclusión escrita en lenguaje natural ("El tipo más demandado fue…").</summary>
    public class InsightReporte
    {
        public string Texto { get; set; } = string.Empty;
        public TonoInsight Tono { get; set; }
    }

    /// <summary>Fila de la tabla detallada por tipo de habitación.</summary>
    public class ResumenTipoHabitacion
    {
        public string Tipo { get; set; } = string.Empty;
        public int Habitaciones { get; set; }
        public int Estadias { get; set; }
        public int NochesOcupadas { get; set; }
        public double OcupacionPorcentaje { get; set; }
        public decimal Ingresos { get; set; }
        public decimal IngresoPromedioPorEstadia { get; set; }
        public double ParticipacionIngresosPorcentaje { get; set; }
    }

    /// <summary>Fila de la tabla detallada por habitación.</summary>
    public class ResumenHabitacion
    {
        public int NroHabitacion { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string EstadoActual { get; set; } = string.Empty;
        public int Estadias { get; set; }
        public int NochesOcupadas { get; set; }
        public decimal Ingresos { get; set; }
    }

    /// <summary>
    /// Resultado agregado que arma Logica.GestionReportes para la pantalla de Reportes del
    /// Administrador, a partir de datos reales de Hospedaje/RegistroLimpieza en un rango de fechas.
    /// </summary>
    public class ReporteHotel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        // --- Series diarias (las usa también el PDF: un punto por día del rango) ---
        public List<PuntoSerie> OcupacionDiaria { get; set; } = new();
        public List<PuntoSerie> IngresosDiarios { get; set; } = new();
        public List<PuntoSerie> IngresosPorHabitacion { get; set; } = new();
        public List<PuntoSerie> RotacionPorHabitacion { get; set; } = new();

        public int TotalOcupaciones { get; set; }
        public decimal TotalIngresos { get; set; }
        public double PromedioOcupacionPorcentaje { get; set; }
        public double PromedioDuracionEstadiaDias { get; set; }
        public double PromedioLimpiezaMinutos { get; set; }

        // --- Contexto del período y comparativa automática ---
        /// <summary>Texto con el que se compara: "ayer", "el mes pasado", "el período anterior"…</summary>
        public string EtiquetaComparacion { get; set; } = string.Empty;
        public DateTime DesdeAnterior { get; set; }
        public DateTime HastaAnterior { get; set; }
        public int TotalOcupacionesAnterior { get; set; }
        public decimal TotalIngresosAnterior { get; set; }
        public double PromedioOcupacionAnteriorPorcentaje { get; set; }
        public double PromedioDuracionEstadiaAnteriorDias { get; set; }
        public double MetaOcupacionPorcentaje { get; set; }

        // --- Foto actual del hotel (no depende del período) ---
        public int TotalHabitaciones { get; set; }
        public int HabitacionesOcupadasAhora { get; set; }
        public double OcupacionActualPorcentaje { get; set; }
        public int HabitacionesEnMantenimiento { get; set; }
        public List<SegmentoDistribucion> EstadoActualHabitaciones { get; set; } = new();

        // --- Tendencias (por día o por mes según el largo del período) ---
        /// <summary>"día" o "mes": cómo está agrupada la tendencia.</summary>
        public string GranularidadTendencia { get; set; } = "día";
        public List<PuntoSerie> TendenciaOcupacionPorcentaje { get; set; } = new();
        public List<PuntoSerie> TendenciaIngresos { get; set; } = new();

        // --- Distribuciones ---
        public List<SegmentoDistribucion> EstadiasPorTipoHabitacion { get; set; } = new();
        public List<SegmentoDistribucion> EstadiasPorFormaPago { get; set; } = new();
        public List<SegmentoDistribucion> EstadiasPorTipoHuesped { get; set; } = new();
        public int HuespedesDistintos { get; set; }

        // --- Detalle ---
        public List<ResumenTipoHabitacion> DetallePorTipo { get; set; } = new();
        public List<ResumenHabitacion> DetallePorHabitacion { get; set; } = new();

        /// <summary>Conclusiones rápidas en lenguaje natural, de la más a la menos importante.</summary>
        public List<InsightReporte> Insights { get; set; } = new();
    }
}
