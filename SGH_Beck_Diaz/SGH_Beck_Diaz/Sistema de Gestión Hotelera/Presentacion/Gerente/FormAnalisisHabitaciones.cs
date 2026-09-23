using Entidades;
using Logica;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Gerente
{
    public class FormAnalisisHabitaciones : Form
    {
        private GestionGerente gestionGerente = new GestionGerente();

        // Controles dinámicos para los estados
        private Label lblDisponiblesVal;
        private Label lblOcupadasVal;
        private Label lblLimpiezaVal;
        private Label lblMantenimientoVal;

        // Controles dinámicos para los tipos de habitación
        private Label lblIndividualVal;
        private Label lblEstandarVal;
        private Label lblDobleVal;
        private Label lblSuiteVal;

        public FormAnalisisHabitaciones()
        {
            InitializeComponentes();
        }

        private void InitializeComponentes()
        {
            this.BackColor = Color.FromArgb(240, 243, 246);
            this.Text = "Análisis de Habitaciones";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel contenedor principal
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));   // Título / Info
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));    // Sección Estados
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));    // Sección Tipos

            // Header informativo
            Label lblInfo = new Label
            {
                Text = "🔍 Supervisión General de Habitaciones: Ocupadas, Libres, Limpieza y Mantenimiento.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            mainLayout.Controls.Add(lblInfo, 0, 0);

            // --- SECCIÓN 1: ESTADOS DE HABITACIÓN ---
            GroupBox gbEstados = new GroupBox
            {
                Text = " Estado Actual de las Habitaciones (Haz clic en una tarjeta para ver el detalle) ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(49, 50, 68)
            };

            TableLayoutPanel gridEstados = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10)
            };
            for (int i = 0; i < 4; i++)
            {
                gridEstados.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            // Se vincula el evento de clic pasando el nombre del estado
            lblDisponiblesVal = CrearTarjetaKPI(gridEstados, 0, "Disponibles", Color.FromArgb(40, 167, 69), "Disponible");
            lblOcupadasVal = CrearTarjetaKPI(gridEstados, 1, "Ocupadas", Color.FromArgb(220, 53, 69), "Ocupado");
            lblLimpiezaVal = CrearTarjetaKPI(gridEstados, 2, "En Limpieza", Color.FromArgb(255, 193, 7), "Limpieza");
            lblMantenimientoVal = CrearTarjetaKPI(gridEstados, 3, "Mantenimiento", Color.FromArgb(13, 110, 253), "Mantenimiento");

            gbEstados.Controls.Add(gridEstados);
            mainLayout.Controls.Add(gbEstados, 0, 1);

            // --- SECCIÓN 2: TIPOS DE HABITACIÓN ---
            GroupBox gbTipos = new GroupBox
            {
                Text = " Distribución por Tipo de Habitación ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(49, 50, 68)
            };

            TableLayoutPanel gridTipos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(10)
            };
            for (int i = 0; i < 4; i++)
            {
                gridTipos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            lblIndividualVal = CrearTarjetaKPI(gridTipos, 0, "Individual", Color.FromArgb(23, 162, 184));
            lblEstandarVal = CrearTarjetaKPI(gridTipos, 1, "Estándar", Color.FromArgb(0, 123, 255));
            lblDobleVal = CrearTarjetaKPI(gridTipos, 2, "Doble Matrimonial", Color.FromArgb(111, 66, 193));
            lblSuiteVal = CrearTarjetaKPI(gridTipos, 3, "Suite Executive", Color.FromArgb(253, 126, 20));

            gbTipos.Controls.Add(gridTipos);
            mainLayout.Controls.Add(gbTipos, 0, 2);

            this.Controls.Add(mainLayout);

            // Cargar métricas al inicializar la pantalla
            this.cargarDatos();
        }

        /// <summary>
        /// Sobrecarga del método para tarjetas que abren el modal (Sección Estados).
        /// </summary>
        private Label CrearTarjetaKPI(TableLayoutPanel contenedor, int columna, string titulo, Color colorEncabezado, string estadoFiltro)
        {
            Label lblValor = CrearTarjetaKPI(contenedor, columna, titulo, colorEncabezado);
            Panel cardPanel = (Panel)contenedor.GetControlFromPosition(columna, 0);

            // Cambiar el cursor a mano para indicar interacción
            cardPanel.Cursor = Cursors.Hand;
            foreach (Control child in cardPanel.Controls)
            {
                child.Cursor = Cursors.Hand;
            }

            // Manejador del evento Click
            EventHandler abrirModalHandler = (s, e) =>
            {
                FormModalHabitaciones modal = new FormModalHabitaciones(estadoFiltro, colorEncabezado, gestionGerente);
                modal.ShowDialog(this);
            };

            // Suscribir eventos al panel y a todos sus hijos para que el clic funcione en cualquier punto
            cardPanel.Click += abrirModalHandler;
            foreach (Control c in cardPanel.Controls)
            {
                c.Click += abrirModalHandler;
            }

            return lblValor;
        }

        /// <summary>
        /// Crea un panel estilizado tipo tarjeta para mostrar el título y valor de cada categoría.
        /// </summary>
        private Label CrearTarjetaKPI(TableLayoutPanel contenedor, int columna, string titulo, Color colorEncabezado)
        {
            Panel cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblTituloCard = new Label
            {
                Text = titulo,
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = colorEncabezado,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblValorCard = new Label
            {
                Text = "0",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleCenter
            };

            cardPanel.Controls.Add(lblValorCard);
            cardPanel.Controls.Add(lblTituloCard);

            contenedor.Controls.Add(cardPanel, columna, 0);

            return lblValorCard;
        }

        /// <summary>
        /// Consulta la capa de negocio y actualiza las tarjetas KPI con el total de habitaciones por estado y tipo.
        /// </summary>
        private void cargarDatos()
        {
            try
            {
                var (vectorEstados, vectorTipos) = gestionGerente.ObtenerEstadisticasHabitaciones();

                if (vectorEstados != null && vectorEstados.Length >= 4)
                {
                    lblDisponiblesVal.Text = vectorEstados[0].ToString();
                    lblOcupadasVal.Text = vectorEstados[1].ToString();
                    lblLimpiezaVal.Text = vectorEstados[2].ToString();
                    lblMantenimientoVal.Text = vectorEstados[3].ToString();
                }

                if (vectorTipos != null && vectorTipos.Length >= 4)
                {
                    lblIndividualVal.Text = vectorTipos[0].ToString();
                    lblEstandarVal.Text = vectorTipos[1].ToString();
                    lblDobleVal.Text = vectorTipos[2].ToString();
                    lblSuiteVal.Text = vectorTipos[3].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al cargar las métricas de habitaciones: {ex.Message}",
                                "Error de Carga",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}