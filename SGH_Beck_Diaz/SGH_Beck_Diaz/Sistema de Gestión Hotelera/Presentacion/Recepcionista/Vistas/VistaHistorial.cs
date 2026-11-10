using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.Vistas
{
    /// <summary>
    /// Histórico administrativo y de operaciones del rol Recepcionista:
    /// Auditoría unificada de check-in, check-out, prórrogas de estadía y ventas adicionales.
    /// Grilla de alta legibilidad con badges por tipo de movimiento, detalles completos y filtros ágiles.
    /// </summary>
    internal class VistaHistorial : UserControl
    {
        private readonly GestionHistorialRecepcion _gestionHistorial = new();

        // Controles de filtros
        private readonly ComboBox _cmbTipo;
        private readonly TextBox _txtHuesped;
        private readonly NumericUpDown _numHabitacion;
        private readonly TextBox _txtUsuario;
        private readonly CheckBox _chkUsarFecha;
        private readonly DateTimePicker _dtpFecha;

        // KPI chips
        private readonly Label _lblKpiTotal;
        private readonly Label _lblKpiCheckIn;
        private readonly Label _lblKpiCheckOut;
        private readonly Label _lblKpiProrroga;
        private readonly Label _lblKpiVenta;
        private readonly Label _lblKpiMontoTotal;

        // Grilla y contenedor
        private readonly Label _lblContadorFilas;
        private readonly DataGridView _grilla;
        private readonly Panel _panelEmptyState;

        // Columnas
        private readonly DataGridViewTextBoxColumn _colFecha;
        private readonly DataGridViewTextBoxColumn _colHora;
        private readonly DataGridViewTextBoxColumn _colTipo;
        private readonly DataGridViewTextBoxColumn _colHuesped;
        private readonly DataGridViewTextBoxColumn _colDni;
        private readonly DataGridViewTextBoxColumn _colHabitacion;
        private readonly DataGridViewTextBoxColumn _colDescripcion;
        private readonly DataGridViewTextBoxColumn _colUsuario;
        private readonly DataGridViewTextBoxColumn _colMonto;

        public VistaHistorial()
        {
            Dock = DockStyle.Fill;
            BackColor = Paleta.FondoApp;

            // ==========================================
            // 1. PANEL SUPERIOR (Título + Filtros + KPIs)
            // ==========================================
            var panelSuperior = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Paleta.FondoApp,
                Padding = new Padding(0, 0, 0, 10)
            };

            // Título y subtítulo
            var panelTitulos = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            var lblTitulo = new Label
            {
                Text = "Histórico Administrativo de Operaciones",
                Font = Paleta.FuenteTitulo,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var lblSubtitulo = new Label
            {
                Text = "Registro y auditoría en tiempo real de check-in, check-out, prórrogas y ventas adicionales",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Location = new Point(1, 26)
            };

            panelTitulos.Controls.Add(lblTitulo);
            panelTitulos.Controls.Add(lblSubtitulo);

            // Tarjeta de Filtros
            var cardFiltros = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Paleta.FondoTarjeta,
                Padding = new Padding(14, 10, 14, 12),
                Margin = new Padding(0, 4, 0, 8)
            };
            cardFiltros.Paint += (s, e) =>
            {
                using var lapiz = new Pen(Paleta.Borde, 1f);
                e.Graphics.DrawRectangle(lapiz, 0, 0, cardFiltros.Width - 1, cardFiltros.Height - 1);
            };

            var flowFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent
            };

            // 1. Tipo
            var grpTipo = CrearGrupoFiltro("Tipo de operación", _cmbTipo = CamposFormulario.Combo(Point.Empty, 130));
            _cmbTipo.Items.AddRange(new object[] { "Todas", "Check-in", "Check-out", "Prórroga", "Venta" });
            _cmbTipo.SelectedIndex = 0;

            // 2. Huésped / DNI
            var grpHuesped = CrearGrupoFiltro("Huésped o DNI", _txtHuesped = CamposFormulario.Texto(Point.Empty, 160));
            _txtHuesped.PlaceholderText = "Nombre o documento...";

            // 3. Habitación
            var grpHabitacion = CrearGrupoFiltro("Habitación", _numHabitacion = CamposFormulario.Numerico(Point.Empty, 85, 0, 999, 0));

            // 4. Operador / Usuario
            var grpUsuario = CrearGrupoFiltro("Operador / Usuario", _txtUsuario = CamposFormulario.Texto(Point.Empty, 140));
            _txtUsuario.PlaceholderText = "Nombre recepcionista...";

            // 5. Fecha
            _chkUsarFecha = new CheckBox
            {
                Text = "Filtrar por fecha",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };
            _dtpFecha = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 115,
                Height = 32,
                Enabled = false,
                Font = Paleta.FuenteBase
            };
            _chkUsarFecha.CheckedChanged += (s, e) => _dtpFecha.Enabled = _chkUsarFecha.Checked;

            var grpFecha = new Panel { AutoSize = true, Margin = new Padding(0, 0, 14, 4) };
            grpFecha.Controls.Add(_chkUsarFecha);
            grpFecha.Controls.Add(_dtpFecha);
            _dtpFecha.Location = new Point(0, 20);

            // Botones de acción
            var pnlBotones = new Panel { AutoSize = true, Margin = new Padding(0, 18, 0, 4) };
            var btnBuscar = EstiloBoton.Primario(new Button
            {
                Text = "Filtrar",
                Size = new Size(88, 33),
                Location = new Point(0, 0)
            });
            btnBuscar.Click += (s, e) => Refrescar();

            var btnLimpiar = EstiloBoton.Secundario(new Button
            {
                Text = "Limpiar",
                Size = new Size(88, 33),
                Location = new Point(94, 0)
            });
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            pnlBotones.Controls.Add(btnBuscar);
            pnlBotones.Controls.Add(btnLimpiar);

            // Eventos inmediatos
            _txtHuesped.TextChanged += (s, e) => Refrescar();
            _txtHuesped.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Refrescar(); };
            _txtUsuario.TextChanged += (s, e) => Refrescar();
            _txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Refrescar(); };
            _cmbTipo.SelectedIndexChanged += (s, e) => Refrescar();
            _numHabitacion.ValueChanged += (s, e) => Refrescar();
            _dtpFecha.ValueChanged += (s, e) => { if (_chkUsarFecha.Checked) Refrescar(); };

            flowFiltros.Controls.Add(grpTipo);
            flowFiltros.Controls.Add(grpHuesped);
            flowFiltros.Controls.Add(grpHabitacion);
            flowFiltros.Controls.Add(grpUsuario);
            flowFiltros.Controls.Add(grpFecha);
            flowFiltros.Controls.Add(pnlBotones);

            cardFiltros.Controls.Add(flowFiltros);

            // Barra de KPI Chips
            var pnlKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 2)
            };

            pnlKpis.Controls.Add(CrearKpiChip("Total operaciones", out _lblKpiTotal, Paleta.TextoPrimario, Paleta.FondoTarjeta));
            pnlKpis.Controls.Add(CrearKpiChip("Check-in", out _lblKpiCheckIn, Paleta.Exito, Paleta.ExitoSuave));
            pnlKpis.Controls.Add(CrearKpiChip("Check-out", out _lblKpiCheckOut, Paleta.Peligro, Paleta.PeligroSuave));
            pnlKpis.Controls.Add(CrearKpiChip("Prórrogas", out _lblKpiProrroga, Color.FromArgb(180, 83, 9), Paleta.AdvertenciaSuave));
            pnlKpis.Controls.Add(CrearKpiChip("Ventas", out _lblKpiVenta, Paleta.Primario, Paleta.PrimarioSuave));
            pnlKpis.Controls.Add(CrearKpiChip("Total facturado", out _lblKpiMontoTotal, Paleta.Exito, Paleta.FondoTarjeta, esMonto: true));

            panelSuperior.Controls.Add(pnlKpis);
            panelSuperior.Controls.Add(cardFiltros);
            panelSuperior.Controls.Add(panelTitulos);

            // ==========================================
            // 2. PANEL DE GRILLA (Card blanco contenedora)
            // ==========================================
            var cardGrilla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.FondoTarjeta,
                Padding = new Padding(1)
            };
            cardGrilla.Paint += (s, e) =>
            {
                using var lapiz = new Pen(Paleta.Borde, 1f);
                e.Graphics.DrawRectangle(lapiz, 0, 0, cardGrilla.Width - 1, cardGrilla.Height - 1);
            };

            // Cabecera de la tarjeta con título del listado y totalizador
            var headerCardGrilla = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(249, 250, 252),
                Padding = new Padding(14, 0, 14, 0)
            };
            headerCardGrilla.Paint += (s, e) =>
            {
                using var lapiz = new Pen(Paleta.BordeSuave, 1f);
                e.Graphics.DrawLine(lapiz, 0, headerCardGrilla.Height - 1, headerCardGrilla.Width, headerCardGrilla.Height - 1);
            };

            var lblTituloGrilla = new Label
            {
                Text = "Detalle cronológico de movimientos",
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true,
                Location = new Point(14, 10)
            };

            _lblContadorFilas = new Label
            {
                Text = "0 registros",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(headerCardGrilla.Width - 130, 11)
            };
            headerCardGrilla.Resize += (s, e) =>
            {
                _lblContadorFilas.Location = new Point(headerCardGrilla.Width - _lblContadorFilas.Width - 16, 11);
            };

            headerCardGrilla.Controls.Add(lblTituloGrilla);
            headerCardGrilla.Controls.Add(_lblContadorFilas);

            // DataGridView principal
            _grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Paleta.FondoTarjeta,
                ShowCellToolTips = true,
                AllowUserToResizeColumns = true,
                AllowUserToOrderColumns = true
            };
            HabilitarDoubleBuffer(_grilla);
            EstiloGrid.Aplicar(_grilla);
            _grilla.AllowUserToResizeColumns = true; // Restaurar permiso de resize que EstiloGrid desactiva

            // Definición de columnas de alta legibilidad
            _colFecha = new DataGridViewTextBoxColumn
            {
                Name = "fecha",
                HeaderText = "Fecha",
                FillWeight = 8,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 4, 0, 4) }
            };

            _colHora = new DataGridViewTextBoxColumn
            {
                Name = "hora",
                HeaderText = "Hora",
                FillWeight = 6,
                MinimumWidth = 65,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(0, 4, 0, 4) }
            };

            _colTipo = new DataGridViewTextBoxColumn
            {
                Name = "tipo",
                HeaderText = "Tipo",
                FillWeight = 10,
                MinimumWidth = 105,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            _colHuesped = new DataGridViewTextBoxColumn
            {
                Name = "huesped",
                HeaderText = "Huésped",
                FillWeight = 16,
                MinimumWidth = 135,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(8, 4, 4, 4), Font = Paleta.FuenteBaseNegrita }
            };

            _colDni = new DataGridViewTextBoxColumn
            {
                Name = "dni",
                HeaderText = "DNI",
                FillWeight = 9,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Paleta.TextoSecundario }
            };

            _colHabitacion = new DataGridViewTextBoxColumn
            {
                Name = "habitacion",
                HeaderText = "Habitación",
                FillWeight = 8,
                MinimumWidth = 80,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            _colDescripcion = new DataGridViewTextBoxColumn
            {
                Name = "descripcion",
                HeaderText = "Descripción / Detalle",
                FillWeight = 26,
                MinimumWidth = 190,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(8, 4, 8, 4) }
            };

            _colUsuario = new DataGridViewTextBoxColumn
            {
                Name = "usuario",
                HeaderText = "Operador",
                FillWeight = 10,
                MinimumWidth = 95,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(6, 4, 4, 4), ForeColor = Paleta.TextoSecundario }
            };

            _colMonto = new DataGridViewTextBoxColumn
            {
                Name = "monto",
                HeaderText = "Monto",
                FillWeight = 8,
                MinimumWidth = 85,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight, Padding = new Padding(0, 0, 10, 0) } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Padding = new Padding(0, 4, 10, 4), Font = Paleta.FuenteBaseNegrita }
            };

            _grilla.Columns.AddRange(new DataGridViewColumn[]
            {
                _colFecha,
                _colHora,
                _colTipo,
                _colHuesped,
                _colDni,
                _colHabitacion,
                _colDescripcion,
                _colUsuario,
                _colMonto
            });

            // Pintado personalizado de celdas (Badges de Tipo y Habitación)
            _grilla.CellPainting += Grilla_CellPainting;

            // Empty state overlay
            _panelEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Paleta.FondoTarjeta,
                Visible = false
            };
            var lblEmptyTitulo = new Label
            {
                Text = "No se encontraron operaciones",
                Font = Paleta.FuenteSeccion,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = true
            };
            var lblEmptySub = new Label
            {
                Text = "Probá ajustando o limpiando los filtros para ver otros movimientos.",
                Font = Paleta.FuenteBase,
                ForeColor = Paleta.TextoSecundario,
                AutoSize = true
            };
            var btnEmptyLimpiar = EstiloBoton.Secundario(new Button
            {
                Text = "Limpiar filtros",
                Size = new Size(130, 34)
            });
            btnEmptyLimpiar.Click += (s, e) => LimpiarFiltros();

            _panelEmptyState.Controls.Add(lblEmptyTitulo);
            _panelEmptyState.Controls.Add(lblEmptySub);
            _panelEmptyState.Controls.Add(btnEmptyLimpiar);

            _panelEmptyState.Resize += (s, e) =>
            {
                int cy = Math.Max(30, (_panelEmptyState.Height - 120) / 2);
                lblEmptyTitulo.Location = new Point((_panelEmptyState.Width - lblEmptyTitulo.Width) / 2, cy);
                lblEmptySub.Location = new Point((_panelEmptyState.Width - lblEmptySub.Width) / 2, cy + 32);
                btnEmptyLimpiar.Location = new Point((_panelEmptyState.Width - btnEmptyLimpiar.Width) / 2, cy + 68);
            };

            var panelCuerpoGrilla = new Panel { Dock = DockStyle.Fill };
            panelCuerpoGrilla.Controls.Add(_panelEmptyState);
            panelCuerpoGrilla.Controls.Add(_grilla);

            cardGrilla.Controls.Add(panelCuerpoGrilla);
            cardGrilla.Controls.Add(headerCardGrilla);

            // Agregar al control principal
            Controls.Add(cardGrilla);
            Controls.Add(panelSuperior);

            Refrescar();
        }

        private static Panel CrearGrupoFiltro(string etiqueta, Control control)
        {
            var pnl = new Panel { AutoSize = true, Margin = new Padding(0, 0, 12, 4) };
            var lbl = new Label
            {
                Text = etiqueta,
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            control.Location = new Point(0, 18);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
            return pnl;
        }

        private static Panel CrearKpiChip(string titulo, out Label lblValor, Color colorTexto, Color colorFondo, bool esMonto = false)
        {
            var panel = new Panel
            {
                Height = 34,
                AutoSize = true,
                BackColor = colorFondo,
                Margin = new Padding(0, 0, 8, 4),
                Padding = new Padding(10, 6, 12, 6)
            };
            panel.Paint += (s, e) =>
            {
                using var lapiz = new Pen(Paleta.Borde, 1f);
                e.Graphics.DrawRectangle(lapiz, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var lblTit = new Label
            {
                Text = titulo + ":",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            var valorLabel = new Label
            {
                Text = esMonto ? "$ 0,00" : "0",
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = colorTexto,
                AutoSize = true,
                Location = new Point(lblTit.Right + 4, 7)
            };
            lblValor = valorLabel;

            panel.Controls.Add(lblTit);
            panel.Controls.Add(valorLabel);

            valorLabel.TextChanged += (s, e) =>
            {
                valorLabel.Left = lblTit.Right + 4;
                panel.Width = valorLabel.Right + 12;
            };

            return panel;
        }

        private void Grilla_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. Badge para Tipo de Operación
            if (e.ColumnIndex == _colTipo.Index && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);

                string tipo = e.Value.ToString() ?? "";
                Color colorFondo;
                Color colorBorde;
                Color colorTexto;

                switch (tipo)
                {
                    case "Check-in":
                        colorFondo = Paleta.ExitoSuave;
                        colorBorde = Color.FromArgb(167, 243, 208);
                        colorTexto = Paleta.Exito;
                        break;
                    case "Check-out":
                        colorFondo = Paleta.PeligroSuave;
                        colorBorde = Color.FromArgb(254, 202, 202);
                        colorTexto = Paleta.Peligro;
                        break;
                    case "Prórroga":
                        colorFondo = Paleta.AdvertenciaSuave;
                        colorBorde = Color.FromArgb(253, 230, 138);
                        colorTexto = Color.FromArgb(180, 83, 9);
                        break;
                    case "Venta":
                        colorFondo = Paleta.PrimarioSuave;
                        colorBorde = Color.FromArgb(191, 219, 254);
                        colorTexto = Paleta.Primario;
                        break;
                    default:
                        colorFondo = Color.FromArgb(243, 244, 246);
                        colorBorde = Color.FromArgb(229, 231, 235);
                        colorTexto = Paleta.TextoSecundario;
                        break;
                }

                Rectangle r = e.CellBounds;
                int badgeAncho = Math.Min(r.Width - 14, 94);
                int badgeAlto = 24;
                int x = r.X + (r.Width - badgeAncho) / 2;
                int y = r.Y + (r.Height - badgeAlto) / 2;
                var badgeRect = new RectangleF(x, y, badgeAncho, badgeAlto);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = DibujoUtil.RutaRedondeada(badgeRect, 10))
                {
                    using var brush = new SolidBrush(colorFondo);
                    e.Graphics.FillPath(brush, path);

                    using var pen = new Pen(colorBorde, 1f);
                    e.Graphics.DrawPath(pen, path);
                }

                TextRenderer.DrawText(e.Graphics, tipo, Paleta.FuenteChica,
                    Rectangle.Round(badgeRect), colorTexto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
            // 2. Badge suave para Habitación
            else if (e.ColumnIndex == _colHabitacion.Index && e.Value != null)
            {
                string hab = e.Value.ToString() ?? "";
                if (hab != "-" && !string.IsNullOrWhiteSpace(hab))
                {
                    e.PaintBackground(e.ClipBounds, true);

                    Rectangle r = e.CellBounds;
                    int badgeAncho = Math.Min(r.Width - 16, 56);
                    int badgeAlto = 22;
                    int x = r.X + (r.Width - badgeAncho) / 2;
                    int y = r.Y + (r.Height - badgeAlto) / 2;
                    var badgeRect = new RectangleF(x, y, badgeAncho, badgeAlto);

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = DibujoUtil.RutaRedondeada(badgeRect, 6))
                    {
                        using var brush = new SolidBrush(Color.FromArgb(241, 245, 249));
                        e.Graphics.FillPath(brush, path);

                        using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
                        e.Graphics.DrawPath(pen, path);
                    }

                    TextRenderer.DrawText(e.Graphics, hab, Paleta.FuenteBaseNegrita,
                        Rectangle.Round(badgeRect), Paleta.TextoPrimario,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    e.Handled = true;
                }
            }
            // 3. Monto formateado
            else if (e.ColumnIndex == _colMonto.Index && e.Value != null)
            {
                string montoStr = e.Value.ToString() ?? "";
                if (montoStr != "-" && !string.IsNullOrWhiteSpace(montoStr))
                {
                    e.CellStyle.ForeColor = Paleta.Exito;
                    e.CellStyle.SelectionForeColor = Paleta.Exito;
                }
                else
                {
                    e.CellStyle.ForeColor = Paleta.TextoTerciario;
                    e.CellStyle.SelectionForeColor = Paleta.TextoTerciario;
                }
            }
        }

        public void Refrescar()
        {
            DateTime? fecha = _chkUsarFecha.Checked ? _dtpFecha.Value.Date : null;
            int? nroHabitacion = _numHabitacion.Value > 0 ? (int)_numHabitacion.Value : null;
            string? tipoFiltro = _cmbTipo.SelectedItem?.ToString();
            if (string.Equals(tipoFiltro, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                tipoFiltro = null;
            }

            var operaciones = _gestionHistorial.Obtener(
                fecha: fecha,
                huesped: string.IsNullOrWhiteSpace(_txtHuesped.Text) ? null : _txtHuesped.Text.Trim(),
                nroHabitacion: nroHabitacion,
                usuario: string.IsNullOrWhiteSpace(_txtUsuario.Text) ? null : _txtUsuario.Text.Trim(),
                tipo: tipoFiltro);

            _grilla.Rows.Clear();

            int totalCheckIn = 0;
            int totalCheckOut = 0;
            int totalProrroga = 0;
            int totalVenta = 0;
            decimal totalFacturado = 0m;

            foreach (OperacionHistorial operacion in operaciones)
            {
                string strMonto = "-";
                if (operacion.Monto.HasValue)
                {
                    totalFacturado += operacion.Monto.Value;
                    strMonto = operacion.Monto.Value.ToString("C2", new CultureInfo("es-AR"));
                }

                switch (operacion.Tipo)
                {
                    case "Check-in": totalCheckIn++; break;
                    case "Check-out": totalCheckOut++; break;
                    case "Prórroga": totalProrroga++; break;
                    case "Venta": totalVenta++; break;
                }

                int rowIndex = _grilla.Rows.Add(
                    operacion.Fecha.ToString("dd/MM/yyyy"),
                    operacion.Hora.ToString(@"hh\:mm"),
                    operacion.Tipo,
                    string.IsNullOrWhiteSpace(operacion.Huesped) ? "-" : operacion.Huesped,
                    string.IsNullOrWhiteSpace(operacion.DniHuesped) ? "-" : operacion.DniHuesped,
                    operacion.NroHabitacion.HasValue ? operacion.NroHabitacion.Value.ToString() : "-",
                    operacion.Descripcion,
                    operacion.Usuario,
                    strMonto);

                // Tooltip enriquecido para celdas de texto largo
                _grilla.Rows[rowIndex].Cells[_colDescripcion.Index].ToolTipText = operacion.Descripcion;
            }

            // Actualizar KPIs
            _lblKpiTotal.Text = operaciones.Count.ToString("N0", new CultureInfo("es-AR"));
            _lblKpiCheckIn.Text = totalCheckIn.ToString();
            _lblKpiCheckOut.Text = totalCheckOut.ToString();
            _lblKpiProrroga.Text = totalProrroga.ToString();
            _lblKpiVenta.Text = totalVenta.ToString();
            _lblKpiMontoTotal.Text = totalFacturado.ToString("C2", new CultureInfo("es-AR"));

            // Actualizar contador de grilla
            _lblContadorFilas.Text = $"{operaciones.Count} registro{(operaciones.Count == 1 ? "" : "s")} encontrado{(operaciones.Count == 1 ? "" : "s")}";

            // Empty state
            _panelEmptyState.Visible = operaciones.Count == 0;
        }

        private void LimpiarFiltros()
        {
            _cmbTipo.SelectedIndex = 0;
            _txtHuesped.Clear();
            _numHabitacion.Value = 0;
            _txtUsuario.Clear();
            _chkUsarFecha.Checked = false;
            _dtpFecha.Value = DateTime.Now;
            _dtpFecha.Enabled = false;

            Refrescar();
        }

        private static void HabilitarDoubleBuffer(DataGridView dgv)
        {
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                    null, dgv, new object[] { true });
            }
            catch
            {
                // Ignorar si el runtime de Windows Forms no lo permite por reflection
            }
        }
    }
}
