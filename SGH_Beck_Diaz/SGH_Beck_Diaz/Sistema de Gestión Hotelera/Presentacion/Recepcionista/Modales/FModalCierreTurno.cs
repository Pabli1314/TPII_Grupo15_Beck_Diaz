using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Modales
{
    /// <summary>Cierre de turno: muestra los totales calculados y compara el efectivo contado
    /// contra el esperado (monto inicial + cobros en efectivo).</summary>
    internal class FModalCierreTurno : FormModalBase
    {
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();
        private readonly int _idTurno;
        private readonly ResumenTurno _resumen;
        private readonly NumericUpDown _numEfectivoContado;
        private readonly TextBox _txtObservaciones;
        private readonly Label _lblError;

        public FModalCierreTurno(int idTurno)
        {
            _idTurno = idTurno;
            _gestionTurnoCaja = new GestionTurnoCaja();
            _resumen = _gestionTurnoCaja.ObtenerResumen(idTurno);

            Size = new Size(460, 560);
            EstablecerTitulo("Cierre de turno");

            const int ancho = 380;
            int y = 0;

            AgregarFila("Monto inicial", _resumen.Turno.MontoInicial, ref y, ancho);
            AgregarFila("Cobros en efectivo", _resumen.TotalEfectivo, ref y, ancho);
            AgregarFila("Ventas adicionales", _resumen.TotalVentas, ref y, ancho, negrita: false);
            var separador = new Panel { Location = new Point(0, y), Size = new Size(ancho, 1), BackColor = Paleta.Borde };
            Contenido.Controls.Add(separador);
            y += 10;
            AgregarFila("Efectivo esperado", _resumen.EfectivoEsperado, ref y, ancho, negrita: true);
            y += 8;
            AgregarFila("Total tarjeta", _resumen.TotalTarjeta, ref y, ancho);
            AgregarFila("Total transferencia", _resumen.TotalTransferencia, ref y, ancho);
            AgregarFila("Total general", _resumen.TotalGeneral, ref y, ancho, negrita: true);
            y += 14;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Efectivo contado", new Point(0, y)));
            _numEfectivoContado = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 10_000_000, 2);
            _numEfectivoContado.Value = _resumen.EfectivoEsperado;
            Contenido.Controls.Add(_numEfectivoContado);
            y += 62;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Observaciones", new Point(0, y)));
            _txtObservaciones = CamposFormulario.Texto(new Point(0, y + 22), ancho, esPassword: false);
            Contenido.Controls.Add(_txtObservaciones);
            y += 54;

            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 140, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnCerrar = EstiloBoton.Primario(new Button { Text = "Cerrar turno", Size = new Size(140, 38), Location = new Point(ancho - 140, y) });
            btnCerrar.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnCerrar);
        }

        private void AgregarFila(string etiqueta, decimal monto, ref int y, int ancho, bool negrita = false)
        {
            Contenido.Controls.Add(new Label
            {
                Text = etiqueta,
                Location = new Point(0, y),
                AutoSize = true,
                Font = negrita ? Paleta.FuenteBaseNegrita : Paleta.FuenteBase,
                ForeColor = Paleta.TextoPrimario
            });
            Contenido.Controls.Add(new Label
            {
                Text = monto.ToString("C"),
                Location = new Point(ancho - 140, y),
                Size = new Size(140, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = negrita ? Paleta.FuenteBaseNegrita : Paleta.FuenteBase,
                ForeColor = Paleta.TextoPrimario
            });
            y += 26;
        }

        private void Confirmar()
        {
            DialogResult confirmacion = MessageBox.Show(
                "¿Confirma el cierre del turno? Esta acción no se puede deshacer.",
                "Cerrar turno",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            try
            {
                decimal diferencia = _gestionTurnoCaja.CerrarTurno(_idTurno, _numEfectivoContado.Value, _txtObservaciones.Text.Trim());

                string mensaje = diferencia == 0
                    ? "Turno cerrado sin diferencias de caja."
                    : $"Turno cerrado. Diferencia de caja: {diferencia:C}.";

                MessageBox.Show(mensaje, "Turno cerrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
