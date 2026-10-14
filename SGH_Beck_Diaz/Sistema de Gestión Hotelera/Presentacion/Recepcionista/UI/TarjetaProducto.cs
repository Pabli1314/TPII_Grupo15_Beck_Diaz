using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.UI
{
    /// <summary>Tarjeta de producto para Ventas Adicionales: nombre, precio, stock y botón "Agregar".</summary>
    internal class TarjetaProducto : Panel
    {
        public Producto Producto { get; }
        public event EventHandler? AgregarClick;

        public TarjetaProducto(Producto producto)
        {
            Producto = producto;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(220, 150);
            BackColor = Paleta.FondoTarjeta;
            Padding = new Padding(16);

            var lblNombre = new Label { Text = producto.Nombre, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, AutoSize = false, Size = new Size(188, 40), Location = new Point(16, 14), BackColor = Color.Transparent };
            var lblPrecio = new Label { Text = producto.Precio.ToString("C"), Font = new Font("Segoe UI Semibold", 15f), ForeColor = Paleta.Primario, AutoSize = true, Location = new Point(16, 56), BackColor = Color.Transparent };
            // La base no deja que el stock quede por debajo del mínimo (CH_producto_disponible).
            int vendibles = GestionVentas.UnidadesVendibles(producto);
            var lblStock = new Label
            {
                Text = vendibles == 0 ? $"Sin stock para vender (mínimo {producto.StockMinimo})" : $"Disponibles para venta: {vendibles}",
                Font = Paleta.FuenteChica,
                ForeColor = vendibles == 0 ? Paleta.Peligro : Paleta.TextoTerciario,
                AutoSize = true,
                Location = new Point(16, 84),
                BackColor = Color.Transparent
            };

            var btnAgregar = EstiloBoton.Primario(new Button { Text = "Agregar", Size = new Size(188, 34), Location = new Point(16, 106), Enabled = vendibles > 0 });
            btnAgregar.Click += (s, e) => AgregarClick?.Invoke(this, EventArgs.Empty);

            Controls.Add(lblNombre);
            Controls.Add(lblPrecio);
            Controls.Add(lblStock);
            Controls.Add(btnAgregar);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, 0, Width - 1, Height - 1), 12);
            using var brochaFondo = new SolidBrush(Paleta.FondoTarjeta);
            e.Graphics.FillPath(brochaFondo, ruta);
            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawPath(lapiz, ruta);
        }
    }
}
