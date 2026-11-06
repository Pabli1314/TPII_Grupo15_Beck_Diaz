using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>
    /// Combina check-in/check-out (hospedaje) y ventas adicionales en una sola línea de
    /// tiempo, para la pantalla Historial.
    /// </summary>
    public class GestionHistorialRecepcion
    {
        public List<OperacionHistorial> Obtener(DateTime? fecha = null, string? huesped = null, int? nroHabitacion = null, string? usuario = null, string? tipo = null)
        {
            List<TurnoCaja> turnos = TurnoCajaDAO.ObtenerTodos()
                .Where(t => !string.IsNullOrWhiteSpace(t.DniUsuario))
                .ToList();

            // Mapeo: id_turno -> dni_usuario
            Dictionary<int, string> usuarioPorTurno = turnos.ToDictionary(t => t.IdTurno, t => t.DniUsuario.Trim());

            // Identifica el turno activo durante la venta
            int? TurnoDeVenta(Venta venta) => turnos
                .Where(t => t.FechaApertura.Date + t.HoraApertura <= venta.Momento
                         && (!t.FechaCierre.HasValue || t.FechaCierre.Value.Date + (t.HoraCierre ?? TimeSpan.Zero) >= venta.Momento))
                .OrderByDescending(t => t.FechaApertura.Date + t.HoraApertura)
                .Select(t => (int?)t.IdTurno)
                .FirstOrDefault();

            // Mapeo: dni_usuario -> Nombre Completo (Apellido, Nombre)
            Dictionary<string, string> nombreUsuario = UsuarioDAO.ObtenerTodos()
                .Where(u => !string.IsNullOrWhiteSpace(u.DniUsuario))
                .GroupBy(u => u.DniUsuario.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().ApeUsuario} {g.First().NomUsuario}".Trim());

            string NombreUsuarioDeTurno(int idTurno) =>
                usuarioPorTurno.TryGetValue(idTurno, out string? dniUsuario) && nombreUsuario.TryGetValue(dniUsuario, out string? nombre)
                    ? nombre
                    : "-";

            // Mapeo: dni_huesped -> Nombre Completo (Nombre Apellido)
            Dictionary<string, string> nombreHuespedPorDni = HuespedDAO.ObtenerTodos()
                .Where(h => !string.IsNullOrWhiteSpace(h.DniHuesped))
                .GroupBy(h => h.DniHuesped.Trim())
                .ToDictionary(g => g.Key, g => $"{g.First().Nombre} {g.First().Apellido}".Trim());

            string ObtenerNombreHuesped(string? dni) =>
                !string.IsNullOrWhiteSpace(dni) && nombreHuespedPorDni.TryGetValue(dni.Trim(), out string? nom)
                    ? nom
                    : (dni ?? "-");

            var operaciones = new List<OperacionHistorial>();

            // Recupera la lista de hospedajes especificando el parámetro nombrado para evitar CS0121
            List<Hospedaje> hospedajes = HospedajeDAO.BuscarDetalle(termino: null);

            foreach (Hospedaje hospedaje in hospedajes)
            {
                string nombreH = ObtenerNombreHuesped(hospedaje.DniHuesped);

                operaciones.Add(new OperacionHistorial
                {
                    Fecha = hospedaje.FechaEntrada,
                    Hora = hospedaje.HoraEntrada,
                    Tipo = "Check-in",
                    Descripcion = $"Check-in de {nombreH} en habitación {hospedaje.NroHabitacion}",
                    Huesped = nombreH,
                    NroHabitacion = hospedaje.NroHabitacion,
                    Usuario = NombreUsuarioDeTurno(hospedaje.IdTurno)
                });

                // Reemplazo de la evaluación .Finalizada en memoria sin modificar la entidad Hospedaje
                if ((hospedaje.FechaSalida.Date + hospedaje.HoraSalida) <= DateTime.Now)
                {
                    operaciones.Add(new OperacionHistorial
                    {
                        Fecha = hospedaje.FechaSalida,
                        Hora = hospedaje.HoraSalida,
                        Tipo = "Check-out",
                        Descripcion = $"Check-out de {nombreH} de habitación {hospedaje.NroHabitacion}",
                        Huesped = nombreH,
                        NroHabitacion = hospedaje.NroHabitacion,
                        Usuario = NombreUsuarioDeTurno(hospedaje.IdTurno)
                    });
                }
            }

            foreach (Venta venta in VentaDAO.ObtenerTodas())
            {
                int? idTurnoVenta = TurnoDeVenta(venta);

                // Estadía del huésped vigente al momento de la venta
                Hospedaje? estadia = venta.DniHuesped == null ? null : hospedajes
                    .Where(h => h.DniHuesped == venta.DniHuesped && h.FechaEntrada.Date + h.HoraEntrada <= venta.Momento)
                    .OrderByDescending(h => h.FechaEntrada.Date + h.HoraEntrada)
                    .FirstOrDefault();

                string? nombreHuespedVenta = estadia != null
                    ? ObtenerNombreHuesped(estadia.DniHuesped)
                    : (venta.DniHuesped != null ? ObtenerNombreHuesped(venta.DniHuesped) : null);

                operaciones.Add(new OperacionHistorial
                {
                    Fecha = venta.FechaVenta,
                    Hora = venta.HoraVenta,
                    Tipo = "Venta",
                    Descripcion = estadia != null
                        ? $"Venta adicional #{venta.IdVenta} a {nombreHuespedVenta} (habitación {estadia.NroHabitacion})"
                        : $"Venta adicional #{venta.IdVenta} (mostrador)",
                    Huesped = nombreHuespedVenta,
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
                string filtroHuesped = huesped.Trim();
                resultado = resultado.Where(o => o.Huesped != null && o.Huesped.Contains(filtroHuesped, StringComparison.OrdinalIgnoreCase));
            }

            if (nroHabitacion.HasValue)
            {
                resultado = resultado.Where(o => o.NroHabitacion == nroHabitacion.Value);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                string filtroUsuario = usuario.Trim();
                resultado = resultado.Where(o => o.Usuario != null && o.Usuario.Contains(filtroUsuario, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipo) && !tipo.Equals("Todas", StringComparison.OrdinalIgnoreCase))
            {
                resultado = resultado.Where(o => o.Tipo.Equals(tipo.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return resultado
                .OrderByDescending(o => o.Fecha)
                .ThenByDescending(o => o.Hora)
                .ToList();
        }
    }
}