using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador.Modales
{
    /// <summary>
    /// Ficha de solo lectura con los datos personales del huésped alojado en una habitación
    /// Ocupada y los datos de su estadía vigente. Se abre desde las tarjetas de habitación del
    /// Dashboard y de Supervisión de habitaciones.
    /// </summary>
    internal class FModalDatosHuesped : FormModalBase
    {
        private const int AnchoColumna = 190;

        private readonly GestionHospedajes _gestionHospedajes = new();

        public FModalDatosHuesped(int nroHabitacion, Huesped huesped, Hospedaje hospedaje)
        {
            Size = new Size(460, 520);
            EstablecerTitulo($"Huésped · Habitación {nroHabitacion}");

            string nombreCompleto = $"{huesped.Nombre} {huesped.Apellido}".Trim();
            var avatar = new AvatarCircular { Size = new Size(64, 64), Location = new Point(0, 0), Iniciales = ObtenerIniciales(huesped) };
            var lblNombre = new Label { Text = nombreCompleto, Font = new Font("Segoe UI Semibold", 14f), ForeColor = Paleta.TextoPrimario, AutoSize = true, MaximumSize = new Size(310, 0), Location = new Point(80, 6) };
            var badge = new BadgeEstado { Location = new Point(80, 38) };
            badge.FijarEstado($"HAB. {nroHabitacion} · OCUPADA", Paleta.Ocupada);

            Contenido.Controls.Add(avatar);
            Contenido.Controls.Add(lblNombre);
            Contenido.Controls.Add(badge);

            int y = 86;
            AgregarSeccion("Datos personales", ref y);
            AgregarFila("DNI", huesped.DniHuesped, "Teléfono", huesped.Telefono, ref y);
            AgregarFila("Nombre", huesped.Nombre, "Apellido", huesped.Apellido, ref y);
            AgregarFila("Dirección", huesped.Direccion, "Correo", huesped.Correo, ref y);

            y += 8;
            AgregarSeccion("Estadía actual", ref y);
            DateTime entrada = hospedaje.FechaEntrada.Date + hospedaje.HoraEntrada;
            DateTime salida = hospedaje.FechaSalida.Date + hospedaje.HoraSalida;
            AgregarFila("Entrada", entrada.ToString("dd/MM/yyyy HH:mm"), "Salida estimada", salida.ToString("dd/MM/yyyy HH:mm"), ref y);
            AgregarDatoAncho("Método de pago", ObtenerMetodoPago(hospedaje.IdMetodo), ref y);

            var btnCerrar = EstiloBoton.Secundario(new Button { Text = "Cerrar", Size = new Size(110, 36), Location = new Point(AnchoColumna * 2 - 110, y + 10) });
            btnCerrar.Click += (s, e) => Close();
            Contenido.Controls.Add(btnCerrar);
        }

        /// <summary>
        /// Busca el huésped alojado en la habitación y abre la ficha. Si la habitación figura
        /// Ocupada pero no tiene un hospedaje vigente cargado, avisa en lugar de abrir el modal.
        /// </summary>
        public static void Mostrar(IWin32Window owner, int nroHabitacion)
        {
            try
            {
                var datos = new GestionHospedajes().ObtenerHuespedActivoPorHabitacion(nroHabitacion);
                if (datos == null)
                {
                    MessageBox.Show(owner, $"La habitación {nroHabitacion} no tiene un hospedaje vigente con huésped registrado.",
                        "Sin huésped", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var modal = new FModalDatosHuesped(nroHabitacion, datos.Value.Huesped, datos.Value.Hospedaje);
                modal.ShowDialog(owner);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message, "No se pudieron cargar los datos del huésped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string ObtenerMetodoPago(int idMetodo)
        {
            MetodoPago? metodo = _gestionHospedajes.ObtenerMetodosPago().FirstOrDefault(m => m.IdMetodo == idMetodo);
            return metodo?.NomMetodoPago ?? "-";
        }

        private void AgregarSeccion(string titulo, ref int y)
        {
            Contenido.Controls.Add(new Label { Text = titulo, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.Primario, AutoSize = true, Location = new Point(0, y) });
            Contenido.Controls.Add(new Panel { BackColor = Paleta.Borde, Location = new Point(0, y + 24), Size = new Size(AnchoColumna * 2, 1) });
            y += 36;
        }

        private void AgregarFila(string etiqueta1, string? valor1, string etiqueta2, string? valor2, ref int y)
        {
            AgregarDato(etiqueta1, valor1, 0, AnchoColumna - 10, y);
            AgregarDato(etiqueta2, valor2, AnchoColumna, AnchoColumna - 10, y);
            y += 48;
        }

        private void AgregarDatoAncho(string etiqueta, string? valor, ref int y)
        {
            AgregarDato(etiqueta, valor, 0, AnchoColumna * 2, y);
            y += 48;
        }

        private void AgregarDato(string etiqueta, string? valor, int x, int ancho, int y)
        {
            Contenido.Controls.Add(new Label { Text = etiqueta, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario, AutoSize = true, Location = new Point(x, y) });
            Contenido.Controls.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(valor) ? "-" : valor,
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Paleta.TextoPrimario,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(ancho, 20),
                Location = new Point(x, y + 18)
            });
        }

        private static string ObtenerIniciales(Huesped huesped)
        {
            string nombre = huesped.Nombre ?? string.Empty;
            string apellido = huesped.Apellido ?? string.Empty;
            char inicial1 = nombre.Length > 0 ? nombre[0] : 'H';
            char inicial2 = apellido.Length > 0 ? apellido[0] : (nombre.Length > 1 ? nombre[1] : 'U');
            return $"{inicial1}{inicial2}".ToUpperInvariant();
        }
    }
}
