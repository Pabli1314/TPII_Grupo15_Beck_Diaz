using Entidades;
using Presentacion;
using Sistema_de_Gestión_Hotelera;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Gerente
{
    public partial class FGerente : Form
    {
        // Colores del Tema Ejecutivo / Corporativo
        private readonly Color colorSidebarBg = Color.FromArgb(24, 32, 45);        // Azul oscuro profesional
        private readonly Color colorSidebarHeader = Color.FromArgb(18, 24, 34);    // Sombra header
        private readonly Color colorBtnNormal = Color.FromArgb(24, 32, 45);
        private readonly Color colorBtnHover = Color.FromArgb(38, 50, 68);
        private readonly Color colorBtnSelected = Color.FromArgb(13, 110, 253);   // Azul acento/activo
        private readonly Color colorTextInactive = Color.FromArgb(160, 174, 192);
        private readonly Color colorTextActive = Color.White;

        // Referencias de Control
        private Panel pnlSidebarIzquierdo;
        private Panel pnlHeader;
        private Panel pnlContentContainer;
        private Button btnNavSeleccionado;
        private Label lblTituloSeccion;

        public FGerente()
        {
            InitializeComponentes();
            ConstruirLayout();
        }

        private void InitializeComponentes()
        {
            this.Text = "Sistema de Gestión Hotelera (SGH) - Panel Ejecutivo de Gerencia";
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 600);
            this.BackColor = Color.FromArgb(240, 243, 246);
        }

        private void ConstruirLayout()
        {
            // ----------------------------------------------------
            // 1. SIDEBAR IZQUIERDO (Menú principal)
            // ----------------------------------------------------
            pnlSidebarIzquierdo = new Panel
            {
                Dock = DockStyle.Left, // Ubicación a la izquierda
                Width = 250,
                BackColor = colorSidebarBg
            };

            // Header del Sidebar (Branding SGH)
            Panel pnlBranding = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = colorSidebarHeader,
                Padding = new Padding(15)
            };

            Label lblLogo = new Label
            {
                Text = "🏨 SGH",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };

            Label lblSubLogo = new Label
            {
                Text = "Sistema de Gestión Hotelera",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = colorTextInactive,
                Location = new Point(18, 45),
                AutoSize = true
            };

            pnlBranding.Controls.Add(lblLogo);
            pnlBranding.Controls.Add(lblSubLogo);

            // Card Perfil Gerente
            Panel pnlUsuario = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            Label lblUser = new Label
            {
                Text = "👤 Usuario: Gerente",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };

            Label lblRol = new Label
            {
                Text = "Rol: GERENTE",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(52, 211, 153), // Verde esmeralda
                Location = new Point(15, 35),
                AutoSize = true
            };

            pnlUsuario.Controls.Add(lblUser);
            pnlUsuario.Controls.Add(lblRol);

            // Botón Cerrar Sesión (Anclado al fondo del Sidebar)
            Button btnCerrarSesion = new Button
            {
                Text = "  🚪  Cerrar Sesión",
                Dock = DockStyle.Bottom,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(220, 53, 69), // Color rojo solicitado
                BackColor = colorSidebarBg,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            btnCerrarSesion.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 25, 30);
            btnCerrarSesion.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 20, 25);
            btnCerrarSesion.Click += (sender, e) => CerrarSesion();

            // Menu Scrollable Container
            Panel pnlMenuNav = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // Botones de Navegación
            Button btnDashboard = CrearBotonNavegacion("📊  Dashboard", "Dashboard Ejecutivo");
            Button btnHabitaciones = CrearBotonNavegacion("🛏️  Habitaciones", "Estado General de Habitaciones");
            Button btnIngresos = CrearBotonNavegacion("💰  Ingresos", "Análisis Financiero e Ingresos");
            Button btnHistoricos = CrearBotonNavegacion("🕒  Históricos", "Auditoría e Históricos Operativos");

            // Añadir botones (al usar Dock = Top, se agregan en orden inverso)
            pnlMenuNav.Controls.Add(btnHistoricos);
            pnlMenuNav.Controls.Add(btnIngresos);
            pnlMenuNav.Controls.Add(btnHabitaciones);
            pnlMenuNav.Controls.Add(btnDashboard);

            // Ensamble del Sidebar (Se agrega el botón abajo y los demás elementos arriba/centro)
            pnlSidebarIzquierdo.Controls.Add(pnlMenuNav);
            pnlSidebarIzquierdo.Controls.Add(btnCerrarSesion);
            pnlSidebarIzquierdo.Controls.Add(pnlUsuario);
            pnlSidebarIzquierdo.Controls.Add(pnlBranding);

            // ----------------------------------------------------
            // 2. HEADER SUPERIOR
            // ----------------------------------------------------
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };

            lblTituloSeccion = new Label
            {
                Text = "Dashboard Ejecutivo",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            Label lblFecha = new Label
            {
                Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy").ToUpper(),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.Gray,
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblTituloSeccion);
            pnlHeader.Controls.Add(lblFecha);

            // ----------------------------------------------------
            // 3. CONTENEDOR CENTRAL DE MÓDULOS
            // ----------------------------------------------------
            pnlContentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 246)
            };

            // ----------------------------------------------------
            // ENSAMBLAJE DE PANELES
            // ----------------------------------------------------
            this.Controls.Add(pnlContentContainer);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebarIzquierdo);

            // Cargar Dashboard por defecto
            NavegarA(btnDashboard, "Dashboard Ejecutivo", new FormDashboardEjecutivo());
        }

        private Button CrearBotonNavegacion(string texto, string tituloSeccion)
        {
            Button btn = new Button
            {
                Text = "   " + texto,
                Tag = tituloSeccion,
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = colorTextInactive,
                BackColor = colorBtnNormal,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = colorBtnSelected;
            btn.FlatAppearance.MouseOverBackColor = colorBtnHover;

            btn.Click += (sender, e) =>
            {
                Form formApertura = ObtenerFormularioPorModulo(tituloSeccion);
                NavegarA(btn, tituloSeccion, formApertura);
            };

            return btn;
        }

        private void NavegarA(Button btn, string titulo, Form formHijo)
        {
            // Cambiar resaltado visual del botón activo
            if (btnNavSeleccionado != null)
            {
                btnNavSeleccionado.BackColor = colorBtnNormal;
                btnNavSeleccionado.ForeColor = colorTextInactive;
                btnNavSeleccionado.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            }

            btnNavSeleccionado = btn;
            btnNavSeleccionado.BackColor = colorBtnSelected;
            btnNavSeleccionado.ForeColor = colorTextActive;
            btnNavSeleccionado.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Actualizar título en la barra superior
            lblTituloSeccion.Text = titulo;

            // Renderizar el formulario seleccionado en el contenedor central
            AbrirFormularioEnContenedor(formHijo);
        }

        private void AbrirFormularioEnContenedor(Form formHijo)
        {
            if (pnlContentContainer.Controls.Count > 0)
                pnlContentContainer.Controls.RemoveAt(0);

            if (formHijo == null) return;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;
            pnlContentContainer.Controls.Add(formHijo);
            pnlContentContainer.Tag = formHijo;
            formHijo.Show();
        }

        private Form ObtenerFormularioPorModulo(string tituloSeccion)
        {
            switch (tituloSeccion)
            {
                case "Dashboard Ejecutivo":
                    return new FormDashboardEjecutivo();
                case "Estado General de Habitaciones":
                    return new FormAnalisisHabitaciones();
                case "Análisis Financiero e Ingresos":
                    return new FormAnalisisFinanciero();
                case "Auditoría e Históricos Operativos":
                    return new FormHistoricos();
                default:
                    return CrearVistaModuloEnDesarrollo(tituloSeccion);
            }
        }

        private Form CrearVistaModuloEnDesarrollo(string nombreModulo)
        {
            Form formTemp = new Form { BackColor = Color.FromArgb(240, 243, 246) };
            Label lblInfo = new Label
            {
                Text = $"Módulo Supervisional: {nombreModulo}\n\n[Vista restringida únicamente a lectura, analíticas y reportes para Gerencia]",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            formTemp.Controls.Add(lblInfo);
            return formTemp;
        }

        private void CerrarSesion()
        {
            DialogResult confirmacion = MessageBox.Show(
                "¿Confirma que desea cerrar la sesión?",
                "Cerrar sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            // Si responde NO o cancela, se interrumpe la ejecución sin cerrar la pantalla
            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            // Si responde SÍ: se abre la pantalla de selección de usuario y se cierra la actual
            FSeleccionUsuario fSeleccionUsuario = new FSeleccionUsuario();
            fSeleccionUsuario.Show();

            this.Close();
        }
    }
}

