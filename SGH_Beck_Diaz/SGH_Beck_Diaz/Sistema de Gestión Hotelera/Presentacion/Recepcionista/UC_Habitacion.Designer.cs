namespace Presentacion.Recepcionista
{
    partial class UC_Habitacion
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblNumero = new System.Windows.Forms.Label();
            this.lblTipo = new System.Windows.Forms.Label();
            this.badgeEstado = new Presentacion.Recepcionista.BadgeEstado();
            this.lblHuesped = new System.Windows.Forms.Label();
            this.lblTemporizador = new System.Windows.Forms.Label();
            this.lblPie = new System.Windows.Forms.Label();
            this.temporizador = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // lblNumero
            //
            this.lblNumero.AutoSize = true;
            this.lblNumero.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblNumero.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.lblNumero.Location = new System.Drawing.Point(12, 10);
            this.lblNumero.Name = "lblNumero";
            this.lblNumero.Size = new System.Drawing.Size(45, 32);
            this.lblNumero.TabIndex = 0;
            this.lblNumero.Text = "101";
            //
            // lblTipo
            //
            this.lblTipo.AutoSize = true;
            this.lblTipo.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblTipo.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.lblTipo.Location = new System.Drawing.Point(13, 44);
            this.lblTipo.Name = "lblTipo";
            this.lblTipo.Size = new System.Drawing.Size(52, 13);
            this.lblTipo.TabIndex = 1;
            this.lblTipo.Text = "Estándar";
            //
            // badgeEstado
            //
            this.badgeEstado.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.badgeEstado.ForeColor = System.Drawing.Color.White;
            this.badgeEstado.Location = new System.Drawing.Point(94, 10);
            this.badgeEstado.Name = "badgeEstado";
            this.badgeEstado.Size = new System.Drawing.Size(76, 20);
            this.badgeEstado.TabIndex = 2;
            this.badgeEstado.Text = "Disponible";
            //
            // lblHuesped
            //
            this.lblHuesped.AutoSize = true;
            this.lblHuesped.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHuesped.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.lblHuesped.Location = new System.Drawing.Point(13, 64);
            this.lblHuesped.Name = "lblHuesped";
            this.lblHuesped.Size = new System.Drawing.Size(89, 15);
            this.lblHuesped.TabIndex = 3;
            this.lblHuesped.Text = "Carlos Méndez";
            //
            // lblTemporizador
            //
            this.lblTemporizador.AutoSize = true;
            this.lblTemporizador.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTemporizador.ForeColor = System.Drawing.Color.FromArgb(234, 84, 85);
            this.lblTemporizador.Location = new System.Drawing.Point(12, 84);
            this.lblTemporizador.Name = "lblTemporizador";
            this.lblTemporizador.Size = new System.Drawing.Size(70, 20);
            this.lblTemporizador.TabIndex = 4;
            this.lblTemporizador.Text = "00:08:52";
            //
            // lblPie
            //
            this.lblPie.AutoSize = true;
            this.lblPie.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.lblPie.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.lblPie.Location = new System.Drawing.Point(13, 109);
            this.lblPie.Name = "lblPie";
            this.lblPie.Size = new System.Drawing.Size(96, 12);
            this.lblPie.TabIndex = 5;
            this.lblPie.Text = "Toca para gestionar";
            //
            // temporizador
            //
            this.temporizador.Interval = 1000;
            this.temporizador.Tick += new System.EventHandler(this.temporizador_Tick);
            //
            // UC_Habitacion
            //
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.lblPie);
            this.Controls.Add(this.lblTemporizador);
            this.Controls.Add(this.lblHuesped);
            this.Controls.Add(this.badgeEstado);
            this.Controls.Add(this.lblTipo);
            this.Controls.Add(this.lblNumero);
            this.Name = "UC_Habitacion";
            this.Size = new System.Drawing.Size(180, 130);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblNumero;
        private System.Windows.Forms.Label lblTipo;
        private Presentacion.Recepcionista.BadgeEstado badgeEstado;
        private System.Windows.Forms.Label lblHuesped;
        private System.Windows.Forms.Label lblTemporizador;
        private System.Windows.Forms.Label lblPie;
        private System.Windows.Forms.Timer temporizador;
    }
}
