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
        private readonly ComboBox _cmbRol;
        private readonly ComboBox _cmbEstado;
        private readonly TextBox _txtBuscar;
        private List<Usuario> _usuarios = new();

        // Usuarios que se muestran en la grilla (los que pasan los filtros), en el mismo orden
        // que las filas: los botones Editar / Activar toman el usuario de esta lista por índice de fila.
        private List<Usuario> _usuariosVisibles = new();

        public VistaUsuarios()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            // Panel superior con la barra de filtros (mismo formato que el Historial de Recepción)
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Paleta.FondoApp
            };

            var panelFiltros = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                BackColor = Paleta.FondoApp
            };

            // 1. Selector de Rol
            _cmbRol = CamposFormulario.Combo(Point.Empty, 140);
            _cmbRol.Items.Add("Todos los roles");
            try
            {
                foreach (Rol rol in _gestionUsuarios.ObtenerRoles())
                {
                    _cmbRol.Items.Add(rol.NomRol);
                }
            }
            catch
            {
                // Sin roles cargados, el filtro queda solo con "Todos los roles".
            }
            _cmbRol.SelectedIndex = 0;
            _cmbRol.Margin = new Padding(0, 0, 10, 0);

            // 2. Selector de Estado
            _cmbEstado = CamposFormulario.Combo(Point.Empty, 110);
            _cmbEstado.Items.AddRange(new object[] { "Todos", "Activo", "Inactivo" });
            _cmbEstado.SelectedIndex = 0;
            _cmbEstado.Margin = new Padding(0, 0, 4, 0);
            var lblEstado = new Label
            {
                Text = "Estado",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 3. Filtro por Nombre / DNI (también busca por apellido, correo, teléfono o rol)
            _txtBuscar = CamposFormulario.Texto(Point.Empty, 190);
            _txtBuscar.Margin = new Padding(0, 0, 4, 0);
            var lblBuscar = new Label
            {
                Text = "Nombre / DNI",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 4. Botones de Acción
            var btnFiltrar = EstiloBoton.Primario(new Button
            {
                Text = "Filtrar",
                Size = new Size(90, 34),
                Margin = new Padding(0, 0, 6, 0)
            });
            btnFiltrar.Click += (s, e) => MostrarUsuarios();

            var btnLimpiar = EstiloBoton.Secundario(new Button
            {
                Text = "Limpiar filtros",
                Size = new Size(110, 34),
                Margin = new Padding(0, 0, 15, 0)
            });
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            var btnNuevo = EstiloBoton.Primario(new Button { Text = "+  Nuevo usuario", Size = new Size(170, 34) });
            btnNuevo.Click += (s, e) => AbrirModal(null);

            // Eventos para auto-filtrar de forma inmediata
            _txtBuscar.TextChanged += (s, e) => MostrarUsuarios();
            _txtBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) MostrarUsuarios();
                if (e.KeyCode == Keys.Escape) _txtBuscar.Clear();
            };
            _cmbRol.SelectedIndexChanged += (s, e) => MostrarUsuarios();
            _cmbEstado.SelectedIndexChanged += (s, e) => MostrarUsuarios();

            panelFiltros.Controls.Add(_cmbRol);
            panelFiltros.Controls.Add(_cmbEstado);
            panelFiltros.Controls.Add(lblEstado);
            panelFiltros.Controls.Add(_txtBuscar);
            panelFiltros.Controls.Add(lblBuscar);
            panelFiltros.Controls.Add(btnFiltrar);
            panelFiltros.Controls.Add(btnLimpiar);
            panelFiltros.Controls.Add(btnNuevo);

            panelSuperior.Controls.Add(panelFiltros);

            _dgv = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();
            _dgv.CellContentClick += Dgv_CellContentClick;

            var panelGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0) };
            panelGrid.Controls.Add(_dgv);

            Controls.Add(panelGrid);
            Controls.Add(panelSuperior);
        }

        private void LimpiarFiltros()
        {
            _cmbRol.SelectedIndex = 0;
            _cmbEstado.SelectedIndex = 0;
            _txtBuscar.Clear();

            MostrarUsuarios();
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
                MostrarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cargar la lista de usuarios.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Carga en la grilla los usuarios que pasan todos los filtros (rol, estado y texto).</summary>
        private void MostrarUsuarios()
        {
            string termino = Normalizar(_txtBuscar.Text);
            string? rol = _cmbRol.SelectedIndex > 0 ? _cmbRol.SelectedItem?.ToString() : null;
            string estado = _cmbEstado.SelectedItem?.ToString() ?? "Todos";

            _usuariosVisibles = _usuarios.FindAll(u =>
                (rol == null || string.Equals(u.Rol?.NomRol, rol, StringComparison.OrdinalIgnoreCase))
                && (estado == "Todos" || (estado == "Activo") == u.Estado)
                && (string.IsNullOrEmpty(termino) || Coincide(u, termino)));

            _dgv.Rows.Clear();

            foreach (Usuario usuario in _usuariosVisibles)
            {
                string fechaAlta = usuario.AltaUsuario != default
                    ? usuario.AltaUsuario.ToString("dd/MM/yyyy")
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

        /// <summary>True si el término aparece en el DNI, nombre, apellido, nombre completo, correo, teléfono o rol.</summary>
        private static bool Coincide(Usuario usuario, string termino)
        {
            string[] campos =
            {
                usuario.DniUsuario,
                usuario.NomUsuario,
                usuario.ApeUsuario,
                $"{usuario.NomUsuario} {usuario.ApeUsuario}",
                $"{usuario.ApeUsuario} {usuario.NomUsuario}",
                usuario.CorreoUsuario,
                usuario.TelefonoUsuario,
                usuario.Rol?.NomRol ?? string.Empty
            };

            foreach (string campo in campos)
            {
                if (Normalizar(campo).Contains(termino)) return true;
            }

            return false;
        }

        /// <summary>Minúsculas y sin tildes, para que "gomez" encuentre a "Gómez".</summary>
        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string descompuesto = texto.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var resultado = new System.Text.StringBuilder(descompuesto.Length);
            foreach (char c in descompuesto)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(c);
                }
            }

            return resultado.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _usuariosVisibles.Count) return;

            Usuario usuario = _usuariosVisibles[e.RowIndex];
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