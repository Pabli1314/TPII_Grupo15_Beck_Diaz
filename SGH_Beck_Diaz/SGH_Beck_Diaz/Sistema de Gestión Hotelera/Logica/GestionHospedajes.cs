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
        /// momento actual.
        /// </summary>
        public void RegistrarCheckIn(int nroHabitacion, string dniUsuario, Huesped huesped, int idMetodo, DateTime fechaSalida, TimeSpan horaSalida, decimal montoTotal = 0)
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

            TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(dniUsuario);
            if (turno == null)
            {
                throw new InvalidOperationException("La caja se encuentra cerrada. Debe abrir la caja antes de registrar un Check-in y realizar el cobro.");
            }

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
                monto_total = (float)montoTotal,
                IdMetodo = idMetodo,
                NroHabitacion = nroHabitacion,
                IdTurno = turno.IdTurno,
                DniHuesped = huesped.DniHuesped
            });

            HabitacionDAO.ActualizarEstado(nroHabitacion, estadoOcupada.IdEstado);
        }

        /// <summary>Número de la habitación Ocupada cuyo hospedaje vigente es de ese huésped, o null si no está alojado.</summary>
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
        public List<Hospedaje> BuscarHospedajes(string? dni = null, string? nombre = null, int? nroHabitacion = null, DateTime? fecha = null, string? termino = null)
        {
            dni = string.IsNullOrWhiteSpace(dni) ? null : dni.Trim();
            nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            termino = string.IsNullOrWhiteSpace(termino) ? null : termino.Trim();

            List<Hospedaje> resultado = HospedajeDAO.BuscarDetalle(
                dni: dni,
                nombre: nombre,
                nroHabitacion: nroHabitacion,
                fecha: fecha,
                termino: termino);

            return resultado ?? new List<Hospedaje>();
        }

        /// <summary>Habitación donde el huésped está alojado ahora mismo, o null si no tiene una estadía en curso.</summary>
        public Hospedaje? ObtenerEstadiaActual(string dniHuesped)
        {
            List<Hospedaje> lista = HospedajeDAO.BuscarDetalle(
                dni: dniHuesped,
                nombre: null,
                nroHabitacion: null,
                fecha: null,
                termino: null);

            return lista?.FirstOrDefault(h => (h.FechaSalida.Date + h.HoraSalida) > DateTime.Now);
        }

        /// <summary>Huésped y hospedaje vigentes de la habitación, para precargar la pantalla de check-out.</summary>
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

        /// <summary>Consumos (ventas adicionales asignadas al huésped) desde su check-in en este hospedaje.</summary>
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

        /// <summary>
        /// Calcula el monto estimado de la estadía según la tarifa diaria del tipo de habitación
        /// y la cantidad de noches entre hoy y <paramref name="fechaSalida"/>.
        /// </summary>
        /// <returns>Monto total estimado (tarifa × noches) y la tarifa diaria utilizada.</returns>
        public (decimal MontoTotal, decimal TarifaDiaria, int Noches, string TipoHabitacion) CalcularMontoEstadia(
            int nroHabitacion, DateTime fechaSalida)
        {
            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion);
            if (habitacion == null)
                throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

            TipoHabitacion? tipo = TipoHabitacionDAO.ObtenerTodos()
                .FirstOrDefault(t => t.IdTipoHabitacion == habitacion.IdTipoHabitacion);

            if (tipo == null)
                throw new InvalidOperationException("No se encontró el tipo de habitación.");

            int noches = Math.Max(1, (fechaSalida.Date - DateTime.Today).Days);
            decimal monto = tipo.Tarifa * noches;

            return (monto, tipo.Tarifa, noches, tipo.Descripcion);
        }

        /// <summary>Actualiza los datos de contacto del huésped.</summary>
        public void ActualizarHuesped(Huesped huesped)
        {
            GestionHuespedes.Validar(huesped, esNuevo: false);
            HuespedDAO.Actualizar(huesped);
        }

        /// <summary>
        /// Cierra el hospedaje vigente de la habitación y la pasa a Limpieza.
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

        /// <summary>
        /// Calcula el importe adicional para la prórroga de estadía: (Nuevas noches × Tarifa diaria).
        /// </summary>
        public (decimal MontoAdicional, decimal TarifaDiaria, int DiasAdicionales, string TipoHabitacion, DateTime FechaSalidaActual, TimeSpan HoraSalidaActual) CalcularProrrogaEstadia(
            int nroHabitacion, DateTime nuevaFechaSalida)
        {
            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion)
                ?? throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

            TipoHabitacion? tipo = TipoHabitacionDAO.ObtenerTodos()
                .FirstOrDefault(t => t.IdTipoHabitacion == habitacion.IdTipoHabitacion)
                ?? throw new InvalidOperationException("No se encontró el tipo de habitación.");

            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion)
                ?? throw new InvalidOperationException($"No se encontró una estadía activa para la habitación {nroHabitacion}.");

            int diasAdicionales = (nuevaFechaSalida.Date - hospedaje.FechaSalida.Date).Days;
            if (diasAdicionales < 0) diasAdicionales = 0;

            decimal montoAdicional = diasAdicionales * tipo.Tarifa;

            return (montoAdicional, tipo.Tarifa, diasAdicionales, tipo.Descripcion, hospedaje.FechaSalida, hospedaje.HoraSalida);
        }

        /// <summary>
        /// Registra la prórroga de estadía actualizando fecha/hora de salida y monto acumulado en el hospedaje,
        /// e inserta la operación en la tabla de prórrogas para el historial y ticket comprobante.
        /// </summary>
        public ProrrogaEstadia RegistrarProrrogaEstadia(
            int nroHabitacion,
            string dniUsuario,
            DateTime nuevaFechaSalida,
            TimeSpan nuevaHoraSalida,
            int idMetodo,
            decimal montoAdicional,
            int diasAdicionales)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new InvalidOperationException("No hay un usuario identificado para registrar la prórroga.");
            }

            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion)
                ?? throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion)
                ?? throw new InvalidOperationException($"No se encontró la estadía activa para la habitación {nroHabitacion}.");

            DateTime salidaActual = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;
            DateTime nuevaSalida = nuevaFechaSalida.Date + nuevaHoraSalida;

            if (nuevaSalida <= salidaActual)
            {
                throw new InvalidOperationException("La nueva fecha y hora de salida deben ser posteriores a la fecha y hora de salida pactada actual.");
            }

            if (diasAdicionales <= 0)
            {
                throw new InvalidOperationException("Debe seleccionar al menos un día adicional para extender la estadía.");
            }

            if (!MetodoPagoDAO.ObtenerTodos().Any(m => m.IdMetodo == idMetodo))
            {
                throw new InvalidOperationException("El método de pago seleccionado no es válido.");
            }

            TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(dniUsuario)
                ?? throw new InvalidOperationException("La caja se encuentra cerrada. Debe abrir la caja antes de registrar la extensión de estadía.");

            decimal nuevoMontoTotal = hospedaje.MontoTotal + montoAdicional;

            // 1. Actualiza fecha/hora de salida y monto acumulado en la tabla hospedaje
            HospedajeDAO.ActualizarEstadia(hospedaje.IdHospedaje, nuevaFechaSalida.Date, nuevaHoraSalida, nuevoMontoTotal);

            // 2. Registra la prórroga en prorroga_estadia para auditoría e historial
            var prorroga = new ProrrogaEstadia
            {
                IdHospedaje = hospedaje.IdHospedaje,
                FechaProrroga = DateTime.Today,
                HoraProrroga = DateTime.Now.TimeOfDay,
                FechaSalidaAnterior = hospedaje.FechaSalida,
                HoraSalidaAnterior = hospedaje.HoraSalida,
                NuevaFechaSalida = nuevaFechaSalida.Date,
                NuevaHoraSalida = nuevaHoraSalida,
                DiasAdicionales = diasAdicionales,
                MontoAdicional = montoAdicional,
                IdMetodo = idMetodo,
                IdTurno = turno.IdTurno
            };

            ProrrogaDAO.Crear(prorroga);

            return prorroga;
        }

        /// <summary>
        /// Evalúa la liquidación de Check-out analizando el retraso respecto a la fecha/hora pactada:
        /// Margen de tolerancia: si el retraso es menor o igual a 2 horas, no se cobra penalización ni días extra.
        /// Si supera las 2 horas, calcula y cobra el/los día(s) extra correspondiente(s) según la tarifa diaria.
        /// </summary>
        public LiquidacionCheckOut EvaluarLiquidacionCheckOut(int nroHabitacion, DateTime momentoSalidaReal)
        {
            var activo = ObtenerHuespedActivoPorHabitacion(nroHabitacion)
                ?? throw new InvalidOperationException($"No se encontró el hospedaje activo para la habitación {nroHabitacion}.");

            (Huesped huesped, Hospedaje hospedaje) = activo;

            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion)
                ?? throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

            TipoHabitacion? tipo = TipoHabitacionDAO.ObtenerTodos()
                .FirstOrDefault(t => t.IdTipoHabitacion == habitacion.IdTipoHabitacion)
                ?? throw new InvalidOperationException("No se encontró el tipo de habitación.");

            DateTime fechaIngresoTotal = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
            DateTime salidaPactada = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;

            int diasPactados = Math.Max(1, (hospedaje.FechaSalida.Date - hospedaje.FechaEntrada.Date).Days);
            decimal tarifaDiaria = tipo.Tarifa;

            TimeSpan retraso = momentoSalidaReal - salidaPactada;
            bool superaTolerancia = false;
            int diasExtras = 0;
            decimal montoDiasExtras = 0m;

            if (retraso > TimeSpan.Zero)
            {
                // Margen de tolerancia: <= 2 horas no se cobra penalización
                if (retraso.TotalHours > 2.0)
                {
                    superaTolerancia = true;
                    // Cálculo diario: se cobran los días transcurridos superada la tolerancia
                    diasExtras = Math.Max(1, (int)Math.Ceiling((retraso.TotalHours - 2.0) / 24.0));
                    montoDiasExtras = diasExtras * tarifaDiaria;
                }
            }
            else
            {
                retraso = TimeSpan.Zero;
            }

            List<VentaRealizada> consumos = ObtenerConsumosDeEstadia(hospedaje);
            decimal totalConsumos = consumos.Sum(v => v.Total);

            return new LiquidacionCheckOut
            {
                IdHospedaje = hospedaje.IdHospedaje,
                NroHabitacion = nroHabitacion,
                TipoHabitacion = tipo.Descripcion,
                TarifaDiaria = tarifaDiaria,
                DniHuesped = huesped.DniHuesped,
                NombreHuesped = $"{huesped.Nombre} {huesped.Apellido}".Trim(),
                FechaIngreso = hospedaje.FechaEntrada,
                HoraIngreso = hospedaje.HoraEntrada,
                FechaSalidaPactada = hospedaje.FechaSalida,
                HoraSalidaPactada = hospedaje.HoraSalida,
                FechaSalidaReal = momentoSalidaReal.Date,
                HoraSalidaReal = momentoSalidaReal.TimeOfDay,
                DiasPactadosOriginales = diasPactados,
                MontoHospedajeOriginal = hospedaje.MontoTotal,
                Retraso = retraso,
                SuperaTolerancia = superaTolerancia,
                DiasExtras = diasExtras,
                MontoDiasExtras = montoDiasExtras,
                Consumos = consumos,
                TotalConsumos = totalConsumos
            };
        }

        /// <summary>
        /// Cierra el hospedaje registrando la salida con la liquidación final (incluyendo recargo por días extras si correspondiera)
        /// y pasa la habitación al estado Limpieza.
        /// </summary>
        public void RegistrarCheckOutConLiquidacion(
            int nroHabitacion,
            string? dniUsuario,
            LiquidacionCheckOut liquidacion,
            int? idMetodoPagoExtras = null)
        {
            if (liquidacion == null) throw new ArgumentNullException(nameof(liquidacion));

            Habitacion? habitacion = HabitacionDAO.ObtenerPorNumero(nroHabitacion)
                ?? throw new InvalidOperationException($"No existe la habitación {nroHabitacion}.");

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

            Hospedaje? hospedaje = HospedajeDAO.ObtenerActivoPorHabitacion(nroHabitacion)
                ?? throw new InvalidOperationException($"No se encontró el hospedaje vigente de la habitación {nroHabitacion}.");

            // Si hay recargo por días extras y se especificó usuario, validar que la caja esté abierta
            if (liquidacion.DiasExtras > 0 && !string.IsNullOrWhiteSpace(dniUsuario))
            {
                TurnoCaja? turno = _gestionTurnoCaja.ObtenerTurnoAbierto(dniUsuario);
                if (turno == null)
                {
                    throw new InvalidOperationException("La caja se encuentra cerrada. Debe abrir la caja antes de registrar un Check-out con cobro de recargo por estadía vencida.");
                }
            }

            decimal montoTotalFinal = hospedaje.MontoTotal + liquidacion.MontoDiasExtras;

            HospedajeDAO.RegistrarSalidaConMonto(
                hospedaje.IdHospedaje,
                liquidacion.FechaSalidaReal.Date,
                liquidacion.HoraSalidaReal,
                montoTotalFinal);

            HabitacionDAO.ActualizarEstado(nroHabitacion, estadoLimpieza.IdEstado);
        }
    }
}
