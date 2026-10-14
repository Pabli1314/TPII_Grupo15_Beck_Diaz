using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>Estadísticas y reportes consumidos por las pantallas de Gerencia
    /// (dashboard ejecutivo, análisis de habitaciones e históricos de caja).</summary>
    public class GestionGerente
    {
        private static readonly string[] ORDEN_ESTADOS = { "Disponible", "Ocupada", "Limpieza", "Mantenimiento" };

        // Mismo orden en que las pantallas de Gerencia muestran las tarjetas por tipo de
        // habitación; no se puede asumir que coincida con el orden por id_tipo_habitacion.
        private static readonly string[] ORDEN_TIPOS = { "Individual", "Estandar", "Doble Matrimonial", "Suite Executive" };

        /// <summary>
        /// Recaudación de un turno cerrado tomada solo de los datos consultados de Turno_caja:
        /// monto_final (arqueo al cierre) menos monto_inicial. Turno abierto o sin monto final = 0.
        /// </summary>
        public static decimal RecaudacionDeTurno(TurnoCaja turno)
        {
            if (turno.FechaCierre == null || !turno.MontoFinal.HasValue)
            {
                return 0m;
            }

            return Math.Max(0m, turno.MontoFinal.Value - turno.MontoInicial);
        }

        public Dictionary<string, decimal> ObtenerEstadisticasIngresosMensuales()
        {
            var culturaEs = new CultureInfo("es-ES");

            var gruposPorMes = TurnoCajaDAO.ObtenerTodos()
                .Where(t => t.FechaCierre != null)
                .GroupBy(t => new DateTime(t.FechaApertura.Year, t.FechaApertura.Month, 1))
                .OrderBy(g => g.Key);

            var resultado = new Dictionary<string, decimal>();
            foreach (var grupo in gruposPorMes)
            {
                decimal total = grupo.Sum(RecaudacionDeTurno);
                string etiqueta = culturaEs.TextInfo.ToTitleCase(grupo.Key.ToString("MMM yyyy", culturaEs));
                resultado[etiqueta] = total;
            }

            return resultado;
        }

        public Dictionary<string, int> ObtenerEstadisticasHabitacionesDashboard()
        {
            List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();
            List<EstadoHabitacion> estados = EstadoHabitacionDAO.ObtenerTodos();

            var resultado = new Dictionary<string, int>();
            foreach (EstadoHabitacion estado in estados)
            {
                resultado[estado.NomEstadoHabitacion] = habitaciones.Count(h => h.IdEstado == estado.IdEstado);
            }

            return resultado;
        }

        /// <summary>Vector de 4 posiciones por estado (Disponible, Ocupada, Limpieza, Mantenimiento)
        /// y vector de 4 posiciones por tipo de habitación, ordenados por id.</summary>
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

        /// <summary>Nombre real del tipo de habitación (según la tabla Tipo_habitacion), para no
        /// depender de que el id coincida con una posición fija en una lista hardcodeada.</summary>
        public string ObtenerNombreTipoHabitacion(int idTipoHabitacion)
        {
            TipoHabitacion? tipo = TipoHabitacionDAO.ObtenerTodos()
                .FirstOrDefault(t => t.IdTipoHabitacion == idTipoHabitacion);

            return tipo?.Descripcion ?? "Sin Definir";
        }

        public List<KeyValuePair<string, string>> ObtenerListaUsuarios()
        {
            return UsuarioDAO.ObtenerTodos()
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .Select(u => new KeyValuePair<string, string>(u.DniUsuario.Trim(), $"{u.ApeUsuario} {u.NomUsuario}".Trim()))
                .ToList();
        }

        public HistorialOperativoResultado ConsultarHistorialOperativo(DateTime desde, DateTime hasta, string? dniUsuarioFiltro)
        {
            List<Usuario> usuarios = UsuarioDAO.ObtenerTodos();

            IEnumerable<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos()
                .Where(t => t.FechaCierre != null && t.FechaApertura >= desde.Date && t.FechaApertura <= hasta);

            if (!string.IsNullOrWhiteSpace(dniUsuarioFiltro))
            {
                string dniLimpio = dniUsuarioFiltro.Trim();
                turnos = turnos.Where(t => t.DniUsuario.Trim().Equals(dniLimpio, StringComparison.OrdinalIgnoreCase));
            }

            var resultado = new HistorialOperativoResultado();
            var recaudacionPorUsuario = new Dictionary<string, decimal>();

            foreach (TurnoCaja turno in turnos.OrderByDescending(t => t.IdTurno))
            {
                decimal recaudado = RecaudacionDeTurno(turno);
                Usuario? usuario = usuarios.FirstOrDefault(u => u.DniUsuario.Trim().Equals(turno.DniUsuario.Trim(), StringComparison.OrdinalIgnoreCase));

                string dni = !string.IsNullOrWhiteSpace(turno.DniUsuario) ? turno.DniUsuario.Trim() : (usuario?.DniUsuario.Trim() ?? string.Empty);

                resultado.Lista.Add(new TurnoHistorialItem
                {
                    IdTurno = turno.IdTurno,
                    FechaApertura = turno.FechaApertura,
                    HoraApertura = turno.HoraApertura,
                    FechaCierre = turno.FechaCierre,
                    HoraCierre = turno.HoraCierre,
                    MontoInicial = turno.MontoInicial,
                    MontoFinal = turno.MontoFinal,
                    Observaciones = turno.Observaciones,
                    DniUsuario = dni
                });

                resultado.TotalRecaudado += recaudado;
                recaudacionPorUsuario[dni] = recaudacionPorUsuario.GetValueOrDefault(dni) + recaudado;
            }

            resultado.TotalTurnosCerrados = resultado.Lista.Count;
            resultado.PromedioTurno = resultado.TotalTurnosCerrados > 0
                ? resultado.TotalRecaudado / resultado.TotalTurnosCerrados
                : 0;
            resultado.RecaudacionPorUsuario = recaudacionPorUsuario;

            return resultado;
        }

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
    }

    public class HistorialOperativoResultado
    {
        public decimal TotalRecaudado { get; set; }
        public decimal PromedioTurno { get; set; }
        public int TotalTurnosCerrados { get; set; }
        public List<TurnoHistorialItem> Lista { get; set; } = new List<TurnoHistorialItem>();
        public Dictionary<string, decimal> RecaudacionPorUsuario { get; set; } = new Dictionary<string, decimal>();
    }

    public class TurnoHistorialItem
    {
        public int IdTurno { get; set; }
        public DateTime FechaApertura { get; set; }
        public TimeSpan HoraApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public TimeSpan? HoraCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal? MontoFinal { get; set; }
        public string? Observaciones { get; set; }
        public string DniUsuario { get; set; } = string.Empty;
    }
}