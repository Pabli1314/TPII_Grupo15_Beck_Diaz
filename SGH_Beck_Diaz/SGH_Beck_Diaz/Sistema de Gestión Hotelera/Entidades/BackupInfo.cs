using System;

namespace Entidades
{
    /// <summary>
    /// Todavía no hay una rutina real de backup: Logica.GestionBackups simula el historial en
    /// memoria hasta que se conecte a un mecanismo real (p. ej. BACKUP DATABASE de SQL Server).
    /// </summary>
    public class BackupInfo
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = string.Empty; // Exitoso / Fallido
        public double TamanioMB { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public bool Automatico { get; set; }
    }
}
