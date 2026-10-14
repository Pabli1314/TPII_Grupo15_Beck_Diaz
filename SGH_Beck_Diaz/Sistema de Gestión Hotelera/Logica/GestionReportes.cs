using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Reportes y estadísticas del Administrador. Ocupación y rotación salen de Hospedaje real;
    /// los ingresos se estiman según la tarifa vigente asociada al tipo de habitación de cada unidad.
    /// </summary>
    public class GestionReportes
    {
        private readonly GestionTarifas _gestionTarifas = new();

        public ReporteHotel Generar(DateTime desde, DateTime hasta)
        {
            desde = desde.Date;
            hasta = hasta.Date;

            List<Hospedaje> hospedajes = HospedajeDAO.ObtenerEnRango(desde, hasta);
            List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();
            List<RegistroLimpieza> limpiezas = RegistroLimpiezaDAO.ObtenerEnRango(desde, hasta);

            // Obtiene las tarifas vigentes por Tipo de Habitación
            List<Tarifa> tarifas = _gestionTarifas.ObtenerTarifas();
            Dictionary<int, decimal> tarifaPorTipo = tarifas
                .GroupBy(t => t.IdTipoHabitacion)
                .ToDictionary(g => g.Key, g => g.First().PrecioPorHora); // Precio base por día/estadía

            // Mapea la tarifa correspondiente a cada número de habitación
            Dictionary<int, decimal> tarifaPorHabitacion = habitaciones
                .ToDictionary(
                    h => h.NroHabitacion,
                    h => tarifaPorTipo.TryGetValue(h.IdTipoHabitacion, out decimal precio) ? precio : 0m
                );

            var reporte = new ReporteHotel { Desde = desde, Hasta = hasta };

            reporte.OcupacionDiaria = hospedajes
                .GroupBy(h => h.FechaEntrada.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PuntoSerie { Etiqueta = g.Key.ToString("dd/MM"), Valor = g.Count() })
                .ToList();

            reporte.IngresosDiarios = hospedajes
                .GroupBy(h => h.FechaEntrada.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PuntoSerie
                {
                    Etiqueta = g.Key.ToString("dd/MM"),
                    Valor = (double)g.Sum(h => CalcularMontoEstadia(h, tarifaPorHabitacion))
                })
                .ToList();

            reporte.IngresosPorHabitacion = hospedajes
                .GroupBy(h => h.NroHabitacion)
                .Select(g => new PuntoSerie
                {
                    Etiqueta = $"Hab. {g.Key}",
                    Valor = (double)g.Sum(h => CalcularMontoEstadia(h, tarifaPorHabitacion))
                })
                .OrderByDescending(p => p.Valor)
                .Take(10)
                .ToList();

            reporte.RotacionPorHabitacion = hospedajes
                .GroupBy(h => h.NroHabitacion)
                .Select(g => new PuntoSerie { Etiqueta = $"Hab. {g.Key}", Valor = g.Count() })
                .OrderByDescending(p => p.Valor)
                .ToList();

            reporte.TotalOcupaciones = hospedajes.Count;
            reporte.TotalIngresos = hospedajes.Sum(h => CalcularMontoEstadia(h, tarifaPorHabitacion));

            int diasRango = Math.Max(1, (hasta - desde).Days + 1);
            int totalHabitaciones = Math.Max(1, habitaciones.Count);
            reporte.PromedioOcupacionPorcentaje = Math.Min(100, hospedajes.Count * 100.0 / (totalHabitaciones * diasRango));

            reporte.PromedioDuracionEstadiaDias = hospedajes.Count > 0
                ? hospedajes.Average(h => Math.Max(1, (h.FechaSalida.Date - h.FechaEntrada.Date).Days))
                : 0;

            reporte.PromedioLimpiezaMinutos = limpiezas.Count > 0
                ? limpiezas.Average(l => (l.HoraFin - l.HoraInicio).TotalMinutes)
                : 0;

            return reporte;
        }

        /// <summary>Estimación del monto de una estadía según la tarifa asignada a la habitación por cada noche.</summary>
        private static decimal CalcularMontoEstadia(Hospedaje hospedaje, Dictionary<int, decimal> tarifaPorHabitacion)
        {
            decimal tarifa = tarifaPorHabitacion.TryGetValue(hospedaje.NroHabitacion, out decimal valor) ? valor : 0m;
            int noches = Math.Max(1, (hospedaje.FechaSalida.Date - hospedaje.FechaEntrada.Date).Days);
            return tarifa * noches;
        }
    }
}