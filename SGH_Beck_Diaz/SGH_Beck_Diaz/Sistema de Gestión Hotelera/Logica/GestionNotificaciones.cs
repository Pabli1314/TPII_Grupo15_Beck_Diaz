using Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Genera las notificaciones del Administrador combinando datos reales (habitaciones
    /// próximas a vencer según la tolerancia configurada, habitaciones en limpieza) con los
    /// módulos que todavía viven en memoria (stock bajo, último backup).
    /// </summary>
    public class GestionNotificaciones
    {
        private static readonly HashSet<string> _descartadas = new();

        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly GestionConfiguracion _gestionConfiguracion = new();
        private readonly GestionInventario _gestionInventario = new();
        private readonly GestionBackups _gestionBackups = new();

        public List<NotificacionItem> ObtenerActivas()
        {
            var notificaciones = new List<NotificacionItem>();
            int id = 1;

            ConfiguracionAlertas configuracion = _gestionConfiguracion.Obtener();
            List<HabitacionResumen> habitaciones = _gestionHabitaciones.ObtenerResumenHabitaciones();

            foreach (HabitacionResumen habitacion in habitaciones.Where(h => h.Ocupada && h.TiempoRestante.HasValue))
            {
                TimeSpan restante = habitacion.TiempoRestante!.Value;
                if (restante.TotalMinutes <= configuracion.MinutosTolerancia)
                {
                    string texto = restante.TotalMinutes <= 0
                        ? $"La habitación {habitacion.NroHabitacion} ya venció su hora de salida estimada."
                        : $"La habitación {habitacion.NroHabitacion} está próxima a vencer ({(int)restante.TotalMinutes} min).";

                    Agregar(notificaciones, ref id, $"venc-{habitacion.NroHabitacion}", "Ocupación por vencer", texto, TipoNotificacion.Advertencia);
                }
            }

            foreach (HabitacionResumen habitacion in habitaciones.Where(h => h.EnLimpieza))
            {
                Agregar(notificaciones, ref id, $"limp-{habitacion.NroHabitacion}", "Requiere limpieza",
                    $"La habitación {habitacion.NroHabitacion} requiere limpieza.", TipoNotificacion.Info);
            }

            // La tabla "producto" es una incorporación reciente: si todavía no se ejecutó la
            // migración (Datos/Sql/AgregarVentas.sql) en esta base, se omiten las notificaciones
            // de stock en vez de tirar abajo toda la campana de notificaciones.
            try
            {
                foreach (Producto producto in _gestionInventario.ObtenerProductos().Where(p => p.StockBajo || p.Agotado))
                {
                    TipoNotificacion tipo = producto.Agotado ? TipoNotificacion.Error : TipoNotificacion.Advertencia;
                    string texto = producto.Agotado
                        ? $"El producto \"{producto.Nombre}\" está agotado."
                        : $"El producto \"{producto.Nombre}\" tiene stock bajo ({producto.Stock} unidades).";

                    Agregar(notificaciones, ref id, $"stock-{producto.Codigo}", "Inventario", texto, tipo);
                }
            }
            catch (Exception)
            {
                // Sin tabla "producto" todavía: no hay notificaciones de stock que mostrar.
            }

            BackupInfo? ultimoBackup = _gestionBackups.ObtenerUltimo();
            if (ultimoBackup != null && ultimoBackup.FechaHora.Date == DateTime.Today)
            {
                Agregar(notificaciones, ref id, $"backup-{ultimoBackup.Id}", "Copia de seguridad",
                    "Se realizó una copia de seguridad correctamente.", TipoNotificacion.Exito);
            }

            return notificaciones.OrderByDescending(n => n.Fecha).ToList();
        }

        public void Descartar(string clave) => _descartadas.Add(clave);

        public void DescartarTodas(IEnumerable<string> claves)
        {
            foreach (string clave in claves)
            {
                _descartadas.Add(clave);
            }
        }

        private static void Agregar(List<NotificacionItem> lista, ref int id, string clave, string titulo, string mensaje, TipoNotificacion tipo)
        {
            if (_descartadas.Contains(clave))
            {
                return;
            }

            lista.Add(new NotificacionItem
            {
                Id = id++,
                Clave = clave,
                Titulo = titulo,
                Mensaje = mensaje,
                Fecha = DateTime.Now,
                Tipo = tipo
            });
        }
    }
}
