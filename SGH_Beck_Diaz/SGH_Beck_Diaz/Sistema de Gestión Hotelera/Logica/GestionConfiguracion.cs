using Entidades;

namespace Logica
{
    /// <summary>
    /// Configuración global de alertas del Administrador. Todavía no hay una tabla de
    /// configuración: se guarda en memoria (estática) mientras la app está abierta.
    /// </summary>
    public class GestionConfiguracion
    {
        private static readonly ConfiguracionAlertas _configuracion = new();

        public ConfiguracionAlertas Obtener() => _configuracion;

        public void Guardar(ConfiguracionAlertas configuracion)
        {
            if (configuracion.MinutosTolerancia < 0)
            {
                throw new System.ArgumentException("Los minutos de tolerancia no pueden ser negativos.");
            }

            _configuracion.MinutosTolerancia = configuracion.MinutosTolerancia;
            _configuracion.AlertasVisualesActivas = configuracion.AlertasVisualesActivas;
            _configuracion.AlertasSonorasActivas = configuracion.AlertasSonorasActivas;
            _configuracion.BackupAutomaticoActivo = configuracion.BackupAutomaticoActivo;
            _configuracion.BackupAutomaticoHora = configuracion.BackupAutomaticoHora;
        }
    }
}
