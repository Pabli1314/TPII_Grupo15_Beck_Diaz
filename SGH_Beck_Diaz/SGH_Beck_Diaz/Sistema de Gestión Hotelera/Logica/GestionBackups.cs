using Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Todavía no hay una rutina real de backup contra la base: se simula en memoria (estática)
    /// para poder mostrar la pantalla completa y su flujo. El día que se conecte un backup real
    /// (p. ej. BACKUP DATABASE / RESTORE DATABASE de SQL Server), esta clase pasa a ejecutarlo
    /// sin cambiar su forma pública.
    /// </summary>
    public class GestionBackups
    {
        private static readonly List<BackupInfo> _backups = new();
        private static int _siguienteId = 1;
        private static readonly Random _random = new();

        public List<BackupInfo> ObtenerHistorial() => _backups.OrderByDescending(b => b.FechaHora).ToList();

        public BackupInfo? ObtenerUltimo() => _backups.OrderByDescending(b => b.FechaHora).FirstOrDefault();

        public BackupInfo CrearBackup(bool automatico = false)
        {
            var backup = new BackupInfo
            {
                Id = _siguienteId++,
                FechaHora = DateTime.Now,
                Estado = "Exitoso",
                TamanioMB = Math.Round(80 + _random.NextDouble() * 40, 1),
                RutaArchivo = $"C:\\Backups\\SGH\\backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
                Automatico = automatico
            };

            _backups.Add(backup);
            return backup;
        }

        public void RestaurarBackup(int idBackup)
        {
            if (!_backups.Any(b => b.Id == idBackup))
            {
                throw new InvalidOperationException("No se encontró la copia de seguridad seleccionada.");
            }

            // Simulado: todavía no hay una rutina real de RESTORE DATABASE conectada.
        }
    }
}
