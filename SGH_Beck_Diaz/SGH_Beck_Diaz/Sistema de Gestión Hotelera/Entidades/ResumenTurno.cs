namespace Entidades
{
    /// <summary>DTO en memoria (no persiste) con los totales de un turno de caja, para mostrar
    /// el estado de Caja y calcular el cierre.</summary>
    public class ResumenTurno
    {
        public TurnoCaja Turno { get; set; } = new TurnoCaja();
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }
        public decimal TotalAlojamiento { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal EfectivoEsperado => Turno.MontoInicial + TotalEfectivo;
        public decimal TotalGeneral => TotalEfectivo + TotalTarjeta + TotalTransferencia;
    }
}
