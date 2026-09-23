using System;
using System.Drawing;
using System.Windows.Forms;
using Entidades;
using Logica;

namespace Presentacion.Administrador.Modales
{
    public class FModalDetalleHabitacion : Form
    {
        private readonly GestionHabitaciones _gestionHabitaciones = new();

        public FModalDetalleHabitacion(HabitacionResumen hab)
        {
            if (hab == null) throw new ArgumentNullException(nameof(hab));

            Text = $"Detalle de Habitación N° {hab.NroHabitacion}";
            Size = new Size(420, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            InicializarComponentes(hab);
        }

        private void InicializarComponentes(HabitacionResumen hab)
        {
            var lblTitulo = new Label
            {
                Text = $"Habitación {hab.NroHabitacion} - {hab.TipoHabitacion}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };

            bool estaOcupada = string.Equals(hab.Estado, "Ocupada", StringComparison.OrdinalIgnoreCase);

            var lblEstado = new Label
            {
                Text = $"Estado: {hab.Estado}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 42),
                ForeColor = estaOcupada ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74),
                AutoSize = true
            };

            var panelDetalle = new GroupBox
            {
                Text = estaOcupada ? "Información del Huésped" : "Información de la Habitación",
                Location = new Point(20, 75),
                Size = new Size(365, 210),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            int top = 28;

            if (estaOcupada)
            {
                // Obtenemos la entidad Huesped directamente mediante la Capa de Lógica
                Huesped? h = _gestionHabitaciones.ObtenerHuespedOcupante(hab.NroHabitacion);

                if (h != null)
                {
                    AgregarFila(panelDetalle, "DNI:", h.DniHuesped ?? "-", ref top);
                    AgregarFila(panelDetalle, "Nombre:", $"{h.Nombre} {h.Apellido}".Trim(), ref top);
                    AgregarFila(panelDetalle, "Teléfono:", string.IsNullOrWhiteSpace(h.Telefono) ? "-" : h.Telefono, ref top);
                    AgregarFila(panelDetalle, "Dirección:", string.IsNullOrWhiteSpace(h.Direccion) ? "-" : h.Direccion, ref top);
                }
                else
                {
                    AgregarFila(panelDetalle, "Huésped:", hab.Huesped ?? "S/D", ref top);
                }

                if (hab.HoraEntrada.HasValue)
                    AgregarFila(panelDetalle, "Entrada:", hab.HoraEntrada.Value.ToString("dd/MM/yyyy HH:mm"), ref top);

                if (hab.HoraSalidaEstimada.HasValue)
                    AgregarFila(panelDetalle, "Salida Est.:", hab.HoraSalidaEstimada.Value.ToString("dd/MM/yyyy HH:mm"), ref top);
            }
            else
            {
                AgregarFila(panelDetalle, "Piso:", hab.Piso.ToString(), ref top);
                AgregarFila(panelDetalle, "Última Limpieza:", hab.UltimaLimpieza?.ToString("dd/MM/yyyy HH:mm") ?? "Sin registro", ref top);
            }

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                DialogResult = DialogResult.OK,
                Location = new Point(285, 300),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                FlatStyle = FlatStyle.System
            };

            Controls.Add(lblTitulo);
            Controls.Add(lblEstado);
            Controls.Add(panelDetalle);
            Controls.Add(btnCerrar);
        }

        private void AgregarFila(Control contenedor, string titulo, string valor, ref int top)
        {
            var lblTag = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(15, top),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            var lblVal = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(110, top),
                AutoSize = true,
                ForeColor = Color.Black
            };

            contenedor.Controls.Add(lblTag);
            contenedor.Controls.Add(lblVal);
            top += 26;
        }
    }
}