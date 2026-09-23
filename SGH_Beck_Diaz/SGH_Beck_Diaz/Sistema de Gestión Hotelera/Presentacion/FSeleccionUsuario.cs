using Entidades;
using Presentacion;
using Presentacion.Administrador;
using Presentacion.Recepcionista;
using Presentacion.Gerente;
using System;
using System.Windows.Forms;

namespace Sistema_de_Gestión_Hotelera
{
    public partial class FSeleccionUsuario : Form
    {
        private readonly Usuario? _usuario;

        public FSeleccionUsuario() : this(null)
        {
        }

        public FSeleccionUsuario(Usuario? usuario)
        {
            InitializeComponent();

            // Sin login (usuario null) no se inventa un usuario: cada módulo toma un usuario real y
            // activo de su rol desde la base. Un DNI inexistente rompe las FK (FK_TurnoCaja_Usuario).
            _usuario = usuario;
        }

        private void FSeleccionUsuario_Load(object sender, EventArgs e)
        {
        }

        private void btnAdministrador_Click(object sender, EventArgs e)
        {
            AbrirModulo(() => new FAdministrador(_usuario));
        }

        /*
        private void btnSupervisor_Click(object sender, EventArgs e)
        {
            AbrirModulo(new FSupervisor(_usuario));
        }
        */

        private void btnRecepcionista_Click(object sender, EventArgs e)
        {
            AbrirModulo(() => new FRecepcionista(_usuario));
        }

        private void AbrirModulo(Func<Form> crearModulo)
        {
            Form modulo;
            try
            {
                modulo = crearModulo();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "No se puede abrir el módulo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            modulo.Show();
            Hide();
        }

        private void btnVolver_Click(object sender, EventArgs e)
        {
            FLogin fLogin = new FLogin();
            fLogin.Show();
            Hide();
        }

        private void btnGerente_Click(object sender, EventArgs e)
        {
            AbrirModulo(() => new FGerente());
        }
    }
}