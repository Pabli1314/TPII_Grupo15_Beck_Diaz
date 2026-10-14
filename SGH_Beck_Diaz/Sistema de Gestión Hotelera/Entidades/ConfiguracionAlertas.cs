namespace Entidades
{
    /// <summary>
    /// Configuración global de tolerancia/alertas de vencimiento de ocupación (RF de configuración
    /// del Administrador). Persistida en memoria por Logica.GestionConfiguracion hasta que exista
    /// una tabla de configuración real.
    /// </summary>
    public class ConfiguracionAlertas
    {
        public int MinutosTolerancia { get; set; } = 30;
        public bool AlertasVisualesActivas { get; set; } = true;
        public bool AlertasSonorasActivas { get; set; } = false;
        public bool BackupAutomaticoActivo { get; set; } = false;
        public int BackupAutomaticoHora { get; set; } = 3; // 0-23
    }
}
