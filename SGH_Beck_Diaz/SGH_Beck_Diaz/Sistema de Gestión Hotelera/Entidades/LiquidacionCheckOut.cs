using System;
using System.Collections.Generic;

namespace Entidades
{
    /// <summary>
    /// Modelo que encapsula el cálculo y desglose de liquidación final al momento del check-out,
    /// incluyendo evaluación de tolerancia (2 horas), días extras por vencimiento y consumos.
    /// </summary>
    public class LiquidacionCheckOut
    {
        public int IdHospedaje { get; set; }
        public int NroHabitacion { get; set; }
        public string TipoHabitacion { get; set; } = string.Empty;
        public decimal TarifaDiaria { get; set; }
        public string DniHuesped { get; set; } = string.Empty;
        public string NombreHuesped { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; }
        public TimeSpan HoraIngreso { get; set; }

        public DateTime FechaSalidaPactada { get; set; }
        public TimeSpan HoraSalidaPactada { get; set; }

        public DateTime FechaSalidaReal { get; set; }
        public TimeSpan HoraSalidaReal { get; set; }

        /// <summary>Días o noches pactadas originalmente en el hospedaje.</summary>
        public int DiasPactadosOriginales { get; set; }

        /// <summary>Monto abonado por el hospedaje base/original.</summary>
        public decimal MontoHospedajeOriginal { get; set; }

        /// <summary>Diferencia de tiempo entre el momento real de salida y la fecha/hora pactada.</summary>
        public TimeSpan Retraso { get; set; }

        /// <summary>Indica si el retraso superó el margen de tolerancia de 2 horas.</summary>
        public bool SuperaTolerancia { get; set; }

        /// <summary>Cantidad de días extras calculados por exceder la tolerancia.</summary>
        public int DiasExtras { get; set; }

        /// <summary>Importe del recargo por días extras: DiasExtras * TarifaDiaria.</summary>
        public decimal MontoDiasExtras { get; set; }

        /// <summary>Consumos realizados durante la estadía (ventas adicionales).</summary>
        public List<VentaRealizada> Consumos { get; set; } = new();

        /// <summary>Suma total de consumos registrados.</summary>
        public decimal TotalConsumos { get; set; }

        /// <summary>Importe total cancelado de la estadía completa (Hospedaje original + Días extras + Consumos).</summary>
        public decimal TotalLiquidacion => MontoHospedajeOriginal + MontoDiasExtras + TotalConsumos;

        /// <summary>Saldo extra a abonar en el acto de check-out (recargo por días extras).</summary>
        public decimal SaldoAbonarCheckOut => MontoDiasExtras;
    }
}
