using Entidades;
using Logica;
using Presentacion.Administrador.Modales;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaInventario : UserControl, IVistaAdministrador
    {
        private const string ColEditar = "colEditar";
        private const string ColAjustar = "colAjustar";
        private const string ColEliminar = "colEliminar";

        private readonly GestionInventario _gestionInventario = new();
        private readonly DataGridView _dgv;
        private readonly FlowLayoutPanel _panelTarjetas;
        private List<Producto> _productos = new();

        public VistaInventario()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            _panelTarjetas = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var barraSuperior = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(0, 12, 0, 0)
            };

            var btnNuevo = EstiloBoton.Primario(new Button
            {
                Text = "+  Nuevo producto",
                Size = new Size(180, 38),
                Dock = DockStyle.Right
            });
            btnNuevo.Click += (s, e) => AbrirModalProducto(null);
            barraSuperior.Controls.Add(btnNuevo);

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();
            _dgv.CellContentClick += Dgv_CellContentClick;

            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };
            panelGrid.Controls.Add(_dgv);

            Controls.Add(panelGrid);
            Controls.Add(barraSuperior);
            Controls.Add(_panelTarjetas);
        }

        public void Refrescar() => CargarDatos();

        private void ConfigurarColumnas()
        {
            _dgv.Columns.Clear();

            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Código", Width = 100 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Producto", Width = 220 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria", HeaderText = "Categoría", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Stock", Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockMinimo", HeaderText = "Stock mínimo", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEditar, HeaderText = "", Text = "Editar", UseColumnTextForButtonValue = true, Width = 70, FlatStyle = FlatStyle.Flat });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColAjustar, HeaderText = "", Text = "Ajustar stock", UseColumnTextForButtonValue = true, Width = 110, FlatStyle = FlatStyle.Flat });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEliminar, HeaderText = "", Text = "Eliminar", UseColumnTextForButtonValue = true, Width = 80, FlatStyle = FlatStyle.Flat });
        }

        private void CargarDatos()
        {

            try
            {
                _productos = _gestionInventario.ObtenerProductos();

                int stockBajo = _productos.Count(p => p.StockBajo);
                int agotados = _productos.Count(p => p.Agotado);
                decimal valorTotal = _productos.Sum(p => p.Precio * p.Stock);

                _panelTarjetas.Controls.Clear();
                AgregarTarjeta("Total de productos", _productos.Count.ToString(), Paleta.Primario, Icono.Caja);
                AgregarTarjeta("Stock bajo", stockBajo.ToString(), Color.FromArgb(217, 119, 6), Icono.Alerta);
                AgregarTarjeta("Agotados", agotados.ToString(), Paleta.Peligro, Icono.X);
                AgregarTarjeta("Valor total de inventario", valorTotal.ToString("C0"), Paleta.Exito, Icono.Etiqueta);

                _dgv.Rows.Clear();
                foreach (Producto producto in _productos)
                {
                    string estadoText = producto.Agotado
                        ? "Agotado"
                        : producto.StockBajo
                            ? "Stock bajo"
                            : (producto.Activo ? "Activo" : "Inactivo");

                    int fila = _dgv.Rows.Add(
                        producto.Codigo,
                        producto.Nombre,
                        _gestionInventario.ConsultarCategoriaProducto(producto.IdCategoria),
                        producto.Stock,
                        producto.StockMinimo,
                        producto.Precio.ToString("C0"),
                        estadoText
                    );

                    _dgv.Rows[fila].Tag = producto;

                    Color colorEstado = producto.Agotado
                        ? Paleta.Peligro
                        : producto.StockBajo
                            ? Color.FromArgb(217, 119, 6)
                            : Paleta.Exito;

                    _dgv.Rows[fila].Cells["Estado"].Style.ForeColor = colorEstado;
                    _dgv.Rows[fila].Cells["Estado"].Style.Font = Paleta.FuenteBaseNegrita;

                    if (producto.Agotado)
                    {
                        _dgv.Rows[fila].DefaultCellStyle.BackColor = Paleta.PeligroSuave;
                    }
                    else if (producto.StockBajo)
                    {
                        _dgv.Rows[fila].DefaultCellStyle.BackColor = Paleta.AdvertenciaSuave;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los productos: {ex.Message}", "Error de Carga", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AgregarTarjeta(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 16) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelTarjetas.Controls.Add(tarjeta);
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Recuperar el producto guardado en el Tag de la fila cliqueada
            if (_dgv.Rows[e.RowIndex].Tag is not Producto producto) return;

            string columna = _dgv.Columns[e.ColumnIndex].Name;

            if (columna == ColEditar)
            {
                AbrirModalProducto(producto);
            }
            else if (columna == ColAjustar)
            {
                using var modal = new FModalAjusteStock(producto);
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    CargarDatos();
                }
            }
            else if (columna == ColEliminar)
            {
                DialogResult confirmacion = MessageBox.Show(
                    $"¿Confirma que desea eliminar el producto \"{producto.Nombre}\" ({producto.Codigo})?",
                    "Eliminar producto",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmacion == DialogResult.Yes)
                {
                    try
                    {
                        _gestionInventario.EliminarProducto(producto.Codigo);
                        CargarDatos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ocurrió un error al intentar eliminar el producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AbrirModalProducto(Producto? producto)
        {
            using var modal = new FModalProducto(producto);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                CargarDatos();
            }
        }
    }
}