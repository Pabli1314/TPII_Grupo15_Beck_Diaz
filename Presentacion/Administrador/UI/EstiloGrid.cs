using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Administrador.UI
{
    internal static class EstiloGrid
    {
        public static void Aplicar(DataGridView dgv)
        {
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Paleta.FondoTarjeta;
            dgv.GridColor = Paleta.BordeSuave;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowTemplate.Height = 46;
            dgv.ColumnHeadersHeight = 42;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Paleta.FondoApp,
                ForeColor = Paleta.TextoSecundario,
                Font = Paleta.FuenteBaseNegrita,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Paleta.FondoApp,
                SelectionForeColor = Paleta.TextoSecundario
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Paleta.FondoTarjeta,
                ForeColor = Paleta.TextoPrimario,
                Font = Paleta.FuenteBase,
                Padding = new Padding(10, 6, 0, 6),
                SelectionBackColor = Paleta.PrimarioSuave,
                SelectionForeColor = Paleta.TextoPrimario
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 250, 252)
            };
            dgv.RowsDefaultCellStyle.SelectionBackColor = Paleta.PrimarioSuave;
            dgv.RowsDefaultCellStyle.SelectionForeColor = Paleta.TextoPrimario;
        }
    }
}
