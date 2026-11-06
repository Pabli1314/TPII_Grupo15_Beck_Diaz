using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Presentacion.Administrador.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Gerente
{
    /// <summary>
    /// Armado común de las pantallas de Gerencia. Reutiliza el sistema visual del Administrador
    /// (Paleta, TarjetaKpi, PanelReporte, EstiloGrid) para que todo el SGH se vea igual.
    /// </summary>
    internal static class ComponentesGerencia
    {
        public const int Separacion = 16;
        public static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");

        /// <summary>Monto en pesos. El espacio es no separable para que "$" nunca quede solo al final de una línea.</summary>
        public static string Moneda(decimal valor) => valor.ToString("C0", CulturaAr).Replace(' ', ' ');
        public static string Porcentaje(double valor) => $"{valor.ToString("0.#", CulturaAr)}%";

        /// <summary>Monto abreviado para ejes de gráficos: $ 1,2 M · $ 350 k · $ 900.</summary>
        public static string MonedaCorta(double valor)
        {
            double abs = Math.Abs(valor);
            if (abs >= 1_000_000) return $"$ {(valor / 1_000_000).ToString("0.#", CulturaAr)} M";
            if (abs >= 1_000) return $"$ {(valor / 1_000).ToString("0", CulturaAr)} k";
            return $"$ {valor.ToString("0", CulturaAr)}";
        }

        /// <summary>"noviembre 2026" para nombrar el mes de un período.</summary>
        public static string NombreMes(DateTime fecha) => fecha.ToString("MMMM yyyy", CulturaAr);

        public static FlowLayoutPanel CrearRaiz() => new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Paleta.FondoApp,
            Padding = new Padding(24, 20, 24, 24)
        };

        public static Panel TituloSeccion(string titulo, string descripcion)
        {
            var panel = new Panel { Height = 46, BackColor = Paleta.FondoApp, Margin = new Padding(0, 6, 0, 4) };
            panel.Controls.Add(new Label { Text = titulo.ToUpper(CulturaAr), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f), ForeColor = Paleta.TextoSecundario, Location = new Point(0, 4) });
            panel.Controls.Add(new Label { Text = descripcion, AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, Location = new Point(0, 24) });
            return panel;
        }

        /// <summary>Encabezado de la pantalla: qué período se está mirando y contra qué se compara.</summary>
        public static (Panel Panel, Label Titulo, Label Detalle) CrearEncabezado()
        {
            var panel = new Panel { Height = 58, BackColor = Paleta.FondoApp, Margin = new Padding(0, 0, 0, 6) };
            var titulo = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 13f), ForeColor = Paleta.TextoPrimario, Location = new Point(0, 2), Text = "Cargando…" };
            var detalle = new Label { AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario, Location = new Point(1, 32) };
            panel.Controls.Add(titulo);
            panel.Controls.Add(detalle);
            return (panel, titulo, detalle);
        }

        public static FlowLayoutPanel CrearFila(params Control[] controles)
        {
            var fila = new FlowLayoutPanel { WrapContents = true, BackColor = Paleta.FondoApp, Margin = Padding.Empty };
            fila.Controls.AddRange(controles);
            return fila;
        }

        /// <summary>Reparte los controles de una fila en N columnas iguales con separación fija.</summary>
        public static void DistribuirEnFila(FlowLayoutPanel fila, int ancho, int columnas, int alto)
        {
            int n = fila.Controls.Count;
            columnas = Math.Max(1, Math.Min(columnas, n));
            int anchoCelda = (ancho - Separacion * (columnas - 1)) / columnas;

            for (int i = 0; i < n; i++)
            {
                Control c = fila.Controls[i];
                bool ultimaColumna = i % columnas == columnas - 1;
                c.Margin = new Padding(0, 0, ultimaColumna ? 0 : Separacion, Separacion);
                c.Size = new Size(anchoCelda, alto);
            }

            int filas = (int)Math.Ceiling(n / (double)columnas);
            fila.Size = new Size(ancho, filas * (alto + Separacion));
        }

        /// <summary>Ancho útil de la raíz descontando padding y la barra de desplazamiento.</summary>
        public static int AnchoUtil(FlowLayoutPanel raiz) =>
            Math.Max(480, raiz.ClientSize.Width - raiz.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2);

        public static DataGridView CrearGrilla()
        {
            var dgv = new DataGridView { AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Vertical };
            EstiloGrid.Aplicar(dgv);
            dgv.RowTemplate.Height = 38;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 40;
            return dgv;
        }

        /// <summary>Peso de cada columna en el reparto del ancho (el resto queda en 100).</summary>
        public static void PesosColumnas(DataGridView dgv, params (string Columna, float Peso)[] pesos)
        {
            foreach (var (columna, peso) in pesos)
            {
                dgv.Columns[columna]!.FillWeight = peso;
            }
        }

        /// <summary>Columnas numéricas: alineadas a la derecha, encabezado incluido.</summary>
        public static void AlinearDerecha(DataGridView dgv, params string[] columnas)
        {
            foreach (string nombre in columnas)
            {
                DataGridViewColumn col = dgv.Columns[nombre]!;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Padding = new Padding(0, 6, 12, 6);
                col.HeaderCell.Style.Padding = new Padding(0, 0, 12, 0);
            }
        }

        /// <summary>Alto de un PanelReporte que contiene una grilla, para que no sobre ni falte espacio.</summary>
        public static int AltoTabla(DataGridView dgv, int filasVisibles)
        {
            int filas = Math.Max(1, Math.Min(filasVisibles, dgv.Rows.Count));
            // 34 encabezado + 48 pie + 26 de márgenes internos del PanelReporte
            return 34 + 48 + 26 + dgv.ColumnHeadersHeight + filas * dgv.RowTemplate.Height + 4;
        }

        /// <summary>Pinta de verde o rojo una celda de variación según el signo guardado en su Tag.</summary>
        public static void ColorearVariacion(DataGridView dgv, DataGridViewCellFormattingEventArgs e, params string[] columnas)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || !columnas.Contains(dgv.Columns[e.ColumnIndex].Name)) return;
            if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is int signo && signo != 0)
            {
                e.CellStyle.ForeColor = signo > 0 ? Paleta.Exito : Paleta.Peligro;
                e.CellStyle.Font = Paleta.FuenteBaseNegrita;
            }
        }

        /// <summary>Texto de una variación relativa para tablas ("▲ 12%"), con su signo (1, -1, 0).</summary>
        public static (string Texto, int Signo) Variacion(double actual, double anterior)
        {
            if (anterior <= 0) return actual > 0 ? ("Nuevo", 0) : ("—", 0);
            double v = (actual - anterior) / anterior * 100;
            if (Math.Abs(v) < 0.5) return ("= 0%", 0);
            return ($"{(v > 0 ? "▲" : "▼")} {Math.Abs(v).ToString("0", CulturaAr)}%", Math.Sign(v));
        }

        /// <summary>Variación en puntos porcentuales para métricas que ya son %, como la ocupación.</summary>
        public static (string Texto, int Signo) VariacionPuntos(double actual, double anterior)
        {
            double d = actual - anterior;
            if (Math.Abs(d) < 0.5) return ("= 0 pts", 0);
            return ($"{(d > 0 ? "▲" : "▼")} {Math.Abs(d).ToString("0", CulturaAr)} pts", Math.Sign(d));
        }

        // ---------------- Gráficos (LiveCharts) ----------------

        public static SKColor Skia(Color c) => new(c.R, c.G, c.B, c.A);

        public static Axis EjeCategorias(IEnumerable<string> etiquetas) => new()
        {
            Labels = etiquetas.ToArray(),
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(Skia(Paleta.TextoSecundario)),
            SeparatorsPaint = null,
            ShowSeparatorLines = false
        };

        public static Axis EjeValores(Func<double, string> formato, double? maximo = null) => new()
        {
            Labeler = formato,
            MinLimit = 0,
            MaxLimit = maximo,
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(Skia(Paleta.TextoTerciario)),
            SeparatorsPaint = new SolidColorPaint(Skia(Paleta.BordeSuave)) { StrokeThickness = 1 }
        };
    }
}
