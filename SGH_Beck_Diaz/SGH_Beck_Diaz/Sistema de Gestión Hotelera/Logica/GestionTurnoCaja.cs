using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>
    /// Apertura, resumen y cierre del turno de caja del recepcionista.
    ///
    /// Política "Cierre Manual Obligatorio con Bloqueo Operativo":
    ///  - La caja NUNCA se cierra sola: no hay timers ni cierres automáticos a medianoche. Un turno
    ///    solo se cierra cuando un usuario hace el arqueo, declara el monto y confirma (<see cref="CerrarTurno"/>).
    ///  - Un turno con fecha_cierre NULL abierto hoy está "Abierto"; si se abrió un día anterior queda
    ///    "Pendiente de cierre" (turno vencido).
    ///  - Solo con la caja "Abierta y vigente para hoy" se permiten operaciones con dinero
    ///    (check-in, check-out, ventas, prórrogas). Ver <see cref="ValidarCajaOperativa"/>.
    /// </summary>
    public class GestionTurnoCaja
    {
        private const string METODO_EFECTIVO = "Efectivo";
        private const string METODO_TARJETA = "Tarjeta";
        private const string METODO_TRANSFERENCIA = "Transferencia";

        /// <summary>Largo máximo de Turno_caja.observaciones (VARCHAR(255)).</summary>
        private const int LARGO_MAX_OBSERVACIONES = 255;

        /// <summary>Hora con la que se registra el cierre de un turno vencido: fin de su propia jornada.</summary>
        private static readonly TimeSpan FinDeJornada = new TimeSpan(23, 59, 59);

        // ───────────────────────────── Estado de la caja ─────────────────────────────

        /// <summary>
        /// Clasifica la caja del usuario: Cerrada (sin turno abierto), Abierta (turno de hoy) o
        /// PendienteCierre (turno abierto de un día anterior). No modifica la base de datos.
        /// </summary>
        public EstadoTurnoCaja ConsultarEstadoCaja(string? dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                return new EstadoTurnoCaja { Estado = EstadoCaja.Cerrada };
            }

            TurnoCaja? turno = TurnoCajaDAO.ObtenerAbierto(dniUsuario.Trim());
            if (turno == null)
            {
                return new EstadoTurnoCaja { Estado = EstadoCaja.Cerrada };
            }

            return new EstadoTurnoCaja
            {
                Estado = EsTurnoVencido(turno) ? EstadoCaja.PendienteCierre : EstadoCaja.Abierta,
                Turno = turno
            };
        }

        /// <summary>True si el turno sigue abierto pero se abrió antes de hoy (fecha_apertura &lt; DateTime.Today).</summary>
        public static bool EsTurnoVencido(TurnoCaja turno)
        {
            return turno != null && !turno.FechaCierre.HasValue && turno.FechaApertura.Date < DateTime.Today;
        }

        /// <summary>
        /// Devuelve el turno abierto y vigente para hoy, o null si la caja está cerrada o tiene un turno
        /// vencido pendiente de cierre. Lo usan los flujos que necesitan un turno operativo.
        /// </summary>
        public TurnoCaja? ObtenerTurnoAbierto(string? dniUsuario)
        {
            EstadoTurnoCaja estado = ConsultarEstadoCaja(dniUsuario);
            return estado.PermiteOperar ? estado.Turno : null;
        }

        /// <summary>
        /// Punto único de bloqueo operativo para la capa de Lógica: devuelve el turno vigente o lanza
        /// <see cref="InvalidOperationException"/> con la causa concreta (caja cerrada o turno vencido).
        /// </summary>
        public TurnoCaja ValidarCajaOperativa(string? dniUsuario)
        {
            EstadoTurnoCaja estado = ConsultarEstadoCaja(dniUsuario);
            if (!estado.PermiteOperar)
            {
                throw new InvalidOperationException(estado.MensajeBloqueo);
            }

            return estado.Turno!;
        }

        public List<TurnoCaja> ObtenerHistorialTurnos()
        {
            return TurnoCajaDAO.ObtenerTodos() ?? new List<TurnoCaja>();
        }

        // ───────────────────────────── Apertura ─────────────────────────────

        /// <summary>
        /// Apertura explícita, con confirmación del usuario, desde la pantalla de Caja para la fecha actual.
        /// Se rechaza si ya hay un turno de hoy abierto o si queda un turno vencido sin cerrar.
        /// </summary>
        public TurnoCaja AbrirTurno(string dniUsuario, decimal montoInicial)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new ArgumentException("El DNI del usuario es requerido.", nameof(dniUsuario));
            }

            if (montoInicial < 0)
            {
                throw new ArgumentException("El monto inicial no puede ser negativo.");
            }

            string dniLimpio = dniUsuario.Trim();
            EstadoTurnoCaja estado = ConsultarEstadoCaja(dniLimpio);

            switch (estado.Estado)
            {
                case EstadoCaja.Abierta:
                    throw new InvalidOperationException("Ya existe un turno abierto para este usuario en el día de hoy.");

                case EstadoCaja.PendienteCierre:
                    throw new InvalidOperationException(
                        $"Existe un turno abierto del día {estado.Turno!.FechaApertura:dd/MM/yyyy} sin cerrar. " +
                        "Debe realizar el cierre y arqueo de dicho turno antes de abrir la jornada actual.");
            }

            ValidarUsuarioParaTurno(dniLimpio);
            return TurnoCajaDAO.Abrir(dniLimpio, montoInicial);
        }

        // ───────────────────────────── Resumen ─────────────────────────────

        public ResumenTurno ObtenerResumen(int idTurno)
        {
            TurnoCaja turno = TurnoCajaDAO.ObtenerPorId(idTurno)
                ?? throw new InvalidOperationException($"No existe el turno {idTurno}.");

            var metodos = (MetodoPagoDAO.ObtenerTodos() ?? new List<MetodoPago>())
                .ToDictionary(m => m.IdMetodo, m => m.NomMetodoPago);

            // La tabla venta no tiene id_turno: al turno le corresponden las ventas hechas entre su
            // apertura y su cierre (o hasta ahora, si sigue abierto).
            return CalcularResumen(turno, HospedajeDAO.ObtenerPorTurno(idTurno), ObtenerVentasDelTurno(turno), metodos);
        }

        /// <summary>
        /// Totales de un turno a partir de sus datos ya consultados. Cada check-in del turno aporta lo que
        /// se le cobró al huésped: tarifa por día × noches (mismo cálculo que muestra el Check-in); cada
        /// venta, su total. Sirve tanto para un turno (ObtenerResumen) como para calcular muchos turnos
        /// de una vez sin consultar la base por cada uno (Históricos de Gerencia).
        /// </summary>
        public static ResumenTurno CalcularResumen(TurnoCaja turno, IEnumerable<(Hospedaje Hospedaje, decimal TarifaBase)> hospedajes,
            IEnumerable<Venta> ventas, IReadOnlyDictionary<int, string> metodos)
        {
            var resumen = new ResumenTurno { Turno = turno };

            foreach (var (hospedaje, tarifaBase) in hospedajes)
            {
                // Monto guardado en el hospedaje (check-in + prórrogas); si el registro no lo tiene,
                // se calcula como tarifa por día × noches.
                decimal cobrado = hospedaje.MontoTotal > 0
                    ? hospedaje.MontoTotal
                    : GestionHospedajes.CalcularCostoHospedaje(tarifaBase, hospedaje.FechaEntrada, hospedaje.FechaSalida);
                resumen.TotalAlojamiento += cobrado;
                AcumularPorMetodo(resumen, metodos.GetValueOrDefault(hospedaje.IdMetodo), cobrado);
            }

            foreach (Venta venta in ventas)
            {
                resumen.TotalVentas += venta.Total;
                AcumularPorMetodo(resumen, metodos.GetValueOrDefault(venta.IdMetodo), venta.Total);
            }

            return resumen;
        }

        /// <summary>True si la venta se hizo dentro del horario del turno (mismo criterio que ObtenerVentasDelTurno).</summary>
        public static bool VentaPerteneceATurno(Venta venta, TurnoCaja turno)
        {
            DateTime desde = turno.FechaApertura.Date + turno.HoraApertura;
            DateTime? hasta = turno.FechaCierre.HasValue
                ? turno.FechaCierre.Value.Date + (turno.HoraCierre ?? TimeSpan.Zero)
                : null;

            return venta.Momento >= desde && (hasta == null || venta.Momento <= hasta);
        }

        /// <summary>Ventas cuyo horario cae dentro del turno (apertura → cierre; si sigue abierto, hasta ahora,
        /// o hasta el fin de su jornada si es un turno vencido).</summary>
        public static List<Venta> ObtenerVentasDelTurno(TurnoCaja turno)
        {
            DateTime desde = turno.FechaApertura.Date + turno.HoraApertura;
            DateTime? hasta;

            if (turno.FechaCierre.HasValue)
            {
                hasta = turno.FechaCierre.Value.Date + (turno.HoraCierre ?? TimeSpan.Zero);
            }
            else if (EsTurnoVencido(turno))
            {
                // Un turno vencido no pudo registrar operaciones después de su día (el bloqueo lo impide),
                // así que no se le imputan ventas de otras jornadas.
                hasta = turno.FechaApertura.Date + FinDeJornada;
            }
            else
            {
                hasta = null;
            }

            return VentaDAO.ObtenerEnPeriodo(desde, hasta);
        }

        // ───────────────────────────── Cierre manual ─────────────────────────────

        /// <summary>
        /// Cierre MANUAL del turno: compara el monto final declarado por el usuario contra el esperado
        /// (monto inicial + total general de operaciones del turno) y lo registra en la base.
        ///
        /// Si el turno es vencido (de un día anterior), el cierre se registra contablemente al fin de su
        /// propia jornada (fecha de apertura, 23:59:59) para que su ventana de ventas no se superponga con
        /// la jornada actual, y se deja constancia en observaciones de cuándo se hizo realmente el arqueo.
        /// </summary>
        /// <returns>Diferencia entre lo declarado y lo esperado (0 = caja cuadrada).</returns>
        public decimal CerrarTurno(int idTurno, decimal montoFinal, string? observaciones = null)
        {
            if (montoFinal < 0)
            {
                throw new ArgumentException("El monto final no puede ser negativo.");
            }

            ResumenTurno resumen = ObtenerResumen(idTurno);
            TurnoCaja turno = resumen.Turno;

            if (turno.FechaCierre != null)
            {
                throw new InvalidOperationException("Este turno ya está cerrado.");
            }

            decimal montoEsperado = turno.MontoInicial + resumen.TotalGeneral;
            decimal diferencia = montoFinal - montoEsperado;

            DateTime ahora = DateTime.Now;
            bool vencido = EsTurnoVencido(turno);

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                partes.Add(observaciones.Trim());
            }
            if (vencido)
            {
                partes.Add($"Cierre manual tardío: arqueo realizado el {ahora:dd/MM/yyyy HH:mm}");
            }
            if (diferencia != 0)
            {
                partes.Add($"Diferencia de caja: {diferencia:C}");
            }

            string observacionFinal = string.Join(" — ", partes);
            if (observacionFinal.Length > LARGO_MAX_OBSERVACIONES)
            {
                observacionFinal = observacionFinal.Substring(0, LARGO_MAX_OBSERVACIONES);
            }

            DateTime fechaCierre = vencido ? turno.FechaApertura.Date : ahora.Date;
            TimeSpan horaCierre = vencido ? FinDeJornada : ahora.TimeOfDay;

            TurnoCajaDAO.Cerrar(idTurno, fechaCierre, horaCierre, montoFinal, observacionFinal);

            return diferencia;
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        /// <summary>FK_TurnoCaja_Usuario: el turno solo se puede abrir a nombre de un usuario que exista
        /// en la tabla Usuario. Además se exige que esté activo.</summary>
        private static void ValidarUsuarioParaTurno(string dniUsuario)
        {
            Usuario? usuario = UsuarioDAO.ObtenerPorDni(dniUsuario);
            if (usuario == null)
            {
                throw new InvalidOperationException($"El usuario de la sesión (DNI {dniUsuario}) no existe en la base de datos. Inicie sesión con un usuario registrado.");
            }

            if (!usuario.Estado)
            {
                throw new InvalidOperationException($"El usuario {usuario.NomUsuario} {usuario.ApeUsuario} está inactivo y no puede abrir un turno de caja.");
            }
        }

        private static void AcumularPorMetodo(ResumenTurno resumen, string? nombreMetodo, decimal monto)
        {
            if (string.IsNullOrWhiteSpace(nombreMetodo)) return;

            switch (nombreMetodo)
            {
                case METODO_EFECTIVO:
                    resumen.TotalEfectivo += monto;
                    break;
                case METODO_TARJETA:
                    resumen.TotalTarjeta += monto;
                    break;
                case METODO_TRANSFERENCIA:
                    resumen.TotalTransferencia += monto;
                    break;
            }
        }
    }
}
