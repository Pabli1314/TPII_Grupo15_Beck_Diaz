using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace Presentacion.Gerente
{
    public class FormModalHabitaciones : Form
    {
        private readonly GestionGerente _lGerente;

        public FormModalHabitaciones(string estadoSeleccionado, Color colorCabecera, GestionGerente gestionGerente)
        {
            _lGerente = gestionGerente;
            InitializeComponentes(estadoSeleccionado, colorCabecera);
            CargarHabitaciones(estadoSeleccionado);
        }

        private void InitializeComponentes(string estadoSeleccionado, Color colorCabecera)
        {
            this.Text = $"Detalle de Habitaciones - {estadoSeleccionado.ToUpper()}";
            this.Size = new Size(680, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 243, 246);

            // Cabecera Modal
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = colorCabecera,
                Padding = new Padding(12)
            };

            Label lblTitulo = new Label
            {
                Text = $"Listado de Habitaciones en Estado: {estadoSeleccionado.ToUpper()}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitulo);

            // Grilla de Datos
            DataGridView dgv = new DataGridView
            {
                Name = "dgvModalHabitaciones",
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(49, 50, 68);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 32;

            // Panel Inferior con Botón
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.White
            };

            Button btnCerrar = new Button
            {
                Text = "Cerrar",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(90, 30),
                Location = new Point(560, 8),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(dgv);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private void CargarHabitaciones(string estado)
        {
            try
            {
                List<Habitacion> lista = _lGerente.ConsultarHabitacionesPorEstado(estado);
                DataGridView dgv = (DataGridView)this.Controls["dgvModalHabitaciones"];

                // Proyección a objeto anónimo para omitir IdEstado y formatear Tipo
                var listaVista = lista.Select(h => new
                {
                    NroHabitacion = h.NroHabitacion,
                    Piso = h.Piso,
                    CantCamas = h.CantCamas,
                    TarifaBase = h.TarifaBase,
                    TipoHabitacion = _lGerente.ObtenerNombreTipoHabitacion(h.IdTipoHabitacion)
                }).ToList();

                dgv.DataSource = null;
                dgv.DataSource = listaVista;

                if (dgv.Columns.Count > 0)
                {
                    if (dgv.Columns["NroHabitacion"] != null) dgv.Columns["NroHabitacion"].HeaderText = "N° Habitación";
                    if (dgv.Columns["Piso"] != null) dgv.Columns["Piso"].HeaderText = "Piso";
                    if (dgv.Columns["CantCamas"] != null) dgv.Columns["CantCamas"].HeaderText = "Cant. Camas";
                    if (dgv.Columns["TarifaBase"] != null)
                    {
                        dgv.Columns["TarifaBase"].HeaderText = "Tarifa Base";
                        dgv.Columns["TarifaBase"].DefaultCellStyle.Format = "C2";
                    }
                    if (dgv.Columns["TipoHabitacion"] != null) dgv.Columns["TipoHabitacion"].HeaderText = "Tipo de Habitación";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al cargar la lista de habitaciones: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}