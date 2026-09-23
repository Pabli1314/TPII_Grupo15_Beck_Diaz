using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    public class GestionHospedajes
    {
        private const string ESTADO_DISPONIBLE = "Disponible";
        private const string ESTADO_OCUPADA = "Ocupada";
        private const string ESTADO_LIMPIEZA = "Limpieza";

        private readonly GestionTurnoCaja _gestionTurnoCaja = new GestionTurnoCaja();

        public List<MetodoPago> ObtenerMetodosPago()
        {
            return MetodoPagoDAO.ObtenerTodos();
        }

        /// <summary>Para el autocompletado por DNI en Check-in: null si el huésped todavía no existe.</summary>
        public Huesped? BuscarHuespedPorDni(string dniHuesped)
        {
            return HuespedDAO.ObtenerPorDni(dniHuesped);
        }

        /// <summary>
        /// Da de alta el hospedaje de un huésped nuevo o existente y pasa la habitación a Ocupada.
        /// Requiere una habitación Disponible y una fecha/hora de salida planificada posterior al
        /// momento actual (hospedaje.fecha_salida/hora_salida son obligatorias en la base, no se puede
        /// dejar "abierto"). Valida todo antes de escribir, para no dejar un huésped o turno creado
        /// a medias si algún dato es inválido. Si el huésped ya existe, actualiza sus datos de contacto.
        /// </summary>
        public void RegistrarCheckIn(int nroHabitacion, string dniUsuario, Huesped huesped, int idMetodo, DateTime fechaSalida, TimeSpan horaSalida)
        {
            if (huesped == null) throw new ArgumentNullException(nameof(huesped));

            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new InvalidOperationException("No hay un usuario identificado para registrar el check-in.");
            }

            DateTime salidaPlanificada = fechaSalida.Date + horaSalida;
            if (salidaPlanificada <= DateTime.Now)
            {
                throw new InvalidOperationException("La fecha y hora de salida deben ser posteriores al momento actual.");
            }

            if (!MetodoPagoDAO.ObtenerTodos().Any(m => m.IdMetodo == idMetodo))
            {
                throw new InvalidOperationException("El método de pago seleccionado no es válido.");
            }

            bool huespedExiste = HuespedDAO.ObtenerPorDni((huesped.DniHuesped ?? string.Empty).Trim()) != null;
            GestionHuespedes.Validar(huesped, esNuevo: !huespedExiste);

            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion);
            if (habitacion == null)
            {
                throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");
            }

            List<EstadoHabitacion> estados = EstadoHabitacionDAO.ObtenerTodos();
            EstadoHabitacion? estadoDisponible = estados.FirstOrDefault(e => e.NomEstadoHabitacion == ESTADO_DISPONIBLE);
            EstadoHabitacion? estadoOcupada = estados.FirstOrDefault(e => e.NomEstadoHabitacion == ESTADO_OCUPADA);

            if (estadoDisponible == null || estadoOcupada == null)
            {
                throw new InvalidOperationException("No se encontraron los estados de habitación 'Disponible'/'Ocupada' en la base de datos.");
            }

            int? habitacionActual = ObtenerHabitacionOcupadaPor(huesped.DniHuesped, estadoOcupada.IdEstado);
            if (habitacionActual.HasValue)
            {
                throw new InvalidOperationException($"El huésped con DNI {huesped.DniHuesped} ya está alojado en la habitación {habitacionActual}.");
            }

            if (habitacion.IdEstado != estadoDisponible.IdEstado)
            {
                throw new InvalidOperationException($"La habitación {nroHabitacion} no está Disponible.");
            }

            // El turno va antes de tocar al huésped: si el usuario no es válido para abrir turno
            // (FK_TurnoCaja_Usuario), no queda un huésped guardado sin hospedaje.
            TurnoCaja turno = _gestionTurnoCaja.ObtenerOAbrirTurno(dniUsuario);

            if (huespedExiste)
            {
                HuespedDAO.Actualizar(huesped);
            }
            else
            {
                HuespedDAO.Crear(huesped);
            }

            HospedajeDAO.Crear(new Hospedaje
            {
                FechaSalida = fechaSalida,
                HoraSalida = horaSalida,
                IdMetodo = idMetodo,
                NroHabitacion = nroHabitacion,
                IdTurno = turno.IdTurno,
                DniHuesped = huesped.DniHuesped
            });

            HabitacionDAO.ActualizarEstado(nroHabitacion, estadoOcupada.IdEstado);
        }

        /// <summary>Número de la habitación Ocupada cuyo hospedaje vigente es de ese huésped, o null si
        /// no está alojado. No depende de la salida estimada: un huésped con la salida vencida que
        /// todavía no hizo check-out sigue contando como alojado.</summary>
        private static int? ObtenerHabitacionOcupadaPor(string dniHuesped, int idEstadoOcupada)
        {
            foreach (Habitacion ocupada in HabitacionDAO.ObtenerTodas().Where(h => h.IdEstado == idEstadoOcupada))
            {
                Hospedaje? vigente = HospedajeDAO.ObtenerActivoPorHabitacion(ocupada.NroHabitacion);
                if (vigente != null && vigente.DniHuesped == dniHuesped)
                {
                    return ocupada.NroHabitacion;
                }
            }

            return null;
        }

        /// <summary>Búsqueda para la pantalla de Reservas: todos los filtros son opcionales y combinables.</summary>
        public List<HospedajeDetalle> BuscarHospedajes(string? dni = null, string? nombre = null, int? nroHabitacion = null, DateTime? fecha = null)
        {
            return HospedajeDAO.BuscarDetalle(dni, nombre, nroHabitacion, fecha);
        }

        /// <summary>Habitación donde el huésped está alojado ahora mismo, o null si no tiene una estadía en curso.</summary>
        public HospedajeDetalle? ObtenerEstadiaActual(string dniHuesped)
        {
            return HospedajeDAO.BuscarDetalle(dni: dniHuesped).FirstOrDefault(h => !h.Finalizada);
        }

        /// <summary>Huésped y hospedaje vigentes de la habitación, para precargar la pantalla de check-out.
        /// Null si la habitación no tiene un hospedaje cargado.</summary>
        public (Huesped Huesped, Hospedaje Hospedaje)? ObtenerHuespedActivoPorHabitacion(int nroHabitacion)
        {
            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion);
            if (hospedaje == null)
            {
                return null;
            }

            Huesped? huesped = HuespedDAO.ObtenerPorDni(hospedaje.DniHuesped);
            if (huesped == null)
            {
                return null;
            }

            return (huesped, hospedaje);
        }

        /// <summary>Consumos (ventas adicionales asignadas al huésped) desde su check-in en este hospedaje,
        /// para mostrarlos en el check-out. Cada venta ya tiene su método de pago registrado.</summary>
        public List<VentaRealizada> ObtenerConsumosDeEstadia(Hospedaje hospedaje)
        {
            DateTime entrada = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
            List<VentaRealizada> consumos = VentaDAO.ObtenerRealizadas(hospedaje.DniHuesped, entrada);

            foreach (VentaRealizada venta in consumos)
            {
                venta.NroHabitacion = hospedaje.NroHabitacion;
            }

            return consumos;
        }

        /// <summary>Actualiza los datos de contacto del huésped (se usa para corregirlos al confirmar el check-out).</summary>
        public void ActualizarHuesped(Huesped huesped)
        {
            GestionHuespedes.Validar(huesped, esNuevo: false);
            HuespedDAO.Actualizar(huesped);
        }

        /// <summary>
        /// Cierra el hospedaje vigente de la habitación (pisa fecha/hora de salida con el momento real
        /// del check-out) y la pasa a Limpieza.
        /// </summary>
        public void RegistrarCheckOut(int nroHabitacion)
        {
            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion);
            if (habitacion == null)
            {
                throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");
            }

            EstadoHabitacion? estadoOcupada = EstadoHabitacionDAO.ObtenerTodos().FirstOrDefault(e => e.NomEstadoHabitacion == ESTADO_OCUPADA);
            EstadoHabitacion? estadoLimpieza = EstadoHabitacionDAO.ObtenerTodos().FirstOrDefault(e => e.NomEstadoHabitacion == ESTADO_LIMPIEZA);

            if (estadoOcupada == null || estadoLimpieza == null)
            {
                throw new InvalidOperationException("No se encontraron los estados de habitación 'Ocupada'/'Limpieza' en la base de datos.");
            }

            if (habitacion.IdEstado != estadoOcupada.IdEstado)
            {
                throw new InvalidOperationException($"La habitación {nroHabitacion} no está Ocupada.");
            }

            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion);
            if (hospedaje == null)
            {
                throw new InvalidOperationException($"No se encontró el hospedaje vigente de la habitación {nroHabitacion}.");
            }

            HospedajeDAO.RegistrarSalida(hospedaje.IdHospedaje, DateTime.Now.Date, DateTime.Now.TimeOfDay);
            HabitacionDAO.ActualizarEstado(nroHabitacion, estadoLimpieza.IdEstado);
        }
    }
}
