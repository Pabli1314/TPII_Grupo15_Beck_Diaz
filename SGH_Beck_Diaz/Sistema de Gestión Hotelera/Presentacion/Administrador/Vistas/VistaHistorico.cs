using Entidades;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Logica;
using Presentacion.Administrador.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Vistas
{
    /// <summary>
    /// Histórico del Administrador, equivalente al de Gerencia pero limitado a lo que gestiona este
    /// rol: limpiezas, ocupación de habitaciones, salidas de stock y altas de usuarios. No muestra
    /// recaudación ni arqueos de caja (eso queda en el histórico de Gerencia).
    /// </summary>
    internal class VistaHistorico : UserControl, IVistaAdministrador
    {
        private const string OpcionTodos = "-- Todos --";

        private readonly GestionHistorialAdministrador _gestionHistorial = new();

        private readonly DateTimePicker _dtpDesde;
        private readonly DateTimePicker _dtpHasta;
        private readonly ComboBox _cmbUsuario;
        private readonly ComboBox _cmbTipo;
        private readonly FlowLayoutPanel _panelKpis;
        private readonly CartesianChart _chartPorTipo;
        private readonly CartesianChart _chartPorUsuario;
        private readonly DataGridView _dgv;
        private bool _usuariosCargados;

        public VistaHistorico()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            var barraFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _dtpDesde = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1), Margin = new Padding(0, 4, 8, 0) };
            _dtpHasta = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today, Margin = new Padding(0, 4, 16, 0) };

            _cmbUsuario = CamposFormulario.Combo(Point.Empty, 200);
            _cmbUsuario.Margin = new Padding(0, 2, 8, 0);

            _cmbTipo = CamposFormulario.Combo(Point.Empty, 170);
            _cmbTipo.Margin = new Padding(0, 2, 16, 0);
            _cmbTipo.Items.Add(OpcionTodos);
            _cmbTipo.Items.AddRange(GestionHistorialAdministrador.Tipos);
            _cmbTipo.SelectedIndex = 0;

            var btnFiltrar = EstiloBoton.Primario(new Button { Text = "Filtrar", Size = new Size(110, 34), Margin = new Padding(0, 0, 0, 0) });
            btnFiltrar.Click += (s, e) => CargarHistorial();

            barraFiltros.Controls.Add(CrearEtiqueta("Desde"));
            barraFiltros.Controls.Add(_dtpDesde);
            barraFiltros.Controls.Add(CrearEtiqueta("Hasta"));
            barraFiltros.Controls.Add(_dtpHasta);
            barraFiltros.Controls.Add(CrearEtiqueta("Usuario"));
            barraFiltros.Controls.Add(_cmbUsuario);
            barraFiltros.Controls.Add(CrearEtiqueta("Tipo"));
            barraFiltros.Controls.Add(_cmbTipo);
            barraFiltros.Controls.Add(btnFiltrar);

            _panelKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 132,
                Padding = new Padding(0, 8, 0, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            _chartPorTipo = CrearGrafico();
            _chartPorUsuario = CrearGrafico();

            var panelGraficos = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 280,
                ColumnCount = 2,
                RowCount = 1
            };
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelGraficos.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelGraficos.Controls.Add(EnvolverConTitulo("Actividad por tipo", _chartPorTipo), 0, 0);
            panelGraficos.Controls.Add(EnvolverConTitulo("Actividad por usuario", _chartPorUsuario), 1, 0);

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloGrid.Aplicar(_dgv);
            ConfigurarColumnas();

            var contenedorGrilla = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 0) };
            contenedorGrilla.Controls.Add(_dgv);

            Controls.Add(contenedorGrilla);
            Controls.Add(panelGraficos);
            Controls.Add(_panelKpis);
            Controls.Add(barraFiltros);
        }

        private static Label CrearEtiqueta(string texto) => new Label
        {
            Text = texto,
            AutoSize = true,
            Font = Paleta.FuenteBaseNegrita,
            ForeColor = Paleta.TextoSecundario,
            Margin = new Padding(0, 9, 6, 0)
        };

        private static CartesianChart CrearGrafico() => new CartesianChart
        {
            Dock = DockStyle.Fill,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden
        };

        private static Panel EnvolverConTitulo(string titulo, Control grafico)
        {
            var contenedor = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Paleta.FondoTarjeta, Padding = new Padding(16) };
            var lblTitulo = new Label { Text = titulo, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario, Dock = DockStyle.Top, Height = 26 };
            grafico.Dock = DockStyle.Fill;
            contenedor.Controls.Add(grafico);
            contenedor.Controls.Add(lblTitulo);
            return contenedor;
        }

        private void ConfigurarColumnas()
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Fecha), HeaderText = "Fecha", FillWeight = 12, DefaultCellStyle = { Format = "dd/MM/yyyy" } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Hora), HeaderText = "Hora", FillWeight = 8, DefaultCellStyle = { Format = @"hh\:mm" } });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Tipo), HeaderText = "Tipo", FillWeight = 14 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Descripcion), HeaderText = "Descripción", FillWeight = 40 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.NroHabitacion), HeaderText = "Habitación", FillWeight = 10 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(EventoAdministrativo.Usuario), HeaderText = "Usuario", FillWeight = 18 });
        }

        public void Refrescar()
        {
            if (!_usuariosCargados)
            {
                CargarComboUsuarios();
            }

            CargarHistorial();
        }

        private void CargarComboUsuarios()
        {
            try
            {
                var usuarios = new List<KeyValuePair<string, string>> { new(string.Empty, OpcionTodos) };
                usuarios.AddRange(_gestionHistorial.ObtenerListaUsuarios());

                _cmbUsuario.DataSource = usuarios;
                _cmbUsuario.DisplayMember = "Value";
                _cmbUsuario.ValueMember = "Key";
                _cmbUsuario.SelectedIndex = 0;
                _usuariosCargados = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de usuarios: {ex.Message}", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CargarHistorial()
        {
            if (_dtpDesde.Value.Date > _dtpHasta.Value.Date)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string? dniUsuario = _cmbUsuario.SelectedValue as string;
                string? tipo = _cmbTipo.SelectedItem as string == OpcionTodos ? null : _cmbTipo.SelectedItem as string;

                HistorialAdministradorResultado datos = _gestionHistorial.Consultar(_dtpDesde.Value, _dtpHasta.Value, dniUsuario, tipo);

                _panelKpis.Controls.Clear();
                AgregarKpi("Eventos registrados", datos.TotalEventos.ToString(), Paleta.Primario, Icono.Reloj);
                AgregarKpi("Limpiezas", datos.TotalLimpiezas.ToString(), Paleta.Limpieza, Icono.Check);
                AgregarKpi("Ocupaciones", datos.TotalOcupaciones.ToString(), Paleta.Ocupada, Icono.Cama);
                AgregarKpi("Unidades de stock salidas", datos.UnidadesSalidaStock.ToString(), Paleta.Mantenimiento, Icono.Caja);
                AgregarKpi("Altas de usuarios", datos.TotalAltasUsuarios.ToString(), Paleta.Exito, Icono.Personas);

                CargarGrafico(_chartPorTipo, datos.EventosPorTipo.Where(kvp => kvp.Value > 0), Paleta.Primario);
                CargarGrafico(_chartPorUsuario, datos.EventosPorUsuario.Take(8), Paleta.Exito);

                _dgv.DataSource = null;
                _dgv.DataSource = datos.Lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar el histórico: {ex.Message}", "Histórico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CargarGrafico(CartesianChart chart, IEnumerable<KeyValuePair<string, int>> datos, Color color)
        {
            var puntos = datos.ToList();

            chart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = puntos.Select(p => p.Value).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(color.R, color.G, color.B)),
                    MaxBarWidth = 32
                }
            };

            chart.XAxes = new[] { new Axis { Labels = puntos.Select(p => p.Key).ToArray(), LabelsRotation = 0 } };
            chart.YAxes = new[] { new Axis { MinLimit = 0, MinStep = 1 } };
        }

        private void AgregarKpi(string titulo, string valor, Color color, Icono icono)
        {
            var tarjeta = new TarjetaEstadistica { Margin = new Padding(0, 0, 16, 8) };
            tarjeta.Configurar(titulo, valor, color, icono);
            _panelKpis.Controls.Add(tarjeta);
        }
    }
}
