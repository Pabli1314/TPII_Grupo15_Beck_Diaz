using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Pantalla de gestión de una habitación puntual. Todas las transiciones de estado
    /// (check-in, check-out, limpieza→disponible, mantenimiento) están conectadas a la base
    /// de datos real; <see cref="_habitacion"/> se actualiza en memoria solo después de que
    /// la operación contra la base tuvo éxito, para que la tarjeta del tablero (VistaHabitaciones)
    /// nunca muestre algo distinto de lo que quedó persistido.
    /// </summary>
    internal class FGestionHabitacion : FormModalBase
    {
        private readonly HabitacionInfo _habitacion;
        private readonly Usuario? _usuario;
        private readonly GestionHabitaciones _gestionHabitaciones = new GestionHabitaciones();
        private readonly GestionHospedajes _gestionHospedajes = new GestionHospedajes();

        private readonly Label _lblTipo;
        private readonly BadgeEstado _badgeEstado;
        private readonly Label _lblHuespedTitulo;
        private readonly Label _lblHuespedValor;
        private readonly Label _lblIngresoTitulo;
        private readonly Label _lblIngresoValor;
        private readonly Label _lblMensaje;
        private readonly Button _btnAccionPrincipal;
        private readonly Button _btnAccionSecundaria;

        public FGestionHabitacion(HabitacionInfo habitacion) : this(habitacion, null)
        {
        }

        public FGestionHabitacion(HabitacionInfo habitacion, Usuario? usuario)
        {
            _habitacion = habitacion ?? throw new ArgumentNullException(nameof(habitacion));
            _usuario = usuario;

            Size = new Size(440, 420);

            const int ancho = 360;
            int y = 0;

            _lblTipo = new Label { Location = new Point(0, y), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario };
            Contenido.Controls.Add(_lblTipo);
            y += 26;

            _badgeEstado = new BadgeEstado { Location = new Point(0, y) };
            Contenido.Controls.Add(_badgeEstado);
            y += 40;

            _lblHuespedTitulo = new Label { Text = "Huésped", Location = new Point(0, y), AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario };
            _lblHuespedValor = new Label { Location = new Point(0, y + 18), AutoSize = true, Font = Paleta.FuenteBaseNegrita, ForeColor = Paleta.TextoPrimario };
            Contenido.Controls.Add(_lblHuespedTitulo);
            Contenido.Controls.Add(_lblHuespedValor);
            y += 54;

            _lblIngresoTitulo = new Label { Text = "Hora de entrada", Location = new Point(0, y), AutoSize = true, Font = Paleta.FuenteChica, ForeColor = Paleta.TextoTerciario };
            _lblIngresoValor = new Label { Location = new Point(0, y + 18), AutoSize = true, Font = Paleta.FuenteBase, ForeColor = Paleta.TextoPrimario };
            Contenido.Controls.Add(_lblIngresoTitulo);
            Contenido.Controls.Add(_lblIngresoValor);
            y += 54;

            _lblMensaje = new Label { Location = new Point(0, y), Size = new Size(ancho, 40), Font = Paleta.FuenteBase, ForeColor = Paleta.TextoSecundario };
            Contenido.Controls.Add(_lblMensaje);
            y += 60;

            _btnAccionSecundaria = EstiloBoton.Secundario(new Button { Size = new Size(ancho, 40), Location = new Point(0, y) });
            _btnAccionSecundaria.Click += (s, e) => EnviarAMantenimiento();
            Contenido.Controls.Add(_btnAccionSecundaria);
            y += 50;

            _btnAccionPrincipal = EstiloBoton.Primario(new Button { Size = new Size(ancho, 44), Location = new Point(0, y) });
            _btnAccionPrincipal.Click += (s, e) => EjecutarAccionPrincipal();
            Contenido.Controls.Add(_btnAccionPrincipal);

            Load += (s, e) => CargarDatos();
        }

        private void CargarDatos()
        {
            EstablecerTitulo($"Habitación {_habitacion.NroHabitacion}");
            _lblTipo.Text = _habitacion.Tipo;
            _badgeEstado.FijarEstado(DibujoUtil.TextoPorEstado(_habitacion.Estado), DibujoUtil.ColorPorEstado(_habitacion.Estado));

            bool esOcupada = _habitacion.Estado == EstadoOcupacion.Ocupada;
            bool esDisponible = _habitacion.Estado == EstadoOcupacion.Disponible;

            _lblHuespedTitulo.Visible = esOcupada;
            _lblHuespedValor.Visible = esOcupada;
            _lblIngresoTitulo.Visible = esOcupada;
            _lblIngresoValor.Visible = esOcupada;
            _lblMensaje.Visible = !esOcupada;
            _btnAccionSecundaria.Visible = esDisponible;

            if (esOcupada)
            {
                _lblHuespedValor.Text = _habitacion.Huesped;
                _lblIngresoValor.Text = _habitacion.HoraIngreso?.ToString("dd/MM/yyyy HH:mm") ?? "-";
            }
            else if (esDisponible)
            {
                _lblMensaje.Text = "Esta habitación está disponible.";
            }
            else if (_habitacion.Estado == EstadoOcupacion.Limpieza)
            {
                _lblMensaje.Text = "Habitación en limpieza.";
            }
            else if (_habitacion.Estado == EstadoOcupacion.Mantenimiento)
            {
                _lblMensaje.Text = "Habitación en mantenimiento.";
            }

            _btnAccionSecundaria.Text = "Enviar a mantenimiento";
            _btnAccionPrincipal.Text = _habitacion.Estado switch
            {
                EstadoOcupacion.Disponible => "Registrar check-in",
                EstadoOcupacion.Ocupada => "Registrar check-out",
                EstadoOcupacion.Limpieza => "Marcar como disponible",
                EstadoOcupacion.Mantenimiento => "Finalizar mantenimiento",
                _ => "Confirmar"
            };
        }

        private void EjecutarAccionPrincipal()
        {
            switch (_habitacion.Estado)
            {
                case EstadoOcupacion.Disponible:
                    RegistrarCheckIn();
                    break;
                case EstadoOcupacion.Ocupada:
                    RegistrarCheckOut();
                    break;
                case EstadoOcupacion.Limpieza:
                    MarcarComoDisponibleDesdeLimpieza();
                    break;
                case EstadoOcupacion.Mantenimiento:
                    FinalizarMantenimiento();
                    break;
            }
        }

        private void RegistrarCheckIn()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            using var fCheckIn = new FCheckIn(_habitacion.NroHabitacion, _usuario!.DniUsuario);
            if (fCheckIn.ShowDialog(this) == DialogResult.OK)
            {
                _habitacion.Huesped = fCheckIn.NombreCompleto;
                _habitacion.HoraIngreso = fCheckIn.HoraIngreso;
                _habitacion.Estado = EstadoOcupacion.Ocupada;
                CerrarConCambios();
            }
        }

        private void RegistrarCheckOut()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            using var fCheckOut = new FCheckOut(_habitacion.NroHabitacion);
            if (fCheckOut.ShowDialog(this) == DialogResult.OK)
            {
                _habitacion.Huesped = string.Empty;
                _habitacion.HoraIngreso = null;
                _habitacion.Estado = EstadoOcupacion.Limpieza;
                CerrarConCambios();
            }
        }

        private void MarcarComoDisponibleDesdeLimpieza()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            DialogResult confirmacion = MessageBox.Show(
                $"¿Confirma que la habitación {_habitacion.NroHabitacion} está lista para volver a Disponible?",
                "Marcar como disponible",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            try
            {
                //_gestionHabitaciones.marcarComoDisponible(nroHabitacion, _usuario!.DniUsuario);
                _habitacion.Estado = EstadoOcupacion.Disponible;
                CerrarConCambios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FinalizarMantenimiento()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            try
            {
                _gestionHabitaciones.FinalizarMantenimiento(nroHabitacion);
                _habitacion.Estado = EstadoOcupacion.Disponible;
                CerrarConCambios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnviarAMantenimiento()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            DialogResult confirmacion = MessageBox.Show(
                $"¿Confirma enviar la habitación {_habitacion.NroHabitacion} a mantenimiento?",
                "Enviar a mantenimiento",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _gestionHabitaciones.EnviarAMantenimiento(nroHabitacion);
                _habitacion.Estado = EstadoOcupacion.Mantenimiento;
                CerrarConCambios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidarUsuarioYNumero(out int nroHabitacion)
        {
            if (_usuario == null || string.IsNullOrWhiteSpace(_usuario.DniUsuario))
            {
                MessageBox.Show("No se pudo identificar al usuario de la sesión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                nroHabitacion = 0;
                return false;
            }

            if (!int.TryParse(_habitacion.NroHabitacion, out nroHabitacion))
            {
                MessageBox.Show("Número de habitación inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void CerrarConCambios()
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}