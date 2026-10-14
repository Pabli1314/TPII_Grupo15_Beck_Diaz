using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>
    /// Histórico del Administrador: arma una línea de tiempo con las operaciones de las áreas que
    /// administra ese rol (limpiezas, ocupación de habitaciones, salidas de stock y altas de usuarios).
    /// No expone montos de ventas ni de caja: ese análisis financiero es del histórico de Gerencia.
    /// </summary>
    public class GestionHistorialAdministrador
    {
        public const string TipoLimpieza = "Limpieza";
        public const string TipoOcupacion = "Ocupación";
        public const string TipoLiberacion = "Liberación";
        public const string TipoSalidaStock = "Salida de stock";
        public const string TipoAltaUsuario = "Alta de usuario";

        public static readonly string[] Tipos = { TipoLimpieza, TipoOcupacion, TipoLiberacion, TipoSalidaStock, TipoAltaUsuario };

        public List<KeyValuePair<string, string>> ObtenerListaUsuarios()
        {
            return UsuarioDAO.ObtenerTodos()
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .Select(u => new KeyValuePair<string, string>(u.DniUsuario.Trim(), $"{u.ApeUsuario} {u.NomUsuario}".Trim()))
                .ToList();
        }

        public HistorialAdministradorResultado Consultar(DateTime desde, DateTime hasta, string? dniUsuarioFiltro, string? tipoFiltro)
        {
            DateTime inicio = desde.Date;
            DateTime fin = hasta.Date.AddDays(1).AddTicks(-1);

            List<Usuario> usuarios = UsuarioDAO.ObtenerTodos();
            Dictionary<string, string> nombrePorDni = usuarios
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .GroupBy(u => u.DniUsuario.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().ApeUsuario} {g.First().NomUsuario}".Trim());

            string NombreUsuario(string? dni) =>
                !string.IsNullOrWhiteSpace(dni) && nombrePorDni.TryGetValue(dni.Trim(), out string? nombre) ? nombre : "-";

            List<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos()
                .Where(t => !string.IsNullOrWhiteSpace(t.DniUsuario))
                .ToList();
            Dictionary<int, string> dniPorTurno = turnos.ToDictionary(t => t.IdTurno, t => t.DniUsuario.Trim());

            // La venta no guarda el turno: se toma el turno que estaba abierto en ese momento.
            string? DniDeVenta(DateTime momento) => turnos
                .Where(t => t.FechaApertura.Date + t.HoraApertura <= momento
                         && (!t.FechaCierre.HasValue || t.FechaCierre.Value.Date + (t.HoraCierre ?? TimeSpan.Zero) >= momento))
                .OrderByDescending(t => t.FechaApertura.Date + t.HoraApertura)
                .Select(t => t.DniUsuario.Trim())
                .FirstOrDefault();

            bool EnRango(DateTime momento) => momento >= inicio && momento <= fin;

            var eventos = new List<EventoAdministrativo>();

            foreach (RegistroLimpieza limpieza in RegistroLimpiezaDAO.ObtenerEnRango(inicio, fin))
            {
                int minutos = (int)Math.Max(0, (limpieza.HoraFin - limpieza.HoraInicio).TotalMinutes);
                string dni = limpieza.DniUsuario?.Trim() ?? string.Empty;

                eventos.Add(new EventoAdministrativo
                {
                    Fecha = limpieza.FechaLimpieza,
                    Hora = limpieza.HoraInicio,
                    Tipo = TipoLimpieza,
                    Descripcion = $"Limpieza de habitación {limpieza.NroHabitacion} ({limpieza.HoraInicio:hh\\:mm} a {limpieza.HoraFin:hh\\:mm}, {minutos} min)",
                    NroHabitacion = limpieza.NroHabitacion,
                    Usuario = NombreUsuario(dni),
                    DniUsuario = dni
                });
            }

            foreach (Hospedaje hospedaje in HospedajeDAO.BuscarDetalle(termino: null))
            {
                string dni = dniPorTurno.TryGetValue(hospedaje.IdTurno, out string? dniTurno) ? dniTurno : string.Empty;
                DateTime entrada = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
                DateTime salida = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;

                if (EnRango(entrada))
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = hospedaje.FechaEntrada,
                        Hora = hospedaje.HoraEntrada,
                        Tipo = TipoOcupacion,
                        Descripcion = $"Habitación {hospedaje.NroHabitacion} pasa a ocupada",
                        NroHabitacion = hospedaje.NroHabitacion,
                        Usuario = NombreUsuario(dni),
                        DniUsuario = dni
                    });
                }

                // Una salida futura todavía no ocurrió: solo se registra la que ya pasó.
                if (salida <= DateTime.Now && EnRango(salida))
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = hospedaje.FechaSalida,
                        Hora = hospedaje.HoraSalida,
                        Tipo = TipoLiberacion,
                        Descripcion = $"Habitación {hospedaje.NroHabitacion} liberada (pasa a limpieza)",
                        NroHabitacion = hospedaje.NroHabitacion,
                        Usuario = NombreUsuario(dni),
                        DniUsuario = dni
                    });
                }
            }

            foreach (VentaRealizada venta in VentaDAO.ObtenerRealizadas(desde: inicio).Where(v => v.Momento <= fin))
            {
                string dni = DniDeVenta(venta.Momento) ?? string.Empty;

                foreach (ItemConsumido item in venta.Items)
                {
                    eventos.Add(new EventoAdministrativo
                    {
                        Fecha = venta.FechaVenta,
                        Hora = venta.HoraVenta,
                        Tipo = TipoSalidaStock,
                        Descripcion = $"Salida de {item.Cantidad} u. de {item.Producto} (venta #{venta.IdVenta})",
                        Producto = item.Producto,
                        Cantidad = item.Cantidad,
                        Usuario = NombreUsuario(dni),
                        DniUsuario = dni
                    });
                }
            }

            foreach (Usuario usuario in usuarios.Where(u => EnRango(u.AltaUsuario)))
            {
                string dni = usuario.DniUsuario.Trim();
                string rol = string.IsNullOrWhiteSpace(usuario.Rol?.NomRol) ? "sin rol" : usuario.Rol.NomRol;

                eventos.Add(new EventoAdministrativo
                {
                    Fecha = usuario.AltaUsuario.Date,
                    Hora = usuario.AltaUsuario.TimeOfDay,
                    Tipo = TipoAltaUsuario,
                    Descripcion = $"Alta de {NombreUsuario(dni)} como {rol}{(usuario.Estado ? string.Empty : " (hoy inactivo)")}",
                    Usuario = NombreUsuario(dni),
                    DniUsuario = dni
                });
            }

            IEnumerable<EventoAdministrativo> filtrados = eventos;

            if (!string.IsNullOrWhiteSpace(dniUsuarioFiltro))
            {
                string dniLimpio = dniUsuarioFiltro.Trim();
                filtrados = filtrados.Where(e => e.DniUsuario.Equals(dniLimpio, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipoFiltro) && Tipos.Contains(tipoFiltro))
            {
                filtrados = filtrados.Where(e => e.Tipo == tipoFiltro);
            }

            List<EventoAdministrativo> lista = filtrados
                .OrderByDescending(e => e.Momento)
                .ToList();

            List<EventoAdministrativo> limpiezas = lista.Where(e => e.Tipo == TipoLimpieza).ToList();

            return new HistorialAdministradorResultado
            {
                Lista = lista,
                TotalEventos = lista.Count,
                TotalLimpiezas = limpiezas.Count,
                TotalOcupaciones = lista.Count(e => e.Tipo == TipoOcupacion),
                UnidadesSalidaStock = lista.Where(e => e.Tipo == TipoSalidaStock).Sum(e => e.Cantidad ?? 0),
                TotalAltasUsuarios = lista.Count(e => e.Tipo == TipoAltaUsuario),
                EventosPorTipo = Tipos.ToDictionary(t => t, t => lista.Count(e => e.Tipo == t)),
                EventosPorUsuario = lista
                    .GroupBy(e => e.Usuario)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }

    public class HistorialAdministradorResultado
    {
        public int TotalEventos { get; set; }
        public int TotalLimpiezas { get; set; }
        public int TotalOcupaciones { get; set; }
        public int UnidadesSalidaStock { get; set; }
        public int TotalAltasUsuarios { get; set; }
        public List<EventoAdministrativo> Lista { get; set; } = new List<EventoAdministrativo>();
        public Dictionary<string, int> EventosPorTipo { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EventosPorUsuario { get; set; } = new Dictionary<string, int>();
    }
}
