using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Entidades;

namespace Presentacion.Administrador
{
    public partial class FHuespedes : Form
    {
        // Utiliza directamente la clase Huesped de la Capa de Entidades
        private List<Huesped> listaHuespedesDemo;

        public FHuespedes()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
        }

        private void FHuespedes_Load(object sender, EventArgs e)
        {
            ConfigurarGrilla();
            CargarDatosDemo();
        }

        private void ConfigurarGrilla()
        {
            dgvHuespedes.AutoGenerateColumns = false;
            dgvHuespedes.Columns.Clear();

            // DataPropertyName debe coincidir exactamente con el Nombre de las propiedades de tu clase Huesped
            dgvHuespedes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dni", HeaderText = "DNI / Doc.", DataPropertyName = "DniHuesped", Width = 110 });
            dgvHuespedes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Apellido", HeaderText = "Apellido", DataPropertyName = "Apellido", Width = 140 });
            dgvHuespedes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre", DataPropertyName = "Nombre", Width = 140 });
            dgvHuespedes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefono", HeaderText = "Teléfono", DataPropertyName = "Telefono", Width = 130 });
            dgvHuespedes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Direccion", HeaderText = "Dirección", DataPropertyName = "Direccion", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            dgvHuespedes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHuespedes.MultiSelect = false;
            dgvHuespedes.ReadOnly = true;
            dgvHuespedes.AllowUserToAddRows = false;
        }

        private void CargarDatosDemo()
        {
            // Instanciamos objetos utilizando directamente la Entidad Huesped
            listaHuespedesDemo = new List<Huesped>
            {
                new Huesped { DniHuesped = "38452109", Apellido = "García", Nombre = "Juan Pablo", Telefono = "3794123456", Direccion = "Av. 3 de Abril 1020" },
                new Huesped { DniHuesped = "29118402", Apellido = "López", Nombre = "María Elena", Telefono = "3794987654", Direccion = "Calle Junín 450" },
                new Huesped { DniHuesped = "41903221", Apellido = "Martínez", Nombre = "Carlos", Telefono = "3794554433", Direccion = "San Martín 1890" }
            };

            ActualizarGrilla(listaHuespedesDemo);
        }

        private void ActualizarGrilla(List<Huesped> lista)
        {
            dgvHuespedes.DataSource = null;
            dgvHuespedes.DataSource = lista;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string filtro = txtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                ActualizarGrilla(listaHuespedesDemo);
            }
            else
            {
                var filtrados = listaHuespedesDemo.Where(h =>
                    h.DniHuesped.Contains(filtro) ||
                    h.Apellido.ToLower().Contains(filtro) ||
                    h.Nombre.ToLower().Contains(filtro)
                ).ToList();

                ActualizarGrilla(filtrados);
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBuscar.Clear(); //vacía el capo de texto
            ActualizarGrilla(listaHuespedesDemo);//Y vuelve a poner los datos que estaban originalmente
        }
    }

}