using Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Logica
{
    /// <summary>
    /// Generador de comprobantes / tickets en formato estándar PDF (PDF 1.4)
    /// sin dependencias externas. Genera un ticket en formato térmico (80 mm / 280 pt).
    /// </summary>
    public static class GeneradorTicketPdf
    {
        private const float AnchoPagina = 280f;
        private const float MargenIzq = 16f;
        private const float MargenDer = 264f;

        public static void GenerarArchivo(VentaRealizada venta, string rutaArchivo, string? emitidoPor = null)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));
            if (string.IsNullOrWhiteSpace(rutaArchivo)) throw new ArgumentException("Ruta de archivo no válida", nameof(rutaArchivo));

            byte[] bytes = GenerarBytes(venta, emitidoPor);
            File.WriteAllBytes(rutaArchivo, bytes);
        }

        // ----------------------------------------------------------------
        //  Ticket de Check-In / Comprobante de Hospedaje
        // ----------------------------------------------------------------

        /// <summary>
        /// Genera un comprobante PDF de check-in con los datos del huésped, habitación,
        /// fechas de estadía, tarifa diaria, cantidad de noches y monto total.
        /// </summary>
        public static byte[] GenerarBytesCheckIn(
            string nombreHuesped,
            string dniHuesped,
            int nroHabitacion,
            string tipoHabitacion,
            DateTime fechaEntrada,
            DateTime fechaSalida,
            TimeSpan horaSalida,
            decimal tarifaDiaria,
            int noches,
            decimal montoTotal,
            string metodoPago,
            string? emitidoPor = null)
        {
            float altoPagina = 580f;
            var sbContent = new StringBuilder();
            float y = altoPagina - 26f;

            // --- Encabezado Hotel ---
            EscribirCentrado(sbContent, "HOTEL SGH", 13f, esBold: true, y, AnchoPagina);
            y -= 15f;
            EscribirCentrado(sbContent, "Sistema de Gestion Hotelera", 8f, esBold: false, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Av. San Martin 123 - Tel: (379) 412-3456", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "IVA Resp. Inscripto - CUIT: 30-71234567-8", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            EscribirCentrado(sbContent, "COMPROBANTE DE HOSPEDAJE", 10f, esBold: true, y, AnchoPagina);
            y -= 15f;

            EscribirTexto(sbContent, MargenIzq, y, "Fecha emision:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 8f, esBold: false);
            y -= 12f;

            if (!string.IsNullOrWhiteSpace(emitidoPor))
            {
                EscribirTexto(sbContent, MargenIzq, y, "Atendido por:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(emitidoPor, 26), 8f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos del Huesped ---
            EscribirCentrado(sbContent, "DATOS DEL HUESPED", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Nombre:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(nombreHuesped, 26), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "DNI:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, dniHuesped, 8f, esBold: false);
            y -= 12f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos de la Habitacion y Estadia ---
            EscribirCentrado(sbContent, "DETALLE DE LA ESTADIA", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Habitacion:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, $"Nro {nroHabitacion} - {Truncar(tipoHabitacion, 18)}", 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Check-in:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, fechaEntrada.ToString("dd/MM/yyyy HH:mm"), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Check-out:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y,
                $"{fechaSalida:dd/MM/yyyy} {horaSalida:hh\\:mm}", 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Noches:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, noches.ToString(), 8f, esBold: false);
            y -= 12f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 14f;

            // --- Desglose del Costo ---
            EscribirTexto(sbContent, MargenIzq, y, "Tarifa por noche:", 8f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y,
                tarifaDiaria.ToString("C", CultureInfo.CurrentCulture), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, $"{noches} noche(s) x tarifa:", 8f, esBold: false);
            EscribirAlineadoDerecha(sbContent, MargenDer, y,
                montoTotal.ToString("C", CultureInfo.CurrentCulture), 8f, esBold: false);
            y -= 12f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 16f;

            EscribirTexto(sbContent, MargenIzq, y, "TOTAL:", 11f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y,
                montoTotal.ToString("C", CultureInfo.CurrentCulture), 11f, esBold: true);
            y -= 15f;

            EscribirTexto(sbContent, MargenIzq, y, "Forma de pago:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(metodoPago, 22), 8f, esBold: false);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 15f;

            // --- Pie ---
            EscribirCentrado(sbContent, "Bienvenido al Hotel SGH", 8f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "Comprobante de hospedaje interno", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, "No valido como factura fiscal", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, $"Impreso el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 6.5f, esBold: false, y, AnchoPagina);

            return EnsamblarDocumentoPdf(sbContent.ToString(), AnchoPagina, altoPagina);
        }

        // ----------------------------------------------------------------
        //  Ticket de Prórroga / Extensión de Estadía
        // ----------------------------------------------------------------

        /// <summary>
        /// Genera el ticket comprobante de prórroga de estadía con los datos de salida anterior,
        /// nueva fecha/hora de salida, noches adicionales e importe abonado.
        /// </summary>
        public static byte[] GenerarBytesProrrogaEstadia(
            string nombreHuesped,
            string dniHuesped,
            int nroHabitacion,
            string tipoHabitacion,
            DateTime fechaSalidaAnterior,
            TimeSpan horaSalidaAnterior,
            DateTime nuevaFechaSalida,
            TimeSpan nuevaHoraSalida,
            int diasAdicionales,
            decimal tarifaDiaria,
            decimal montoAdicional,
            decimal montoTotalAcumulado,
            string metodoPago,
            string? emitidoPor = null)
        {
            float altoPagina = 620f;
            var sbContent = new StringBuilder();
            float y = altoPagina - 26f;

            // --- Encabezado Hotel ---
            EscribirCentrado(sbContent, "HOTEL SGH", 13f, esBold: true, y, AnchoPagina);
            y -= 15f;
            EscribirCentrado(sbContent, "Sistema de Gestion Hotelera", 8f, esBold: false, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Av. San Martin 123 - Tel: (379) 412-3456", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "IVA Resp. Inscripto - CUIT: 30-71234567-8", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            EscribirCentrado(sbContent, "PRORROGA DE ESTADIA", 10.5f, esBold: true, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Comprobante de Extension de Hospedaje", 7.5f, esBold: false, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Fecha emision:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 8f, esBold: false);
            y -= 12f;

            if (!string.IsNullOrWhiteSpace(emitidoPor))
            {
                EscribirTexto(sbContent, MargenIzq, y, "Atendido por:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(emitidoPor, 26), 8f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos del Huésped ---
            EscribirCentrado(sbContent, "DATOS DEL HUESPED", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Huesped:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(nombreHuesped, 26), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "DNI:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, dniHuesped, 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Habitacion:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, $"Hab. {nroHabitacion} - {Truncar(tipoHabitacion, 18)}", 8f, esBold: false);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 14f;

            // --- Detalle de la Prórroga ---
            EscribirCentrado(sbContent, "DETALLE DE LA PRORROGA", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Salida pactada anterior:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 110f, y, $"{fechaSalidaAnterior:dd/MM/yyyy} {horaSalidaAnterior:hh\\:mm}", 7.5f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Nueva salida acordada:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 110f, y, $"{nuevaFechaSalida:dd/MM/yyyy} {nuevaHoraSalida:hh\\:mm}", 7.5f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Noches adicionales:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 110f, y, $"{diasAdicionales} noche(s)", 7.5f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Tarifa diaria:", 7.5f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, tarifaDiaria.ToString("C", CultureInfo.CurrentCulture), 7.5f, esBold: false);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Liquidación de la prórroga ---
            EscribirTexto(sbContent, MargenIzq, y, $"{diasAdicionales} noche(s) x {tarifaDiaria:C}:", 8f, esBold: false);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, montoAdicional.ToString("C", CultureInfo.CurrentCulture), 8f, esBold: false);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "TOTAL ABONADO:", 10.5f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, montoAdicional.ToString("C", CultureInfo.CurrentCulture), 10.5f, esBold: true);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Metodo de pago:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(metodoPago, 22), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Total acumulado estadia:", 7.5f, esBold: false);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, montoTotalAcumulado.ToString("C", CultureInfo.CurrentCulture), 7.5f, esBold: true);
            y -= 15f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 15f;

            // --- Pie ---
            EscribirCentrado(sbContent, "Gracias por continuar su estadia con nosotros", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "Comprobante de prorroga interno", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, "No valido como factura fiscal", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, $"Impreso el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 6.5f, esBold: false, y, AnchoPagina);

            return EnsamblarDocumentoPdf(sbContent.ToString(), AnchoPagina, altoPagina);
        }

        // ----------------------------------------------------------------
        //  Ticket Final de Liquidación / Check-Out
        // ----------------------------------------------------------------

        /// <summary>
        /// Genera el ticket final de liquidación al finalizar el check-out detallando:
        /// días pactados originales, días extras cobrados por vencimiento (tolerancia 2 hs),
        /// consumos/ventas adicionales y el importe total cancelado.
        /// </summary>
        public static byte[] GenerarBytesLiquidacionCheckOut(
            LiquidacionCheckOut liquidacion,
            string? emitidoPor = null,
            string? metodoPagoExtras = null)
        {
            if (liquidacion == null) throw new ArgumentNullException(nameof(liquidacion));

            int cantidadConsumos = Math.Max(0, liquidacion.Consumos.Count);
            float altoPagina = Math.Max(660f, 620f + (cantidadConsumos * 14f));
            var sbContent = new StringBuilder();
            float y = altoPagina - 26f;

            // --- Encabezado Hotel ---
            EscribirCentrado(sbContent, "HOTEL SGH", 13f, esBold: true, y, AnchoPagina);
            y -= 15f;
            EscribirCentrado(sbContent, "Sistema de Gestion Hotelera", 8f, esBold: false, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Av. San Martin 123 - Tel: (379) 412-3456", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "IVA Resp. Inscripto - CUIT: 30-71234567-8", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            EscribirCentrado(sbContent, "TICKET FINAL DE LIQUIDACION", 10.5f, esBold: true, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Check-Out y Cierre de Estadia", 7.5f, esBold: false, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Fecha liquidacion:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 85f, y, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 8f, esBold: false);
            y -= 12f;

            if (!string.IsNullOrWhiteSpace(emitidoPor))
            {
                EscribirTexto(sbContent, MargenIzq, y, "Atendido por:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 85f, y, Truncar(emitidoPor, 26), 8f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos del Huésped y Habitación ---
            EscribirCentrado(sbContent, "DATOS DE LA ESTADIA", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, "Huesped:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, Truncar(liquidacion.NombreHuesped, 26), 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "DNI:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, liquidacion.DniHuesped, 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Habitacion:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, $"Hab. {liquidacion.NroHabitacion} ({Truncar(liquidacion.TipoHabitacion, 16)})", 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Tarifa diaria:", 8f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, liquidacion.TarifaDiaria.ToString("C", CultureInfo.CurrentCulture), 8f, esBold: false);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 14f;

            // --- Cronograma de Estadía ---
            EscribirTexto(sbContent, MargenIzq, y, "Ingreso:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, (liquidacion.FechaIngreso.Date + liquidacion.HoraIngreso).ToString("dd/MM/yyyy HH:mm"), 7.5f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Salida pactada:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, (liquidacion.FechaSalidaPactada.Date + liquidacion.HoraSalidaPactada).ToString("dd/MM/yyyy HH:mm"), 7.5f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Salida real:", 7.5f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 80f, y, (liquidacion.FechaSalidaReal.Date + liquidacion.HoraSalidaReal).ToString("dd/MM/yyyy HH:mm"), 7.5f, esBold: false);
            y -= 12f;

            // Evaluación de tolerancia y retraso
            if (liquidacion.Retraso <= TimeSpan.Zero)
            {
                EscribirTexto(sbContent, MargenIzq, y, "Puntualidad:", 7.5f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 80f, y, "Salida a tiempo (sin retraso)", 7.5f, esBold: false);
                y -= 12f;
            }
            else if (!liquidacion.SuperaTolerancia)
            {
                EscribirTexto(sbContent, MargenIzq, y, "Tolerancia:", 7.5f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 80f, y, $"Retraso de {liquidacion.Retraso:hh\\:mm} (<= 2 hs tolerancia)", 7.5f, esBold: false);
                y -= 12f;
            }
            else
            {
                EscribirTexto(sbContent, MargenIzq, y, "Vencimiento:", 7.5f, esBold: true);
                int horasTotales = (int)liquidacion.Retraso.TotalHours;
                int minutos = liquidacion.Retraso.Minutes;
                EscribirTexto(sbContent, MargenIzq + 80f, y, $"Retraso de {horasTotales}h {minutos}m (> 2 hs)", 7.5f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Liquidación de Días y Hospedaje ---
            EscribirCentrado(sbContent, "DESGLOSE DE HOSPEDAJE", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            EscribirTexto(sbContent, MargenIzq, y, $"Dias pactados ({liquidacion.DiasPactadosOriginales} d):", 7.5f, esBold: false);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, liquidacion.MontoHospedajeOriginal.ToString("C", CultureInfo.CurrentCulture), 7.5f, esBold: false);
            y -= 12f;

            if (liquidacion.DiasExtras > 0)
            {
                EscribirTexto(sbContent, MargenIzq, y, $"Dias extras vencimiento ({liquidacion.DiasExtras} d):", 7.5f, esBold: true);
                EscribirAlineadoDerecha(sbContent, MargenDer, y, liquidacion.MontoDiasExtras.ToString("C", CultureInfo.CurrentCulture), 7.5f, esBold: true);
                y -= 12f;
            }
            else
            {
                EscribirTexto(sbContent, MargenIzq, y, "Dias extras por retraso:", 7.5f, esBold: false);
                EscribirAlineadoDerecha(sbContent, MargenDer, y, "$0,00", 7.5f, esBold: false);
                y -= 12f;
            }

            // --- Consumos / Ventas Adicionales ---
            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;
            EscribirCentrado(sbContent, "CONSUMOS DURANTE LA ESTADIA", 8.5f, esBold: true, y, AnchoPagina);
            y -= 14f;

            if (liquidacion.Consumos.Count == 0)
            {
                EscribirTexto(sbContent, MargenIzq, y, "Sin consumos adicionales registrados.", 7.5f, esBold: false);
                y -= 12f;
            }
            else
            {
                foreach (VentaRealizada venta in liquidacion.Consumos)
                {
                    string resumen = $"{venta.Momento:dd/MM HH:mm} - {Truncar(venta.ResumenProductos, 22)}";
                    EscribirTexto(sbContent, MargenIzq, y, resumen, 7f, esBold: false);
                    EscribirAlineadoDerecha(sbContent, MargenDer, y, venta.Total.ToString("C", CultureInfo.CurrentCulture), 7f, esBold: false);
                    y -= 11f;
                }
            }

            EscribirTexto(sbContent, MargenIzq, y, "Subtotal consumos:", 7.5f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, liquidacion.TotalConsumos.ToString("C", CultureInfo.CurrentCulture), 7.5f, esBold: true);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 15f;

            // --- Total Liquidado ---
            EscribirTexto(sbContent, MargenIzq, y, "TOTAL CANCELADO:", 11f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, liquidacion.TotalLiquidacion.ToString("C", CultureInfo.CurrentCulture), 11f, esBold: true);
            y -= 14f;

            if (liquidacion.DiasExtras > 0 && !string.IsNullOrWhiteSpace(metodoPagoExtras))
            {
                EscribirTexto(sbContent, MargenIzq, y, "Pago recargo dias extras:", 7.5f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 110f, y, Truncar(metodoPagoExtras, 20), 7.5f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 15f;

            // --- Pie ---
            EscribirCentrado(sbContent, "Esperamos que haya disfrutado de su estadia", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "Hotel SGH - Siempre a su servicio", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, "Comprobante de check-out y liquidacion interna", 6.5f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, $"Impreso el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 6.5f, esBold: false, y, AnchoPagina);

            return EnsamblarDocumentoPdf(sbContent.ToString(), AnchoPagina, altoPagina);
        }

        // ----------------------------------------------------------------
        //  Ticket de Venta
        // ----------------------------------------------------------------

        public static byte[] GenerarBytes(VentaRealizada venta, string? emitidoPor = null)
        {
            if (venta == null) throw new ArgumentNullException(nameof(venta));

            int cantidadItems = Math.Max(1, venta.Items.Count);
            float altoPagina = Math.Max(460f, 370f + (cantidadItems * 18f));

            var sbContent = new StringBuilder();
            float y = altoPagina - 26f;

            // --- Encabezado Hotel ---
            EscribirCentrado(sbContent, "HOTEL SGH", 13f, esBold: true, y, AnchoPagina);
            y -= 15f;
            EscribirCentrado(sbContent, "Sistema de Gestión Hotelera", 8f, esBold: false, y, AnchoPagina);
            y -= 12f;
            EscribirCentrado(sbContent, "Av. San Martín 123 · Tel: (379) 412-3456", 7.5f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "IVA Resp. Inscripto · CUIT: 30-71234567-8", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;

            // Línea divisoria discontinua
            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos del Ticket ---
            EscribirCentrado(sbContent, "TICKET DE VENTA", 10f, esBold: true, y, AnchoPagina);
            y -= 15f;

            EscribirTexto(sbContent, MargenIzq, y, "Comprobante N°:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 75f, y, $"0001 - {venta.IdVenta:D8}", 8f, esBold: false);
            y -= 12f;

            EscribirTexto(sbContent, MargenIzq, y, "Fecha y hora:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 75f, y, venta.Momento.ToString("dd/MM/yyyy HH:mm"), 8f, esBold: false);
            y -= 12f;

            if (!string.IsNullOrWhiteSpace(emitidoPor))
            {
                EscribirTexto(sbContent, MargenIzq, y, "Atendido por:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 75f, y, Truncar(emitidoPor, 26), 8f, esBold: false);
                y -= 12f;
            }

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 14f;

            // --- Datos del Cliente ---
            if (venta.EsDeMostrador)
            {
                EscribirTexto(sbContent, MargenIzq, y, "Tipo cliente:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 75f, y, "Venta de Mostrador", 8f, esBold: false);
                y -= 13f;
            }
            else
            {
                EscribirTexto(sbContent, MargenIzq, y, "Huésped:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 75f, y, Truncar(venta.NombreHuesped ?? "-", 26), 8f, esBold: false);
                y -= 12f;

                EscribirTexto(sbContent, MargenIzq, y, "DNI:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 75f, y, venta.DniHuesped ?? "-", 8f, esBold: false);
                y -= 12f;

                EscribirTexto(sbContent, MargenIzq, y, "Habitación:", 8f, esBold: true);
                EscribirTexto(sbContent, MargenIzq + 75f, y, venta.NroHabitacion.HasValue ? $"Hab. {venta.NroHabitacion.Value}" : "-", 8f, esBold: false);
                y -= 13f;
            }

            // Línea continua antes de la tabla de items
            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 13f;

            // --- Encabezados de tabla de ítems ---
            EscribirTexto(sbContent, MargenIzq, y, "CANT. DESCRIPCIÓN", 7.5f, esBold: true);
            EscribirAlineadoDerecha(sbContent, 205f, y, "P.UNIT", 7.5f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, "SUBTOTAL", 7.5f, esBold: true);
            y -= 6f;
            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 12f;

            // --- Renglones de productos ---
            if (venta.Items.Count == 0)
            {
                EscribirTexto(sbContent, MargenIzq, y, "Consumos varios", 8f, esBold: false);
                EscribirAlineadoDerecha(sbContent, MargenDer, y, venta.Total.ToString("C", CultureInfo.CurrentCulture), 8f, esBold: false);
                y -= 14f;
            }
            else
            {
                foreach (ItemConsumido item in venta.Items)
                {
                    string descripcion = $"{item.Cantidad}x {Truncar(item.Producto, 18)}";
                    EscribirTexto(sbContent, MargenIzq, y, descripcion, 7.5f, esBold: false);
                    EscribirAlineadoDerecha(sbContent, 205f, y, item.PrecioUnitario.ToString("N2", CultureInfo.CurrentCulture), 7.5f, esBold: false);
                    EscribirAlineadoDerecha(sbContent, MargenDer, y, item.Subtotal.ToString("N2", CultureInfo.CurrentCulture), 7.5f, esBold: false);
                    y -= 14f;
                }
            }

            // Línea discontinua antes del total
            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: true);
            y -= 16f;

            // --- Total y Forma de Pago ---
            EscribirTexto(sbContent, MargenIzq, y, "TOTAL:", 11f, esBold: true);
            EscribirAlineadoDerecha(sbContent, MargenDer, y, venta.Total.ToString("C", CultureInfo.CurrentCulture), 11f, esBold: true);
            y -= 15f;

            EscribirTexto(sbContent, MargenIzq, y, "Forma de pago:", 8f, esBold: true);
            EscribirTexto(sbContent, MargenIzq + 75f, y, venta.MetodoPago, 8f, esBold: false);
            y -= 14f;

            DibujarLinea(sbContent, MargenIzq, MargenDer, y, esPunteada: false);
            y -= 15f;

            // --- Pie del comprobante ---
            EscribirCentrado(sbContent, "¡Gracias por su compra!", 8f, esBold: false, y, AnchoPagina);
            y -= 11f;
            EscribirCentrado(sbContent, "Comprobante de consumo interno", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, "No válido como factura fiscal", 7f, esBold: false, y, AnchoPagina);
            y -= 10f;
            EscribirCentrado(sbContent, $"Impreso el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 6.5f, esBold: false, y, AnchoPagina);

            return EnsamblarDocumentoPdf(sbContent.ToString(), AnchoPagina, altoPagina);
        }

        private static void EscribirTexto(StringBuilder sb, float x, float y, string texto, float tamano, bool esBold)
        {
            sb.Append("BT\r\n");
            sb.Append(esBold ? "/F2 " : "/F1 ").Append(tamano.ToString("0.#", CultureInfo.InvariantCulture)).Append(" Tf\r\n");
            sb.Append(x.ToString("0.#", CultureInfo.InvariantCulture)).Append(' ')
              .Append(y.ToString("0.#", CultureInfo.InvariantCulture)).Append(" Td\r\n");
            sb.Append('(').Append(EscaparPdf(texto)).Append(") Tj\r\n");
            sb.Append("ET\r\n");
        }

        private static void EscribirCentrado(StringBuilder sb, string texto, float tamano, bool esBold, float y, float anchoPagina)
        {
            float ancho = MedirTexto(texto, tamano, esBold);
            float x = Math.Max(MargenIzq, (anchoPagina - ancho) / 2f);
            EscribirTexto(sb, x, y, texto, tamano, esBold);
        }

        private static void EscribirAlineadoDerecha(StringBuilder sb, float xDer, float y, string texto, float tamano, bool esBold)
        {
            float ancho = MedirTexto(texto, tamano, esBold);
            float x = Math.Max(MargenIzq, xDer - ancho);
            EscribirTexto(sb, x, y, texto, tamano, esBold);
        }

        private static void DibujarLinea(StringBuilder sb, float x1, float x2, float y, bool esPunteada)
        {
            sb.Append("q\r\n");
            sb.Append("0.6 w\r\n");
            if (esPunteada)
            {
                sb.Append("[2 2] 0 d\r\n");
            }
            sb.Append(x1.ToString("0.#", CultureInfo.InvariantCulture)).Append(' ')
              .Append(y.ToString("0.#", CultureInfo.InvariantCulture)).Append(" m ");
            sb.Append(x2.ToString("0.#", CultureInfo.InvariantCulture)).Append(' ')
              .Append(y.ToString("0.#", CultureInfo.InvariantCulture)).Append(" l S\r\n");
            sb.Append("Q\r\n");
        }

        private static float MedirTexto(string texto, float tamano, bool esBold)
        {
            if (string.IsNullOrEmpty(texto)) return 0f;
            float ancho = 0f;
            foreach (char c in texto)
            {
                float charWidth = c switch
                {
                    ' ' => 278f,
                    '.' or ',' or ':' or ';' or '!' or '|' or '\'' => 278f,
                    'i' or 'l' or 'j' or 't' or 'r' or 'f' => 333f,
                    'm' or 'w' or 'M' or 'W' => 850f,
                    >= '0' and <= '9' => 556f,
                    '$' or '%' or '#' or '+' or '=' => 556f,
                    >= 'A' and <= 'Z' => 667f,
                    _ => 500f
                };
                if (esBold) charWidth *= 1.08f;
                ancho += (charWidth / 1000f) * tamano;
            }
            return ancho;
        }

        private static string Truncar(string texto, int maxLongitud)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            return texto.Length <= maxLongitud ? texto : texto.Substring(0, maxLongitud - 2) + "..";
        }

        private static string EscaparPdf(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            var sb = new StringBuilder(texto.Length + 10);
            foreach (char c in texto)
            {
                switch (c)
                {
                    case '(': sb.Append(@"\("); break;
                    case ')': sb.Append(@"\)"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '\r': case '\n': break;
                    default:
                        if (c >= 32 && c <= 126)
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            byte b = c switch
                            {
                                'á' => 0xE1,
                                'é' => 0xE9,
                                'í' => 0xED,
                                'ó' => 0xF3,
                                'ú' => 0xFA,
                                'Á' => 0xC1,
                                'É' => 0xC9,
                                'Í' => 0xCD,
                                'Ó' => 0xD3,
                                'Ú' => 0xDA,
                                'ñ' => 0xF1,
                                'Ñ' => 0xD1,
                                'ü' => 0xFC,
                                'Ü' => 0xDC,
                                '°' => 0xB0,
                                '¿' => 0xBF,
                                '¡' => 0xA1,
                                '•' => 0x95,
                                _ => (c <= 255) ? (byte)c : (byte)'?'
                            };
                            sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private static byte[] EnsamblarDocumentoPdf(string streamContent, float ancho, float alto)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII);
            var offsets = new List<long>();

            void RegistrarObjeto(string contenidoObjeto)
            {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.Write(contenidoObjeto);
            }

            writer.Write("%PDF-1.4\r\n");

            // Obj 1: Catalog
            RegistrarObjeto("1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n");

            // Obj 2: Pages
            RegistrarObjeto("2 0 obj\r\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\r\nendobj\r\n");

            // Obj 3: Page
            string mediaBox = $"[0 0 {ancho.ToString("0.#", CultureInfo.InvariantCulture)} {alto.ToString("0.#", CultureInfo.InvariantCulture)}]";
            RegistrarObjeto($"3 0 obj\r\n<< /Type /Page /Parent 2 0 R /MediaBox {mediaBox} /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\r\nendobj\r\n");

            // Obj 4: Content stream
            byte[] streamBytes = Encoding.ASCII.GetBytes(streamContent);
            RegistrarObjeto($"4 0 obj\r\n<< /Length {streamBytes.Length} >>\r\nstream\r\n{streamContent}\r\nendstream\r\nendobj\r\n");

            // Obj 5: Font F1 (Helvetica)
            RegistrarObjeto("5 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\r\nendobj\r\n");

            // Obj 6: Font F2 (Helvetica-Bold)
            RegistrarObjeto("6 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\r\nendobj\r\n");

            writer.Flush();
            long xrefOffset = ms.Position;

            writer.Write("xref\r\n");
            writer.Write($"0 {offsets.Count + 1}\r\n");
            writer.Write("0000000000 65535 f \r\n");
            foreach (long offset in offsets)
            {
                writer.Write($"{offset:D10} 00000 n \r\n");
            }

            writer.Write($"trailer\r\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\r\nstartxref\r\n{xrefOffset}\r\n%%EOF\r\n");
            writer.Flush();

            return ms.ToArray();
        }
    }
}
