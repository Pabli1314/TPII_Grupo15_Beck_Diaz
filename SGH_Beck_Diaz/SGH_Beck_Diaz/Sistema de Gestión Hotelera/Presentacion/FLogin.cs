using Entidades;
using Logica;
using Presentacion.Administrador;
using Presentacion.Gerente;
using Presentacion.Recepcionista;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion
{
    public class FLogin : Form
    {
        private readonly IniciarSesion _logicaLogin;

        private TextBox txtDni;
        private TextBox txtPassword;
        private Button btnIngresar;
        private Button btnCancelar;
        private Label lblError;

        public Usuario? UsuarioAutenticado { get; private set; }

        public FLogin()
        {
            _logicaLogin = new IniciarSesion();
            InitializeComponentes();
        }

        private void InitializeComponentes()
        {
            this.Text = "Acceso al Sistema - Gestión Hotelera";
            this.Size = new Size(380, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 243, 246);

            // --- PANEL DE TITULO ---
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            Label lblTitulo = new Label
            {
                Text = "INICIAR SESIÓN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblTitulo);

            // --- CONTROLES DE ENTRADA ---
            Label lblDni = new Label
            {
                Text = "DNI de Usuario:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(40, 95),
                AutoSize = true
            };

            txtDni = new TextBox
            {
                Location = new Point(40, 120),
                Width = 280,
                Font = new Font("Segoe UI", 10F),
                MaxLength = 8
            };

            Label lblPassword = new Label
            {
                Text = "Contraseña:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(40, 165),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(40, 190),
                Width = 280,
                Font = new Font("Segoe UI", 10F),
                PasswordChar = '●',
                MaxLength = 50
            };

            lblError = new Label
            {
                Location = new Point(40, 228),
                Width = 280,
                Height = 35,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 53, 69),
                Text = string.Empty,
                TextAlign = ContentAlignment.TopLeft
            };

            // --- BOTONES ---
            btnIngresar = new Button
            {
                Text = "Ingresar",
                Location = new Point(40, 275),
                Width = 130,
                Height = 38,
                BackColor = Color.FromArgb(13, 110, 253), 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIngresar.FlatAppearance.BorderSize = 0;
            btnIngresar.Click += BtnIngresar_Click;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(190, 275),
                Width = 130,
                Height = 38,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.AcceptButton = btnIngresar;

            // Panag-add kadagiti componentes iti Form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblDni);
            this.Controls.Add(txtDni);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblError);
            this.Controls.Add(btnIngresar);
            this.Controls.Add(btnCancelar);
        }

        private void BtnIngresar_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            string dni = txtDni.Text.Trim();
            string pass = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(pass))
            {
                lblError.Text = "Por favor, ingrese su DNI y contraseña.";
                return;
            }

            var (usuario, rol) = _logicaLogin.ValidarDniYRol(dni, pass);

            if (usuario == null)
            {
                lblError.Text = "DNI o contraseña incorrectos, o usuario inactivo.";
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            this.UsuarioAutenticado = usuario;
            this.Hide(); // Oculta la pantalla de login

            Form? formSiguiente = null;

            switch (rol)
            {
                case 1:
                    formSiguiente = new FAdministrador(usuario);
                    break;
                case 2:
                    formSiguiente = new FGerente(); // O new FGerente(usuario);
                    break;
                case 3:
                    formSiguiente = new FRecepcionista(usuario);
                    break;
                default:
                    MessageBox.Show("Rol no reconocido.", "Error de Autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Show();
                    return;
            }

            // Al cerrar la ventana principal del rol, se cierra completamente la aplicación
            formSiguiente.FormClosed += (s, args) => this.Close();
            formSiguiente.Show();
        }
    }
}