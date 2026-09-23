using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Entidades;
using Logica;

namespace Presentacion.Gerente
{
    public class FormHistoricos : Form
    {
        private readonly GestionGerente _lGerente;
        private Dictionary<string, string> _diccionarioUsuarios = new Dictionary<string, string>();

        // Controles GUI
        private DateTimePicker dtpDesde;
        private DateTimePicker dtpHasta;
        private ComboBox cbUsuarios;
        private Button btnFiltrar;

        private Label lblTotalRecaudadoVal;
        private Label lblPromedioTurnoVal;
        private Label lblTotalTurnosVal;

        private Chart chartRecaudacionUsuario;
        private Chart chartDistribucionTurnos;
        private DataGridView dgvHistorico;

        public FormHistoricos()
        {
            _lGerente = new GestionGerente();

            // Configuración responsiva del formulario
            this.Size = new Size(1024, 720);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 243, 246);
            this.Text = "Auditoría e Históricos Operativos";

            InicializarComponentesResponsivos();

            if (btnFiltrar != null)
            {
                btnFiltrar.Click -= BtnFiltrar_Click;
                btnFiltrar.Click += BtnFiltrar_Click;
            }

            this.Shown += (s, e) =>
            {
                CargarComboUsuarios();
                EjecutarFiltro();
            };
        }

        private void BtnFiltrar_Click(object? sender, EventArgs e)
        {
            EjecutarFiltro();
        }

        private void InicializarComponentesResponsivos()
        {
            this.SuspendLayout();

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

            // 1. PANEL SUPERIOR
            FlowLayoutPanel pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            dtpDesde = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-1), Margin = new Padding(0, 5, 10, 0) };
            dtpHasta = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 5, 10, 0) };
            cbUsuarios = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 10, 0) };
            btnFiltrar = new Button { Text = "Filtrar", Width = 80, Height = 25, Margin = new Padding(0, 4, 20, 0) };

            Label lblRec = new Label { Text = "Total: ", AutoSize = true, Margin = new Padding(10, 8, 0, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblTotalRecaudadoVal = new Label { AutoSize = true, Text = "$0.00", Margin = new Padding(0, 8, 15, 0), Font = new Font("Segoe UI", 9, FontStyle.Regular) };

            Label lblProm = new Label { Text = "Promedio: ", AutoSize = true, Margin = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblPromedioTurnoVal = new Label { AutoSize = true, Text = "$0.00", Margin = new Padding(0, 8, 15, 0), Font = new Font("Segoe UI", 9, FontStyle.Regular) };

            Label lblTurnos = new Label { Text = "Turnos: ", AutoSize = true, Margin = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblTotalTurnosVal = new Label { AutoSize = true, Text = "0", Margin = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 9, FontStyle.Regular) };

            pnlTop.Controls.AddRange(new Control[] {
                dtpDesde, dtpHasta, cbUsuarios, btnFiltrar,
                lblRec, lblTotalRecaudadoVal,
                lblProm, lblPromedioTurnoVal,
                lblTurnos, lblTotalTurnosVal
            });

            // 2. PANEL MEDIO (GRÁFICOS)
            TableLayoutPanel pnlCharts = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10)
            };
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Chart Barras
            chartRecaudacionUsuario = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea areaBarras = new ChartArea("AreaBarras");
            areaBarras.AxisX.Interval = 1; // Fuerza la visualización de la etiqueta en cada barra
            areaBarras.AxisX.IsLabelAutoFit = true;
            areaBarras.AxisY.Title = "Monto Recaudado ($)";
            areaBarras.AxisY.TitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
            areaBarras.AxisY.LabelStyle.Format = "C0";
            chartRecaudacionUsuario.ChartAreas.Add(areaBarras);

            Series serieBarras = new Series("Recaudación")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                LabelFormat = "{0:C0}",
                // X es texto (nombre del usuario): sin esto todas las barras caen en la misma posición.
                IsXValueIndexed = true
            };
            chartRecaudacionUsuario.Series.Add(serieBarras);

            // Chart Torta
            chartDistribucionTurnos = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea areaTorta = new ChartArea("AreaTorta");
            chartDistribucionTurnos.ChartAreas.Add(areaTorta);

            Legend leyenda = new Legend("LeyendaTorta")
            {
                Docking = Docking.Right,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            chartDistribucionTurnos.Legends.Add(leyenda);

            Series serieTorta = new Series("Distribucion")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                LabelFormat = "{0:C2}",
                Legend = "LeyendaTorta"
            };
            chartDistribucionTurnos.Series.Add(serieTorta);

            pnlCharts.Controls.Add(chartRecaudacionUsuario, 0, 0);
            pnlCharts.Controls.Add(chartDistribucionTurnos, 1, 0);

            // 3. PANEL INFERIOR (DATAGRIDVIEW)
            dgvHistorico = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                BackgroundColor = Color.White
            };

            mainLayout.Controls.Add(pnlTop, 0, 0);
            mainLayout.Controls.Add(pnlCharts, 0, 1);
            mainLayout.Controls.Add(dgvHistorico, 0, 2);

            this.Controls.Add(mainLayout);
            this.ResumeLayout(false);
        }

        private void CargarComboUsuarios()
        {
            try
            {
                var usuarios = _lGerente.ObtenerListaUsuarios() ?? new List<KeyValuePair<string, string>>();

                _diccionarioUsuarios = usuarios
                    .Where(u => !string.IsNullOrWhiteSpace(u.Key))
                    .GroupBy(u => u.Key)
                    .ToDictionary(g => g.Key.Trim(), g => g.First().Value);

                var listaBinding = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>(string.Empty, "-- Todos --")
                };
                listaBinding.AddRange(usuarios);

                cbUsuarios.DataSource = listaBinding;
                cbUsuarios.DisplayMember = "Value";
                cbUsuarios.ValueMember = "Key";
                if (cbUsuarios.Items.Count > 0)
                {
                    cbUsuarios.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de usuarios: {ex.Message}", "Error de Carga", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void EjecutarFiltro()
        {
            try
            {
                string? dniUsuarioFiltro = cbUsuarios.SelectedValue?.ToString();
                if (string.IsNullOrWhiteSpace(dniUsuarioFiltro))
                {
                    dniUsuarioFiltro = null;
                }

                DateTime desde = dtpDesde.Value.Date;
                DateTime hasta = dtpHasta.Value.Date.AddDays(1).AddTicks(-1);

                var datos = _lGerente.ConsultarHistorialOperativo(desde, hasta, dniUsuarioFiltro);

                // 1. KPI Cards
                lblTotalRecaudadoVal.Text = datos.TotalRecaudado.ToString("C2");
                lblPromedioTurnoVal.Text = datos.PromedioTurno.ToString("C2");
                lblTotalTurnosVal.Text = datos.TotalTurnosCerrados.ToString();

                // 2. DataGridView
                dgvHistorico.DataSource = null;
                if (datos.Lista != null && datos.Lista.Count > 0)
                {
                    dgvHistorico.DataSource = datos.Lista;
                    FormatearGrilla();
                }

                // 3. Limpiar Puntos de Gráficos
                chartRecaudacionUsuario.Series["Recaudación"].Points.Clear();
                chartDistribucionTurnos.Series["Distribucion"].Points.Clear();

                Color[] paleta = new Color[]
                {
                    Color.FromArgb(16, 185, 129),
                    Color.FromArgb(59, 130, 246),
                    Color.FromArgb(245, 158, 11),
                    Color.FromArgb(139, 92, 246),
                    Color.FromArgb(236, 72, 153)
                };
                int colorIndex = 0;

                // Crear un mapa normalizado DNI -> MontoRecaudado independientemente de cómo venga la clave desde la lógica
                Dictionary<string, decimal> mapaRecaudacionPorDni = new Dictionary<string, decimal>();

                if (datos.RecaudacionPorUsuario != null)
                {
                    foreach (var kvp in datos.RecaudacionPorUsuario)
                    {
                        string dniKey = kvp.Key;

                        if (dniKey.StartsWith("DNI: "))
                        {
                            dniKey = dniKey.Replace("DNI: ", "").Trim();
                        }
                        else
                        {
                            // Si en la capa de lógica la clave fue guardada con el nombre completo, buscamos su DNI
                            var match = _diccionarioUsuarios.FirstOrDefault(x => x.Value.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                            if (!string.IsNullOrEmpty(match.Key))
                            {
                                dniKey = match.Key;
                            }
                        }

                        mapaRecaudacionPorDni[dniKey] = kvp.Value;
                    }
                }

                // Solo los usuarios que aparecen en los turnos consultados (los mismos de la grilla).
                foreach (var (dni, monto) in mapaRecaudacionPorDni.OrderByDescending(kvp => kvp.Value).Select(kvp => (kvp.Key, kvp.Value)))
                {
                    string nombreMostrar = _diccionarioUsuarios.TryGetValue(dni, out string? nombre) && !string.IsNullOrWhiteSpace(nombre)
                        ? nombre
                        : $"DNI {dni}";

                    // Agregar barra asignando explícitamente el NOMBRE en el Eje X y el MONTO en el Eje Y
                    chartRecaudacionUsuario.Series["Recaudación"].Points.AddXY(nombreMostrar, monto);

                    // Para el gráfico de torta, omitimos porciones de $0 para mantener limpio el diseño
                    if (monto > 0)
                    {
                        int idx = chartDistribucionTurnos.Series["Distribucion"].Points.AddXY(nombreMostrar, monto);
                        DataPoint punto = chartDistribucionTurnos.Series["Distribucion"].Points[idx];
                        punto.Color = paleta[colorIndex % paleta.Length];
                        punto.LegendText = nombreMostrar;
                        colorIndex++;
                    }
                }

                chartRecaudacionUsuario.Invalidate();
                chartDistribucionTurnos.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar históricos: {ex.Message}", "Error de Auditoría", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatearGrilla()
        {
            if (dgvHistorico.Columns.Count == 0) return;

            if (dgvHistorico.Columns["IdTurno"] != null) dgvHistorico.Columns["IdTurno"].HeaderText = "N° Turno";
            if (dgvHistorico.Columns["FechaApertura"] != null) dgvHistorico.Columns["FechaApertura"].HeaderText = "F. Apertura";
            if (dgvHistorico.Columns["HoraApertura"] != null) dgvHistorico.Columns["HoraApertura"].HeaderText = "H. Apertura";
            if (dgvHistorico.Columns["FechaCierre"] != null) dgvHistorico.Columns["FechaCierre"].HeaderText = "F. Cierre";
            if (dgvHistorico.Columns["HoraCierre"] != null) dgvHistorico.Columns["HoraCierre"].HeaderText = "H. Cierre";

            if (dgvHistorico.Columns["MontoInicial"] != null)
            {
                dgvHistorico.Columns["MontoInicial"].HeaderText = "M. Inicial";
                dgvHistorico.Columns["MontoInicial"].DefaultCellStyle.Format = "C2";
            }
            if (dgvHistorico.Columns["MontoFinal"] != null)
            {
                dgvHistorico.Columns["MontoFinal"].HeaderText = "M. Final";
                dgvHistorico.Columns["MontoFinal"].DefaultCellStyle.Format = "C2";
            }
            if (dgvHistorico.Columns["Observaciones"] != null) dgvHistorico.Columns["Observaciones"].HeaderText = "Observaciones / Arqueo";
            if (dgvHistorico.Columns["DniUsuario"] != null) dgvHistorico.Columns["DniUsuario"].HeaderText = "DNI Usuario";
            if (dgvHistorico.Columns["IdUsuario"] != null) dgvHistorico.Columns["IdUsuario"].Visible = false;
        }
    }
}