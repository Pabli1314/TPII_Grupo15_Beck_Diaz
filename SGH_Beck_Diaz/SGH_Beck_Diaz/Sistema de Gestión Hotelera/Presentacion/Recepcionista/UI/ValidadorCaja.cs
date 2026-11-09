using Entidades;
using Logica;
using System.Windows.Forms;

namespace Presentacion.Recepcionista.UI
{
    /// <summary>
    /// Bloqueo operativo en la capa de Presentación: antes de check-in, check-out, ventas o prórrogas
    /// se consulta el estado de la caja y, si no está "Abierta y vigente para hoy", se avisa al usuario
    /// con la causa concreta. La capa de Lógica repite la validación (GestionTurnoCaja.ValidarCajaOperativa)
    /// como segunda barrera.
    /// </summary>
    internal static class ValidadorCaja
    {
        /// <summary>True si se puede operar; si no, muestra la advertencia y devuelve false.</summary>
        public static bool PuedeOperar(string? dniUsuario, IWin32Window? owner = null)
        {
            EstadoTurnoCaja estado = new GestionTurnoCaja().ConsultarEstadoCaja(dniUsuario);
            if (estado.PermiteOperar)
            {
                return true;
            }

            MessageBox.Show(owner, estado.MensajeBloqueo, estado.TituloBloqueo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}
