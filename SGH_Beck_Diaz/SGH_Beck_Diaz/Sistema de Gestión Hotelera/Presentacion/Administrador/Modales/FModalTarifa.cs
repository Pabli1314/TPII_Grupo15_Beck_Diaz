using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    internal class FModalTarifa : FormModalBase
    {
        private readonly GestionTarifas _gestionTarifas = new();
        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly Tarifa? _tarifaOriginal;

        private readonly ComboBox _cmbTipo;
        private readonly NumericUpDown _numHora;
        private readonly NumericUpDown _numFraccion;
        private readonly NumericUpDown _numAdicional;
        private readonly ToggleSwitch _switchEstado;
        private readonly Label _lblError;

        public FModalTarifa(Tarifa? tarifa)
        {
            _tarifaOriginal = tarifa;
            Size = new Size(460, 460);
            EstablecerTitulo(tarifa == null ? "Nueva tarifa" : "Editar tarifa");

            int y = 0;
            const int ancho = 380;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Tipo de habitación", new Point(0, y)));
            _cmbTipo = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            foreach (TipoHabitacion tipo in _gestionHabitaciones.ObtenerTiposHabitacion())
            {
                _cmbTipo.Items.Add(tipo);
            }
            _cmbTipo.DisplayMember = nameof(TipoHabitacion.Descripcion);
            Contenido.Controls.Add(_cmbTipo);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Precio por dia ($)", new Point(0, y)));
            _numHora = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 9_999_999, 2);
            Contenido.Controls.Add(_numHora);
            y += 62;


            Contenido.Controls.Add(CamposFormulario.Etiqueta("Precio adicional ($)", new Point(0, y)));
            _numAdicional = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 9_999_999, 2);
            Contenido.Controls.Add(_numAdicional);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Estado", new Point(0, y)));
            _switchEstado = new ToggleSwitch { Location = new Point(0, y + 24), Checked = true };
            Contenido.Controls.Add(_switchEstado);
            y += 56;

            _lblError = new Label { Location = new Point(0, y), Size = new Size(ancho, 20), ForeColor = Paleta.Peligro, Font = Paleta.FuenteChica, Visible = false };
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Guardar", Size = new Size(120, 38), Location = new Point(ancho - 120, y) });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);

            CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            if (_tarifaOriginal == null)
            {
                if (_cmbTipo.Items.Count > 0) _cmbTipo.SelectedIndex = 0;
                return;
            }

            foreach (var item in _cmbTipo.Items)
            {
                if (item is TipoHabitacion tipo && tipo.IdTipoHabitacion == _tarifaOriginal.IdTipoHabitacion)
                {
                    _cmbTipo.SelectedItem = tipo;
                    break;
                }
            }

            _numHora.Value = _tarifaOriginal.PrecioPorHora;
            _numAdicional.Value = _tarifaOriginal.PrecioAdicional;
            _switchEstado.Checked = _tarifaOriginal.Activa;
        }

        private void Guardar()
        {
            if (_cmbTipo.SelectedItem is not TipoHabitacion tipo)
            {
                _lblError.Text = "Debe seleccionar un tipo de habitación.";
                _lblError.Visible = true;
                return;
            }

            var tarifa = new Tarifa
            {
                IdTarifa = _tarifaOriginal?.IdTarifa ?? 0,
                IdTipoHabitacion = tipo.IdTipoHabitacion,
                TipoHabitacionNombre = tipo.Descripcion,
                PrecioPorHora = _numHora.Value,
                PrecioAdicional = _numAdicional.Value,
                Activa = _switchEstado.Checked
            };

            try
            {
                if (_tarifaOriginal == null)
                {
                    _gestionTarifas.CrearTarifa(tarifa);
                }
                else
                {
                    _gestionTarifas.EditarTarifa(tarifa);
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
