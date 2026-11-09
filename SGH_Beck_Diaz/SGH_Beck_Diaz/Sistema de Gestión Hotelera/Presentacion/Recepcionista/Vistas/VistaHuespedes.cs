using Datos;
using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.Modales;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>Búsqueda, filtrado multi-criterio, alta y edición de huéspedes con visualización responsive.</summary>
    internal class VistaHuespedes : UserControl
    {
        private readonly GestionHuespedes _gestionHuespedes = new();
        private readonly GestionHospedajes _gestionHospedajes = new();
        private readonly HabitacionesDAO _habitacionesDAO = new();

        private readonly TextBox _txtBuscar;
        private readonly ComboBox _cmbEstado;
        private readonly ComboBox _cmbHabitacion;
        private readonly DataGridView _grilla;

        public VistaHuespedes()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            // Panel superior con título y barra de filtros responsive
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 0, 0, 12)
            };

            var lblTitulo = new Label
            {
                Text = "Huéspedes",
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            // Contenedor de Filtros de Búsqueda
            var panelAcciones = new FlowLayoutPanel
            {
                Location = new Point(0, 34),
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                BackColor = Paleta.FondoApp
            };

            // 1. Campo de texto libre (DNI, Nombre, Apellido, Correo)
            _txtBuscar = CamposFormulario.Texto(Point.Empty, 220);
            _txtBuscar.Margin = new Padding(0, 0, 10, 0);

            // 2. Selector de Estado de Estadía
            _cmbEstado = CamposFormulario.Combo(Point.Empty, 160);
            _cmbEstado.Items.AddRange(new object[] { "Todos", "Alojado", "Sin estadía activa" });
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

            // 3. Dropdown de Habitaciones (Cargado dinámicamente desde la BD)
            _cmbHabitacion = CamposFormulario.Combo(Point.Empty, 110);
            CargarHabitacionesEnCombo();
            _cmbHabitacion.Margin = new Padding(0, 0, 4, 0);

            var lblHabitacion = new Label
            {
                Text = "Habitación",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            // 4. Botones
            var btnBuscar = EstiloBoton.Secundario(new Button
            {
                Text = "Filtrar",
                Size = new Size(90, 34),
                Margin = new Padding(0, 0, 6, 0)
            });
            btnBuscar.Click += (s, e) => Refrescar();

            var btnLimpiar = EstiloBoton.Secundario(new Button
            {
                Text = "Limpiar filtros",
                Size = new Size(110, 34),
                Margin = new Padding(0, 0, 15, 0)
            });
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            var btnRegistrar = EstiloBoton.Primario(new Button
            {
                Text = "Registrar huésped",
                Size = new Size(160, 34)
            });
            btnRegistrar.Click += (s, e) => RegistrarHuesped();

            // Eventos para auto-filtrar de forma inmediata
            _txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Refrescar(); };
            _cmbEstado.SelectedIndexChanged += (s, e) => Refrescar();
            _cmbHabitacion.SelectedIndexChanged += (s, e) => Refrescar();

            panelAcciones.Controls.Add(_txtBuscar);
            panelAcciones.Controls.Add(_cmbEstado);
            panelAcciones.Controls.Add(lblEstado);
            panelAcciones.Controls.Add(_cmbHabitacion);
            panelAcciones.Controls.Add(lblHabitacion);
            panelAcciones.Controls.Add(btnBuscar);
            panelAcciones.Controls.Add(btnLimpiar);
            panelAcciones.Controls.Add(btnRegistrar);

            panelSuperior.Controls.Add(lblTitulo);
            panelSuperior.Controls.Add(panelAcciones);

            // Contenedor principal de la grilla (Dock = Fill)
            var panelGrilla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 6, 0, 0)
            };

            _grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both,
                AutoGenerateColumns = false
            };
            EstiloGrid.Aplicar(_grilla);

            ConfigurarColumnas();

            _grilla.CellContentClick += Grilla_CellContentClick;
            _grilla.CellClick += Grilla_CellContentClick;
            panelGrilla.Controls.Add(_grilla);

            Controls.Add(panelGrilla);
            Controls.Add(panelSuperior);

            Refrescar();
        }

        private void CargarHabitacionesEnCombo()
        {
            _cmbHabitacion.Items.Clear();
            _cmbHabitacion.Items.Add("Todas");

            List<Habitacion> habitaciones = _habitacionesDAO.ListarHabitaciones();
            foreach (var hab in habitaciones)
            {
                _cmbHabitacion.Items.Add(hab.NroHabitacion.ToString());
            }

            _cmbHabitacion.SelectedIndex = 0;
        }

        private void ConfigurarColumnas()
        {
            _grilla.Columns.Clear();

            // 1. DNI
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "dni",
                HeaderText = "DNI",
                FillWeight = 8,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 2. Nombre
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "nombre",
                HeaderText = "Nombre",
                FillWeight = 11,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 3. Apellido
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "apellido",
                HeaderText = "Apellido",
                FillWeight = 11,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 4. Teléfono
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "telefono",
                HeaderText = "Teléfono",
                FillWeight = 10,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 5. Correo
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "correo",
                HeaderText = "Correo",
                FillWeight = 18,
                MinimumWidth = 140,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 6. Dirección
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "direccion",
                HeaderText = "Dirección",
                FillWeight = 16,
                MinimumWidth = 120,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 7. Habitación
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "habitacionActual",
                HeaderText = "Habitación",
                FillWeight = 8,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 8. Estado
            _grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "estadoEstadia",
                HeaderText = "Estado",
                FillWeight = 11,
                MinimumWidth = 115,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // 9. Botón Editar
            _grilla.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "btnEditar",
                HeaderText = "Acciones",
                Text = "Editar",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 7,
                MinimumWidth = 75,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle =
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(6, 4, 6, 4)
                }
            });
        }

        public void Refrescar()
        {
            string termino = _txtBuscar.Text.Trim();
            string estado = _cmbEstado.SelectedItem?.ToString() ?? "Todos";

            int? nroHabitacion = null;
            if (_cmbHabitacion.SelectedItem != null &&
                int.TryParse(_cmbHabitacion.SelectedItem.ToString(), out int nro))
            {
                nroHabitacion = nro;
            }

            var huespedes = _gestionHuespedes.BuscarFiltrado(termino, estado, nroHabitacion);

            _grilla.Rows.Clear();
            foreach (Huesped huesped in huespedes)
            {
                Hospedaje? estadia = _gestionHospedajes.ObtenerEstadiaActual(huesped.DniHuesped);
                bool alojado = estadia != null;

                int rowIndex = _grilla.Rows.Add(
                    huesped.DniHuesped,
                    huesped.Nombre,
                    huesped.Apellido,
                    huesped.Telefono,
                    huesped.Correo,
                    huesped.Direccion,
                    alojado ? estadia!.NroHabitacion.ToString() : "-",
                    alojado ? "Alojado" : "Sin estadía activa");

                var fila = _grilla.Rows[rowIndex];

                // Estilo para celda de Estado
                var celdaEstado = fila.Cells["estadoEstadia"];
                if (alojado)
                {
                    celdaEstado.Style.ForeColor = Paleta.Exito;
                    celdaEstado.Style.SelectionForeColor = Paleta.Exito;
                    celdaEstado.Style.Font = Paleta.FuenteBaseNegrita;
                }
                else
                {
                    celdaEstado.Style.ForeColor = Paleta.TextoTerciario;
                    celdaEstado.Style.SelectionForeColor = Paleta.TextoTerciario;
                }

                // Destacar número de habitación
                if (alojado)
                {
                    fila.Cells["habitacionActual"].Style.Font = Paleta.FuenteBaseNegrita;
                }
            }
        }

        private void LimpiarFiltros()
        {
            _txtBuscar.Clear();
            _cmbEstado.SelectedIndex = 0;
            _cmbHabitacion.SelectedIndex = 0;
            Refrescar();
        }

        private void Grilla_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && _grilla.Columns[e.ColumnIndex].Name == "btnEditar")
            {
                string? dni = _grilla.Rows[e.RowIndex].Cells["dni"].Value?.ToString();

                if (!string.IsNullOrEmpty(dni))
                {
                    ModificarHuesped(dni);
                }
            }
        }

        private void RegistrarHuesped()
        {
            using var modal = new FModalRegistrarHuesped();
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarHabitacionesEnCombo();
                Refrescar();
            }
        }

        private void ModificarHuesped(string dniHuesped)
        {
            Huesped? huesped = HuespedDAO.ObtenerPorDni(dniHuesped);

            if (huesped != null)
            {
                using var modal = new FModalRegistrarHuesped(huesped);
                if (modal.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    CargarHabitacionesEnCombo();
                    Refrescar();
                }
            }
            else
            {
                MessageBox.Show("No se encontró la información del huésped seleccionado.",
                                "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
