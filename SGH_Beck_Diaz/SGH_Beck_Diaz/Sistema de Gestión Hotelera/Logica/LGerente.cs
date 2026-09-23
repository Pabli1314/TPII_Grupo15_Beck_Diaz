using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    public class LGerente
    {
        private readonly HabitacionesDAO habitacionesDAO = new HabitacionesDAO();

        public (int[] VectorEstados, int[] VectorTipos) ObtenerEstadisticasHabitaciones()
        {
            List<Habitacion> listaHabitaciones = habitacionesDAO.ListarHabitaciones() ?? new List<Habitacion>();

            int[] vectorEstados = new int[4];
            vectorEstados[0] = listaHabitaciones.Count(h => h.IdEstado == 1); // Disponible
            vectorEstados[1] = listaHabitaciones.Count(h => h.IdEstado == 2); // Ocupado / Ocupada
            vectorEstados[2] = listaHabitaciones.Count(h => h.IdEstado == 3); // Limpieza
            vectorEstados[3] = listaHabitaciones.Count(h => h.IdEstado == 4); // Mantenimiento

            int[] vectorTipos = new int[4];
            vectorTipos[0] = listaHabitaciones.Count(h => h.IdTipoHabitacion == 1); // Individual
            vectorTipos[1] = listaHabitaciones.Count(h => h.IdTipoHabitacion == 2); // Estándar
            vectorTipos[2] = listaHabitaciones.Count(h => h.IdTipoHabitacion == 3); // Doble Matrimonial
            vectorTipos[3] = listaHabitaciones.Count(h => h.IdTipoHabitacion == 4); // Suite Executive

            return (vectorEstados, vectorTipos);
        }

        public Dictionary<string, int> ObtenerEstadisticasHabitacionesDashboard()
        {
            return habitacionesDAO.ObtenerEstadoHabitaciones() ?? new Dictionary<string, int>();
        }

        public Dictionary<string, decimal> ObtenerEstadisticasIngresosMensuales()
        {
            return TurnoCajaDAO.ObtenerIngresosPorMes() ?? new Dictionary<string, decimal>();
        }

        public List<KeyValuePair<string, string>> ObtenerListaUsuarios()
        {
            return UsuarioDAO.ObtenerRecepcionista() ?? new List<KeyValuePair<string, string>>();
        }

        public (List<TurnoCaja> Lista, decimal TotalRecaudado, decimal PromedioTurno, int TotalTurnosCerrados, Dictionary<string, decimal> RecaudacionPorUsuario)
        ConsultarHistorialOperativo(DateTime fechaDesde, DateTime fechaHasta, string? dniUsuario = null)
        {
            if (fechaDesde.Date > fechaHasta.Date)
            {
                throw new ArgumentException("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
            }

            string? dniFiltro = string.IsNullOrWhiteSpace(dniUsuario) ? null : dniUsuario.Trim();

            DateTime inicio = fechaDesde.Date;
            DateTime fin = fechaHasta.Date.AddDays(1).AddTicks(-1);

            List<TurnoCaja> lista = TurnoCajaDAO.ObtenerHistorialFiltrado(inicio, fin, dniFiltro) ?? new List<TurnoCaja>();

            var cerrados = lista.Where(t => t.MontoFinal.HasValue).ToList();

            decimal totalRecaudado = cerrados.Sum(t => t.MontoFinal!.Value);
            int cantidadCerrados = cerrados.Count;
            decimal promedioTurno = cantidadCerrados > 0 ? totalRecaudado / cantidadCerrados : 0m;

            // Agrupamos para evitar excepciones si se repiten DNI en la lista de usuarios
            var mapaUsuarios = ObtenerListaUsuarios()
                .Where(u => !string.IsNullOrEmpty(u.Key))
                .GroupBy(u => u.Key)
                .ToDictionary(g => g.Key, g => g.First().Value);

            Dictionary<string, decimal> recaudacionPorUsuario = cerrados
                .GroupBy(t => t.DniUsuario)
                .ToDictionary(
                    g => !string.IsNullOrEmpty(g.Key) && mapaUsuarios.ContainsKey(g.Key) ? mapaUsuarios[g.Key] : $"DNI: {g.Key}",
                    g => g.Sum(t => t.MontoFinal!.Value)
                );

            return (lista, totalRecaudado, promedioTurno, cantidadCerrados, recaudacionPorUsuario);
        }

        public List<Habitacion> ConsultarHabitacionesPorEstado(string nombreEstado)
        {
            if (string.IsNullOrWhiteSpace(nombreEstado))
                return new List<Habitacion>();

            string estadoFiltro = nombreEstado.Trim();

            if (estadoFiltro.Equals("Ocupado", StringComparison.OrdinalIgnoreCase) ||
              estadoFiltro.Equals("Ocupada", StringComparison.OrdinalIgnoreCase))
            {
                estadoFiltro = "Ocupad";
            }
            else if (estadoFiltro.Equals("Disponible", StringComparison.OrdinalIgnoreCase) ||
                estadoFiltro.Equals("Disponibles", StringComparison.OrdinalIgnoreCase))
            {
                estadoFiltro = "Disponib";
            }

            return habitacionesDAO.ListarHabitacionesPorEstado(estadoFiltro) ?? new List<Habitacion>();
        }
    }
}