using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Modales
{
    /// <summary>Apertura explícita del turno de caja del recepcionista.</summary>
    internal class FModalAperturaTurno : FormModalBase
    {
        private readonly GestionTurnoCaja _gestionTurnoCaja = new();
        private readonly Usuario _usuario;
        private readonly NumericUpDown _numMontoInicial;
        private readonly Label _lblError;

        public TurnoCaja? TurnoAbierto { get; private set; }

        public FModalAperturaTurno(Usuario usuario)
        {
            _usuario = usuario;
            Size = new Size(420, 360);
            EstablecerTitulo("Apertura de turno");

            const int ancho = 340;
            int y = 0;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Fecha", new Point(0, y)));
            Contenido.Controls.Add(new Label { Text = DateTime.Now.ToString("dd/MM/yyyy"), Location = new Point(0, y + 22), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario });
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Hora", new Point(0, y)));
            Contenido.Controls.Add(new Label { Text = DateTime.Now.ToString("HH:mm"), Location = new Point(0, y + 22), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario });
            y += 54;

            string nombreCompleto = $"{usuario.NomUsuario} {usuario.ApeUsuario}".Trim();
            Contenido.Controls.Add(CamposFormulario.Etiqueta("Usuario", new Point(0, y)));
            Contenido.Controls.Add(new Label { Text = nombreCompleto, Location = new Point(0, y + 22), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario });
            y += 54;

            Contenido.Controls.Add(CamposFormulario.Etiqueta("Monto inicial", new Point(0, y)));
            _numMontoInicial = CamposFormulario.Numerico(new Point(0, y + 22), ancho, 0, 10_000_000, 2);
            Contenido.Controls.Add(_numMontoInicial);
            y += 62;

            _lblError = CamposFormulario.Error(new Point(0, y), ancho);
            Contenido.Controls.Add(_lblError);
            y += 30;

            var btnCancelar = EstiloBoton.Secundario(new Button { Text = "Cancelar", Size = new Size(120, 38), Location = new Point(ancho - 120 - 130, y) });
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnAbrir = EstiloBoton.Primario(new Button { Text = "Abrir turno", Size = new Size(130, 38), Location = new Point(ancho - 130, y) });
            btnAbrir.Click += (s, e) => Confirmar();

            Contenido.Controls.Add(btnCancelar);
            Contenido.Controls.Add(btnAbrir);
        }

        private void Confirmar()
        {
            try
            {
                TurnoAbierto = _gestionTurnoCaja.AbrirTurno(_usuario.DniUsuario, _numMontoInicial.Value);
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