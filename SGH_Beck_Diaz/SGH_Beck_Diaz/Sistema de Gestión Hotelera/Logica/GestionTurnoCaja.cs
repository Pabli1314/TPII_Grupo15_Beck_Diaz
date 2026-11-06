using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    /// <summary>Apertura, resumen y cierre del turno de caja del recepcionista.</summary>
    public class GestionTurnoCaja
    {
        public const string OBSERVACION_CIERRE_AUTOMATICO = "Cierre automático del sistema por fin de jornada (23:59)";

        private const string METODO_EFECTIVO = "Efectivo";
        private const string METODO_TARJETA = "Tarjeta";
        private const string METODO_TRANSFERENCIA = "Transferencia";

        /// <summary>
        /// Determina si un turno de caja debe forzar su cierre automático por haber superado
        /// las 23:59:59 del día de su apertura.
        /// </summary>
        public static bool DebeCerrarsePorFinDeJornada(TurnoCaja turno, DateTime? fechaHoraReferencia = null)
        {
            if (turno == null || turno.FechaCierre.HasValue)
            {
                return false;
            }

            DateTime ahora = fechaHoraReferencia ?? DateTime.Now;

            // Caso 1: La fecha de apertura pertenece a un día calendario anterior
            if (turno.FechaApertura.Date < ahora.Date)
            {
                return true;
            }

            // Caso 2: Es el mismo día pero ya se alcanzaron o superaron las 23:59:59
            if (turno.FechaApertura.Date == ahora.Date && ahora.TimeOfDay >= new TimeSpan(23, 59, 59))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ejecuta el cierre automático de un turno vencido registrando como fecha la fecha de apertura original,
        /// hora 23:59:59, monto final computado (monto inicial + total recaudado) y la leyenda reglamentaria.
        /// </summary>
        public void CerrarTurnoAutomaticamente(TurnoCaja turno)
        {
            if (turno == null || turno.FechaCierre.HasValue)
            {
                return;
            }

            DateTime fechaCierre = turno.FechaApertura.Date;
            TimeSpan horaCierre = new TimeSpan(23, 59, 59);

            decimal montoFinal;
            try
            {
                ResumenTurno resumen = ObtenerResumen(turno.IdTurno);
                montoFinal = resumen.Turno.MontoInicial + resumen.TotalGeneral;
            }
            catch
            {
                // Resguardo de seguridad ante inconsistencias de cálculo
                montoFinal = turno.MontoInicial;
            }

            string obsPrevias = (turno.Observaciones ?? string.Empty).Trim();
            string observacionFinal = string.IsNullOrWhiteSpace(obsPrevias)
                ? OBSERVACION_CIERRE_AUTOMATICO
                : $"{obsPrevias} — {OBSERVACION_CIERRE_AUTOMATICO}".Trim();

            TurnoCajaDAO.Cerrar(turno.IdTurno, fechaCierre, horaCierre, montoFinal, observacionFinal);

            // Reflejar cambios en la instancia en memoria
            turno.FechaCierre = fechaCierre;
            turno.HoraCierre = horaCierre;
            turno.MontoFinal = montoFinal;
            turno.Observaciones = observacionFinal;
        }

        /// <summary>
        /// Examina todos los turnos abiertos en la base de datos y fuerza el cierre de aquellos
        /// que hayan superado las 23:59:59 de su jornada de apertura.
        /// Retorna la cantidad de turnos que fueron cerrados.
        /// </summary>
        public int VerificarYCerrarTurnosVencidos()
        {
            int cantidadCerrados = 0;
            List<TurnoCaja> turnosAbiertos = TurnoCajaDAO.ObtenerTodosAbiertos();

            foreach (var turno in turnosAbiertos)
            {
                if (DebeCerrarsePorFinDeJornada(turno))
                {
                    CerrarTurnoAutomaticamente(turno);
                    cantidadCerrados++;
                }
            }

            return cantidadCerrados;
        }

        /// <summary>
        /// Usada por otros flujos (check-in, ventas) que necesitan un turno para poder cargar
        /// su operación, asegurando que corresponda a la jornada diaria en curso (DateTime.Today).
        /// Verifica de forma reactiva turnos vencidos cerrándolos antes de habilitar la jornada actual.
        /// </summary>
        public TurnoCaja ObtenerOAbrirTurno(string dniUsuario, decimal montoInicial = 0)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new ArgumentException("El DNI del usuario es requerido.", nameof(dniUsuario));
            }

            string dniLimpio = dniUsuario.Trim();

            // Detección reactiva previa a nivel global
            VerificarYCerrarTurnosVencidos();

            TurnoCaja? turnoAbierto = TurnoCajaDAO.ObtenerAbierto(dniLimpio);
            if (turnoAbierto != null)
            {
                if (!DebeCerrarsePorFinDeJornada(turnoAbierto))
                {
                    return turnoAbierto;
                }

                // Si por alguna razón sigue abierto pero vencido, forzar su cierre
                CerrarTurnoAutomaticamente(turnoAbierto);
            }

            ValidarUsuarioParaTurno(dniLimpio);
            return TurnoCajaDAO.Abrir(dniLimpio, montoInicial);
        }

        /// <summary>
        /// Obtiene el turno de caja abierto para el usuario asegurando que no haya expirado (no supere 23:59:59).
        /// Si existe un turno abierto vencido, se fuerza su cierre de jornada previa.
        /// Si autoAbrirSiExpiro es false (por defecto para vistas de caja), retorna null para permitir la apertura explícita.
        /// </summary>
        public TurnoCaja? ObtenerTurnoAbierto(string dniUsuario, bool autoAbrirSiExpiro = false)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                return null;
            }

            string dniLimpio = dniUsuario.Trim();
            TurnoCaja? turno = TurnoCajaDAO.ObtenerAbierto(dniLimpio);
            if (turno == null)
            {
                return null;
            }

            if (DebeCerrarsePorFinDeJornada(turno))
            {
                // Cierre forzado por fin de jornada
                CerrarTurnoAutomaticamente(turno);

                if (autoAbrirSiExpiro)
                {
                    ValidarUsuarioParaTurno(dniLimpio);
                    return TurnoCajaDAO.Abrir(dniLimpio, 0);
                }

                return null;
            }

            return turno;
        }

        public List<TurnoCaja> ObtenerHistorialTurnos()
        {
            return TurnoCajaDAO.ObtenerTodos() ?? new List<TurnoCaja>();
        }

        /// <summary>Apertura explícita, con confirmación del usuario, desde la pantalla de Caja para la fecha actual.</summary>
        public TurnoCaja AbrirTurno(string dniUsuario, decimal montoInicial)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new ArgumentException("El DNI del usuario es requerido.", nameof(dniUsuario));
            }

            string dniLimpio = dniUsuario.Trim();

            // Garantiza que cualquier turno vencido quede formalmente cerrado
            VerificarYCerrarTurnosVencidos();

            TurnoCaja? turnoExistente = TurnoCajaDAO.ObtenerAbierto(dniLimpio);
            if (turnoExistente != null)
            {
                if (DebeCerrarsePorFinDeJornada(turnoExistente))
                {
                    CerrarTurnoAutomaticamente(turnoExistente);
                }
                else
                {
                    throw new InvalidOperationException("Ya existe un turno abierto para este usuario en el día de hoy.");
                }
            }

            if (montoInicial < 0)
            {
                throw new ArgumentException("El monto inicial no puede ser negativo.");
            }

            ValidarUsuarioParaTurno(dniLimpio);
            return TurnoCajaDAO.Abrir(dniLimpio, montoInicial);
        }

        public ResumenTurno ObtenerResumen(int idTurno)
        {
            TurnoCaja turno = TurnoCajaDAO.ObtenerPorId(idTurno)
                ?? throw new InvalidOperationException($"No existe el turno {idTurno}.");

            var metodos = (MetodoPagoDAO.ObtenerTodos() ?? new List<MetodoPago>())
                .ToDictionary(m => m.IdMetodo, m => m.NomMetodoPago);

            var resumen = new ResumenTurno { Turno = turno };

            foreach (var (hospedaje, montoEstadia) in HospedajeDAO.ObtenerPorTurno(idTurno))
            {
                resumen.TotalAlojamiento += montoEstadia;
                AcumularPorMetodo(resumen, metodos.GetValueOrDefault(hospedaje.IdMetodo), montoEstadia);
            }

            // La tabla venta no tiene id_turno: al turno le corresponden las ventas hechas entre su
            // apertura y su cierre (o hasta ahora, si sigue abierto).
            foreach (Venta venta in ObtenerVentasDelTurno(turno))
            {
                resumen.TotalVentas += venta.Total;
                AcumularPorMetodo(resumen, metodos.GetValueOrDefault(venta.IdMetodo), venta.Total);
            }

            return resumen;
        }

        /// <summary>Cierra el turno comparando el monto final ingresado contra el esperado (monto inicial + total general
        /// de operaciones del turno). Asigna el monto_final en la base de datos.</summary>
        public decimal CerrarTurno(int idTurno, decimal montoFinal, string? observaciones = null)
        {
            ResumenTurno resumen = ObtenerResumen(idTurno);

            if (resumen.Turno.FechaCierre != null)
            {
                throw new InvalidOperationException("Este turno ya está cerrado.");
            }

            decimal montoEsperado = resumen.Turno.MontoInicial + resumen.TotalGeneral;
            decimal diferencia = montoFinal - montoEsperado;
            string observacionFinal = diferencia == 0
                ? observaciones ?? string.Empty
                : $"{observaciones}{(string.IsNullOrWhiteSpace(observaciones) ? "" : " — ")}Diferencia de caja: {diferencia:C}".Trim();

            TurnoCajaDAO.Cerrar(idTurno, DateTime.Now.Date, DateTime.Now.TimeOfDay, montoFinal, observacionFinal);

            return diferencia;
        }

        /// <summary>Compatibilidad hacia atrás: delega en CerrarTurnoAutomaticamente.</summary>
        private void ForzarCierreTurnoPrevio(TurnoCaja turnoPrevio)
        {
            CerrarTurnoAutomaticamente(turnoPrevio);
        }

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

        /// <summary>Ventas cuyo horario cae dentro del turno (apertura → cierre, o fin de jornada/ahora si sigue abierto).</summary>
        public static List<Venta> ObtenerVentasDelTurno(TurnoCaja turno)
        {
            DateTime desde = turno.FechaApertura.Date + turno.HoraApertura;
            DateTime? hasta;

            if (turno.FechaCierre.HasValue)
            {
                hasta = turno.FechaCierre.Value.Date + (turno.HoraCierre ?? TimeSpan.Zero);
            }
            else if (turno.FechaApertura.Date < DateTime.Today || DateTime.Now.TimeOfDay >= new TimeSpan(23, 59, 59))
            {
                // Si el turno abierto es de una fecha previa o superó las 23:59:59, acotar las ventas hasta el final de dicha jornada
                hasta = turno.FechaApertura.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                hasta = null;
            }

            return VentaDAO.ObtenerEnPeriodo(desde, hasta);
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
