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
            this.lblSalida = new System.Windows.Forms.Label();
            this.lblTemporizador = new System.Windows.Forms.Label();
            this.lblPie = new System.Windows.Forms.Label();
            this.btnExtender = new System.Windows.Forms.Button();
            this.temporizador = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblNumero
            // 
            this.lblNumero.AutoSize = true;
            this.lblNumero.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblNumero.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.lblNumero.Location = new System.Drawing.Point(12, 8);
            this.lblNumero.Name = "lblNumero";
            this.lblNumero.Size = new System.Drawing.Size(42, 25);
            this.lblNumero.TabIndex = 0;
            this.lblNumero.Text = "101";
            // 
            // lblTipo
            // 
            this.lblTipo.AutoSize = true;
            this.lblTipo.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblTipo.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.lblTipo.Location = new System.Drawing.Point(13, 36);
            this.lblTipo.Name = "lblTipo";
            this.lblTipo.Size = new System.Drawing.Size(52, 13);
            this.lblTipo.TabIndex = 1;
            this.lblTipo.Text = "Estándar";
            // 
            // badgeEstado
            // 
            this.badgeEstado.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.badgeEstado.ForeColor = System.Drawing.Color.White;
            this.badgeEstado.Location = new System.Drawing.Point(94, 8);
            this.badgeEstado.Name = "badgeEstado";
            this.badgeEstado.Size = new System.Drawing.Size(76, 20);
            this.badgeEstado.TabIndex = 2;
            this.badgeEstado.Text = "Disponible";
            // 
            // lblHuesped
            // 
            this.lblHuesped.AutoSize = true;
            this.lblHuesped.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHuesped.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            this.lblHuesped.Location = new System.Drawing.Point(13, 54);
            this.lblHuesped.Name = "lblHuesped";
            this.lblHuesped.Size = new System.Drawing.Size(86, 15);
            this.lblHuesped.TabIndex = 3;
            this.lblHuesped.Text = "Carlos Méndez";
            //
            // lblSalida
            //
            this.lblSalida.AutoSize = true;
            this.lblSalida.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblSalida.ForeColor = System.Drawing.Color.FromArgb(73, 80, 87);
            this.lblSalida.Location = new System.Drawing.Point(13, 72);
            this.lblSalida.Name = "lblSalida";
            this.lblSalida.Size = new System.Drawing.Size(110, 13);
            this.lblSalida.TabIndex = 6;
            this.lblSalida.Text = "Salida: 21/10 12:00";
            //
            // lblTemporizador
            // 
            this.lblTemporizador.AutoSize = true;
            this.lblTemporizador.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTemporizador.ForeColor = System.Drawing.Color.FromArgb(234, 84, 85);
            this.lblTemporizador.Location = new System.Drawing.Point(12, 88);
            this.lblTemporizador.Name = "lblTemporizador";
            this.lblTemporizador.Size = new System.Drawing.Size(65, 19);
            this.lblTemporizador.TabIndex = 4;
            this.lblTemporizador.Text = "00:08:52";
            // 
            // lblPie
            // 
            this.lblPie.AutoSize = true;
            this.lblPie.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.lblPie.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.lblPie.Location = new System.Drawing.Point(13, 112);
            this.lblPie.Name = "lblPie";
            this.lblPie.Size = new System.Drawing.Size(96, 12);
            this.lblPie.TabIndex = 5;
            this.lblPie.Text = "Toca para gestionar";
            // 
            // btnExtender
            // 
            this.btnExtender.BackColor = System.Drawing.Color.FromArgb(224, 234, 253);
            this.btnExtender.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExtender.FlatAppearance.BorderSize = 0;
            this.btnExtender.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExtender.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnExtender.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnExtender.Location = new System.Drawing.Point(12, 132);
            this.btnExtender.Name = "btnExtender";
            this.btnExtender.Size = new System.Drawing.Size(156, 28);
            this.btnExtender.TabIndex = 7;
            this.btnExtender.Text = "Extender estadía";
            this.btnExtender.UseVisualStyleBackColor = false;
            this.btnExtender.Visible = false;
            // 
            // temporizador
            // 
            this.temporizador.Interval = 1000;
            this.temporizador.Tick += new System.EventHandler(this.temporizador_Tick);
            // 
            // UC_Habitacion
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.btnExtender);
            this.Controls.Add(this.lblPie);
            this.Controls.Add(this.lblTemporizador);
            this.Controls.Add(this.lblSalida);
            this.Controls.Add(this.lblHuesped);
            this.Controls.Add(this.badgeEstado);
            this.Controls.Add(this.lblTipo);
            this.Controls.Add(this.lblNumero);
            this.Name = "UC_Habitacion";
            this.Size = new System.Drawing.Size(180, 170);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblNumero;
        private System.Windows.Forms.Label lblTipo;
        private Presentacion.Recepcionista.BadgeEstado badgeEstado;
        private System.Windows.Forms.Label lblHuesped;
        private System.Windows.Forms.Label lblSalida;
        private System.Windows.Forms.Label lblTemporizador;
        private System.Windows.Forms.Label lblPie;
        private System.Windows.Forms.Button btnExtender;
        private System.Windows.Forms.Timer temporizador;
    }
}
