using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalProducto : FormModalBase
    {
        private readonly GestionInventario _gestionInventario = new();
        private readonly Producto? _productoOriginal;

        private readonly TextBox _txtCodigo;
        private readonly TextBox _txtNombre;
        private readonly ComboBox _cmbCategoria;
        private readonly NumericUpDown _numStock;
        private readonly NumericUpDown _numStockMinimo;
        private readonly NumericUpDown _numPrecio;
        private readonly ToggleSwitch _switchActivo;
        private readonly Label _lblError;

        public FModalProducto(Producto? producto)
        {
            _productoOriginal = producto;
            Size = new Size(460, 580);
            EstablecerTitulo(producto == null ? "Nuevo producto" : "Editar producto");

            int y = 0;
            const int ancho = 380;

            // Código (VARCHAR 15)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Código", new Point(0, y)));
            _txtCodigo = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtCodigo.MaxLength = 15;
            _txtCodigo.Enabled = producto == null;
            Contenido.Controls.Add(_txtCodigo);
            y += 56;

            // Nombre / Descripción (VARCHAR 20)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre del producto", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtNombre.MaxLength = 20;
            Contenido.Controls.Add(_txtNombre);
            y += 56;

            // Selector Desplegable Categoría (FK INT)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Categoría", new Point(0, y)));
            _cmbCategoria = new ComboBox
            {
                Location = new Point(0, y + 22),
                Width = ancho,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Paleta.FuenteBase,
                FlatStyle = FlatStyle.Flat
            };
            Contenido.Controls.Add(_cmbCategoria);
            CargarCategorias();
            y += 56;

            // Stock Inicial (INT)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Stock inicial", new Point(0, y)));
            _numStock = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 999999);
            _numStock.Enabled = producto == null;
            Contenido.Controls.Add(_numStock);
            y += 56;

            // Stock Mínimo (INT)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Stock mínimo", new Point(0, y)));
            _numStockMinimo = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 999999);
            Contenido.Controls.Add(_numStockMinimo);
            y += 56;

            // Precio (DECIMAL 10,2)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Precio ($)", new Point(0, y)));
            _numPrecio = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 9999999, 2);
            Contenido.Controls.Add(_numPrecio);
            y += 56;

            // Estado (BIT)
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Activo", new Point(0, y)));
            _switchActivo = new ToggleSwitch { Location = new Point(0, y + 24), Checked = true };
            Contenido.Controls.Add(_switchActivo);
            y += 56;

            // Mensaje de Error
            _lblError = new Label
            {
                Location = new Point(0, y),
                Size = new Size(ancho, 36),
                ForeColor = Paleta.Peligro,
                Font = Paleta.FuenteChica,
                Visible = false
            };
            Contenido.Controls.Add(_lblError);
            y += 40;

            // Botones Acciones
            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Guardar", Size = new Size(120, 38), Location = new Point(ancho - 120, y) });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);

            // Cargar datos en edición
            if (producto != null)
            {
                _txtCodigo.Text = producto.Codigo;
                _txtNombre.Text = producto.Nombre;
                _numStock.Value = producto.Stock;
                _numStockMinimo.Value = producto.StockMinimo;
                _numPrecio.Value = producto.Precio;
                _switchActivo.Checked = producto.Activo;

                SeleccionarCategoria(producto.IdCategoria);
            }
        }

        private void CargarCategorias()
        {
            try
            {
                List<Categoria> categorias = _gestionInventario.ObtenerCategorias();
                _cmbCategoria.DataSource = categorias;
                _cmbCategoria.DisplayMember = "Descripcion";
                _cmbCategoria.ValueMember = "IdCategoria";
            }
            catch (Exception ex)
            {
                _lblError.Text = $"Error al obtener categorías: {ex.Message}";
                _lblError.Visible = true;
            }
        }

        private void SeleccionarCategoria(int idCategoria)
        {
            if (_cmbCategoria.DataSource is List<Categoria> categorias)
            {
                var cat = categorias.FirstOrDefault(c => c.IdCategoria == idCategoria);
                if (cat != null)
                {
                    _cmbCategoria.SelectedItem = cat;
                }
            }
        }

        private void Guardar()
        {
            _lblError.Visible = false;

            if (_cmbCategoria.SelectedItem is not Categoria categoriaSeleccionada)
            {
                _lblError.Text = "Debe seleccionar una categoría válida.";
                _lblError.Visible = true;
                return;
            }

            int stock = (int)_numStock.Value;
            int stockMinimo = (int)_numStockMinimo.Value;

            // Validar restricción CHECK: stock_disponible >= stock_min
            if (stock < stockMinimo)
            {
                _lblError.Text = "El stock inicial debe ser mayor o igual al stock mínimo.";
                _lblError.Visible = true;
                return;
            }

            var producto = new Producto
            {
                Codigo = _txtCodigo.Text.Trim(),
                Nombre = _txtNombre.Text.Trim(),
                IdCategoria = categoriaSeleccionada.IdCategoria,
                Stock = stock,
                StockMinimo = stockMinimo,
                Precio = _numPrecio.Value,
                Activo = _switchActivo.Checked
            };

            try
            {
                if (_productoOriginal == null)
                {
                    _gestionInventario.CrearProducto(producto);
                }
                else
                {
                    _gestionInventario.EditarProducto(producto);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
        }
    }
}