namespace Sistema_de_Gestión_Hotelera
{
    partial class FSeleccionUsuario
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblTitulo = new System.Windows.Forms.Label();
            lblNota = new System.Windows.Forms.Label();
            btnAdministrador = new System.Windows.Forms.Button();
            btnRecepcionista = new System.Windows.Forms.Button();
            btnGerente = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new System.Drawing.Font("Sans Serif Collection", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = System.Drawing.Color.White;
            lblTitulo.Location = new System.Drawing.Point(125, 50);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new System.Drawing.Size(538, 102);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "¿Con qué módulo deseas continuar?";
            // 
            // lblNota
            // 
            lblNota.AutoSize = true;
            lblNota.Font = new System.Drawing.Font("Sans Serif Collection", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblNota.ForeColor = System.Drawing.Color.FromArgb(173, 181, 189);
            lblNota.Location = new System.Drawing.Point(129, 112);
            lblNota.Name = "lblNota";
            lblNota.Size = new System.Drawing.Size(499, 51);
            lblNota.TabIndex = 1;
            lblNota.Text = "Selección manual del módulo (no restringe según el rol real del usuario).";
            // 
            // btnAdministrador
            // 
            btnAdministrador.BackColor = System.Drawing.Color.FromArgb(111, 66, 193);
            btnAdministrador.FlatAppearance.BorderSize = 0;
            btnAdministrador.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnAdministrador.Font = new System.Drawing.Font("Sans Serif Collection", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnAdministrador.ForeColor = System.Drawing.Color.White;
            btnAdministrador.Location = new System.Drawing.Point(40, 188);
            btnAdministrador.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnAdministrador.Name = "btnAdministrador";
            btnAdministrador.Size = new System.Drawing.Size(220, 188);
            btnAdministrador.TabIndex = 2;
            btnAdministrador.Text = "Administrador";
            btnAdministrador.UseVisualStyleBackColor = false;
            btnAdministrador.Click += btnAdministrador_Click;
            // 
            // btnRecepcionista
            // 
            btnRecepcionista.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            btnRecepcionista.FlatAppearance.BorderSize = 0;
            btnRecepcionista.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRecepcionista.Font = new System.Drawing.Font("Sans Serif Collection", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnRecepcionista.ForeColor = System.Drawing.Color.White;
            btnRecepcionista.Location = new System.Drawing.Point(520, 188);
            btnRecepcionista.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnRecepcionista.Name = "btnRecepcionista";
            btnRecepcionista.Size = new System.Drawing.Size(220, 188);
            btnRecepcionista.TabIndex = 4;
            btnRecepcionista.Text = "Recepcionista";
            btnRecepcionista.UseVisualStyleBackColor = false;
            btnRecepcionista.Click += btnRecepcionista_Click;
            // 
            // btnGerente
            // 
            btnGerente.BackColor = System.Drawing.Color.FromArgb(13, 110, 253);
            btnGerente.FlatAppearance.BorderSize = 0;
            btnGerente.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnGerente.Font = new System.Drawing.Font("Sans Serif Collection", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnGerente.ForeColor = System.Drawing.Color.White;
            btnGerente.Location = new System.Drawing.Point(281, 188);
            btnGerente.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnGerente.Name = "btnGerente";
            btnGerente.Size = new System.Drawing.Size(220, 188);
            btnGerente.TabIndex = 5;
            btnGerente.Text = "Gerente";
            btnGerente.UseVisualStyleBackColor = false;
            btnGerente.Click += btnGerente_Click;
            // 
            // FSeleccionUsuario
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(33, 37, 41);
            ClientSize = new System.Drawing.Size(780, 525);
            Controls.Add(btnGerente);
            Controls.Add(btnRecepcionista);
            Controls.Add(btnAdministrador);
            Controls.Add(lblNota);
            Controls.Add(lblTitulo);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FSeleccionUsuario";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Sistema de Gestión Hotelera - Selección de módulo";
            Load += FSeleccionUsuario_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblNota;
        private System.Windows.Forms.Button btnAdministrador;
        private System.Windows.Forms.Button btnRecepcionista;
        private System.Windows.Forms.Button btnGerente;
    }
}
