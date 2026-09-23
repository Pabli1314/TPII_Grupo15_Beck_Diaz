using Entidades;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.UI;
using Presentacion.Recepcionista.Vistas;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Panel de Recepcionista: sidebar + header fijos y un panel de contenido donde
    /// se intercambian las vistas (UserControls) de cada sección.
    /// </summary>
    public partial class FRecepcionista : Form
    {
        private readonly Usuario _usuario;
        private readonly SidebarRecepcion _sidebar;
        private readonly HeaderRecepcion _header;
        private readonly Panel _contenido;
        private readonly Dictionary<string, Control> _vistas = new();

        private static readonly Dictionary<string, string> TitulosSeccion = new()
        {
            ["dashboard"] = "Centro de Operaciones",
            ["habitaciones"] = "Mapa de Habitaciones",
            ["reservas"] = "Gestión de Reservas",
            ["check-in"] = "Check-in",
            ["check-out"] = "Check-out",
            ["caja"] = "Gestión de Caja",
            ["ventas"] = "Ventas Adicionales",
            ["huespedes"] = "Huéspedes",
            ["historial"] = "Historial de Operaciones",
        };

        public FRecepcionista() : this(null)
        {
        }

        public FRecepcionista(Usuario? usuario)
        {
            InitializeComponent();
            // Sin login: se opera con el primer Recepcionista activo de la base, para que el turno de
            // caja, el check-in y las ventas queden a nombre de un dni_usuario que exista (FK_TurnoCaja_Usuario).
            _usuario = usuario
                ?? new Logica.GestionUsuarios().ObtenerUsuarioActivoPorRol("Recepcionista")
                ?? throw new InvalidOperationException("No hay ningún usuario Recepcionista activo en la base de datos. Cree uno desde el módulo Administrador o inicie sesión.");

            BackColor = Paleta.FondoApp;

            _sidebar = new SidebarRecepcion();
            _sidebar.NavegacionSeleccionada += Navegar;
            _sidebar.CerrarSesionSolicitado += (s, e) => CerrarSesion();

            _header = new HeaderRecepcion();
            _header.MiPerfilClick += (s, e) => MostrarPerfil();
            _header.CambiarPasswordClick += (s, e) => MostrarCambiarPassword();
            _header.CerrarSesionClick += (s, e) => CerrarSesion();

            _contenido = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.FondoApp, Padding = new Padding(28) };

            Controls.Add(_contenido);
            Controls.Add(_header);
            Controls.Add(_sidebar);

            string nombreCompleto = $"{_usuario.NomUsuario} {_usuario.ApeUsuario}".Trim();
            _sidebar.ConfigurarUsuario(nombreCompleto);
            _header.ConfigurarUsuario(nombreCompleto);

            Navegar("dashboard");
        }

        private void Navegar(string clave)
        {
            if (clave == "perfil")
            {
                MostrarPerfil();
                return;
            }

            if (!_vistas.TryGetValue(clave, out Control? vista))
            {
                vista = CrearVista(clave);
                vista.Dock = DockStyle.Fill;
                _vistas[clave] = vista;
            }

            _contenido.Controls.Clear();
            _contenido.Controls.Add(vista);

            switch (vista)
            {
                case VistaHabitaciones vistaHabitaciones:
                    vistaHabitaciones.Refrescar();
                    break;
                case VistaDashboard vistaDashboard:
                    vistaDashboard.Refrescar();
                    break;
                case VistaCaja vistaCaja:
                    vistaCaja.Refrescar();
                    break;
                case VistaHuespedes vistaHuespedes:
                    vistaHuespedes.Refrescar();
                    break;
                case VistaHistorial vistaHistorial:
                    vistaHistorial.Refrescar();
                    break;
                case VistaVentas vistaVentas:
                    // Recarga huéspedes con check-in activo, stock y ventas: pudo haber check-in/check-out
                    // en otra pantalla desde la última vez. El carrito en curso se conserva.
                    vistaVentas.Refrescar();
                    break;
            }

            _sidebar.EstablecerActivo(clave);
            _header.EstablecerTitulo(TitulosSeccion.TryGetValue(clave, out string? titulo) ? titulo : clave);
        }

        private Control CrearVista(string clave) => clave switch
        {
            "dashboard" => new VistaDashboard(_usuario, () => Navegar("caja")),
            "habitaciones" => new VistaHabitaciones(_usuario),
            "reservas" => new VistaReservas(),
            "check-in" => new VistaHabitaciones(_usuario, EstadoOcupacion.Disponible),
            "check-out" => new VistaHabitaciones(_usuario, EstadoOcupacion.Ocupada),
            "caja" => new VistaCaja(_usuario, () => Navegar("ventas")),
            "ventas" => new VistaVentas(_usuario),
            "huespedes" => new VistaHuespedes(),
            "historial" => new VistaHistorial(),
            _ => new VistaDashboard(_usuario, () => Navegar("caja"))
        };

        private void MostrarPerfil()
        {
            using var modal = new Presentacion.Administrador.Modales.FModalPerfil(_usuario);
            modal.ShowDialog(this);
        }

        private void MostrarCambiarPassword()
        {
            using var modal = new Presentacion.Administrador.Modales.FModalCambiarPassword(_usuario);
            modal.ShowDialog(this);
        }

        private void CerrarSesion()
        {
            DialogResult confirmacion = MessageBox.Show(
                "¿Confirma que desea cerrar la sesión?",
                "Cerrar sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            new Sistema_de_Gestión_Hotelera.FSeleccionUsuario(_usuario).Show();
            Close();
        }
    }
}