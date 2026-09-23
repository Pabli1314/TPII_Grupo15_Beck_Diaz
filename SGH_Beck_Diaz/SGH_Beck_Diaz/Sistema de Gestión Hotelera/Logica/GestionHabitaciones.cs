using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    public class GestionHabitaciones
    {
        private const string ESTADO_LIMPIEZA = "Limpieza";
        private const string ESTADO_DISPONIBLE = "Disponible";
        private const string ESTADO_MANTENIMIENTO = "Mantenimiento";
        private const string ESTADO_OCUPADA = "Ocupada";

        public List<Habitacion> ObtenerHabitaciones() => HabitacionDAO.ObtenerTodas();
        public List<EstadoHabitacion> ObtenerEstados() => EstadoHabitacionDAO.ObtenerTodos();
        public List<TipoHabitacion> ObtenerTiposHabitacion() => TipoHabitacionDAO.ObtenerTodos();

        /// <summary>
        /// Genera la lista resumida para vistas Grid/Tarjetas unificando entidad Habitacion,
        /// su Hospedaje activo si está ocupada y el último RegistroLimpieza.
        /// </summary>
        public List<HabitacionResumen> ObtenerResumenHabitaciones()
        {
            List<Habitacion> habitaciones = HabitacionDAO.ObtenerTodas();

            var dicEstados = EstadoHabitacionDAO.ObtenerTodos()
                .GroupBy(e => e.IdEstado)
                .ToDictionary(g => g.Key, g => g.First());

            var dicTipos = TipoHabitacionDAO.ObtenerTodos()
                .GroupBy(t => t.IdTipoHabitacion)
                .ToDictionary(g => g.Key, g => g.First());

            var resumen = new List<HabitacionResumen>();

            foreach (Habitacion habitacion in habitaciones)
            {
                dicEstados.TryGetValue(habitacion.IdEstado, out var estado);
                dicTipos.TryGetValue(habitacion.IdTipoHabitacion, out var tipo);

                var item = new HabitacionResumen
                {
                    NroHabitacion = habitacion.NroHabitacion,
                    Piso = habitacion.Piso,
                    TipoHabitacion = tipo?.Descripcion ?? "-",
                    IdEstado = habitacion.IdEstado,
                    Estado = estado?.NomEstadoHabitacion ?? "-"
                };

                if (string.Equals(item.Estado, ESTADO_OCUPADA, StringComparison.OrdinalIgnoreCase))
                {
                    Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(habitacion.NroHabitacion);
                    if (hospedaje != null)
                    {
                        Huesped? huesped = HuespedDAO.ObtenerPorDni(hospedaje.DniHuesped);
                        item.DniHuesped = hospedaje.DniHuesped;
                        item.Huesped = huesped != null ? $"{huesped.Nombre} {huesped.Apellido}" : hospedaje.DniHuesped;
                        item.HoraEntrada = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
                        item.HoraSalidaEstimada = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;
                    }
                }

                RegistroLimpieza? ultimaLimpieza = RegistroLimpiezaDAO.ObtenerUltimaPorHabitacion(habitacion.NroHabitacion);
                if (ultimaLimpieza != null)
                {
                    item.UltimaLimpieza = ultimaLimpieza.FechaLimpieza.Date + ultimaLimpieza.HoraFin;
                    item.DuracionUltimaLimpieza = ultimaLimpieza.HoraFin - ultimaLimpieza.HoraInicio;
                }

                resumen.Add(item);
            }

            return resumen;
        }

        /// <summary>
        /// Registra un nuevo hospedaje (Check-in) y cambia el estado de la habitación a 'Ocupada'.
        /// </summary>
        public void RegistrarHospedaje(Hospedaje hospedaje)
        {
            if (hospedaje == null) throw new ArgumentNullException(nameof(hospedaje));
            if (string.IsNullOrWhiteSpace(hospedaje.DniHuesped)) throw new ArgumentException("El DNI del huésped es obligatorio.");

            // Valida y pasa de Disponibilidad a Ocupada
            ValidarYCambiarEstado(hospedaje.NroHabitacion, ESTADO_DISPONIBLE, ESTADO_OCUPADA);

            HospedajeDAO.Crear(hospedaje);
        }

        /// <summary>
        /// Registra el Check-out del huésped y pasa la habitación a estado 'Limpieza'.
        /// </summary>
        public void RegistrarSalida(int idHospedaje, int nroHabitacion, DateTime fechaSalida, TimeSpan horaSalida)
        {
            HospedajeDAO.RegistrarSalida(idHospedaje, fechaSalida, horaSalida);
            ValidarYCambiarEstado(nroHabitacion, ESTADO_OCUPADA, ESTADO_LIMPIEZA);
        }

        public void CrearHabitacion(Habitacion habitacion)
        {
            ValidarHabitacion(habitacion);

            if (HabitacionDAO.ObtenerPorNumero(habitacion.NroHabitacion) != null)
            {
                throw new InvalidOperationException($"Ya existe una habitación con el número {habitacion.NroHabitacion}.");
            }

            HabitacionDAO.Crear(habitacion);
        }

        public void MarcarComoDisponible(int nroHabitacion, string dniUsuario)
        {
            ValidarYCambiarEstado(nroHabitacion, ESTADO_LIMPIEZA, ESTADO_DISPONIBLE);

            DateTime ahora = DateTime.Now;

            RegistroLimpiezaDAO.Registrar(new RegistroLimpieza
            {
                HoraInicio = ahora.TimeOfDay,
                HoraFin = ahora.TimeOfDay,
                NroHabitacion = nroHabitacion,
                DniUsuario = dniUsuario,
                FechaLimpieza = ahora.Date
            });
        }

        public void EnviarAMantenimiento(int nroHabitacion)
        {
            ValidarYCambiarEstado(nroHabitacion, ESTADO_DISPONIBLE, ESTADO_MANTENIMIENTO);
        }

        public void FinalizarMantenimiento(int nroHabitacion)
        {
            ValidarYCambiarEstado(nroHabitacion, ESTADO_MANTENIMIENTO, ESTADO_DISPONIBLE);
        }

        /// <summary>
        /// Obtiene los datos del huésped alojado en una habitación si está ocupada.
        /// Retorna null si la habitación no existe o no tiene un hospedaje activo.
        /// </summary>
        public Huesped? ObtenerHuespedOcupante(int nroHabitacion)
        {
            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion);
            if (hospedaje == null || string.IsNullOrWhiteSpace(hospedaje.DniHuesped))
            {
                return null;
            }

            return HuespedDAO.ObtenerPorDni(hospedaje.DniHuesped);
        }

        #region Métodos Privados / Auxiliares

        private void ValidarYCambiarEstado(int nroHabitacion, string estadoOrigen, string estadoDestino)
        {
            Habitacion habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion)
                ?? throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

            List<EstadoHabitacion> estados = EstadoHabitacionDAO.ObtenerTodos();

            EstadoHabitacion estadoOrigenObj = estados.FirstOrDefault(e =>
                string.Equals(e.NomEstadoHabitacion?.Trim(), estadoOrigen, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No se encontró el estado '{estadoOrigen}' en la base de datos.");

            EstadoHabitacion estadoDestinoObj = estados.FirstOrDefault(e =>
                string.Equals(e.NomEstadoHabitacion?.Trim(), estadoDestino, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No se encontró el estado '{estadoDestino}' en la base de datos.");

            if (habitacion.IdEstado != estadoOrigenObj.IdEstado)
            {
                throw new InvalidOperationException($"La habitación {nroHabitacion} no está en estado {estadoOrigen}.");
            }

            HabitacionDAO.ActualizarEstado(nroHabitacion, estadoDestinoObj.IdEstado);
        }

        private void ValidarHabitacion(Habitacion habitacion)
        {
            if (habitacion == null)
                throw new ArgumentNullException(nameof(habitacion));

            if (habitacion.NroHabitacion <= 0)
                throw new ArgumentException("El número de habitación debe ser mayor a 0.");

            if (habitacion.Piso is < 1 or > 3)
                throw new ArgumentException("El piso debe ser 1, 2 o 3.");

            if (habitacion.CantCamas <= 0)
                throw new ArgumentException("La cantidad de camas debe ser mayor a 0.");

            if (habitacion.TarifaBase < 0)
                throw new ArgumentException("La tarifa base no puede ser negativa.");

            if (habitacion.IdTipoHabitacion <= 0)
                throw new ArgumentException("Debe seleccionar un tipo de habitación.");

            if (habitacion.IdEstado <= 0)
                throw new ArgumentException("Debe seleccionar un estado.");
        }

        #endregion
    }
}