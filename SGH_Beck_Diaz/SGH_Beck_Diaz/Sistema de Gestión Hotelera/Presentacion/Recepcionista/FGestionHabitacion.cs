using Entidades;
using Logica;
using Presentacion.Administrador.UI;
using Presentacion.Recepcionista.Modales;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Presentacion.Recepcionista
{
    /// <summary>
    /// Pantalla de gestión de una habitación puntual. Para habitaciones disponibles,
    /// exhibe de forma destacada la ficha técnica y comercial (Piso, Tipo/Categoría,
    /// cantidad de camas, tarifa por noche e indicador de estado).
    /// Todas las transiciones de estado (check-in, check-out, extender estadía, mantenimiento)
    /// se conectan a la base de datos real y actualizan la vista del mapa de habitaciones.
    /// </summary>
    internal class FGestionHabitacion : FormModalBase
    {
        private const int AnchoContenido = 460;

        private readonly HabitacionInfo _habitacion;
        private readonly Usuario? _usuario;
        private readonly GestionHabitaciones _gestionHabitaciones = new GestionHabitaciones();

        // Encabezado principal
        private readonly Label _lblTituloHabitacion;
        private readonly Label _lblSubtituloHabitacion;
        private readonly BadgeEstado _badgeEstado;

        // Ficha técnica y comercial
        private readonly Panel _pnlFichaTecnica;
        private readonly Label _lblValorPiso;
        private readonly Label _lblValorTipo;
        private readonly Label _lblValorCamas;
        private readonly Label _lblValorTarifa;

        // Panel de información de contexto (Disponible, Ocupada, Limpieza, Mantenimiento)
        private readonly Panel _pnlContexto;
        private readonly Label _lblContextoTitulo;
        private readonly Label _lblContextoDetalle;

        // Botones de acción
        private readonly Button _btnAccionSecundaria;
        private readonly Button _btnAccionPrincipal;

        public FGestionHabitacion(HabitacionInfo habitacion) : this(habitacion, null)
        {
        }

        public FGestionHabitacion(HabitacionInfo habitacion, Usuario? usuario)
        {
            _habitacion = habitacion ?? throw new ArgumentNullException(nameof(habitacion));
            _usuario = usuario;

            Size = new Size(520, 580);

            int y = 0;

            // --- 1. ENCABEZADO SUPERIOR ---
            _lblTituloHabitacion = new Label
            {
                Text = $"Habitación {_habitacion.NroHabitacion}",
                Font = Paleta.FuenteTitulo,
                ForeColor = Paleta.TextoPrimario,
                Location = new Point(0, y),
                AutoSize = true
            };

            _badgeEstado = new BadgeEstado
            {
                Location = new Point(AnchoContenido - 110, y + 4)
            };

            y += 30;

            _lblSubtituloHabitacion = new Label
            {
                Text = "Ficha técnica y gestión operativa de unidad",
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoTerciario,
                Location = new Point(0, y),
                AutoSize = true
            };

            y += 28;

            Contenido.Controls.Add(_lblTituloHabitacion);
            Contenido.Controls.Add(_badgeEstado);
            Contenido.Controls.Add(_lblSubtituloHabitacion);

            // --- 2. PANEL FICHA TÉCNICA Y COMERCIAL ---
            _pnlFichaTecnica = CrearPanelFichaTecnica(new Point(0, y), AnchoContenido, 175,
                out _lblValorPiso, out _lblValorTipo, out _lblValorCamas, out _lblValorTarifa);
            Contenido.Controls.Add(_pnlFichaTecnica);

            y += 185;

            // --- 3. PANEL DE CONTEXTO Y ESTADO ---
            _pnlContexto = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(AnchoContenido, 70),
                BackColor = Paleta.ExitoSuave,
                Padding = new Padding(12)
            };

            _lblContextoTitulo = new Label
            {
                Location = new Point(12, 10),
                AutoSize = true,
                Font = Paleta.FuenteBaseNegrita,
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            _lblContextoDetalle = new Label
            {
                Location = new Point(12, 32),
                Size = new Size(AnchoContenido - 24, 32),
                Font = Paleta.FuenteChica,
                ForeColor = Color.FromArgb(21, 128, 61)
            };

            _pnlContexto.Controls.Add(_lblContextoTitulo);
            _pnlContexto.Controls.Add(_lblContextoDetalle);
            Contenido.Controls.Add(_pnlContexto);

            y += 82;

            // --- 4. BOTONES DE ACCIÓN ---
            _btnAccionSecundaria = EstiloBoton.Secundario(new Button
            {
                Size = new Size(AnchoContenido, 40),
                Location = new Point(0, y)
            });
            _btnAccionSecundaria.Click += (s, e) => EjecutarAccionSecundaria();
            Contenido.Controls.Add(_btnAccionSecundaria);

            y += 48;

            _btnAccionPrincipal = EstiloBoton.Primario(new Button
            {
                Size = new Size(AnchoContenido, 44),
                Location = new Point(0, y)
            });
            _btnAccionPrincipal.Click += (s, e) => EjecutarAccionPrincipal();
            Contenido.Controls.Add(_btnAccionPrincipal);

            Load += (s, e) => CargarDatos();
        }

        /// <summary>
        /// Construye el panel visual de ficha técnica con diseño de tarjetas alineadas.
        /// </summary>
        private Panel CrearPanelFichaTecnica(Point ubicacion, int ancho, int alto,
            out Label lblPiso, out Label lblTipo, out Label lblCamas, out Label lblTarifa)
        {
            var panel = new Panel
            {
                Location = ubicacion,
                Size = new Size(ancho, alto),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            panel.Paint += (s, e) =>
            {
                using var lapiz = new Pen(Paleta.Borde, 1.2f);
                using var ruta = DibujoUtil.RutaRedondeada(new RectangleF(0, 0, panel.Width - 1, panel.Height - 1), 10);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(lapiz, ruta);
            };

            // Encabezado de la Ficha
            var lblFichaEncabezado = new Label
            {
                Text = "CARACTERÍSTICAS TÉCNICAS Y COMERCIALES",
                Font = new Font("Segoe UI Semibold", 8.25f),
                ForeColor = Paleta.TextoTerciario,
                Location = new Point(16, 12),
                AutoSize = true
            };
            panel.Controls.Add(lblFichaEncabezado);

            var separadorFicha = new Panel
            {
                Location = new Point(16, 32),
                Size = new Size(ancho - 32, 1),
                BackColor = Paleta.BordeSuave
            };
            panel.Controls.Add(separadorFicha);

            int col1X = 16;
            int col2X = (ancho / 2) + 8;
            int anchoCol = (ancho / 2) - 24;

            // Fila 1 - Columna 1: Ubicación / Piso
            CrearCampoFicha(panel, "UBICACIÓN / PISO", col1X, 42, anchoCol, out lblPiso);

            // Fila 1 - Columna 2: Tipo / Categoría
            CrearCampoFicha(panel, "CATEGORÍA / TIPO", col2X, 42, anchoCol, out lblTipo);

            // Fila 2 - Columna 1: Capacidad / Camas
            CrearCampoFicha(panel, "CAPACIDAD", col1X, 102, anchoCol, out lblCamas);

            // Fila 2 - Columna 2: Tarifa Comercial por Noche
            CrearCampoFicha(panel, "TARIFA POR NOCHE", col2X, 102, anchoCol, out lblTarifa, esDestacado: true);

            return panel;
        }

        private static void CrearCampoFicha(Panel contenedor, string etiqueta, int x, int y, int ancho, out Label lblValor, bool esDestacado = false)
        {
            var lblTit = new Label
            {
                Text = etiqueta,
                Font = Paleta.FuenteChica,
                ForeColor = Paleta.TextoSecundario,
                Location = new Point(x, y),
                AutoSize = true
            };

            lblValor = new Label
            {
                Location = new Point(x, y + 18),
                Size = new Size(ancho, 26),
                Font = esDestacado ? new Font("Segoe UI Semibold", 12f) : Paleta.FuenteBaseNegrita,
                ForeColor = esDestacado ? Paleta.Exito : Paleta.TextoPrimario,
                AutoEllipsis = true
            };

            contenedor.Controls.Add(lblTit);
            contenedor.Controls.Add(lblValor);
        }

        private void CargarDatos()
        {
            EstablecerTitulo($"Habitación {_habitacion.NroHabitacion}");

            // Carga o actualiza de forma reactiva los detalles técnicos completos mediante la capa de lógica
            if (int.TryParse(_habitacion.NroHabitacion, out int nroHab))
            {
                try
                {
                    HabitacionResumen? detalle = _gestionHabitaciones.ObtenerDetalleHabitacion(nroHab);
                    if (detalle != null)
                    {
                        _habitacion.Piso = detalle.Piso;
                        _habitacion.CantCamas = detalle.CantCamas;
                        _habitacion.Tarifa = detalle.Tarifa;
                        if (!string.IsNullOrWhiteSpace(detalle.TipoHabitacion))
                        {
                            _habitacion.Tipo = detalle.TipoHabitacion;
                        }
                        if (detalle.Ocupada && string.IsNullOrWhiteSpace(_habitacion.Huesped))
                        {
                            _habitacion.Huesped = detalle.Huesped ?? string.Empty;
                            _habitacion.HoraIngreso = detalle.HoraEntrada;
                            _habitacion.HoraSalidaEstimada = detalle.HoraSalidaEstimada;
                        }
                    }
                }
                catch
                {
                    // Fallback a los datos ya presentes en _habitacion
                }
            }

            // Actualizar datos de la ficha técnica
            _lblValorPiso.Text = _habitacion.Piso > 0 ? $"Piso {_habitacion.Piso}" : "Planta Baja";
            _lblValorTipo.Text = !string.IsNullOrWhiteSpace(_habitacion.Tipo) ? _habitacion.Tipo : "Estándar";
            _lblValorCamas.Text = _habitacion.CantCamas > 0
                ? $"{_habitacion.CantCamas} {(_habitacion.CantCamas == 1 ? "cama individual" : "camas")}"
                : "1 cama";
            _lblValorTarifa.Text = _habitacion.Tarifa > 0 ? _habitacion.Tarifa.ToString("C") : "$ 0,00";

            // Actualizar badge de estado
            Color colorEstado = DibujoUtil.ColorPorEstado(_habitacion.Estado);
            _badgeEstado.FijarEstado(DibujoUtil.TextoPorEstado(_habitacion.Estado), colorEstado);
            _badgeEstado.Location = new Point(AnchoContenido - _badgeEstado.Width, _lblTituloHabitacion.Top + 4);

            bool esDisponible = _habitacion.Estado == EstadoOcupacion.Disponible;
            bool esOcupada = _habitacion.Estado == EstadoOcupacion.Ocupada;

            // Configurar mensaje de contexto e indicador visual
            if (esDisponible)
            {
                _pnlContexto.BackColor = Paleta.ExitoSuave;
                _lblContextoTitulo.ForeColor = Color.FromArgb(22, 101, 52);
                _lblContextoDetalle.ForeColor = Color.FromArgb(21, 128, 61);

                _lblContextoTitulo.Text = "● Habitación disponible para asignación inmediata";
                _lblContextoDetalle.Text = "Unidad lista para Check-in. Puede registrar el ingreso del huésped o derivar a mantenimiento preventivo.";

                _btnAccionSecundaria.Visible = true;
                _btnAccionSecundaria.Text = "Enviar a mantenimiento";
                _btnAccionPrincipal.Text = "Registrar check-in";
            }
            else if (esOcupada)
            {
                _pnlContexto.BackColor = Color.FromArgb(254, 242, 242);
                _lblContextoTitulo.ForeColor = Color.FromArgb(185, 28, 28);
                _lblContextoDetalle.ForeColor = Color.FromArgb(153, 27, 27);

                string huespedStr = !string.IsNullOrWhiteSpace(_habitacion.Huesped) ? _habitacion.Huesped : "S/D";
                string salidaStr = _habitacion.HoraSalidaEstimada.HasValue ? _habitacion.HoraSalidaEstimada.Value.ToString("dd/MM/yyyy HH:mm") : "-";

                _lblContextoTitulo.Text = $"● Ocupada por: {huespedStr}";
                _lblContextoDetalle.Text = $"Salida estimada: {salidaStr}. Puede extender la estadía o tramitar el Check-out.";

                _btnAccionSecundaria.Visible = true;
                _btnAccionSecundaria.Text = "Extender estadía";
                _btnAccionPrincipal.Text = "Registrar check-out";
            }
            else if (_habitacion.Estado == EstadoOcupacion.Limpieza)
            {
                _pnlContexto.BackColor = Paleta.AdvertenciaSuave;
                _lblContextoTitulo.ForeColor = Color.FromArgb(146, 64, 14);
                _lblContextoDetalle.ForeColor = Color.FromArgb(180, 83, 9);

                _lblContextoTitulo.Text = "● En proceso de limpieza e higienización";
                _lblContextoDetalle.Text = "Al finalizar las tareas del personal de maestranza, confírmela para volverla Disponible.";

                _btnAccionSecundaria.Visible = false;
                _btnAccionPrincipal.Text = "Marcar como disponible";
            }
            else if (_habitacion.Estado == EstadoOcupacion.Mantenimiento)
            {
                _pnlContexto.BackColor = Paleta.PrimarioSuave;
                _lblContextoTitulo.ForeColor = Color.FromArgb(30, 64, 175);
                _lblContextoDetalle.ForeColor = Color.FromArgb(29, 78, 216);

                _lblContextoTitulo.Text = "● En mantenimiento técnico / reparaciones";
                _lblContextoDetalle.Text = "La habitación se encuentra fuera de servicio. Al culminar las obras, habilítela nuevamente.";

                _btnAccionSecundaria.Visible = false;
                _btnAccionPrincipal.Text = "Finalizar mantenimiento";
            }
        }

        private void EjecutarAccionSecundaria()
        {
            if (_habitacion.Estado == EstadoOcupacion.Disponible)
            {
                EnviarAMantenimiento();
            }
            else if (_habitacion.Estado == EstadoOcupacion.Ocupada)
            {
                ExtenderEstadia();
            }
        }

        private void ExtenderEstadia()
        {
            if (!ValidarUsuarioYNumero(out int nroHabitacion))
            {
                return;
            }

            using var fExtender = new FModalExtenderEstadia(_habitacion.NroHabitacion, _usuario!.DniUsuario);
            if (fExtender.ShowDialog(this) == DialogResult.OK)
            {
                CerrarConCambios();
            }
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

            if (new GestionTurnoCaja().ObtenerTurnoAbierto(_usuario!.DniUsuario) == null)
            {
                MessageBox.Show("La caja se encuentra cerrada. Debe abrir la caja antes de registrar un Check-in y realizar el cobro.", "Caja cerrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            using var fCheckOut = new FCheckOut(_habitacion.NroHabitacion, _usuario);
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
                _gestionHabitaciones.MarcarComoDisponible(nroHabitacion, _usuario!.DniUsuario);
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
                nroHabitacion = 0;
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
