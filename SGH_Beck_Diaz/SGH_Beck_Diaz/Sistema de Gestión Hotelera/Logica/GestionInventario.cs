using Entidades;
using Datos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Administra el catálogo de productos (tabla "producto"), compartido con las Ventas
    /// Adicionales del Recepcionista: el stock que descuenta una venta es el mismo que ve
    /// esta pantalla.
    /// </summary>
    public class GestionInventario
    {
        public List<Producto> ObtenerProductos()
        {
            return ProductoDAO.ObtenerTodos();
        }

        public void CrearProducto(Producto producto)
        {
            Validar(producto);

            if (ProductoDAO.ObtenerPorCodigo(producto.Codigo) != null)
            {
                throw new ArgumentException($"Ya existe un producto con el código \"{producto.Codigo}\".");
            }

            ProductoDAO.Crear(producto);
        }

        public void EditarProducto(Producto producto)
        {
            Validar(producto);

            if (ProductoDAO.ObtenerPorCodigo(producto.Codigo) == null)
            {
                throw new InvalidOperationException("No se encontró el producto a editar.");
            }

            ProductoDAO.Actualizar(producto);
        }

        public void EliminarProducto(string codigo)
        {
            // FK_detalle_producto: un producto que ya se vendió no se puede borrar sin romper el historial.
            if (ProductoDAO.TieneVentas(codigo))
            {
                throw new InvalidOperationException("El producto tiene ventas registradas y no se puede eliminar. Puede desactivarlo para que no se ofrezca más.");
            }

            ProductoDAO.Eliminar(codigo);
        }

        public void AjustarStock(string codigo, int cantidad)
        {
            Producto? existente = ProductoDAO.ObtenerPorCodigo(codigo)
                ?? throw new InvalidOperationException("No se encontró el producto.");

            int nuevoStock = existente.Stock + cantidad;
            if (nuevoStock < existente.StockMinimo)
            {
                throw new ArgumentException($"El stock no puede quedar por debajo del mínimo ({existente.StockMinimo}). Stock actual: {existente.Stock}.");
            }

            existente.Stock = nuevoStock;
            ProductoDAO.Actualizar(existente);
        }

        /// <summary>Normaliza y valida contra la tabla producto: cod_producto VARCHAR(15),
        /// descripcion_product VARCHAR(20), precio DECIMAL(10,2), CH_producto_disponible
        /// (stock_disponible &gt;= stock_min) y FK a categoria_producto.</summary>
        private static void Validar(Producto producto)
        {
            producto.Codigo = (producto.Codigo ?? string.Empty).Trim();
            producto.Nombre = (producto.Nombre ?? string.Empty).Trim();

            if (producto.Codigo.Length == 0)
            {
                throw new ArgumentException("Debe ingresar un código de producto.");
            }

            if (producto.Codigo.Length > LargoCodigo)
            {
                throw new ArgumentException($"El código no puede superar los {LargoCodigo} caracteres.");
            }

            if (producto.Nombre.Length == 0)
            {
                throw new ArgumentException("Debe ingresar un nombre de producto.");
            }

            if (producto.Nombre.Length > LargoNombre)
            {
                throw new ArgumentException($"El nombre no puede superar los {LargoNombre} caracteres.");
            }

            if (producto.Precio < 0)
            {
                throw new ArgumentException("El precio no puede ser negativo.");
            }

            if (producto.Precio > PrecioMaximo || decimal.Round(producto.Precio, 2) != producto.Precio)
            {
                throw new ArgumentException($"El precio debe tener como máximo 2 decimales y no superar {PrecioMaximo:N2}.");
            }

            if (producto.StockMinimo < 0)
            {
                throw new ArgumentException("El stock mínimo no puede ser negativo.");
            }

            if (producto.Stock < producto.StockMinimo)
            {
                throw new ArgumentException($"El stock ({producto.Stock}) no puede ser menor al stock mínimo ({producto.StockMinimo}).");
            }

            if (!CategoriaDAO.ObtenerTodas().Any(c => c.IdCategoria == producto.IdCategoria))
            {
                throw new ArgumentException("Debe seleccionar una categoría válida.");
            }
        }

        private const int LargoCodigo = 15;
        private const int LargoNombre = 20;
        private const decimal PrecioMaximo = 99_999_999.99m;

        public List<Categoria> ObtenerCategorias()
        {
            return CategoriaDAO.ObtenerTodas();
        }

        public string ConsultarCategoriaProducto(int id_producto)
        {
            return CategoriaDAO.ConsultarNombreCategoria(id_producto);
        }
    }
}
