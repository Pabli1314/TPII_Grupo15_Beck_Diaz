using Entidades;
using Logica;
using Presentacion.Administrador.Modales;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    internal class VistaTarifas : UserControl, IVistaAdministrador
    {
        private const string ColEditar = "colEditar";
        private const string ColEstado = "colEstadoAccion";

        private readonly GestionTarifas _gestionTarifas = new();
        private readonly DataGridView _dgv;
        private List<Tarifa> _tarifas = new();

        public VistaTarifas()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var barraSuperior = new Panel { Dock = DockStyle.Top, Height = 48 };
            var btnNueva = EstiloBoton.Primario(new Button { Text = "+  Nueva tarifa", Size = new Size(160, 38), Dock = DockStyle.Right });
            btnNueva.Click += (s, e) => AbrirModal(null);
            barraSuperior.Controls.Add(btnNueva);

            _dgv = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();
            _dgv.CellContentClick += Dgv_CellContentClick;

            var panelGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0) };
            panelGrid.Controls.Add(_dgv);

            Controls.Add(panelGrid);
            Controls.Add(barraSuperior);
        }

        public void Refrescar() => CargarDatos();

        private void ConfigurarColumnas()
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo de habitación", Width = 220 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hora", HeaderText = "Precio por dia", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Adicional", HeaderText = "Precio adicional", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 100 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modificacion", HeaderText = "Última modificación", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEditar, HeaderText = "", Text = "Editar", UseColumnTextForButtonValue = true, Width = 80, FlatStyle = FlatStyle.Flat });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEstado, HeaderText = "", UseColumnTextForButtonValue = false, Width = 110, FlatStyle = FlatStyle.Flat });
        }

        private void CargarDatos()
        {
            _tarifas = _gestionTarifas.ObtenerTarifas();
            _dgv.Rows.Clear();

            foreach (Tarifa tarifa in _tarifas)
            {
                int fila = _dgv.Rows.Add(
                    tarifa.TipoHabitacionNombre,
                    tarifa.PrecioPorHora.ToString("C0"),
                    tarifa.PrecioAdicional.ToString("C0"),
                    tarifa.Activa ? "Activa" : "Inactiva",
                    tarifa.UltimaModificacion.ToString("dd/MM/yyyy HH:mm"));

                _dgv.Rows[fila].Cells[ColEstado].Value = tarifa.Activa ? "Desactivar" : "Activar";
                _dgv.Rows[fila].Cells["Estado"].Style.ForeColor = tarifa.Activa ? Paleta.Exito : Paleta.Peligro;
            }
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _tarifas.Count) return;

            Tarifa tarifa = _tarifas[e.RowIndex];
            string columna = _dgv.Columns[e.ColumnIndex].Name;

            if (columna == ColEditar)
            {
                AbrirModal(tarifa);
            }
            else if (columna == ColEstado)
            {
                try
                {
                    _gestionTarifas.CambiarEstado(tarifa.IdTarifa, !tarifa.Activa);
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AbrirModal(Tarifa? tarifa)
        {
            using var modal = new FModalTarifa(tarifa);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                CargarDatos();
            }
        }
    }
}
