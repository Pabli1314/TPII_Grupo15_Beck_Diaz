using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Modales
{
    internal class FModalRegistrarHuesped : FormModalBase
    {
        private readonly GestionHuespedes _gestionHuespedes = new();
        private readonly TextBox _txtDni;
        private readonly TextBox _txtNombre;
        private readonly TextBox _txtApellido;
        private readonly TextBox _txtTelefono;
        private readonly TextBox _txtDireccion;
        private readonly TextBox _txtCorreo;
        private readonly Label _lblError;

        public FModalRegistrarHuesped()
        {
            EstablecerTitulo("Registrar huésped");

            // Ancho de los campos de texto: alcanza para ver el valor completo mientras se escribe
            // (DNI, nombre, apellido, teléfono, dirección) sin que el modal quede más ancho que la
            // pantalla en resoluciones chicas o con escalado de DPI alto.
            const int ancho = 260;
            // Define un margen izquierdo para que los controles no queden pegados al borde
            const int margenX = 24;
            // Ancho útil del panel de contenido (ancho del modal menos el padding del marco). Se
            // calcula a partir de "ancho" y "margenX" para que quede en sincro con el ancho real de
            // los campos: antes estaba hardcodeado en base a un modal de 240px que ya no existía,
            // por eso el botón "Registrar" quedaba mal ubicado.
            const int anchoContenido = margenX + ancho + margenX;

            // Alto de la etiqueta (fuente en negrita) + separación mínima antes del campo de texto,
            // así el campo queda justo debajo sin superponerse
            const int separacionEtiquetaCampo = 10;
            const int altoCampo = 30;
            // Espacio entre el campo de texto y la etiqueta de la siguiente fila
            const int separacionFilas = 3;
            const int altoFila = separacionEtiquetaCampo + altoCampo + separacionFilas;

            int y = 15;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("DNI", new Point(margenX, y)));
            _txtDni = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho), Huesped.LargoDni);
            Contenido.Controls.Add(_txtDni);
            y += altoFila;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Nombre", new Point(margenX, y)));
            _txtNombre = CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho);
            _txtNombre.MaxLength = Huesped.LargoNombre;
            Contenido.Controls.Add(_txtNombre);
            y += altoFila;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Apellido", new Point(margenX, y)));
            _txtApellido = CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho);
            _txtApellido.MaxLength = Huesped.LargoApellido;
            Contenido.Controls.Add(_txtApellido);
            y += altoFila;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Teléfono", new Point(margenX, y)));
            _txtTelefono = CamposFormulario.SoloNumeros(CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho), Huesped.LargoTelefono);
            Contenido.Controls.Add(_txtTelefono);
            y += altoFila;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Dirección", new Point(margenX, y)));
            _txtDireccion = CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho);
            _txtDireccion.MaxLength = Huesped.LargoDireccion;
            Contenido.Controls.Add(_txtDireccion);
            y += altoFila;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Correo electrónico", new Point(margenX, y)));
            _txtCorreo = CamposFormulario.Texto(new Point(margenX, y + separacionEtiquetaCampo), ancho);
            _txtCorreo.MaxLength = Huesped.LargoCorreo;
            Contenido.Controls.Add(_txtCorreo);
            y += altoFila;

            // Dos líneas de alto: los mensajes de validación (DNI, teléfono o correo repetidos) no
            // entran en una sola línea con este ancho de campo.
            _lblError = CamposFormulario.Error(new Point(margenX, y), ancho);
            _lblError.Height = 36;
            Contenido.Controls.Add(_lblError);
            y += 36 + separacionFilas;

            // Los botones se alinean con el margen izquierdo y con el borde derecho del contenido,
            // no con el ancho (mucho más chico) de los campos de texto, para que no se superpongan
            const int anchoBoton = 130;
            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(anchoBoton, 35), Location = new Point(margenX, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Registrar", Size = new Size(anchoBoton, 35), Location = new Point(anchoContenido - margenX - anchoBoton, y) });
            btnGuardar.Click += (s, e) => Guardar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnGuardar);

            y += 35 + 20;
            // Alto total del modal = contenido calculado + padding del marco (24 arriba y abajo) + barra de título.
            // El ancho sale de anchoContenido + el padding del marco (24 a cada lado), en vez de un
            // valor fijo desacoplado del ancho real de los campos.
            Size = new Size(anchoContenido + 48, y + 48 + 53);
        }

        private void Guardar()
        {
            try
            {
                _gestionHuespedes.RegistrarHuesped(new Huesped
                {
                    DniHuesped = _txtDni.Text.Trim(),
                    Nombre = _txtNombre.Text.Trim(),
                    Apellido = _txtApellido.Text.Trim(),
                    Telefono = _txtTelefono.Text.Trim(),
                    Direccion = _txtDireccion.Text.Trim(),
                    Correo = _txtCorreo.Text.Trim()
                });

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


