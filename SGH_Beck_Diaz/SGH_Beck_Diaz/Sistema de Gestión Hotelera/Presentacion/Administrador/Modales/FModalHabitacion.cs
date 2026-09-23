using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    /// <summary>Alta de una habitación nueva. Por ahora solo se usa para crear (no editar).</summary>
    internal class FModalHabitacion : FormModalBase
    {
        private readonly GestionHabitaciones _gestionHabitaciones = new();

        private readonly NumericUpDown _numNumero;
        private readonly ComboBox _cmbPiso;
        private readonly NumericUpDown _numCamas;
        private readonly NumericUpDown _numTarifa;
        private readonly ComboBox _cmbTipo;
        private readonly ComboBox _cmbEstado;
        private readonly Label _lblError;

        public FModalHabitacion()
        {
            Size = new Size(460, 560);
            EstablecerTitulo("Nueva habitación");

            int y = 0;
            const int ancho = 380;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Número de habitación", new Point(0, y)));
            _numNumero = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 1, 9999, 0);
            Contenido.Controls.Add(_numNumero);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Piso", new Point(0, y)));
            _cmbPiso = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            _cmbPiso.Items.AddRange(new object[] { 1, 2, 3 });
            _cmbPiso.SelectedIndex = 0;
            Contenido.Controls.Add(_cmbPiso);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Cantidad de camas", new Point(0, y)));
            _numCamas = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 1, 10, 0);
            _numCamas.Value = 1;
            Contenido.Controls.Add(_numCamas);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Tarifa base ($)", new Point(0, y)));
            _numTarifa = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 9_999_999, 2);
            Contenido.Controls.Add(_numTarifa);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Tipo de habitación", new Point(0, y)));
            _cmbTipo = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            foreach (TipoHabitacion tipo in _gestionHabitaciones.ObtenerTiposHabitacion())
            {
                _cmbTipo.Items.Add(tipo);
            }
            _cmbTipo.DisplayMember = nameof(TipoHabitacion.Descripcion);
            if (_cmbTipo.Items.Count > 0) _cmbTipo.SelectedIndex = 0;
            Contenido.Controls.Add(_cmbTipo);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Estado", new Point(0, y)));
            _cmbEstado = CamposFormulario.Combo(new Point(0, y + 22), ancho);
            var estados = _gestionHabitaciones.ObtenerEstados();
            foreach (EstadoHabitacion estado in estados)
            {
                _cmbEstado.Items.Add(estado);
            }
            _cmbEstado.DisplayMember = nameof(EstadoHabitacion.NomEstadoHabitacion);
            int indiceDisponible = estados.FindIndex(e => e.NomEstadoHabitacion == "Disponible");
            _cmbEstado.SelectedIndex = indiceDisponible >= 0 ? indiceDisponible : (_cmbEstado.Items.Count > 0 ? 0 : -1);
            Contenido.Controls.Add(_cmbEstado);
            y += 62;

            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Guardar", Size = new Size(120, 38), Location = new Point(ancho - 120, y) });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);
        }

        private void Guardar()
        {
            if (_cmbTipo.SelectedItem is not TipoHabitacion tipo)
            {
                CamposFormulario.MarcarInvalido(_cmbTipo, _lblError, "Debe seleccionar un tipo de habitación.");
                return;
            }

            if (_cmbEstado.SelectedItem is not EstadoHabitacion estado)
            {
                CamposFormulario.MarcarInvalido(_cmbEstado, _lblError, "Debe seleccionar un estado.");
                return;
            }

            var habitacion = new Habitacion
            {
                NroHabitacion = (int)_numNumero.Value,
                Piso = (int)(_cmbPiso.SelectedItem ?? 1),
                CantCamas = (int)_numCamas.Value,
                TarifaBase = _numTarifa.Value,
                IdTipoHabitacion = tipo.IdTipoHabitacion,
                IdEstado = estado.IdEstado
            };

            try
            {
                _gestionHabitaciones.CrearHabitacion(habitacion);
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
