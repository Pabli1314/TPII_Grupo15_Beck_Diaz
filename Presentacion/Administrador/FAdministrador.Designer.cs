namespace Presentacion.Administrador
{
    partial class FAdministrador
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

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1366, 900);
            Name = "FAdministrador";
            Text = "Administración — SGH";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            MinimumSize = new System.Drawing.Size(1200, 720);
            ResumeLayout(false);
        }
    }
}
