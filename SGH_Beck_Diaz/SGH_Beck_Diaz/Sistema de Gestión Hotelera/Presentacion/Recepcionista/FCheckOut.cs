using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>Check-out de una habitación ocupada: precarga los datos del huésped vigente (DNI +
    /// contacto), permite corregirlos y recién entonces cierra el hospedaje contra la base de datos
    /// (GestionHospedajes.RegistrarCheckOut) y pasa la habitación a Limpieza.</summary>
    internal class FCheckOut : FormModalBase
    {
        private readonly int _nroHabitacion;
        private readonly GestionHospedajes _gestionHospedajes = new GestionHospedajes();

        private readonly Label _lblIngresoValor;
        private readonly Label _lblSalidaValor;
        private readonly DataGridView _grillaConsumos;
        private readonly Label _lblTotalConsumos;
        private readonly TextBox _txtDni;
        private readonly TextBox _txtNombre;
        private readonly TextBox _txtApellido;
        private readonly TextBox _txtTelefono;
        private readonly TextBox _txtDireccion;
        private readonly Label _lblError;

        // El check-out no edita el correo, pero HuespedDAO.Actualizar lo pisa: se conserva el cargado.
        private string _correoHuesped = string.Empty;

        public string NombreCompleto { get; private set; } = string.Empty;

        public FCheckOut(string nroHabitacion)
        {
            _nroHabitacion = int.Parse(nroHabitacion);

            Size = new Size(520, 806);
            EstablecerTitulo($"Check-out — Habitación {nroHabitacion}");

            const int ancho = 440;
            int y = 0;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Fecha/hora de ingreso", new Point(0, y)));
            _lblIngresoValor = new Label { Location = new Point(0, y + 22), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario };
            Contenido.Controls.Add(_lblIngresoValor);
            y += 46;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Salida estimada", new Point(0, y)));
            _lblSalidaValor = new Label { Location = new Point(0, y + 22), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario };
            Contenido.Controls.Add(_lblSalidaValor);
            y += 46;

            // Consumos del huésped durante la estadía (ventas adicionales asignadas a su DNI desde el check-in).
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Consumos durante la estadía", new Point(0, y)));
            _grillaConsumos = new DataGridView { Location = new Point(0, y + 24), Size = new Size(ancho, 140), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            EstiloGrid.Aplicar(_grillaConsumos);
            _grillaConsumos.RowTemplate.Height = 30;
            _grillaConsumos.ColumnHeadersHeight = 32;
            _grillaConsumos.Columns.Add("fecha", "Fecha");
            _grillaConsumos.Columns.Add("productos", "Productos");
            _grillaConsumos.Columns.Add("pago", "Pago");
            _grillaConsumos.Columns.Add("total", "Total");
            _grillaConsumos.Columns["fecha"].FillWeight = 90;
            _grillaConsumos.Columns["productos"].FillWeight = 150;
            _grillaConsumos.Columns["pago"].FillWeight = 70;
            _grillaConsumos.Columns["total"].FillWeight = 70;
            _grillaConsumos.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grillaConsumos.Columns["productos"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _grillaConsumos.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            Contenido.Controls.Add(_grillaConsumos);

            _lblTotalConsumos = new Label { Location = new Point(0, y + 170), AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, Text = "Sin consumos registrados." };
            Contenido.Controls.Add(_lblTotalConsumos);
            y += 200;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(0, y)));
            _txtDni = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            _txtDni.ReadOnly = true;
            _txtDni.TabStop = false;
            _txtDni.BackColor = Paleta.FondoApp;
            Contenido.Controls.Add(_txtDni);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(0, y)));
            _txtNombre = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtNombre);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(0, y)));
            _txtApellido = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtApellido);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(0, y)));
            _txtTelefono = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(0, y + 22), ancho), Huesped.LargoTelefono);
            Contenido.Controls.Add(_txtTelefono);
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(0, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(0, y + 22), ancho);
            Contenido.Controls.Add(_txtDireccion);
            y += 62;

            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 40), Location = new Point(ancho - 120 - 200 - 12, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnConfirmar = EstiloBoton.Primario(new Button { Text = "Confirmar check-out", Size = new Size(200, 40), Location = new Point(ancho - 200, y) });
            btnConfirmar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnConfirmar);

            Load += (s, e) => CargarDatos();
        }

        private void CargarDatos()
        {
            var activo = _gestionHospedajes.ObtenerHuespedActivoPorHabitacion(_nroHabitacion);
            if (activo == null)
            {
                _lblError.Text = "No se encontró el hospedaje vigente de esta habitación.";
                _lblError.Visible = true;
                return;
            }

            (Huesped huesped, Hospedaje hospedaje) = activo.Value;

            _txtDni.Text = huesped.DniHuesped;
            _txtNombre.Text = huesped.Nombre;
            _txtApellido.Text = huesped.Apellido;
            _txtTelefono.Text = huesped.Telefono;
            _txtDireccion.Text = huesped.Direccion;
            _correoHuesped = huesped.Correo;
            _lblIngresoValor.Text = (hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada).ToString("dd/MM/yyyy HH:mm");
            _lblSalidaValor.Text = (hospedaje.FechaSalida.Date + hospedaje.HoraSalida).ToString("dd/MM/yyyy HH:mm");

            CargarConsumos(hospedaje);
        }

        private void CargarConsumos(Hospedaje hospedaje)
        {
            _grillaConsumos.Rows.Clear();

            List<VentaRealizada> consumos;
            try
            {
                consumos = _gestionHospedajes.ObtenerConsumosDeEstadia(hospedaje);
            }
            catch (Exception ex)
            {
                _lblTotalConsumos.Text = $"No se pudieron cargar los consumos: {ex.Message}";
                _lblTotalConsumos.ForeColor = Paleta.Peligro;
                return;
            }

            foreach (VentaRealizada venta in consumos)
            {
                _grillaConsumos.Rows.Add(venta.Momento.ToString("dd/MM HH:mm"), venta.ResumenProductos, venta.MetodoPago, venta.Total.ToString("C"));
            }

            _lblTotalConsumos.Text = consumos.Count == 0
                ? "Sin consumos registrados durante la estadía."
                : $"Total consumido: {consumos.Sum(v => v.Total):C} en {consumos.Count} venta(s)";
        }

        private void Confirmar()
        {
            if (!ValidarCampos(out string mensaje, out Control controlInvalido))
            {
                _lblError.Text = mensaje;
                _lblError.Visible = true;
                controlInvalido.Focus();
                return;
            }

            var huesped = new Huesped
            {
                DniHuesped = _txtDni.Text.Trim(),
                Nombre = _txtNombre.Text.Trim(),
                Apellido = _txtApellido.Text.Trim(),
                Telefono = _txtTelefono.Text.Trim(),
                Direccion = _txtDireccion.Text.Trim(),
                Correo = _correoHuesped
            };

            try
            {
                _gestionHospedajes.ActualizarHuesped(huesped);
                _gestionHospedajes.RegistrarCheckOut(_nroHabitacion);

                NombreCompleto = $"{huesped.Nombre} {huesped.Apellido}";

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                _lblError.Text = ex.Message;
                _lblError.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al registrar el check-out.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarCampos(out string mensaje, out Control controlInvalido)
        {
            (string valor, string etiqueta, Control control)[] requeridos =
            {
                (_txtNombre.Text, "el nombre", _txtNombre),
                (_txtApellido.Text, "el apellido", _txtApellido),
                (_txtTelefono.Text, "el teléfono", _txtTelefono),
                (_txtDireccion.Text, "la dirección", _txtDireccion),
            };

            foreach (var (valor, etiqueta, control) in requeridos)
            {
                if (string.IsNullOrWhiteSpace(valor))
                {
                    mensaje = $"Debe ingresar {etiqueta}.";
                    controlInvalido = control;
                    return false;
                }
            }

            mensaje = string.Empty;
            controlInvalido = _txtNombre;
            return true;
        }
    }
}
