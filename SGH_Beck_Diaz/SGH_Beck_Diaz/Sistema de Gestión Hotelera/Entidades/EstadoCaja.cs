using System;

namespace Entidades
{
    /// <summary>Situación operativa de la caja de un recepcionista (se deriva de Turno_caja, no se persiste).</summary>
    public enum EstadoCaja
    {
        /// <summary>No existe ningún turno con fecha_cierre NULL para el usuario.</summary>
        Cerrada,

        /// <summary>Existe un turno abierto cuya fecha_apertura es la de hoy: se puede operar.</summary>
        Abierta,

        /// <summary>Existe un turno abierto de un día anterior que nadie cerró: hay que arquearlo y cerrarlo
        /// manualmente antes de poder abrir la jornada actual.</summary>
        PendienteCierre
    }

    /// <summary>Resultado de consultar la caja: el estado y, si lo hay, el turno sin cerrar.</summary>
    public class EstadoTurnoCaja
    {
        public EstadoCaja Estado { get; init; }

        /// <summary>Turno con fecha_cierre NULL (vigente o vencido). Null cuando la caja está cerrada.</summary>
        public TurnoCaja? Turno { get; init; }

        /// <summary>True solo si la caja está abierta y vigente para el día de hoy.</summary>
        public bool PermiteOperar => Estado == EstadoCaja.Abierta && Turno != null;

        /// <summary>Mensaje que explica por qué no se puede operar; vacío si la caja está operativa.</summary>
        public string MensajeBloqueo => Estado switch
        {
            EstadoCaja.Cerrada =>
                "No es posible operar: la caja se encuentra cerrada. Abra la caja desde \"Gestión de Caja\" para continuar.",
            EstadoCaja.PendienteCierre =>
                $"Operación bloqueada: existe un turno anterior sin cerrar (turno #{Turno?.IdTurno} del {Turno?.FechaApertura:dd/MM/yyyy}) " +
                "que debe ser regularizado. Realice el arqueo y cierre de dicho turno desde \"Gestión de Caja\" y luego abra la jornada actual.",
            _ => string.Empty
        };

        /// <summary>Título corto para el MessageBox de advertencia.</summary>
        public string TituloBloqueo => Estado == EstadoCaja.PendienteCierre ? "Turno pendiente de cierre" : "Caja cerrada";
    }
}
