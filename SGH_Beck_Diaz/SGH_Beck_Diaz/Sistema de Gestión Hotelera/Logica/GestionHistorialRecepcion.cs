using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>Combina check-in/check-out (hospedaje) y ventas adicionales en una sola línea de
    /// tiempo, para la pantalla Historial.</summary>
    public class GestionHistorialRecepcion
    {
        public List<OperacionHistorial> Obtener(DateTime? fecha = null, string? huesped = null, int? nroHabitacion = null, string? usuario = null, string? tipo = null)
        {
            List<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos()
                .Where(t => !string.IsNullOrWhiteSpace(t.DniUsuario))
                .ToList();

            // Mapeo adaptado: id_turno -> dni_usuario (string)
            Dictionary<int, string> usuarioPorTurno = turnos.ToDictionary(t => t.IdTurno, t => t.DniUsuario.Trim());

            // La tabla venta no guarda el turno: se busca el turno que estaba abierto en ese momento.
            int? TurnoDeVenta(Venta venta) => turnos
                .Where(t => t.FechaApertura.Date + t.HoraApertura <= venta.Momento
                         && (!t.FechaCierre.HasValue || t.FechaCierre.Value.Date + (t.HoraCierre ?? TimeSpan.Zero) >= venta.Momento))
                .OrderByDescending(t => t.FechaApertura.Date + t.HoraApertura)
                .Select(t => (int?)t.IdTurno)
                .FirstOrDefault();

            // Mapeo adaptado: dni_usuario (string) -> Nombre Completo (Apellido Nombre)
            Dictionary<string, string> nombreUsuario = UsuarioDAO.ObtenerTodos()
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .GroupBy(u => u.DniUsuario.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().ApeUsuario} {g.First().NomUsuario}".Trim());

            string NombreUsuarioDeTurno(int idTurno) =>
                usuarioPorTurno.TryGetValue(idTurno, out string? dniUsuario) && nombreUsuario.TryGetValue(dniUsuario, out string? nombre)
                    ? nombre
                    : "-";

            var operaciones = new List<OperacionHistorial>();

            List<HospedajeDetalle> hospedajes = HospedajeDAO.BuscarDetalle();

            foreach (HospedajeDetalle hospedaje in hospedajes)
            {
                operaciones.Add(new OperacionHistorial
                {
                    Fecha = hospedaje.FechaEntrada,
                    Hora = hospedaje.HoraEntrada,
                    Tipo = "Check-in",
                    Descripcion = $"Check-in de {hospedaje.NombreHuesped} en habitación {hospedaje.NroHabitacion}",
                    Huesped = hospedaje.NombreHuesped,
                    NroHabitacion = hospedaje.NroHabitacion,
                    Usuario = NombreUsuarioDeTurno(hospedaje.IdTurno)
                });

                if (hospedaje.Finalizada)
                {
                    operaciones.Add(new OperacionHistorial
                    {
                        Fecha = hospedaje.FechaSalida,
                        Hora = hospedaje.HoraSalida,
                        Tipo = "Check-out",
                        Descripcion = $"Check-out de {hospedaje.NombreHuesped} de habitación {hospedaje.NroHabitacion}",
                        Huesped = hospedaje.NombreHuesped,
                        NroHabitacion = hospedaje.NroHabitacion,
                        Usuario = NombreUsuarioDeTurno(hospedaje.IdTurno)
                    });
                }
            }

            foreach (Venta venta in VentaDAO.ObtenerTodas())
            {
                int? idTurnoVenta = TurnoDeVenta(venta);

                // Estadía del huésped en el momento de la venta, para mostrar nombre y habitación.
                HospedajeDetalle? estadia = venta.DniHuesped == null ? null : hospedajes
                    .Where(h => h.DniHuesped == venta.DniHuesped && h.FechaEntrada.Date + h.HoraEntrada <= venta.Momento)
                    .OrderByDescending(h => h.FechaEntrada.Date + h.HoraEntrada)
                    .FirstOrDefault();

                operaciones.Add(new OperacionHistorial
                {
                    Fecha = venta.FechaVenta,
                    Hora = venta.HoraVenta,
                    Tipo = "Venta",
                    Descripcion = estadia != null
                        ? $"Venta adicional #{venta.IdVenta} a {estadia.NombreHuesped} (habitación {estadia.NroHabitacion})"
                        : $"Venta adicional #{venta.IdVenta} (mostrador)",
                    Huesped = estadia?.NombreHuesped,
                    NroHabitacion = estadia?.NroHabitacion,
                    Usuario = idTurnoVenta.HasValue ? NombreUsuarioDeTurno(idTurnoVenta.Value) : "-",
                    Monto = venta.Total
                });
            }

            IEnumerable<OperacionHistorial> resultado = operaciones;

            if (fecha.HasValue)
            {
                resultado = resultado.Where(o => o.Fecha.Date == fecha.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(huesped))
            {
                resultado = resultado.Where(o => o.Huesped != null && o.Huesped.Contains(huesped, StringComparison.OrdinalIgnoreCase));
            }

            if (nroHabitacion.HasValue)
            {
                resultado = resultado.Where(o => o.NroHabitacion == nroHabitacion.Value);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                resultado = resultado.Where(o => o.Usuario != null && o.Usuario.Contains(usuario, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todas")
            {
                resultado = resultado.Where(o => o.Tipo == tipo);
            }

            return resultado.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Hora).ToList();
        }
    }
}