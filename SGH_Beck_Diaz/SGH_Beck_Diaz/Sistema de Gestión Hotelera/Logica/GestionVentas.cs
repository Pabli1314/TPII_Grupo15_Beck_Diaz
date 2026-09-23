using System;
using System.Collections.Generic;
using System.Linq;
using Entidades;
using Datos;

namespace Logica
{
    public class GestionVentas
    {
        private readonly GestionTurnoCaja _gestionTurnoCaja = new GestionTurnoCaja();
        private readonly GestionHabitaciones _gestionHabitaciones = new GestionHabitaciones();

        /// <summary>Habitaciones Ocupadas con su huésped vigente, para elegir a quién se le asigna la venta.</summary>
        public List<HabitacionResumen> ObtenerHuespedesAlojados()
        {
            return _gestionHabitaciones.ObtenerResumenHabitaciones()
                .Where(h => h.Ocupada && !string.IsNullOrWhiteSpace(h.DniHuesped))
                .OrderBy(h => h.NroHabitacion)
                .ToList();
        }

        public List<Producto> ObtenerProductosDisponibles()
        {
            return ProductoDAO.ObtenerTodos().Where(p => p.Activo).ToList();
        }

        public List<MetodoPago> ObtenerMetodosPago()
        {
            return MetodoPagoDAO.ObtenerTodos();
        }

        /// <summary>
        /// Ventas adicionales realizadas, con el huésped que hizo la consumición y lo que consumió.
        /// La habitación se toma de la estadía del huésped vigente al momento de la venta.
        /// <paramref name="filtro"/> busca por nombre, apellido o DNI del huésped, o por número de
        /// habitación; "mostrador" trae las ventas sin huésped. <paramref name="soloHuespedes"/> excluye
        /// las ventas de mostrador.
        /// </summary>
        public List<VentaRealizada> ObtenerVentasRealizadas(string? filtro = null, bool soloHuespedes = false)
        {
            List<VentaRealizada> ventas = VentaDAO.ObtenerRealizadas();
            List<HospedajeDetalle> hospedajes = HospedajeDAO.BuscarDetalle();

            foreach (VentaRealizada venta in ventas.Where(v => v.DniHuesped != null))
            {
                venta.NroHabitacion = hospedajes
                    .Where(h => h.DniHuesped == venta.DniHuesped && h.FechaEntrada.Date + h.HoraEntrada <= venta.Momento)
                    .OrderByDescending(h => h.FechaEntrada.Date + h.HoraEntrada)
                    .Select(h => (int?)h.NroHabitacion)
                    .FirstOrDefault();
            }

            IEnumerable<VentaRealizada> resultado = ventas;

            if (soloHuespedes)
            {
                resultado = resultado.Where(v => !v.EsDeMostrador);
            }

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string texto = filtro.Trim();
                bool buscaMostrador = "mostrador".StartsWith(texto, StringComparison.OrdinalIgnoreCase);

                resultado = resultado.Where(v =>
                    (buscaMostrador && v.EsDeMostrador)
                    || (v.NombreHuesped?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (v.DniHuesped?.Contains(texto) ?? false)
                    || (v.NroHabitacion?.ToString() == texto));
            }

            return resultado.ToList();
        }

        /// <summary>Cuántas unidades se pueden vender sin romper CH_producto_disponible
        /// (el stock no puede quedar por debajo del stock mínimo).</summary>
        public static int UnidadesVendibles(Producto producto)
        {
            return Math.Max(0, producto.Stock - producto.StockMinimo);
        }

        /// <summary>
        /// Registra una venta de mostrador (carrito de productos) y descuenta stock en una sola
        /// transacción. Antes se asegura de que el usuario tenga un turno de caja abierto (lo abre si
        /// hace falta): la base no guarda el turno en la venta, así que la venta se le imputa al turno
        /// por su fecha/hora. Valida contra la base: producto existente y activo, cantidad &gt; 0
        /// (CK_detalle_cantidad), un solo renglón por producto (PK de detalle_venta) y que el stock no
        /// quede por debajo del mínimo (CH_producto_disponible). Si se indica <paramref name="dniHuesped"/>,
        /// la venta queda asignada a ese huésped (venta.dni_huesped), que tiene que estar alojado en
        /// una habitación Ocupada; null = venta de mostrador.
        /// </summary>
        public Venta RegistrarVenta(List<(Producto Producto, int Cantidad)> carrito, int idMetodo, string dniUsuario, string? dniHuesped = null)
        {
            dniHuesped = string.IsNullOrWhiteSpace(dniHuesped) ? null : dniHuesped.Trim();
            if (dniHuesped != null)
            {
                if (HuespedDAO.ObtenerPorDni(dniHuesped) == null)
                {
                    throw new InvalidOperationException($"No existe un huésped registrado con DNI {dniHuesped}.");
                }

                if (!ObtenerHuespedesAlojados().Any(h => h.DniHuesped == dniHuesped))
                {
                    throw new InvalidOperationException($"El huésped con DNI {dniHuesped} no está alojado en ninguna habitación; no se le puede asignar la venta.");
                }
            }

            if (carrito == null || carrito.Count == 0)
            {
                throw new InvalidOperationException("El carrito no puede estar vacío.");
            }

            if (string.IsNullOrWhiteSpace(dniUsuario))
            {
                throw new InvalidOperationException("No hay un usuario identificado para registrar la venta.");
            }

            if (!MetodoPagoDAO.ObtenerTodos().Any(m => m.IdMetodo == idMetodo))
            {
                throw new InvalidOperationException("El método de pago seleccionado no es válido.");
            }

            // detalle_venta tiene PK (id_venta, cod_producto): el mismo producto va en un solo renglón.
            var renglones = carrito
                .GroupBy(item => item.Producto.Codigo)
                .Select(g => (Codigo: g.Key, Nombre: g.First().Producto.Nombre, Cantidad: g.Sum(i => i.Cantidad)))
                .ToList();

            var detalles = new List<DetalleVenta>();

            foreach (var (codigo, nombre, cantidad) in renglones)
            {
                if (cantidad <= 0)
                {
                    throw new InvalidOperationException($"La cantidad de \"{nombre}\" debe ser mayor a cero.");
                }

                // Precio y stock se toman de la base, no del carrito (pueden haber cambiado).
                Producto actual = ProductoDAO.ObtenerPorCodigo(codigo)
                    ?? throw new InvalidOperationException($"El producto \"{nombre}\" ya no existe.");

                if (!actual.Activo)
                {
                    throw new InvalidOperationException($"El producto \"{actual.Nombre}\" está inactivo y no se puede vender.");
                }

                int vendibles = UnidadesVendibles(actual);
                if (cantidad > vendibles)
                {
                    throw new InvalidOperationException(vendibles == 0
                        ? $"\"{actual.Nombre}\" está en su stock mínimo ({actual.StockMinimo}); no se pueden vender más unidades."
                        : $"Solo se pueden vender {vendibles} unidad(es) de \"{actual.Nombre}\" (stock {actual.Stock}, mínimo {actual.StockMinimo}).");
                }

                detalles.Add(new DetalleVenta
                {
                    CodProducto = actual.Codigo,
                    PrecioUnitario = actual.Precio,
                    Cantidad = cantidad,
                    Subtotal = actual.Precio * cantidad
                });
            }

            _gestionTurnoCaja.ObtenerOAbrirTurno(dniUsuario);

            Venta venta = new Venta { Total = detalles.Sum(d => d.Subtotal), IdMetodo = idMetodo, DniHuesped = dniHuesped };
            venta.IdVenta = VentaDAO.RegistrarConDetalle(venta, detalles);

            return venta;
        }
    }
}
