using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>
    /// Histórico del Administrador: arma una línea de tiempo con la actividad de los huéspedes
    /// (altas, ingresos, egresos y consumos), filtrable por huésped y por tipo de evento.
    /// No expone montos de ventas ni de caja: ese análisis financiero es del histórico de Gerencia.
    /// </summary>
    public class GestionHistorialAdministrador
    {
        public const string TipoAltaHuesped = "Alta de huésped";
        public const string TipoIngreso = "Ingreso (check-in)";
        public const string TipoEgreso = "Egreso (check-out)";
        public const string TipoConsumo = "Consumo";

        public static readonly string[] Tipos = { TipoAltaHuesped, TipoIngreso, TipoEgreso, TipoConsumo };

        public List<KeyValuePair<string, string>> ObtenerListaHuespedes()
        {
            return HuespedDAO.ObtenerTodos()
                .Where(h => !string.IsNullOrWhiteSpace(h.DniHuesped))
                .Select(h => new KeyValuePair<string, string>(h.DniHuesped.Trim(), $"{h.Apellido} {h.Nombre} ({h.DniHuesped.Trim()})".Trim()))
                .ToList();
        }

        public HistorialAdministradorResultado Consultar(DateTime desde, DateTime hasta, string? dniHuespedFiltro, string? tipoFiltro)
        {
            DateTime inicio = desde.Date;
            DateTime fin = hasta.Date.AddDays(1).AddTicks(-1);
            string? dniFiltro = string.IsNullOrWhiteSpace(dniHuespedFiltro) ? null : dniHuespedFiltro.Trim();

            List<Huesped> huespedes = HuespedDAO.ObtenerTodos();
            Dictionary<string, string> nombrePorDni = huespedes
                .Where(h => !string.IsNullOrWhiteSpace(h.DniHuesped))
                .GroupBy(h => h.DniHuesped.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().Apellido} {g.First().Nombre}".Trim());

            string NombreHuesped(string? dni) =>
                !string.IsNullOrWhiteSpace(dni) && nombrePorDni.TryGetValue(dni.Trim(), out string? nombre) ? nombre : "-";

            bool EnRango(DateTime momento) => momento >= inicio && momento <= fin;

            List<Hospedaje> hospedajes = HospedajeDAO.BuscarDetalle(dni: dniFiltro);

            // La venta no guarda la habitación: se toma la estadía del huésped vigente en ese momento.
            int? HabitacionDeVenta(string dni, DateTime momento) => hospedajes
                .Where(h => h.DniHuesped.Trim() == dni
                         && h.FechaEntrada.Date + h.HoraEntrada <= momento
                         && h.FechaSalida.Date + h.HoraSalida >= momento)
                .Select(h => (int?)h.NroHabitacion)
                .FirstOrDefault();

            var eventos = new List<EventoAdministrativo>();

            foreach (Huesped huesped in huespedes.Where(h => h.AltaHuesped.HasValue && EnRango(h.AltaHuesped.Value)))
            {
                string dni = huesped.DniHuesped.Trim();

                eventos.Add(new EventoAdministrativo
                {
                    Fecha = huesped.AltaHuesped!.Value.Date,
                    Hora = huesped.AltaHuesped.Value.TimeOfDay,
                    Tipo = TipoAltaHuesped,
                    Huesped = NombreHuesped(dni),
                    DniHuesped = dni
                });
            }

            foreach (Hospedaje hospedaje in hospedajes)
            {
                string dni = hospedaje.DniHuesped?.Trim() ?? string.Empty;
                DateTime entrada = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
                DateTime salida = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;
                int noches = Math.Max(1, (hospedaje.FechaSalida.Date - hospedaje.FechaEntrada.Date).Days);

                if (EnRango(entrada))
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = hospedaje.FechaEntrada,
                        Hora = hospedaje.HoraEntrada,
                        Tipo = TipoIngreso,
                        NroHabitacion = hospedaje.NroHabitacion,
                        Huesped = NombreHuesped(dni),
                        DniHuesped = dni
                    });
                }

                // Una salida futura todavía no ocurrió: solo se registra la que ya pasó.
                if (salida <= DateTime.Now && EnRango(salida))
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = hospedaje.FechaSalida,
                        Hora = hospedaje.HoraSalida,
                        Tipo = TipoEgreso,
                        NroHabitacion = hospedaje.NroHabitacion,
                        Huesped = NombreHuesped(dni),
                        DniHuesped = dni
                    });
                }
            }

            // Solo las ventas asignadas a un huésped; las de mostrador no forman parte de su historial.
            foreach (VentaRealizada venta in VentaDAO.ObtenerRealizadas(dniFiltro, inicio)
                         .Where(v => !v.EsDeMostrador && v.Momento <= fin))
            {
                string dni = venta.DniHuesped!.Trim();
                int? habitacion = venta.NroHabitacion ?? HabitacionDeVenta(dni, venta.Momento);

                foreach (ItemConsumido item in venta.Items)
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = venta.FechaVenta,
                        Hora = venta.HoraVenta,
                        Tipo = TipoConsumo,
                        NroHabitacion = habitacion,
                        Producto = item.Producto,
                        Cantidad = item.Cantidad,
                        Huesped = NombreHuesped(dni),
                        DniHuesped = dni
                    });
                }
            }

            IEnumerable<EventoAdministrativo> filtrados = eventos;

            if (dniFiltro != null)
            {
                filtrados = filtrados.Where(e => e.DniHuesped.Equals(dniFiltro, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipoFiltro) && Tipos.Contains(tipoFiltro))
            {
                filtrados = filtrados.Where(e => e.Tipo == tipoFiltro);
            }

            List<EventoAdministrativo> lista = filtrados
                .OrderByDescending(e => e.Momento)
                .ToList();

            return new HistorialAdministradorResultado
            {
                Lista = lista,
                TotalEventos = lista.Count,
                HuespedesDistintos = lista.Select(e => e.DniHuesped).Distinct().Count(),
                TotalIngresos = lista.Count(e => e.Tipo == TipoIngreso),
                TotalEgresos = lista.Count(e => e.Tipo == TipoEgreso),
                UnidadesConsumidas = lista.Where(e => e.Tipo == TipoConsumo).Sum(e => e.Cantidad ?? 0),
                TotalAltasHuespedes = lista.Count(e => e.Tipo == TipoAltaHuesped),
                EventosPorTipo = Tipos.ToDictionary(t => t, t => lista.Count(e => e.Tipo == t)),
                EventosPorHuesped = lista
                    .GroupBy(e => e.Huesped)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count()),
                RankingHuespedes = lista
                    .GroupBy(e => e.DniHuesped)
                    .Select(g => new ActividadHuesped
                    {
                        DniHuesped = g.Key,
                        Nombre = g.First().Huesped,
                        Altas = g.Count(e => e.Tipo == TipoAltaHuesped),
                        Ingresos = g.Count(e => e.Tipo == TipoIngreso),
                        Egresos = g.Count(e => e.Tipo == TipoEgreso),
                        Consumos = g.Count(e => e.Tipo == TipoConsumo),
                        UnidadesConsumidas = g.Where(e => e.Tipo == TipoConsumo).Sum(e => e.Cantidad ?? 0),
                        UltimaActividad = g.Max(e => e.Momento)
                    })
                    .OrderByDescending(a => a.Total)
                    .ThenByDescending(a => a.UltimaActividad)
                    .ToList()
            };
        }
    }

    /// <summary>Actividad de un huésped en el período, desglosada por tipo de evento.</summary>
    public class ActividadHuesped
    {
        public string DniHuesped { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Altas { get; set; }
        public int Ingresos { get; set; }
        public int Egresos { get; set; }
        public int Consumos { get; set; }
        public int UnidadesConsumidas { get; set; }
        public DateTime UltimaActividad { get; set; }

        public int Total => Altas + Ingresos + Egresos + Consumos;

        /// <summary>Cantidad de eventos de un tipo (usa las constantes de GestionHistorialAdministrador).</summary>
        public int Cantidad(string tipo) => tipo switch
        {
            GestionHistorialAdministrador.TipoAltaHuesped => Altas,
            GestionHistorialAdministrador.TipoIngreso => Ingresos,
            GestionHistorialAdministrador.TipoEgreso => Egresos,
            GestionHistorialAdministrador.TipoConsumo => Consumos,
            _ => 0
        };
    }

    public class HistorialAdministradorResultado
    {
        public int TotalEventos { get; set; }
        public int HuespedesDistintos { get; set; }
        public int TotalIngresos { get; set; }
        public int TotalEgresos { get; set; }
        public int UnidadesConsumidas { get; set; }
        public int TotalAltasHuespedes { get; set; }
        public List<EventoAdministrativo> Lista { get; set; } = new List<EventoAdministrativo>();
        public Dictionary<string, int> EventosPorTipo { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EventosPorHuesped { get; set; } = new Dictionary<string, int>();
        /// <summary>Huéspedes ordenados de mayor a menor actividad, con el detalle por tipo.</summary>
        public List<ActividadHuesped> RankingHuespedes { get; set; } = new List<ActividadHuesped>();
    }
}
