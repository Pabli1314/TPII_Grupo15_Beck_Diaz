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
        private const string METODO_EFECTIVO = "Efectivo";
        private const string METODO_TARJETA = "Tarjeta";
        private const string METODO_TRANSFERENCIA = "Transferencia";

        /// <summary>Usada por otros flujos (check-in, ventas) que necesitan un turno para poder cargar
        /// su operación, sin que el recepcionista tenga que abrirlo a mano primero.</summary>
        public TurnoCaja ObtenerOAbrirTurno(string dniUsuario, decimal montoInicial = 0)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new ArgumentException("El DNI del usuario es requerido.", nameof(dniUsuario));
            }

            string dniLimpio = dniUsuario.Trim();
            TurnoCaja? turnoAbierto = TurnoCajaDAO.ObtenerAbierto(dniLimpio);
            if (turnoAbierto != null)
            {
                return turnoAbierto;
            }

            ValidarUsuarioParaTurno(dniLimpio);
            return TurnoCajaDAO.Abrir(dniLimpio, montoInicial);
        }

        public TurnoCaja? ObtenerTurnoAbierto(string dniUsuario)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                return null;
            }

            return TurnoCajaDAO.ObtenerAbierto(dniUsuario.Trim());
        }

        public List<TurnoCaja> ObtenerHistorialTurnos()
        {
            return TurnoCajaDAO.ObtenerTodos() ?? new List<TurnoCaja>();
        }

        /// <summary>Apertura explícita, con confirmación del usuario, desde la pantalla de Caja.</summary>
        public TurnoCaja AbrirTurno(string dniUsuario, decimal montoInicial)
        {
            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new ArgumentException("El DNI del usuario es requerido.", nameof(dniUsuario));
            }

            string dniLimpio = dniUsuario.Trim();

            if (TurnoCajaDAO.ObtenerAbierto(dniLimpio) != null)
            {
                throw new InvalidOperationException("Ya existe un turno abierto para este usuario.");
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

            foreach (var (hospedaje, tarifaBase) in HospedajeDAO.ObtenerPorTurno(idTurno))
            {
                resumen.TotalAlojamiento += tarifaBase;
                AcumularPorMetodo(resumen, metodos.GetValueOrDefault(hospedaje.IdMetodo), tarifaBase);
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

        /// <summary>Cierra el turno comparando el efectivo contado contra el esperado (monto inicial + cobros
        /// en efectivo). La diferencia queda registrada en observaciones para que quede trazada.</summary>
        public decimal CerrarTurno(int idTurno, decimal efectivoContado, string? observaciones)
        {
            ResumenTurno resumen = ObtenerResumen(idTurno);

            if (resumen.Turno.FechaCierre != null)
            {
                throw new InvalidOperationException("Este turno ya está cerrado.");
            }

            decimal diferencia = efectivoContado - resumen.EfectivoEsperado;
            string observacionFinal = diferencia == 0
                ? observaciones ?? string.Empty
                : $"{observaciones}{(string.IsNullOrWhiteSpace(observaciones) ? "" : " — ")}Diferencia de caja: {diferencia:C}".Trim();

            TurnoCajaDAO.Cerrar(idTurno, DateTime.Now.Date, DateTime.Now.TimeOfDay, efectivoContado, observacionFinal);

            return diferencia;
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

        /// <summary>Ventas cuyo horario cae dentro del turno (apertura → cierre, o → ahora si sigue abierto).</summary>
        public static List<Venta> ObtenerVentasDelTurno(TurnoCaja turno)
        {
            DateTime desde = turno.FechaApertura.Date + turno.HoraApertura;
            DateTime? hasta = turno.FechaCierre.HasValue
                ? turno.FechaCierre.Value.Date + (turno.HoraCierre ?? TimeSpan.Zero)
                : null;

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