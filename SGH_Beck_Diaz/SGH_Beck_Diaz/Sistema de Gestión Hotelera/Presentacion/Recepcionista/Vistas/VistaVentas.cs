using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Venta de productos adicionales (bebidas, snacks, varios) con carrito y confirmación.</summary>
    internal class VistaVentas : UserControl
    {
        private readonly Usuario _usuario;
        private readonly GestionVentas _gestionVentas = new();
        private readonly List<(Producto Producto, int Cantidad)> _carrito = new();

        private readonly FlowLayoutPanel _panelProductos;
        private readonly DataGridView _grillaCarrito;
        private readonly Label _lblTotal;
        private readonly ComboBox _cmbHuesped;
        private readonly ComboBox _cmbMetodoPago;
        private readonly Button _btnConfirmar;

        // Sección "Ventas realizadas": quién consumió qué.
        private const int AnchoSeccionVentas = 708; // mismo ancho que las 3 columnas de productos
        private readonly Panel _panelVentasRealizadas;
        private readonly TextBox _txtFiltroVentas;
        private readonly Label _lblResumenVentas;
        private readonly DataGridView _grillaVentas;
        private List<VentaRealizada> _ventasMostradas = new();

        /// <summary>Ítem del selector de huésped (solo huéspedes alojados).</summary>
        private sealed class OpcionHuesped
        {
            public string DniHuesped { get; init; } = string.Empty;
            public string Texto { get; init; } = string.Empty;
            public override string ToString() => Texto;
        }

        public VistaVentas(Usuario usuario)
        {
            _usuario = usuario;

            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var panelIzquierdo = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.FondoApp, AutoScroll = true };
            var lblTitulo = new Label { Text = "Productos disponibles", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            const int columnasProductos = 3;
            const int anchoTarjetaProducto = 220 + 16; // TarjetaProducto.Width + su margen derecho
            _panelProductos = new FlowLayoutPanel
            {
                Location = new Point(0, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MaximumSize = new Size(anchoTarjetaProducto * columnasProductos, 0)
            };
            panelIzquierdo.Controls.Add(lblTitulo);
            panelIzquierdo.Controls.Add(_panelProductos);

            // --- Ventas realizadas (debajo de los productos; se reubica en Refrescar según su alto) ---
            _panelVentasRealizadas = new Panel { Location = new Point(0, 36), Size = new Size(AnchoSeccionVentas, 460), BackColor = Paleta.FondoApp };

            var lblVentas = new Label { Text = "Ventas realizadas", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            _lblResumenVentas = new Label { Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 28) };

            _txtFiltroVentas = CamposFormulario.Texto(new Point(0, 52), 300);
            _txtFiltroVentas.PlaceholderText = "Buscar por huésped, DNI o habitación";
            _txtFiltroVentas.TextChanged += (s, e) => RefrescarVentasRealizadas();

            _grillaVentas = new DataGridView
            {
                Location = new Point(0, 92),
                Size = new Size(AnchoSeccionVentas, 360),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grillaVentas);
            _grillaVentas.Columns.Add("nro", "N°");
            _grillaVentas.Columns.Add("fecha", "Fecha y hora");
            _grillaVentas.Columns.Add("huesped", "Huésped");
            _grillaVentas.Columns.Add("habitacion", "Hab.");
            _grillaVentas.Columns.Add("productos", "Productos consumidos");
            _grillaVentas.Columns.Add("metodo", "Pago");
            _grillaVentas.Columns.Add("total", "Total");
            _grillaVentas.Columns["nro"].FillWeight = 35;
            _grillaVentas.Columns["fecha"].FillWeight = 80;
            _grillaVentas.Columns["huesped"].FillWeight = 120;
            _grillaVentas.Columns["habitacion"].FillWeight = 40;
            _grillaVentas.Columns["productos"].FillWeight = 150;
            _grillaVentas.Columns["metodo"].FillWeight = 70;
            _grillaVentas.Columns["total"].FillWeight = 65;
            _grillaVentas.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grillaVentas.Columns["habitacion"].MinimumWidth = 50;
            // Huésped (nombre + DNI) y productos pueden ser largos: se muestran en dos líneas en vez de cortarse.
            // (En el estilo de la columna: el DefaultCellStyle de la grilla se reinicia al agregarla al formulario.)
            foreach (string columna in new[] { "fecha", "huesped", "productos" })
            {
                _grillaVentas.Columns[columna].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            _grillaVentas.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grillaVentas.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _grillaVentas.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) MostrarDetalleVenta(_ventasMostradas[e.RowIndex]); };

            var lblAyuda = new Label { Text = "Doble clic en una venta para ver el detalle de lo consumido.", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(0, 456) };

            _panelVentasRealizadas.Controls.Add(lblVentas);
            _panelVentasRealizadas.Controls.Add(_lblResumenVentas);
            _panelVentasRealizadas.Controls.Add(_txtFiltroVentas);
            _panelVentasRealizadas.Controls.Add(_grillaVentas);
            _panelVentasRealizadas.Controls.Add(lblAyuda);
            _panelVentasRealizadas.Height = 480;
            panelIzquierdo.Controls.Add(_panelVentasRealizadas);
            _panelProductos.SizeChanged += (s, e) => _panelVentasRealizadas.Top = _panelProductos.Bottom + 24;
            // La tabla de ventas usa todo el ancho libre a la izquierda del carrito (mínimo el de los productos).
            panelIzquierdo.Resize += (s, e) => _panelVentasRealizadas.Width = Math.Max(AnchoSeccionVentas, panelIzquierdo.ClientSize.Width - 16);

            var panelCarrito = new Panel { Dock = DockStyle.Right, Width = 420, BackColor = Paleta.FondoTarjeta, Padding = new Padding(20) };
            var lblCarrito = new Label { Text = "Carrito de venta", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };

            _grillaCarrito = new DataGridView
            {
                Location = new Point(0, 36),
                Size = new Size(380, 380),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_grillaCarrito);
            _grillaCarrito.ReadOnly = false;
            _grillaCarrito.Columns.Add("producto", "Producto");
            _grillaCarrito.Columns.Add("precio", "Precio");
            _grillaCarrito.Columns.Add("cantidad", "Cantidad");
            _grillaCarrito.Columns.Add("subtotal", "Subtotal");
            var colQuitar = new DataGridViewButtonColumn { Name = "quitar", HeaderText = string.Empty, Text = "Quitar", UseColumnTextForButtonValue = true, Width = 70 };
            _grillaCarrito.Columns.Add(colQuitar);
            _grillaCarrito.Columns["producto"].ReadOnly = true;
            _grillaCarrito.Columns["precio"].ReadOnly = true;
            _grillaCarrito.Columns["subtotal"].ReadOnly = true;
            _grillaCarrito.CellClick += GrillaCarrito_CellClick;
            _grillaCarrito.CellEndEdit += GrillaCarrito_CellEndEdit;

            _lblTotal = new Label { Text = "TOTAL: $0", Font = new Font("Segoe UI Semibold", 16f), ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 430) };

            // La venta se asigna al huésped que compra (venta.dni_huesped); solo se listan huéspedes
            // alojados en habitaciones Ocupadas y es obligatorio elegir uno para confirmar la venta.
            var lblHuesped = CamposFormulario.Etiqueta("Huésped que realiza la compra", new Point(0, 470));
            _cmbHuesped = CamposFormulario.Combo(new Point(0, 492), 380);
            // Al desplegar se vuelve a consultar quién tiene check-in activo, para no mostrar una lista vieja.
            _cmbHuesped.DropDown += (s, e) => RefrescarHuespedes();

            var lblMetodo = CamposFormulario.Etiqueta("Método de pago", new Point(0, 524));
            _cmbMetodoPago = CamposFormulario.Combo(new Point(0, 546), 380);
            _cmbMetodoPago.DisplayMember = nameof(MetodoPago.NomMetodoPago);
            _cmbMetodoPago.ValueMember = nameof(MetodoPago.IdMetodo);
            _cmbMetodoPago.DataSource = _gestionVentas.ObtenerMetodosPago();

            _btnConfirmar = EstiloBoton.Primario(new Button { Text = "Confirmar venta", Size = new Size(380, 44), Location = new Point(0, 584) });
            _btnConfirmar.Click += (s, e) => ConfirmarVenta();

            panelCarrito.Controls.Add(lblCarrito);
            panelCarrito.Controls.Add(_grillaCarrito);
            panelCarrito.Controls.Add(_lblTotal);
            panelCarrito.Controls.Add(lblHuesped);
            panelCarrito.Controls.Add(_cmbHuesped);
            panelCarrito.Controls.Add(lblMetodo);
            panelCarrito.Controls.Add(_cmbMetodoPago);
            panelCarrito.Controls.Add(_btnConfirmar);

            Controls.Add(panelIzquierdo);
            Controls.Add(panelCarrito);

            Refrescar();
        }

        public void Refrescar()
        {
            _panelProductos.Controls.Clear();

            foreach (Producto producto in _gestionVentas.ObtenerProductosDisponibles())
            {
                var tarjeta = new TarjetaProducto(producto) { Margin = new Padding(0, 0, 16, 16) };
                tarjeta.AgregarClick += (s, e) => AgregarAlCarrito(producto);
                _panelProductos.Controls.Add(tarjeta);
            }

            RefrescarHuespedes();
            RedibujarCarrito();

            // La sección de ventas va debajo de las tarjetas de productos, que cambian de alto.
            _panelVentasRealizadas.Top = _panelProductos.Bottom + 24;
            RefrescarVentasRealizadas();
        }

        private void RefrescarVentasRealizadas()
        {
            try
            {
                _ventasMostradas = _gestionVentas.ObtenerVentasRealizadas(_txtFiltroVentas.Text);
            }
            catch (Exception ex)
            {
                _ventasMostradas = new List<VentaRealizada>();
                _lblResumenVentas.Text = $"No se pudieron cargar las ventas: {ex.Message}";
                _grillaVentas.Rows.Clear();
                return;
            }

            _grillaVentas.Rows.Clear();
            foreach (VentaRealizada venta in _ventasMostradas)
            {
                string huesped = venta.EsDeMostrador
                    ? "Selecionar huesped"
                    : $"{venta.NombreHuesped ?? "-"}\nDNI {venta.DniHuesped}";

                int fila = _grillaVentas.Rows.Add(
                    venta.IdVenta,
                    $"{venta.Momento:dd/MM/yyyy}\n{venta.Momento:HH:mm}",
                    huesped,
                    venta.NroHabitacion?.ToString() ?? "-",
                    venta.ResumenProductos,
                    venta.MetodoPago,
                    venta.Total.ToString("C"));

                if (venta.EsDeMostrador)
                {
                    _grillaVentas.Rows[fila].Cells["huesped"].Style.ForeColor = Paleta.TextoTerciario;
                }
            }

            int huespedesDistintos = _ventasMostradas.Where(v => !v.EsDeMostrador).Select(v => v.DniHuesped).Distinct().Count();
            _lblResumenVentas.Text = $"{_ventasMostradas.Count} venta(s) · {huespedesDistintos} huésped(es) · Total {_ventasMostradas.Sum(v => v.Total):C}";
        }

        private void MostrarDetalleVenta(VentaRealizada venta)
        {
            string quien = venta.EsDeMostrador
                ? "Sin huésped asignado"
                : $"Huésped: {venta.NombreHuesped} (DNI {venta.DniHuesped})" + (venta.NroHabitacion.HasValue ? $"\nHabitación: {venta.NroHabitacion}" : string.Empty);

            string items = string.Join("\n", venta.Items.Select(i => $"  • {i.Cantidad} x {i.Producto} ({i.PrecioUnitario:C} c/u) = {i.Subtotal:C}"));

            MessageBox.Show(
                $"{quien}\nFecha: {venta.Momento:dd/MM/yyyy HH:mm}\nMétodo de pago: {venta.MetodoPago}\n\nConsumido:\n{items}\n\nTotal: {venta.Total:C}",
                $"Venta #{venta.IdVenta}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Recarga los huéspedes alojados conservando la selección actual si sigue alojado.</summary>
        private void RefrescarHuespedes()
        {
            string? seleccionActual = (_cmbHuesped.SelectedItem as OpcionHuesped)?.DniHuesped;

            _cmbHuesped.Items.Clear();
            foreach (HabitacionResumen habitacion in _gestionVentas.ObtenerHuespedesAlojados())
            {
                _cmbHuesped.Items.Add(new OpcionHuesped
                {
                    DniHuesped = habitacion.DniHuesped,
                    Texto = $"Hab. {habitacion.NroHabitacion} - {habitacion.Huesped} (DNI {habitacion.DniHuesped})"
                });
            }

            // Sin huéspedes alojados el selector queda vacío y no se puede confirmar la venta.
            int indice = _cmbHuesped.Items.Count > 0 ? 0 : -1;
            for (int i = 0; i < _cmbHuesped.Items.Count; i++)
            {
                if ((_cmbHuesped.Items[i] as OpcionHuesped)?.DniHuesped == seleccionActual)
                {
                    indice = i;
                    break;
                }
            }

            _cmbHuesped.SelectedIndex = indice;
        }

        private void AgregarAlCarrito(Producto producto)
        {
            int indice = _carrito.FindIndex(item => item.Producto.Codigo == producto.Codigo);
            int enCarrito = indice >= 0 ? _carrito[indice].Cantidad : 0;
            if (!HayUnidadesVendibles(producto, enCarrito + 1))
            {
                return;
            }

            if (indice >= 0)
            {
                var actual = _carrito[indice];
                _carrito[indice] = (actual.Producto, actual.Cantidad + 1);
            }
            else
            {
                _carrito.Add((producto, 1));
            }

            RedibujarCarrito();
        }

        private void GrillaCarrito_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _grillaCarrito.Columns[e.ColumnIndex].Name != "quitar")
            {
                return;
            }

            _carrito.RemoveAt(e.RowIndex);
            RedibujarCarrito();
        }

        private void GrillaCarrito_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (_grillaCarrito.Columns[e.ColumnIndex].Name != "cantidad")
            {
                return;
            }

            string? texto = _grillaCarrito.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            var actual = _carrito[e.RowIndex];
            if (!int.TryParse(texto, out int cantidad) || cantidad <= 0 || !HayUnidadesVendibles(actual.Producto, cantidad))
            {
                RedibujarCarrito();
                return;
            }

            _carrito[e.RowIndex] = (actual.Producto, cantidad);
            RedibujarCarrito();
        }

        /// <summary>Avisa y devuelve false si la cantidad pedida dejaría el stock por debajo del mínimo.</summary>
        private bool HayUnidadesVendibles(Producto producto, int cantidad)
        {
            int vendibles = GestionVentas.UnidadesVendibles(producto);
            if (cantidad <= vendibles)
            {
                return true;
            }

            MessageBox.Show(
                $"Solo se pueden vender {vendibles} unidad(es) de \"{producto.Nombre}\" (stock {producto.Stock}, mínimo {producto.StockMinimo}).",
                "Stock insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void RedibujarCarrito()
        {
            _grillaCarrito.Rows.Clear();

            foreach (var (producto, cantidad) in _carrito)
            {
                decimal subtotal = producto.Precio * cantidad;
                _grillaCarrito.Rows.Add(producto.Nombre, producto.Precio.ToString("C"), cantidad, subtotal.ToString("C"), "Quitar");
            }

            decimal total = _carrito.Sum(item => item.Producto.Precio * item.Cantidad);
            _lblTotal.Text = $"TOTAL: {total:C}";
            _btnConfirmar.Enabled = _carrito.Count > 0;
        }

        private void ConfirmarVenta()
        {
            if (_carrito.Count == 0 || _cmbMetodoPago.SelectedValue == null)
            {
                return;
            }

            if (_cmbHuesped.SelectedItem is not OpcionHuesped huesped)
            {
                MessageBox.Show(
                    _cmbHuesped.Items.Count == 0
                        ? "No hay huéspedes alojados. La venta tiene que asignarse a un huésped que esté en una habitación ocupada."
                        : "Seleccione el huésped que realiza la compra.",
                    "Falta el huésped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cmbHuesped.Focus();
                return;
            }

            try
            {
                int idMetodo = (int)_cmbMetodoPago.SelectedValue;
                Venta venta = _gestionVentas.RegistrarVenta(_carrito, idMetodo, _usuario.DniUsuario, huesped.DniHuesped);

                MessageBox.Show($"Venta #{venta.IdVenta} por {venta.Total:C} registrada correctamente y asignada a {huesped.Texto}.", "Venta confirmada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _carrito.Clear();
                Refrescar();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                MessageBox.Show(ex.Message, "No se pudo registrar la venta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al registrar la venta.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
