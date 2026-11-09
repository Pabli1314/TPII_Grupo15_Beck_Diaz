using Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Logica
{
    /// <summary>Qué pestaña de Reportes de Recepción se exporta.</summary>
    public enum SeccionReporteRecepcion
    {
        Ahora,
        MiTurno,
        Semana,
        Mes
    }

    /// <summary>
    /// Plantilla PDF de Reportes de Recepción. Reutiliza el lienzo, las fuentes y el ensamblado del
    /// reporte del Administrador, pero con el mismo lenguaje, resumen y ayudas que la pantalla.
    /// </summary>
    public static partial class GeneradorReportePdf
    {
        private static readonly ColorPdf ColorLibre = new(40, 199, 111);
        private static readonly ColorPdf ColorOcupada = new(234, 84, 85);
        private static readonly ColorPdf ColorPeligro = new(198, 47, 61);
        private static readonly ColorPdf ColorAdvertencia = new(180, 83, 9);

        public static string NombreSeccion(SeccionReporteRecepcion seccion) => seccion switch
        {
            SeccionReporteRecepcion.MiTurno => "Mi turno",
            SeccionReporteRecepcion.Semana => "Semana",
            SeccionReporteRecepcion.Mes => "Mes",
            _ => "Ahora"
        };

        /// <summary>PDF de las pestañas «Semana» o «Mes».</summary>
        public static void GenerarArchivoPeriodo(ReportePeriodo reporte, string rutaArchivo, SeccionReporteRecepcion seccion, string? generadoPor = null)
        {
            if (reporte == null) throw new ArgumentNullException(nameof(reporte));
            if (string.IsNullOrWhiteSpace(rutaArchivo)) throw new ArgumentException("Ruta de archivo no válida", nameof(rutaArchivo));

            byte[] bytes = GenerarBytesPeriodo(reporte, seccion, generadoPor);
            File.WriteAllBytes(rutaArchivo, bytes);
        }

        public static byte[] GenerarBytesPeriodo(ReportePeriodo r, SeccionReporteRecepcion seccion, string? generadoPor = null)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            bool esSemana = seccion == SeccionReporteRecepcion.Semana;
            string nombreSeccion = esSemana ? "Semana" : $"Mes · {Capitalizar(r.NombrePeriodo)}";
            var l = new LienzoPdf();
            l.NuevaPagina();

            DibujarEncabezadoRecepcion(l, r.Generado, nombreSeccion);
            DibujarCajaDatos(l, new[]
            {
                ("Qué muestra", $"Del {r.Desde.ToString("dd/MM/yyyy", CulturaAr)} al {r.Hasta.ToString("dd/MM/yyyy", CulturaAr)}"),
                ("Generado por", string.IsNullOrWhiteSpace(generadoPor) ? "-" : generadoPor!),
                ("Fecha y hora de generación", $"{r.Generado.ToString("dd/MM/yyyy HH:mm", CulturaAr)} hs"),
                ("Habitaciones del hotel", r.TotalHabitaciones.ToString(CulturaAr))
            });
            DibujarResumenRecepcion(l, r.Resumen);

            string ocupacion = $"{r.PorcentajeOcupacion.ToString("0", CulturaAr)} %";
            string contextoOcupacion = GestionReportesRecepcion.HabitacionesPorNoche(r, corto: true);
            string noches = r.NochesPorHuesped > 0 ? r.NochesPorHuesped.ToString("0.#", CulturaAr) : "-";

            if (esSemana)
            {
                int diferencia = r.HuespedesQueEntraron - r.HuespedesPeriodoAnterior;
                DibujarTarjetasRecepcion(l, new[]
                {
                    new TarjetaPdf("Huéspedes que entraron", r.HuespedesQueEntraron.ToString(CulturaAr),
                        diferencia > 0 ? $"{diferencia} más que la semana anterior" : diferencia < 0 ? $"{-diferencia} menos que la semana anterior" : "igual que la semana anterior",
                        Primario, false),
                    new TarjetaPdf("Hotel ocupado", ocupacion, contextoOcupacion, ColorOcupada, false),
                    new TarjetaPdf("Noches por huésped", noches, "noches en promedio", Mantenimiento, false),
                    new TarjetaPdf("Tiempo de limpieza", r.CantidadLimpiezas > 0 ? GestionReportesRecepcion.Minutos(r.MinutosLimpieza) : "-",
                        r.CantidadLimpiezas > 0 ? $"promedio de {r.CantidadLimpiezas} limpiezas" : "sin limpiezas registradas", Limpieza, false)
                });
            }
            else
            {
                DibujarTarjetasRecepcion(l, new[]
                {
                    new TarjetaPdf("Hotel ocupado", ocupacion, contextoOcupacion, ColorOcupada, false),
                    new TarjetaPdf("Noches por huésped", noches, "noches en promedio", Mantenimiento, false),
                    new TarjetaPdf("Huéspedes que vuelven", r.HuespedesQueVuelven.ToString(CulturaAr),
                        r.HuespedesDistintos == 0 ? "nadie entró en el mes" : $"de {r.HuespedesDistintos} huéspedes en el mes", Primario, false),
                    new TarjetaPdf("Ingresos estimados", Moneda0(r.IngresosEstimados), "por alojamiento, sin ventas", Exito, false)
                });
            }

            int indiceHoy = r.Hasta.Date == r.Generado.Date ? r.OcupacionDiaria.Count - 1 : -1;
            DibujarColumnasPdf(l, esSemana ? "¿Qué días estuvo más lleno el hotel?" : "Habitaciones ocupadas por día",
                "Cada columna es un día. La línea punteada es el total del hotel: si la columna la toca, el hotel estuvo lleno.",
                r.OcupacionDiaria, r.TotalHabitaciones, indiceHoy, "No hubo habitaciones ocupadas en el período.");

            if (esSemana)
            {
                DibujarColumnasPdf(l, "¿Cuántos huéspedes entraron cada día?", "Cantidad de check-ins hechos en cada día.",
                    r.LlegadasDiarias, null, indiceHoy, "No hubo check-ins en el período.");
            }

            DibujarBarrasHorizontalesPdf(l, "Habitaciones más pedidas",
                "Cuántas veces se alojó alguien en cada habitación durante el período.",
                r.HabitacionesMasPedidas.Take(esSemana ? 5 : 6).Select(p => (p.Etiqueta, p.Valor, p.Valor == 1 ? "1 vez" : $"{p.Valor:0} veces")).ToArray(),
                "Ninguna habitación se usó en el período.");

            if (!esSemana)
            {
                DibujarBarrasHorizontalesPdf(l, "Habitaciones que más facturaron (estimado)",
                    "Estimación: tarifa vigente de la habitación × noches vendidas. No es el dinero cobrado.",
                    r.HabitacionesQueMasFacturaron.Take(6).Select(p => (p.Etiqueta, p.Valor, Moneda0((decimal)p.Valor))).ToArray(),
                    "No hay ingresos estimados para el período.", Exito);
            }

            DibujarPiesRecepcion(l, r.Generado, nombreSeccion);
            return EnsamblarDocumentoPdf(l.Paginas, r.Generado);
        }

        private static string Capitalizar(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], CulturaAr) + texto.Substring(1);

        public static void GenerarArchivoRecepcion(ReporteRecepcion reporte, string rutaArchivo, SeccionReporteRecepcion seccion, string? generadoPor = null)
        {
            if (reporte == null) throw new ArgumentNullException(nameof(reporte));
            if (string.IsNullOrWhiteSpace(rutaArchivo)) throw new ArgumentException("Ruta de archivo no válida", nameof(rutaArchivo));

            byte[] bytes = GenerarBytesRecepcion(reporte, seccion, generadoPor);
            File.WriteAllBytes(rutaArchivo, bytes);
        }

        public static byte[] GenerarBytesRecepcion(ReporteRecepcion r, SeccionReporteRecepcion seccion, string? generadoPor = null)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            string nombreSeccion = NombreSeccion(seccion);
            var l = new LienzoPdf();
            l.NuevaPagina();

            DibujarEncabezadoRecepcion(l, r.Generado, nombreSeccion);
            DibujarDatosRecepcion(l, r, seccion, generadoPor);
            DibujarResumenRecepcion(l, seccion == SeccionReporteRecepcion.Ahora ? r.ResumenAhora : r.ResumenTurno);

            if (seccion == SeccionReporteRecepcion.Ahora)
            {
                DibujarPestanaAhora(l, r);
            }
            else
            {
                DibujarPestanaTurno(l, r);
            }

            DibujarPiesRecepcion(l, r.Generado, nombreSeccion);
            return EnsamblarDocumentoPdf(l.Paginas, r.Generado);
        }

        // ------------------------------------------------------------------
        // Pestañas
        // ------------------------------------------------------------------

        private static void DibujarPestanaAhora(LienzoPdf l, ReporteRecepcion r)
        {
            List<MovimientoRecepcion> vencidas = r.SalidasVencidas;

            DibujarTarjetasRecepcion(l, new[]
            {
                new TarjetaPdf("Habitaciones libres ahora", r.Libres.ToString(CulturaAr), $"de {r.TotalHabitaciones} habitaciones", ColorLibre, false),
                new TarjetaPdf("Llegadas de hoy", r.Llegadas.Count.ToString(CulturaAr), "check-ins hechos hoy", Primario, false),
                new TarjetaPdf("Salidas que faltan hoy", r.SalidasQueFaltanHoy.ToString(CulturaAr),
                    r.SalidasRealizadasHoy == 1 ? "1 ya se fue hoy" : $"{r.SalidasRealizadasHoy} ya se fueron hoy", ColorAdvertencia, false),
                new TarjetaPdf("Pasaron su hora de salida", vencidas.Count.ToString(CulturaAr),
                    vencidas.Count == 0 ? "ninguna demorada" : "Hab. " + string.Join(", ", vencidas.Select(v => v.NroHabitacion)),
                    ColorPeligro, vencidas.Count > 0)
            });

            // Estado de las habitaciones: barra apilada + leyenda.
            l.AsegurarEspacio(AltoTituloSeccion + 70f);
            DibujarTituloSeccion(l, "¿Cómo están las habitaciones?",
                "La barra completa es el hotel entero; cada color ocupa la parte que le corresponde.");

            var partes = new (string Texto, int Cantidad, ColorPdf Color)[]
            {
                ("Ocupadas", r.Ocupadas, ColorOcupada),
                ("Libres", r.Libres, ColorLibre),
                ("En limpieza", r.EnLimpieza, Limpieza),
                ("Fuera de servicio", r.FueraDeServicio, Mantenimiento)
            };

            const float altoBarra = 18f;
            float yBarra = l.Y - altoBarra;
            int total = Math.Max(1, partes.Sum(p => p.Cantidad));
            float x = Margen;
            foreach (var parte in partes.Where(p => p.Cantidad > 0))
            {
                float ancho = AnchoUtil * parte.Cantidad / total;
                l.Rectangulo(x, yBarra, Math.Max(0f, ancho - 1.5f), altoBarra, parte.Color);
                x += ancho;
            }

            float yLeyenda = yBarra - 16f;
            float anchoLeyenda = AnchoUtil / partes.Length;
            for (int i = 0; i < partes.Length; i++)
            {
                float xl = Margen + i * anchoLeyenda;
                l.Rectangulo(xl, yLeyenda - 1f, 8f, 8f, partes[i].Color);
                l.Texto(xl + 13f, yLeyenda, $"{partes[i].Texto}: {partes[i].Cantidad}", 8.5f, false, TextoPrimario);
            }

            l.Y = yLeyenda - 16f;
            l.Texto(Margen, l.Y, $"El hotel está ocupado al {r.PorcentajeOcupacion.ToString("0", CulturaAr)} % ({r.Ocupadas} de {r.TotalHabitaciones}).", 8.5f, true, TextoPrimario);
            l.Y -= 24f;

            DibujarTablaMovimientos(l, "Salidas de hoy",
                "Ordenadas por urgencia: primero las que ya pasaron su hora de salida.",
                r.Salidas, r.Generado, "No hay salidas previstas para hoy.");

            DibujarTablaMovimientos(l, "Llegadas de hoy",
                "Huéspedes que hicieron el check-in hoy, del más reciente al más antiguo.",
                r.Llegadas, r.Generado, "Todavía no hubo check-ins hoy.");
        }

        private static void DibujarPestanaTurno(LienzoPdf l, ReporteRecepcion r)
        {
            if (!r.HayTurnoAbierto)
            {
                return;
            }

            DibujarTarjetasRecepcion(l, new[]
            {
                new TarjetaPdf("Cobrado en tu turno", Moneda0(r.TotalCobrado), $"desde las {r.InicioTurno:HH:mm}", Exito, false),
                new TarjetaPdf("Check-ins que hiciste", r.CheckInsTurno.ToString(CulturaAr),
                    r.CheckOutsTurno == 1 ? "y 1 check-out desde que abriste" : $"y {r.CheckOutsTurno} check-outs desde que abriste", Primario, false),
                new TarjetaPdf("Ventas adicionales", Moneda0(r.CobradoVentas), $"{r.CantidadVentas} ventas, {r.VentasMostrador} de mostrador", ColorAdvertencia, false)
            });

            var medios = new (string Texto, decimal Monto)[]
            {
                ("Tarjeta", r.CobradoTarjeta),
                ("Efectivo", r.CobradoEfectivo),
                ("Transferencia", r.CobradoTransferencia)
            }.OrderByDescending(m => m.Monto).Select(m => (m.Texto, (double)m.Monto, Moneda0(m.Monto))).ToArray();

            DibujarBarrasHorizontalesPdf(l, "¿Cómo te pagaron?",
                "Cada barra es un medio de pago; la más larga es la que más dinero trajo.", medios,
                "Todavía no hay cobros en este turno.");

            DibujarBarrasHorizontalesPdf(l, "¿Qué cobraste?",
                "Alojamiento cobrado en los check-ins y productos vendidos (frigobar y mostrador).",
                new[]
                {
                    ("Alojamiento", (double)r.CobradoAlojamiento, Moneda0(r.CobradoAlojamiento)),
                    ("Ventas adicionales", (double)r.CobradoVentas, Moneda0(r.CobradoVentas))
                },
                "Todavía no hay cobros en este turno.");

            // Efectivo esperado
            l.AsegurarEspacio(AltoTituloSeccion + 70f);
            DibujarTituloSeccion(l, "Efectivo que debería haber en la caja",
                "Cambio inicial + todo lo cobrado en efectivo. Contá la caja y comparalo con este número antes de cerrar.");

            l.Texto(Margen, l.Y, "Cambio inicial", 9f, false, TextoPrimario);
            l.TextoDerecha(XDerecha, l.Y, Moneda0(r.CambioInicial), 9f, false, TextoPrimario);
            l.Y -= 16f;
            l.Texto(Margen, l.Y, "+ Cobrado en efectivo", 9f, false, TextoPrimario);
            l.TextoDerecha(XDerecha, l.Y, Moneda0(r.CobradoEfectivo), 9f, false, TextoPrimario);
            l.Y -= 9f;
            l.Linea(Margen, l.Y, XDerecha, l.Y, 0.6f, Borde);
            l.Y -= 17f;
            l.Texto(Margen, l.Y, "Total esperado en efectivo", 11f, true, TextoPrimario);
            l.TextoDerecha(XDerecha, l.Y, Moneda0(r.EfectivoEsperado), 13f, true, Exito);
            l.Y -= 28f;
        }

        // ------------------------------------------------------------------
        // Bloques
        // ------------------------------------------------------------------

        private static void DibujarEncabezadoRecepcion(LienzoPdf l, DateTime generado, string nombreSeccion)
        {
            float yBase = AltoPagina - AltoBandaPrincipal;
            l.Rectangulo(0, yBase, AnchoPagina, AltoBandaPrincipal, PrimarioOscuro);
            l.Rectangulo(0, yBase, AnchoPagina, 4f, Primario);

            l.Texto(Margen, AltoPagina - 40f, NombreHotel.ToUpper(CulturaAr), 20f, true, Blanco);
            l.Texto(Margen, AltoPagina - 58f, $"Reporte de Recepción · {nombreSeccion}", 12f, false, Blanco);
            l.Texto(Margen, AltoPagina - 74f, $"{NombreSistema} · Módulo Recepción › Reportes", 8f, false, BlancoSuave);

            l.TextoDerecha(XDerecha, AltoPagina - 38f, "Emitido el", 8f, false, BlancoSuave);
            l.TextoDerecha(XDerecha, AltoPagina - 54f, generado.ToString("dd/MM/yyyy", CulturaAr), 13f, true, Blanco);
            l.TextoDerecha(XDerecha, AltoPagina - 69f, $"{generado:HH:mm} hs", 9f, false, BlancoSuave);

            l.Y = yBase - 26f;
        }

        private static void DibujarDatosRecepcion(LienzoPdf l, ReporteRecepcion r, SeccionReporteRecepcion seccion, string? generadoPor)
        {
            string periodo = seccion == SeccionReporteRecepcion.Ahora
                ? $"Situación al {r.Generado.ToString("dd/MM/yyyy", CulturaAr)} a las {r.Generado:HH:mm} hs"
                : r.HayTurnoAbierto
                    ? $"Turno abierto el {r.InicioTurno!.Value.ToString("dd/MM/yyyy", CulturaAr)} a las {r.InicioTurno.Value:HH:mm} hs"
                    : "Sin turno de caja abierto";

            DibujarCajaDatos(l, new[]
            {
                ("Qué muestra", periodo),
                ("Generado por", string.IsNullOrWhiteSpace(generadoPor) ? "-" : generadoPor!),
                ("Fecha y hora de generación", $"{r.Generado.ToString("dd/MM/yyyy HH:mm", CulturaAr)} hs"),
                ("Moneda", "Pesos argentinos (ARS)")
            });
        }

        private static void DibujarCajaDatos(LienzoPdf l, (string Etiqueta, string Valor)[] datos)
        {
            const float altoFilaDato = 27f;
            float altoCaja = (datos.Length + 1) / 2 * altoFilaDato + 14f;
            float yTop = l.Y;
            float anchoColumna = AnchoUtil / 2f;

            l.Rectangulo(Margen, yTop - altoCaja, AnchoUtil, altoCaja, FondoSuave);
            l.Rectangulo(Margen, yTop - altoCaja, 3f, altoCaja, Primario);

            for (int i = 0; i < datos.Length; i++)
            {
                float x = Margen + 16f + (i % 2) * anchoColumna;
                float y = yTop - 16f - (i / 2) * altoFilaDato;
                l.Texto(x, y, datos[i].Etiqueta.ToUpper(CulturaAr), 6.5f, true, TextoSecundario);
                l.Texto(x, y - 12f, Ajustar(datos[i].Valor, anchoColumna - 24f, 9.5f, true), 9.5f, true, TextoPrimario);
            }

            l.Y = yTop - altoCaja - 18f;
        }

        private static void DibujarResumenRecepcion(LienzoPdf l, string resumen)
        {
            const float tamano = 10f;
            const float interlineado = 14f;
            List<string> lineas = PartirEnLineas("Resumen: " + resumen, AnchoUtil - 28f, tamano, false);

            float alto = lineas.Count * interlineado + 16f;
            float yTop = l.Y;
            l.Rectangulo(Margen, yTop - alto, AnchoUtil, alto, FondoTotal);

            float y = yTop - 18f;
            foreach (string linea in lineas)
            {
                l.Texto(Margen + 14f, y, linea, tamano, false, TextoPrimario);
                y -= interlineado;
            }

            l.Y = yTop - alto - 20f;
        }

        private static void DibujarTarjetasRecepcion(LienzoPdf l, TarjetaPdf[] tarjetas)
        {
            const float separacion = 8f;
            const float altoTarjeta = 62f;
            float anchoTarjeta = (AnchoUtil - separacion * (tarjetas.Length - 1)) / tarjetas.Length;
            float yTop = l.Y;

            for (int i = 0; i < tarjetas.Length; i++)
            {
                TarjetaPdf t = tarjetas[i];
                float x = Margen + i * (anchoTarjeta + separacion);
                float anchoTexto = anchoTarjeta - 18f;

                l.Rectangulo(x, yTop - altoTarjeta, anchoTarjeta, altoTarjeta, FondoSuave);
                l.Rectangulo(x, yTop - altoTarjeta, 3f, altoTarjeta, t.Color);

                float tamTitulo = TamanoQueEntra(t.Titulo, anchoTexto, 7.5f, false, 6f);
                l.Texto(x + 11f, yTop - 15f, Ajustar(t.Titulo, anchoTexto, tamTitulo, false), tamTitulo, false, TextoSecundario);

                float tamValor = TamanoQueEntra(t.Valor, anchoTexto, 16f, true, 9f);
                l.Texto(x + 11f, yTop - 37f, Ajustar(t.Valor, anchoTexto, tamValor, true), tamValor, true, t.Alerta ? ColorPeligro : TextoPrimario);

                l.Texto(x + 11f, yTop - 52f, Ajustar(t.Contexto, anchoTexto, 7f, false), 7f, false, TextoSecundario);
            }

            l.Y = yTop - altoTarjeta - 22f;
        }

        private static void DibujarTablaMovimientos(LienzoPdf l, string titulo, string descripcion,
            List<MovimientoRecepcion> filas, DateTime ahora, string textoVacio)
        {
            l.AsegurarEspacio(AltoTituloSeccion + AltoFila * 3);
            DibujarTituloSeccion(l, titulo, descripcion);

            if (filas.Count == 0)
            {
                l.Texto(Margen, l.Y - 4f, textoVacio, 8.5f, false, TextoSecundario);
                l.Y -= 30f;
                return;
            }

            float xHab = Margen + 8f;
            float xHuesped = Margen + 70f;
            float xHora = Margen + 300f;
            float xEstado = Margen + 380f;

            void Encabezado()
            {
                l.Rectangulo(Margen, l.Y - AltoFila + 5f, AnchoUtil, AltoFila, FondoSuave);
                l.Texto(xHab, l.Y - 7f, "HAB.", 7f, true, TextoSecundario);
                l.Texto(xHuesped, l.Y - 7f, "HUÉSPED", 7f, true, TextoSecundario);
                l.Texto(xHora, l.Y - 7f, "HORA", 7f, true, TextoSecundario);
                l.Texto(xEstado, l.Y - 7f, "SITUACIÓN", 7f, true, TextoSecundario);
                l.Y -= AltoFila;
            }

            Encabezado();

            for (int i = 0; i < filas.Count; i++)
            {
                if (l.Y - AltoFila < LimiteInferior)
                {
                    l.NuevaPagina();
                    Encabezado();
                }

                MovimientoRecepcion m = filas[i];
                if (i % 2 == 1)
                {
                    l.Rectangulo(Margen, l.Y - AltoFila + 5f, AnchoUtil, AltoFila, FondoZebra);
                }

                string hora = m.Hora.Date == ahora.Date ? m.Hora.ToString("HH:mm") : m.Hora.ToString("dd/MM HH:mm", CulturaAr);
                ColorPdf colorEstado = m.Estado switch
                {
                    EstadoMovimiento.SalidaVencida => ColorPeligro,
                    EstadoMovimiento.SalidaProxima => ColorAdvertencia,
                    EstadoMovimiento.SalidaRealizada => Exito,
                    _ => TextoPrimario
                };

                float y = l.Y - 7f;
                l.Texto(xHab, y, m.NroHabitacion.ToString(CulturaAr), 8.5f, true, TextoPrimario);
                l.Texto(xHuesped, y, Ajustar(m.Huesped, xHora - xHuesped - 10f, 8.5f, false), 8.5f, false, TextoPrimario);
                l.Texto(xHora, y, hora, 8.5f, false, TextoPrimario);
                l.Texto(xEstado, y, GestionReportesRecepcion.DescribirEstado(m, ahora), 8.5f, true, colorEstado);
                l.Y -= AltoFila;
            }

            l.Y -= 22f;
        }

        private static void DibujarBarrasHorizontalesPdf(LienzoPdf l, string titulo, string descripcion,
            (string Texto, double Valor, string TextoValor)[] items, string textoVacio, ColorPdf? color = null)
        {
            const float altoBarra = 14f;
            const float paso = 22f;
            l.AsegurarEspacio(AltoTituloSeccion + Math.Max(1, items.Length) * paso + 10f);
            DibujarTituloSeccion(l, titulo, descripcion);

            double maximo = items.Length == 0 ? 0 : items.Max(i => i.Valor);
            if (maximo <= 0)
            {
                l.Texto(Margen, l.Y - 4f, textoVacio, 8.5f, false, TextoSecundario);
                l.Y -= 30f;
                return;
            }

            const float anchoEtiqueta = 110f;
            const float anchoValor = 90f;
            float anchoMaximoBarra = AnchoUtil - anchoEtiqueta - anchoValor;

            foreach (var item in items)
            {
                float yBarra = l.Y - altoBarra;
                l.Texto(Margen, yBarra + 4f, Ajustar(item.Texto, anchoEtiqueta - 8f, 9f, false), 9f, false, TextoPrimario);
                float ancho = (float)(item.Valor / maximo) * anchoMaximoBarra;
                if (ancho > 0) l.Rectangulo(Margen + anchoEtiqueta, yBarra, ancho, altoBarra, color ?? Primario);
                l.Texto(Margen + anchoEtiqueta + ancho + 8f, yBarra + 4f, item.TextoValor, 9f, true, TextoPrimario);
                l.Y -= paso;
            }

            l.Y -= 16f;
        }

        /// <summary>Columnas por día con el valor escrito, eje desde cero y línea punteada del total del hotel.</summary>
        private static void DibujarColumnasPdf(LienzoPdf l, string titulo, string descripcion, List<PuntoSerie> puntos,
            double? capacidad, int resaltado, string textoVacio)
        {
            const float altoArea = 130f;
            l.AsegurarEspacio(AltoTituloSeccion + altoArea + 40f);
            DibujarTituloSeccion(l, titulo, descripcion);

            double maximoDatos = puntos.Count == 0 ? 0 : puntos.Max(p => p.Valor);
            if (maximoDatos <= 0)
            {
                l.Texto(Margen, l.Y - 4f, textoVacio, 8.5f, false, TextoSecundario);
                l.Y -= 30f;
                return;
            }

            double maximo = Math.Max(maximoDatos, capacidad ?? 0);
            double paso = PasoEje(maximo / 4, esEntero: true);
            double tope = Math.Ceiling(maximo / paso) * paso;

            float xIzq = Margen + 22f;
            float ancho = XDerecha - xIzq;
            float yTope = l.Y - 12f;
            float yBase = yTope - altoArea;
            float Y(double v) => yBase + (float)(v / tope) * altoArea;

            for (double v = 0; v <= tope + 0.001; v += paso)
            {
                l.Linea(xIzq, Y(v), XDerecha, Y(v), 0.4f, Borde);
                l.TextoDerecha(xIzq - 5f, Y(v) - 2.5f, v.ToString("0", CulturaAr), 7f, false, TextoSecundario);
            }

            int n = puntos.Count;
            float ranura = ancho / n;
            float anchoColumna = Math.Min(30f, ranura * 0.62f);
            float tamValor = ranura >= 18f ? 7.5f : 6f;
            float tamEtiqueta = ranura >= 30f ? 7.5f : 6.5f;

            for (int i = 0; i < n; i++)
            {
                float xCentro = xIzq + i * ranura + ranura / 2f;
                double valor = puntos[i].Valor;
                if (valor > 0)
                {
                    float alto = Y(valor) - yBase;
                    l.Rectangulo(xCentro - anchoColumna / 2f, yBase, anchoColumna, alto, i == resaltado ? PrimarioOscuro : Primario);

                    string texto = valor.ToString("0", CulturaAr);
                    if (MedirTexto(texto, tamValor, true) <= ranura)
                    {
                        bool adentro = alto >= tamValor + 6f && MedirTexto(texto, tamValor, true) <= anchoColumna;
                        l.TextoCentrado(xCentro, adentro ? Y(valor) - tamValor - 2f : Y(valor) + 3f, texto, tamValor, true, adentro ? Blanco : TextoPrimario);
                    }
                }

                string etiqueta = i == resaltado && n <= 10 ? "Hoy" : puntos[i].Etiqueta;
                l.TextoCentrado(xCentro, yBase - 11f, etiqueta, tamEtiqueta, i == resaltado, i == resaltado ? TextoPrimario : TextoSecundario);
            }

            if (capacidad.HasValue && capacidad > 0)
            {
                float yCap = Y(capacidad.Value);
                for (float x = xIzq; x < XDerecha; x += 7f)
                {
                    l.Linea(x, yCap, Math.Min(x + 4f, XDerecha), yCap, 0.8f, TextoSecundario);
                }
                l.TextoDerecha(XDerecha, yCap + 4f, $"Total del hotel: {capacidad.Value:0}", 7f, false, TextoSecundario);
            }

            l.Y = yBase - 32f;
        }

        private static void DibujarPiesRecepcion(LienzoPdf l, DateTime generado, string nombreSeccion)
        {
            int total = l.Paginas.Count;
            for (int i = 0; i < total; i++)
            {
                l.IrAPagina(i);

                if (i > 0)
                {
                    float yEnc = AltoPagina - 32f;
                    l.Texto(Margen, yEnc, NombreHotel.ToUpper(CulturaAr), 9f, true, PrimarioOscuro);
                    l.TextoDerecha(XDerecha, yEnc, $"Reporte de Recepción · {nombreSeccion}", 7.5f, false, TextoSecundario);
                    l.Linea(Margen, yEnc - 8f, XDerecha, yEnc - 8f, 0.8f, Primario);
                }

                l.Linea(Margen, 44f, XDerecha, 44f, 0.5f, Borde);
                l.Texto(Margen, 31f, $"{NombreSistema} · {NombreHotel}", 7f, false, TextoSecundario);
                l.TextoCentrado(AnchoPagina / 2f, 31f, $"Generado el {generado.ToString("dd/MM/yyyy HH:mm", CulturaAr)} hs", 7f, false, TextoSecundario);
                l.TextoDerecha(XDerecha, 31f, $"Página {i + 1} de {total}", 7f, true, TextoPrimario);
            }
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private static string Moneda0(decimal valor) => valor.ToString("C0", CulturaAr);

        /// <summary>Corta un texto en renglones que entran en el ancho dado, sin partir palabras.</summary>
        private static List<string> PartirEnLineas(string texto, float anchoMax, float tamano, bool esBold)
        {
            var lineas = new List<string>();
            string actual = string.Empty;

            foreach (string palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidata = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (actual.Length > 0 && MedirTexto(candidata, tamano, esBold) > anchoMax)
                {
                    lineas.Add(actual);
                    actual = palabra;
                }
                else
                {
                    actual = candidata;
                }
            }

            if (actual.Length > 0) lineas.Add(actual);
            return lineas;
        }

        private readonly record struct TarjetaPdf(string Titulo, string Valor, string Contexto, ColorPdf Color, bool Alerta);
    }
}
