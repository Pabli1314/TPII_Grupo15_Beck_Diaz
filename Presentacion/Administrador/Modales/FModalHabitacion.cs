using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    /// <summary>Alta de una habitación nueva o edición de una existente (el número no se edita).</summary>
    internal class FModalHabitacion : FormModalBase
    {
        private readonly GestionHabitaciones _gestionHabitaciones = new();
        private readonly bool _esEdicion;

        private readonly NumericUpDown _numNumero;
        private readonly ComboBox _cmbPiso;
        private readonly NumericUpDown _numCamas;
        private readonly ComboBox _cmbTipo;
        private readonly ComboBox _cmbEstado;
        private readonly Label _lblError;

        public FModalHabitacion(Habitacion? habitacionAEditar = null)
        {
            _esEdicion = habitacionAEditar != null;

            Size = new Size(460, 500);
            EstablecerTitulo(_esEdicion ? $"Editar habitación {habitacionAEditar!.NroHabitacion}" : "Nueva habitación");

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

            if (_esEdicion)
            {
                CargarDatos(habitacionAEditar!);
            }
        }

        private void CargarDatos(Habitacion habitacion)
        {
            // El número es la clave de la habitación: en edición queda fijo.
            _numNumero.Value = Math.Min(_numNumero.Maximum, Math.Max(_numNumero.Minimum, habitacion.NroHabitacion));
            _numNumero.Enabled = false;

            int indicePiso = _cmbPiso.Items.IndexOf(habitacion.Piso);
            if (indicePiso >= 0) _cmbPiso.SelectedIndex = indicePiso;

            _numCamas.Value = Math.Min(_numCamas.Maximum, Math.Max(_numCamas.Minimum, habitacion.CantCamas));

            for (int i = 0; i < _cmbTipo.Items.Count; i++)
            {
                if (_cmbTipo.Items[i] is TipoHabitacion tipo && tipo.IdTipoHabitacion == habitacion.IdTipoHabitacion)
                {
                    _cmbTipo.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < _cmbEstado.Items.Count; i++)
            {
                if (_cmbEstado.Items[i] is EstadoHabitacion estado && estado.IdEstado == habitacion.IdEstado)
                {
                    _cmbEstado.SelectedIndex = i;
                    break;
                }
            }
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
                IdTipoHabitacion = tipo.IdTipoHabitacion,
                IdEstado = estado.IdEstado
            };

            try
            {
                if (_esEdicion)
                {
                    _gestionHabitaciones.ModificarHabitacion(habitacion);
                }
                else
                {
                    _gestionHabitaciones.CrearHabitacion(habitacion);
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
