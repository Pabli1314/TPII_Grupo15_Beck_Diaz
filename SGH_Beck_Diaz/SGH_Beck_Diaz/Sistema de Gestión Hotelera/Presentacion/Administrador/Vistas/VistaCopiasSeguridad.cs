using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaCopiasSeguridad : UserControl, IVistaAdministrador
    {
        private const string ColDescargar = "colDescargar";
        private const string ColRestaurar = "colRestaurar";

        private readonly GestionBackups _gestionBackups = new();
        private readonly GestionConfiguracion _gestionConfiguracion = new();

        private readonly FlowLayoutPanel _panelTarjetas;
        private readonly DataGridView _dgv;
        private readonly ToggleSwitch _switchAutomatico;
        private readonly NumericUpDown _numHora;
        private List<BackupInfo> _backups = new();

        public VistaCopiasSeguridad()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            _panelTarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            var barraSuperior = new Panel { Dock = DockStyle.Top, Height = 90, Padding = new Padding(0, 16, 0, 0) };

            var btnCrear = EstiloBoton.Primario(new Button { Text = "Crear copia de seguridad ahora", Size = new Size(240, 42), Location = new Point(0, 16) });
            btnCrear.Click += (s, e) => CrearBackup();

            var lblAuto = new Label { Text = "Backup automático", Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, AutoSize = true, Location = new Point(280, 8) };
            _switchAutomatico = new ToggleSwitch { Location = new Point(280, 30) };
            _switchAutomatico.CheckedChanged += (s, e) => GuardarConfiguracionAutomatica();

            var lblHora = new Label { Text = "Hora (0-23)", Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(336, 8) };
            _numHora = CamposFormulario.Numerico(new Point(336, 28), 70, 0, 23);
            _numHora.ValueChanged += (s, e) => GuardarConfiguracionAutomatica();

            barraSuperior.Controls.Add(btnCrear);
            barraSuperior.Controls.Add(lblAuto);
            barraSuperior.Controls.Add(_switchAutomatico);
            barraSuperior.Controls.Add(lblHora);
            barraSuperior.Controls.Add(_numHora);

            var lblHistorial = new Label { Text = "Historial de copias de seguridad", Font = Paleta.FuenteSeccion, ForeColor = Paleta.TextoPrimario, Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 12, 0, 0) };

            _dgv = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();
            _dgv.CellContentClick += Dgv_CellContentClick;

            var panelGrid = new Panel { Dock = DockStyle.Fill };
            panelGrid.Controls.Add(_dgv);

            Controls.Add(panelGrid);
            Controls.Add(lblHistorial);
            Controls.Add(barraSuperior);
            Controls.Add(_panelTarjetas);
        }

        private void ConfigurarColumnas()
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha y hora", Width = 170 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 110 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tamanio", HeaderText = "Tamaño", Width = 110 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Origen", HeaderText = "Origen", Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColDescargar, HeaderText = "", Text = "Descargar", UseColumnTextForButtonValue = true, Width = 100, FlatStyle = FlatStyle.Flat });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColRestaurar, HeaderText = "", Text = "Restaurar", UseColumnTextForButtonValue = true, Width = 100, FlatStyle = FlatStyle.Flat });
        }

        public void Refrescar()
        {
            ConfiguracionAlertas configuracion = _gestionConfiguracion.Obtener();
            _switchAutomatico.Checked = configuracion.BackupAutomaticoActivo;
            _numHora.Value = configuracion.BackupAutomaticoHora;

            _backups = _gestionBackups.ObtenerHistorial();
            BackupInfo? ultimo = _backups.Count > 0 ? _backups[0] : null;

            _panelTarjetas.Controls.Clear();
            AgregarTarjeta("Último backup", ultimo != null ? ultimo.FechaHora.ToString("dd/MM/yyyy HH:mm") : "Sin registros", Paleta.Primario, Icono.Reloj);
            AgregarTarjeta("Estado", ultimo?.Estado ?? "-", ultimo?.Estado == "Exitoso" ? Paleta.Exito : Paleta.Peligro, Icono.Check);
            AgregarTarjeta("Tamaño", ultimo != null ? $"{ultimo.TamanioMB:0.#} MB" : "-", Paleta.Mantenimiento, Icono.Disco);
            AgregarTarjeta("Backups realizados", _backups.Count.ToString(), Paleta.Limpieza, Icono.Caja);

            _dgv.Rows.Clear();
            foreach (BackupInfo backup in _backups)
            {
                int fila = _dgv.Rows.Add(
                    backup.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    backup.Estado,
                    $"{backup.TamanioMB:0.#} MB",
                    backup.Automatico ? "Automático" : "Manual");

                _dgv.Rows[fila].Cells["Estado"].Style.ForeColor = backup.Estado == "Exitoso" ? Paleta.Exito : Paleta.Peligro;
            }
        }

        private void AgregarTarjeta(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 16) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelTarjetas.Controls.Add(tarjeta);
        }

        private void GuardarConfiguracionAutomatica()
        {
            ConfiguracionAlertas actual = _gestionConfiguracion.Obtener();
            _gestionConfiguracion.Guardar(new ConfiguracionAlertas
            {
                MinutosTolerancia = actual.MinutosTolerancia,
                AlertasVisualesActivas = actual.AlertasVisualesActivas,
                AlertasSonorasActivas = actual.AlertasSonorasActivas,
                BackupAutomaticoActivo = _switchAutomatico.Checked,
                BackupAutomaticoHora = (int)_numHora.Value
            });
        }

        private void CrearBackup()
        {
            _gestionBackups.CrearBackup();
            MessageBox.Show("Copia de seguridad creada correctamente.", "Copias de seguridad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Refrescar();
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _backups.Count) return;

            BackupInfo backup = _backups[e.RowIndex];
            string columna = _dgv.Columns[e.ColumnIndex].Name;

            if (columna == ColDescargar)
            {
                MessageBox.Show($"El archivo se guardaría en:\n{backup.RutaArchivo}", "Descargar backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (columna == ColRestaurar)
            {
                DialogResult confirmacion = MessageBox.Show(
                    $"¿Confirma que desea restaurar la copia de seguridad del {backup.FechaHora:dd/MM/yyyy HH:mm}?\nEsta acción reemplazará los datos actuales.",
                    "Restaurar copia de seguridad", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirmacion == DialogResult.Yes)
                {
                    _gestionBackups.RestaurarBackup(backup.Id);
                    MessageBox.Show("Copia de seguridad restaurada correctamente.", "Copias de seguridad", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
