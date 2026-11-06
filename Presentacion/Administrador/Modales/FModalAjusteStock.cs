using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalAjusteStock : FormModalBase
    {
        private readonly GestionInventario _gestionInventario = new();
        private readonly Producto _producto;
        private readonly Label _lblStockActual;
        private readonly RadioButton _radioIngreso;
        private readonly RadioButton _radioEgreso;
        private readonly NumericUpDown _numCantidad;
        private readonly Label _lblError;

        public FModalAjusteStock(Producto producto)
        {
            _producto = producto;
            Size = new Size(420, 380);
            EstablecerTitulo($"Ajustar stock — {producto.Nombre}");

            const int ancho = 340;

            _lblStockActual = new Label { Text = $"Stock actual: {producto.Stock} unidades", Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            Contenido.Controls.Add(_lblStockActual);

            _radioIngreso = new RadioButton { Text = "Ingreso de mercadería", Location = new Point(0, 40), AutoSize = true, Font = Paleta.FuenteBase, Checked = true };
            _radioEgreso = new RadioButton { Text = "Egreso / consumo", Location = new Point(0, 68), AutoSize = true, Font = Paleta.FuenteBase };
            Contenido.Controls.Add(_radioIngreso);
            Contenido.Controls.Add(_radioEgreso);

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Cantidad", new Point(0, 106)));
            _numCantidad = CamposFormulario.Numerico(new Point(0, 128), ancho, 1, 999999);
            _numCantidad.Value = 1;
            Contenido.Controls.Add(_numCantidad);

            _lblError = new Label { Location = new Point(0, 172), Size = new Size(ancho, 20), ForeColor = Paleta.Peligro, Font = Paleta.FuenteChica, Visible = false };
            Contenido.Controls.Add(_lblError);

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, 210) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button { Text = "Confirmar", Size = new Size(120, 38), Location = new Point(ancho - 120, 210) });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);
        }

        private void Confirmar()
        {
            int cantidad = (int)_numCantidad.Value * (_radioEgreso.Checked ? -1 : 1);

            try
            {
                _gestionInventario.AjustarStock(_producto.Codigo, cantidad);
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
