using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaConfiguracion : UserControl, IVistaAdministrador
    {
        private readonly GestionConfiguracion _gestionConfiguracion = new();

        private readonly NumericUpDown _numMinutos;
        private readonly ToggleSwitch _switchVisuales;
        private readonly ToggleSwitch _switchSonoras;
        private readonly Label _lblConfirmacion;

        public VistaConfiguracion()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var tarjeta = new Panel { Location = new Point(0, 0), Size = new Size(620, 420), BackColor = Paleta.FondoTarjeta, Padding = new Padding(28) };
            tarjeta.Paint += (s, e) => DibujarBordeTarjeta(tarjeta, e);

            var lblTitulo = new Label { Text = "Alertas de vencimiento de ocupación", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            tarjeta.Controls.Add(lblTitulo);

            int y = 44;

            tarjeta.Controls.Add(CamposFormulario.Etiqueta("Alertar X minutos antes del vencimiento", new Point(0, y)));
            y += 26;
            _numMinutos = CamposFormulario.Numerico(new Point(0, y), 160, 1, 240);
            tarjeta.Controls.Add(_numMinutos);
            y += 36;
            tarjeta.Controls.Add(TextoExplicativo("Minutos de tolerancia antes de la hora de salida estimada de una ocupación para que el sistema la marque como próxima a vencer (RF: configurable por el Administrador).", y));
            y += 60;

            var panelVisuales = CrearFilaSwitch("Activar alertas visuales", "Muestra badges y avisos en pantalla cuando una ocupación está por vencer.", out _switchVisuales, y);
            tarjeta.Controls.Add(panelVisuales);
            y += 76;

            var panelSonoras = CrearFilaSwitch("Activar alertas sonoras", "Reproduce un sonido del sistema cuando se genera una alerta de vencimiento.", out _switchSonoras, y);
            tarjeta.Controls.Add(panelSonoras);
            y += 76;

            _lblConfirmacion = new Label { Location = new Point(0, y), AutoSize = true, ForeColor = Paleta.Exito, Font = Paleta.FuenteChica, Visible = false };
            tarjeta.Controls.Add(_lblConfirmacion);
            y += 26;

            var btnGuardar = EstiloBoton.Primario(new Button { Text = "Guardar cambios", Size = new Size(160, 40), Location = new Point(0, y) });
            btnGuardar.Click += (s, e) => Guardar();
            tarjeta.Controls.Add(btnGuardar);

            Controls.Add(tarjeta);
        }

        private static void DibujarBordeTarjeta(Panel tarjeta, PaintEventArgs e)
        {
            using var lapiz = new Pen(Paleta.Borde);
            e.Graphics.DrawRectangle(lapiz, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1);
        }

        private static Label TextoExplicativo(string texto, int y) => new Label
        {
            Text = texto,
            Location = new Point(0, y),
            Size = new Size(560, 40),
            Font = Paleta.FuenteChica,
            ForeColor = Paleta.TextoTerciario
        };

        private static Panel CrearFilaSwitch(string titulo, string explicacion, out ToggleSwitch toggle, int y)
        {
            var panel = new Panel { Location = new Point(0, y), Size = new Size(560, 70) };
            var lblTitulo = new Label { Text = titulo, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(0, 0) };
            var lblExplicacion = new Label { Text = explicacion, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, Location = new Point(0, 22), Size = new Size(460, 40) };
            var switchLocal = new ToggleSwitch { Location = new Point(500, 4) };

            panel.Controls.Add(lblTitulo);
            panel.Controls.Add(lblExplicacion);
            panel.Controls.Add(switchLocal);

            toggle = switchLocal;
            return panel;
        }

        public void Refrescar()
        {
            ConfiguracionAlertas configuracion = _gestionConfiguracion.Obtener();
            _numMinutos.Value = configuracion.MinutosTolerancia;
            _switchVisuales.Checked = configuracion.AlertasVisualesActivas;
            _switchSonoras.Checked = configuracion.AlertasSonorasActivas;
            _lblConfirmacion.Visible = false;
        }

        private void Guardar()
        {
            ConfiguracionAlertas actual = _gestionConfiguracion.Obtener();

            _gestionConfiguracion.Guardar(new ConfiguracionAlertas
            {
                MinutosTolerancia = (int)_numMinutos.Value,
                AlertasVisualesActivas = _switchVisuales.Checked,
                AlertasSonorasActivas = _switchSonoras.Checked,
                BackupAutomaticoActivo = actual.BackupAutomaticoActivo,
                BackupAutomaticoHora = actual.BackupAutomaticoHora
            });

            _lblConfirmacion.Text = "Configuración guardada correctamente.";
            _lblConfirmacion.Visible = true;
        }
    }
}
