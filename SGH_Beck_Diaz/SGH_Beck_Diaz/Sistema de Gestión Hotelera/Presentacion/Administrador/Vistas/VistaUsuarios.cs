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
    internal class VistaUsuarios : UserControl, IVistaAdministrador
    {
        private const string ColEditar = "colEditar";
        private const string ColEstado = "colEstadoAccion";

        private readonly GestionUsuarios _gestionUsuarios = new();
        private readonly DataGridView _dgv;
        private List<Usuario> _usuarios = new();

        public VistaUsuarios()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var barraSuperior = new Panel { Dock = DockStyle.Top, Height = 48 };
            var btnNuevo = EstiloBoton.Primario(new Button { Text = "+  Nuevo usuario", Size = new Size(170, 38), Dock = DockStyle.Right });
            btnNuevo.Click += (s, e) => AbrirModal(null);
            barraSuperior.Controls.Add(btnNuevo);

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
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DniUsuario", HeaderText = "DNI", Width = 105 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NomUsuario", HeaderText = "Nombre", Width = 130 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ApeUsuario", HeaderText = "Apellido", Width = 130 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DirUsuario", HeaderText = "Dirección", Width = 170 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TelefonoUsuario", HeaderText = "Teléfono", Width = 125 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CorreoUsuario", HeaderText = "Correo", Width = 160 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rol", HeaderText = "Rol", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 90 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AltaUsuario", HeaderText = "Fecha de alta", Width = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEditar, HeaderText = "", Text = "Editar", UseColumnTextForButtonValue = true, Width = 80, FlatStyle = FlatStyle.Flat });
            _dgv.Columns.Add(new DataGridViewButtonColumn { Name = ColEstado, HeaderText = "", UseColumnTextForButtonValue = false, Width = 100, FlatStyle = FlatStyle.Flat });
        }

        private void CargarDatos()
        {
            try
            {
                _usuarios = _gestionUsuarios.ObtenerUsuarios();
                _dgv.Rows.Clear();

                foreach (Usuario usuario in _usuarios)
                {
                    string fechaAlta = usuario.AltaUsuario != default
                        ? usuario.AltaUsuario.ToString("dd/MM/yyyy HH:mm")
                        : "-";

                    int fila = _dgv.Rows.Add(
                        usuario.DniUsuario,
                        usuario.NomUsuario,
                        usuario.ApeUsuario,
                        usuario.Direccion,
                        usuario.TelefonoUsuario,
                        usuario.CorreoUsuario,
                        usuario.Rol?.NomRol ?? "-",
                        usuario.Estado ? "Activo" : "Inactivo",
                        fechaAlta);

                    _dgv.Rows[fila].Cells[ColEstado].Value = usuario.Estado ? "Desactivar" : "Activar";
                    _dgv.Rows[fila].Cells["Estado"].Style.ForeColor = usuario.Estado ? Paleta.Exito : Paleta.Peligro;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar la lista de usuarios.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _usuarios.Count) return;

            Usuario usuario = _usuarios[e.RowIndex];
            string columna = _dgv.Columns[e.ColumnIndex].Name;

            if (columna == ColEditar)
            {
                AbrirModal(usuario);
            }
            else if (columna == ColEstado)
            {
                CambiarEstado(usuario);
            }
        }

        private void AbrirModal(Usuario? usuario)
        {
            using var modal = new FModalUsuario(usuario);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                CargarDatos();
            }
        }

        private void CambiarEstado(Usuario usuario)
        {
            string accion = usuario.Estado ? "desactivar" : "activar";
            DialogResult confirmacion = MessageBox.Show(
                $"¿Confirma que desea {accion} al usuario con DNI \"{usuario.DniUsuario}\" ({usuario.ApeUsuario} {usuario.NomUsuario})?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes) return;

            try
            {
                _gestionUsuarios.CambiarEstado(usuario.DniUsuario, !usuario.Estado);
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo actualizar el estado.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}