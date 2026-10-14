using System;
using System.Drawing;
using System.Windows.Forms;
using Logica;
using Entidades;

namespace Presentacion.Gerente
{
    public class FormAnalisisFinanciero : Form
    {
        private GestionTurnoCaja gestionTurnoCaja = new GestionTurnoCaja();
        private string dniUsuarioActual; // DNI del usuario autenticado en el sistema (identificador tipo string)

        // Labels para las métricas financieras (KPIs)
        private Label lblTotalIngresosVal;
        private Label lblMontoAperturaVal;
        private Label lblMontoActualVal;
        private Label lblEstadoCajaVal;

        // DataGridView para mostrar ingresos/movimientos
        private DataGridView dgvIngresos;

        public FormAnalisisFinanciero(string dniUsuario)
        {
            this.dniUsuarioActual = dniUsuario ?? string.Empty;
            InitializeComponentes();
        }

        // Constructor por defecto para mantener compatibilidad con inicializaciones sin parámetros
        public FormAnalisisFinanciero() : this(string.Empty)
        {
        }

        private void InitializeComponentes()
        {
            this.BackColor = Color.FromArgb(240, 243, 246);
            this.Text = "Análisis Financiero de Caja";
            this.Size = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F)); // Sección KPIs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Grilla de datos

            // Title / Subtitle
            Label lblTitle = new Label
            {
                Text = "📈 Análisis Financiero e Ingresos de Caja",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // --- SECCIÓN 1: METRICAS (KPIs) ---
            TableLayoutPanel gridKPIs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 5, 0, 10)
            };
            for (int i = 0; i < 4; i++)
            {
                gridKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            lblEstadoCajaVal = CrearTarjetaKPI(gridKPIs, 0, "Estado de Caja", Color.FromArgb(108, 117, 125));
            lblMontoAperturaVal = CrearTarjetaKPI(gridKPIs, 1, "Monto Inicial", Color.FromArgb(23, 162, 184));
            lblTotalIngresosVal = CrearTarjetaKPI(gridKPIs, 2, "Ingresos Turno", Color.FromArgb(40, 167, 69));
            lblMontoActualVal = CrearTarjetaKPI(gridKPIs, 3, "Total Estimado", Color.FromArgb(0, 123, 255));

            mainLayout.Controls.Add(gridKPIs, 0, 1);

            // --- SECCIÓN 2: TABLA DE INGRESOS ---
            GroupBox gbDetalle = new GroupBox
            {
                Text = " Detalle de Ingresos e Historial del Turno ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(49, 50, 68)
            };

            dgvIngresos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            // Estilos nativos del DataGridView
            dgvIngresos.EnableHeadersVisualStyles = false;
            dgvIngresos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvIngresos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            gbDetalle.Controls.Add(dgvIngresos);
            mainLayout.Controls.Add(gbDetalle, 0, 2);

            this.Controls.Add(mainLayout);

            // Cargar información al abrir el formulario
            this.CargarDatosFinancieros();
        }

        private Label CrearTarjetaKPI(TableLayoutPanel contenedor, int columna, string titulo, Color colorEncabezado)
        {
            Panel cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblTituloCard = new Label
            {
                Text = titulo,
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = colorEncabezado,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblValorCard = new Label
            {
                Text = "---",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleCenter
            };

            cardPanel.Controls.Add(lblValorCard);
            cardPanel.Controls.Add(lblTituloCard);

            contenedor.Controls.Add(cardPanel, columna, 0);

            return lblValorCard;
        }

        private void CargarDatosFinancieros()
        {
            try
            {
                // 1. Cargar el historial completo de turnos
                var historialTurnos = gestionTurnoCaja.ObtenerHistorialTurnos();
                dgvIngresos.DataSource = null;
                dgvIngresos.DataSource = historialTurnos;
                AjustarFormatoGrid();

                // 2. Consultar si hay un turno abierto solo si el DNI es válido
                TurnoCaja? turnoActivo = null;

                if (!string.IsNullOrWhiteSpace(dniUsuarioActual))
                {
                    turnoActivo = gestionTurnoCaja.ObtenerTurnoAbierto(dniUsuarioActual);
                }

                if (turnoActivo != null)
                {
                    lblEstadoCajaVal.Text = "ABIERTA";
                    lblEstadoCajaVal.ForeColor = Color.Green;
                    lblMontoAperturaVal.Text = turnoActivo.MontoInicial.ToString("C");
                }
                else
                {
                    lblEstadoCajaVal.Text = "CERRADA";
                    lblEstadoCajaVal.ForeColor = Color.Red;
                    lblMontoAperturaVal.Text = "$0,00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el análisis financiero: {ex.Message}",
                                "Error de Carga",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Aplica nombres claros y formatos de moneda a las columnas del DataGridView
        /// </summary>
        private void AjustarFormatoGrid()
        {
            if (dgvIngresos.Columns.Count == 0) return;

            if (dgvIngresos.Columns["IdTurno"] != null)
                dgvIngresos.Columns["IdTurno"].HeaderText = "N° Turno";

            if (dgvIngresos.Columns["FechaApertura"] != null)
                dgvIngresos.Columns["FechaApertura"].HeaderText = "F. Apertura";

            if (dgvIngresos.Columns["HoraApertura"] != null)
                dgvIngresos.Columns["HoraApertura"].HeaderText = "H. Apertura";

            if (dgvIngresos.Columns["FechaCierre"] != null)
                dgvIngresos.Columns["FechaCierre"].HeaderText = "F. Cierre";

            if (dgvIngresos.Columns["HoraCierre"] != null)
                dgvIngresos.Columns["HoraCierre"].HeaderText = "H. Cierre";

            if (dgvIngresos.Columns["MontoInicial"] != null)
            {
                dgvIngresos.Columns["MontoInicial"].HeaderText = "Monto Inicial";
                dgvIngresos.Columns["MontoInicial"].DefaultCellStyle.Format = "C2";
            }

            if (dgvIngresos.Columns["MontoFinal"] != null)
            {
                dgvIngresos.Columns["MontoFinal"].HeaderText = "Monto Final";
                dgvIngresos.Columns["MontoFinal"].DefaultCellStyle.Format = "C2";
            }

            if (dgvIngresos.Columns["Observaciones"] != null)
                dgvIngresos.Columns["Observaciones"].HeaderText = "Observaciones";

            if (dgvIngresos.Columns["DniUsuario"] != null)
                dgvIngresos.Columns["DniUsuario"].HeaderText = "DNI Usuario";

            if (dgvIngresos.Columns["IdUsuario"] != null)
                dgvIngresos.Columns["IdUsuario"].Visible = false;
        }
    }

}

