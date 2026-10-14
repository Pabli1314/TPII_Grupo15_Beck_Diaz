namespace Presentacion.Administrador
{
    partial class FHuespedes
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new System.Windows.Forms.Label();
            dgvHuespedes = new System.Windows.Forms.DataGridView();
            txtBuscar = new System.Windows.Forms.TextBox();
            btnBuscar = new System.Windows.Forms.Button();
            btnLimpiar = new System.Windows.Forms.Button();
            dtpHasta = new System.Windows.Forms.DateTimePicker();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            dtpDesde = new System.Windows.Forms.DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)dgvHuespedes).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Sans Serif Collection", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(540, 18);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(163, 78);
            label1.TabIndex = 0;
            label1.Text = "HUESPEDES";
            // 
            // dgvHuespedes
            // 
            dgvHuespedes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHuespedes.Location = new System.Drawing.Point(12, 283);
            dgvHuespedes.Name = "dgvHuespedes";
            dgvHuespedes.RowHeadersWidth = 51;
            dgvHuespedes.Size = new System.Drawing.Size(1245, 456);
            dgvHuespedes.TabIndex = 1;
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new System.Drawing.Point(27, 189);
            txtBuscar.Multiline = true;
            txtBuscar.Name = "txtBuscar";
            txtBuscar.PlaceholderText = "Buscar por DNI. Apellido/Nombre, núm de habitacion";
            txtBuscar.Size = new System.Drawing.Size(399, 48);
            txtBuscar.TabIndex = 2;
            // 
            // btnBuscar
            // 
            btnBuscar.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            btnBuscar.Font = new System.Drawing.Font("Sans Serif Collection", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btnBuscar.ForeColor = System.Drawing.Color.White;
            btnBuscar.Location = new System.Drawing.Point(477, 189);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new System.Drawing.Size(94, 48);
            btnBuscar.TabIndex = 3;
            btnBuscar.Text = "Buscar";
            btnBuscar.UseVisualStyleBackColor = false;
            // 
            // btnLimpiar
            // 
            btnLimpiar.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);
            btnLimpiar.Font = new System.Drawing.Font("Sans Serif Collection", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btnLimpiar.Location = new System.Drawing.Point(609, 189);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new System.Drawing.Size(94, 48);
            btnLimpiar.TabIndex = 4;
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.UseVisualStyleBackColor = false;
            // 
            // dtpHasta
            // 
            dtpHasta.Location = new System.Drawing.Point(613, 108);
            dtpHasta.Name = "dtpHasta";
            dtpHasta.Size = new System.Drawing.Size(294, 27);
            dtpHasta.TabIndex = 16;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new System.Drawing.Font("Sans Serif Collection", 11.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label6.Location = new System.Drawing.Point(483, 96);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(88, 68);
            label6.TabIndex = 15;
            label6.Text = "Hasta:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new System.Drawing.Font("Sans Serif Collection", 11.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label5.Location = new System.Drawing.Point(12, 96);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(93, 68);
            label5.TabIndex = 14;
            label5.Text = "Desde:";
            // 
            // dtpDesde
            // 
            dtpDesde.Location = new System.Drawing.Point(123, 108);
            dtpDesde.Name = "dtpDesde";
            dtpDesde.Size = new System.Drawing.Size(303, 27);
            dtpDesde.TabIndex = 13;
            // 
            // FHuespedes
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1269, 751);
            Controls.Add(dtpHasta);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(dtpDesde);
            Controls.Add(btnLimpiar);
            Controls.Add(btnBuscar);
            Controls.Add(txtBuscar);
            Controls.Add(dgvHuespedes);
            Controls.Add(label1);
            Name = "FHuespedes";
            Text = "FHuespedes";
            Load += FHuespedes_Load;
            ((System.ComponentModel.ISupportInitialize)dgvHuespedes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dgvHuespedes;
        private System.Windows.Forms.TextBox txtBuscar;
        private System.Windows.Forms.Button btnBuscar;
        private System.Windows.Forms.Button btnLimpiar;
        private System.Windows.Forms.DateTimePicker dtpHasta;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dtpDesde;
    }
}