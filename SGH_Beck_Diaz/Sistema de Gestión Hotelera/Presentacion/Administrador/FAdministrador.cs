using Entidades;
using Presentacion.Administrador.Modales;
using Presentacion.Administrador.UI;
using Presentacion.Administrador.Vistas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Presentacion.Administrador
{
    /// <summary>
    /// Shell del panel de Administrador: sidebar + header fijos y un panel de contenido donde
    /// se intercambian las vistas (UserControls) de cada sección. Reemplaza al viejo esquema MDI.
    /// </summary>
    public partial class FAdministrador : Form
    {
        private readonly Usuario _usuario;
        private readonly SidebarAdmin _sidebar;
        private readonly HeaderAdmin _header;
        private readonly Panel _contenido;
        private readonly Dictionary<string, Control> _vistas = new();

        private static readonly Dictionary<string, string> TitulosSeccion = new()
        {
            ["dashboard"] = "Dashboard",
            ["habitaciones"] = "Supervisión de Habitaciones",
            ["usuarios"] = "Gestión de Usuarios",
            ["tarifas"] = "Gestión de Tarifas",
            ["inventario"] = "Gestión de Inventario",
            ["reportes"] = "Reportes y Estadísticas",
            ["historico"] = "Histórico Administrativo",
            ["configuracion"] = "Configuración",
            ["backups"] = "Copias de Seguridad",
        };

        public FAdministrador() : this(null)
        {
        }

        public FAdministrador(Usuario? usuario)
        {
            InitializeComponent();

            // Adaptación a la nueva estructura: DniUsuario como string y NomUsuario/ApeUsuario para el perfil
            // Sin login: se opera con el primer Administrador activo de la base, para que lo que registre
            // (p. ej. limpiezas en registro_limpieza) quede a nombre de un dni_usuario que exista.
            _usuario = usuario
                ?? new Logica.GestionUsuarios().ObtenerUsuarioActivoPorRol("Administrador")
                ?? throw new InvalidOperationException("No hay ningún usuario Administrador activo en la base de datos. Inicie sesión.");

            BackColor = Paleta.FondoApp;

            _sidebar = new SidebarAdmin();
            _sidebar.NavegacionSeleccionada += Navegar;
            _sidebar.CerrarSesionSolicitado += (s, e) => CerrarSesion();

            _header = new HeaderAdmin();
            _header.MiPerfilClick += (s, e) => MostrarPerfil();
            _header.CambiarPasswordClick += (s, e) => MostrarCambiarPassword();
            _header.ConfiguracionClick += (s, e) => Navegar("configuracion");
            _header.CerrarSesionClick += (s, e) => CerrarSesion();

            _contenido = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.FondoApp, Padding = new Padding(28) };

            Controls.Add(_contenido);
            Controls.Add(_header);
            Controls.Add(_sidebar);

            // Mapeo adaptado utilizando ApeUsuario y NomUsuario
            string nombreCompleto = $"{_usuario.ApeUsuario} {_usuario.NomUsuario}".Trim();
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                nombreCompleto = _usuario.NomUsuario;
            }

            _sidebar.ConfigurarUsuario(nombreCompleto);
            _header.ConfigurarUsuario(nombreCompleto);

            Navegar("dashboard");
        }

        private void Navegar(string clave)
        {
            if (!_vistas.TryGetValue(clave, out Control? vista))
            {
                vista = CrearVista(clave);
                vista.Dock = DockStyle.Fill;
                _vistas[clave] = vista;
            }

            _contenido.Controls.Clear();
            _contenido.Controls.Add(vista);

            if ((vista as IVistaAdministrador) is { } vistaRefrescable)
            {
                vistaRefrescable.Refrescar();
            }

            _sidebar.EstablecerActivo(clave);
            _header.EstablecerTitulo(TitulosSeccion.TryGetValue(clave, out string? titulo) ? titulo : clave);
        }

        private Control CrearVista(string clave) => clave switch
        {
            "dashboard" => new VistaDashboard(),
            "habitaciones" => new VistaSupervisionHabitaciones(_usuario),
            "usuarios" => new VistaUsuarios(),
            "tarifas" => new VistaTarifas(),
            "inventario" => new VistaInventario(),
            "reportes" => new VistaReportes(),
            "historico" => new VistaHistorico(),
            "configuracion" => new VistaConfiguracion(),
            "backups" => new VistaCopiasSeguridad(),
            _ => new VistaDashboard()
        };

        private void MostrarPerfil()
        {
            using var modal = new FModalPerfil(_usuario);
            modal.ShowDialog(this);
        }

        private void MostrarCambiarPassword()
        {
            using var modal = new FModalCambiarPassword(_usuario);
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

            // FSeleccionUsuario, que abrió este módulo, se vuelve a mostrar al cerrarse (FormClosed).
            Close();
        }
    }

    /// <summary>Vistas que necesitan recargar sus datos cada vez que el usuario navega hacia ellas.</summary>
    internal interface IVistaAdministrador
    {
        void Refrescar();
    }
}