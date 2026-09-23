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

    /// <summary>
    /// Resultado agregado que arma Logica.GestionReportes para la pantalla de Reportes del
    /// Administrador, a partir de datos reales de Hospedaje/RegistroLimpieza en un rango de fechas.
    /// </summary>
    public class ReporteHotel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        public List<PuntoSerie> OcupacionDiaria { get; set; } = new();
        public List<PuntoSerie> IngresosDiarios { get; set; } = new();
        public List<PuntoSerie> IngresosPorHabitacion { get; set; } = new();
        public List<PuntoSerie> RotacionPorHabitacion { get; set; } = new();

        public int TotalOcupaciones { get; set; }
        public decimal TotalIngresos { get; set; }
        public double PromedioOcupacionPorcentaje { get; set; }
        public double PromedioDuracionEstadiaDias { get; set; }
        public double PromedioLimpiezaMinutos { get; set; }
    }
}
