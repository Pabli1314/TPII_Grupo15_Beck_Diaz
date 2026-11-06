using System;

namespace Entidades
{
    /// <summary>Tabla venta: id_venta, fecha_venta/hora_venta (DEFAULT de la base), total, id_metodo y
    /// dni_huesped (huésped que compra; NULL = venta de mostrador). La base no relaciona la venta con
    /// un turno de caja: el turno se deduce por la fecha/hora (ver GestionTurnoCaja.ObtenerResumen).</summary>
    public class Venta
    {
        public int IdVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public TimeSpan HoraVenta { get; set; }
        public decimal Total { get; set; }
        public int IdMetodo { get; set; }
        public string? DniHuesped { get; set; }

        public DateTime Momento => FechaVenta.Date + HoraVenta;
    }
}
