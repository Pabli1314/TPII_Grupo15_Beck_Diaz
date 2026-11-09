using Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Logica
{
    /// <summary>
    /// Genera el reporte de gestión del Administrador en formato PDF (PDF 1.4), A4 vertical y multipágina,
    /// sin dependencias externas (mismo enfoque que <see cref="GeneradorTicketPdf"/>).
    /// Incluye datos generales, indicadores clave, gráficos de barras vectoriales y tablas
    /// que repiten sus encabezados al continuar en una nueva página.
    /// </summary>
    public static partial class GeneradorReportePdf
    {
        private const string NombreHotel = "Hotel SGH";
        private const string NombreSistema = "Sistema de Gestión Hotelera";
        private const string TituloReporte = "Reporte de Gestión Hotelera";

        // A4 en puntos (1 pt = 1/72")
        private const float AnchoPagina = 595.28f;
        private const float AltoPagina = 841.89f;
        private const float Margen = 42f;
        private const float AnchoUtil = AnchoPagina - 2 * Margen;
        private const float XDerecha = AnchoPagina - Margen;

        private const float AltoBandaPrincipal = 92f;
        private const float InicioContenidoContinuacion = AltoPagina - 62f;
        private const float LimiteInferior = 60f;
        private const float AltoFila = 17f;
        private const float AltoTituloSeccion = 46f;
        private const float AltoAreaGrafico = 130f;
        private const float AltoBloqueGrafico = AltoAreaGrafico + 34f;

        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");
        private static readonly Encoding WinAnsi = CrearCodificacionWinAnsi();

        private static readonly ColorPdf Blanco = new(255, 255, 255);
        private static readonly ColorPdf BlancoSuave = new(191, 219, 254);
        private static readonly ColorPdf Primario = new(37, 99, 235);
        private static readonly ColorPdf PrimarioOscuro = new(30, 58, 138);
        private static readonly ColorPdf Exito = new(25, 135, 84);
        private static readonly ColorPdf Mantenimiento = new(79, 134, 247);
        private static readonly ColorPdf Limpieza = new(234, 170, 0);
        private static readonly ColorPdf Violeta = new(124, 58, 237);
        private static readonly ColorPdf TextoPrimario = new(17, 24, 39);
        private static readonly ColorPdf TextoSecundario = new(107, 114, 128);
        private static readonly ColorPdf Borde = new(229, 231, 235);
        private static readonly ColorPdf FondoSuave = new(243, 244, 246);
        private static readonly ColorPdf FondoZebra = new(249, 250, 251);
        private static readonly ColorPdf FondoTotal = new(219, 234, 254);

        /// <summary>Indica si el reporte tiene información suficiente para exportarse.</summary>
        public static bool TieneDatos(ReporteHotel? reporte) => reporte != null && reporte.TotalOcupaciones > 0;

        public static void GenerarArchivo(ReporteHotel reporte, string rutaArchivo, string filtroAplicado, string? generadoPor = null)
        {
            if (reporte == null) throw new ArgumentNullException(nameof(reporte));
            if (string.IsNullOrWhiteSpace(rutaArchivo)) throw new ArgumentException("Ruta de archivo no válida", nameof(rutaArchivo));

            // Se genera completo en memoria antes de escribir, para no dejar archivos a medio generar.
            byte[] bytes = GenerarBytes(reporte, filtroAplicado, generadoPor);
            File.WriteAllBytes(rutaArchivo, bytes);
        }

        public static byte[] GenerarBytes(ReporteHotel reporte, string filtroAplicado, string? generadoPor = null)
        {
            if (reporte == null) throw new ArgumentNullException(nameof(reporte));

            DateTime generado = DateTime.Now;
            List<SeccionReporte> secciones = CrearSecciones(reporte);
            int totalRegistros = secciones.Sum(s => s.Puntos.Count);

            var lienzo = new LienzoPdf();
            lienzo.NuevaPagina();

            DibujarEncabezadoPrincipal(lienzo, generado);
            DibujarDatosGenerales(lienzo, reporte, filtroAplicado, generadoPor, generado, totalRegistros);
            DibujarIndicadores(lienzo, reporte);

            foreach (SeccionReporte seccion in secciones)
            {
                DibujarSeccion(lienzo, seccion);
            }

            DibujarEncabezadosYPies(lienzo, reporte, generado);

            return EnsamblarDocumentoPdf(lienzo.Paginas, generado);
        }

        // ------------------------------------------------------------------
        // Contenido del reporte
        // ------------------------------------------------------------------

        private static List<SeccionReporte> CrearSecciones(ReporteHotel r)
        {
            double totalIngresos = (double)r.TotalIngresos;

            return new List<SeccionReporte>
            {
                new("Ocupación diaria",
                    "Habitaciones ocupadas en cada día del período consultado.",
                    "Fecha", "Habitaciones ocupadas", r.OcupacionDiaria,
                    (i, p) => DescribirDia(r.Desde.AddDays(i)),
                    FormatoEntero, FormatoEntero, EsEntero: true, Primario,
                    TotalParaPorcentaje: null, EtiquetaTotal: null),

                new("Ingresos por día",
                    "Ingresos estimados agrupados por fecha de ingreso, según la tarifa vigente de cada tipo de habitación.",
                    "Fecha de ingreso", "Ingresos", r.IngresosDiarios,
                    (i, p) => CompletarAnio(p.Etiqueta, r.Hasta),
                    FormatoMoneda, FormatoMonedaCompacta, EsEntero: false, Exito,
                    TotalParaPorcentaje: totalIngresos, EtiquetaTotal: "Total del período"),

                new("Ranking de ingresos por habitación",
                    "Las 10 habitaciones con mayores ingresos estimados en el período.",
                    "Habitación", "Ingresos", r.IngresosPorHabitacion,
                    (i, p) => p.Etiqueta,
                    FormatoMoneda, FormatoMonedaCompacta, EsEntero: false, Mantenimiento,
                    TotalParaPorcentaje: totalIngresos, EtiquetaTotal: "Total del ranking"),

                new("Rotación por habitación",
                    "Cantidad de estadías registradas por habitación en el período.",
                    "Habitación", "Estadías", r.RotacionPorHabitacion,
                    (i, p) => p.Etiqueta,
                    FormatoEntero, FormatoEntero, EsEntero: true, Limpieza,
                    TotalParaPorcentaje: r.TotalOcupaciones, EtiquetaTotal: "Total de estadías"),
            };
        }

        private static void DibujarEncabezadoPrincipal(LienzoPdf l, DateTime generado)
        {
            float yBase = AltoPagina - AltoBandaPrincipal;
            l.Rectangulo(0, yBase, AnchoPagina, AltoBandaPrincipal, PrimarioOscuro);
            l.Rectangulo(0, yBase, AnchoPagina, 4f, Primario);

            l.Texto(Margen, AltoPagina - 40f, NombreHotel.ToUpper(CulturaAr), 20f, true, Blanco);
            l.Texto(Margen, AltoPagina - 58f, TituloReporte, 12f, false, Blanco);
            l.Texto(Margen, AltoPagina - 74f, $"{NombreSistema} · Módulo Administración › Reportes", 8f, false, BlancoSuave);

            l.TextoDerecha(XDerecha, AltoPagina - 38f, "Emitido el", 8f, false, BlancoSuave);
            l.TextoDerecha(XDerecha, AltoPagina - 54f, generado.ToString("dd/MM/yyyy", CulturaAr), 13f, true, Blanco);
            l.TextoDerecha(XDerecha, AltoPagina - 69f, $"{generado:HH:mm} hs", 9f, false, BlancoSuave);

            l.Y = yBase - 26f;
        }

        private static void DibujarDatosGenerales(LienzoPdf l, ReporteHotel r, string filtro, string? generadoPor, DateTime generado, int totalRegistros)
        {
            DibujarTituloSeccion(l, "Información del reporte", null);

            var datos = new (string Etiqueta, string Valor)[]
            {
                ("Período consultado", DescribirPeriodo(r)),
                ("Filtro aplicado", string.IsNullOrWhiteSpace(filtro) ? "-" : filtro),
                ("Fecha y hora de generación", $"{generado.ToString("dd/MM/yyyy HH:mm:ss", CulturaAr)} hs"),
                ("Generado por", string.IsNullOrWhiteSpace(generadoPor) ? "-" : generadoPor!),
                ("Registros exportados", totalRegistros.ToString("N0", CulturaAr)),
                ("Moneda", "Pesos argentinos (ARS)")
            };

            const float altoFilaDato = 27f;
            int filas = (datos.Length + 1) / 2;
            float altoCaja = filas * altoFilaDato + 14f;
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

            l.Y = yTop - altoCaja - 22f;
        }

        private static void DibujarIndicadores(LienzoPdf l, ReporteHotel r)
        {
            DibujarTituloSeccion(l, "Indicadores clave", null);

            var kpis = new (string Titulo, string Valor, ColorPdf Color)[]
            {
                ("Total de ocupaciones", r.TotalOcupaciones.ToString("N0", CulturaAr), Primario),
                ("Total de ingresos", FormatoMoneda((double)r.TotalIngresos), Exito),
                ("Promedio de ocupación", $"{r.PromedioOcupacionPorcentaje.ToString("0.#", CulturaAr)} %", Mantenimiento),
                ("Promedio de duración", $"{r.PromedioDuracionEstadiaDias.ToString("0.#", CulturaAr)} noches", Violeta),
                ("Promedio de limpieza", $"{r.PromedioLimpiezaMinutos.ToString("0", CulturaAr)} min", Limpieza)
            };

            const float separacion = 8f;
            const float altoTarjeta = 54f;
            float anchoTarjeta = (AnchoUtil - separacion * (kpis.Length - 1)) / kpis.Length;
            float yTop = l.Y;

            for (int i = 0; i < kpis.Length; i++)
            {
                float x = Margen + i * (anchoTarjeta + separacion);
                float anchoTexto = anchoTarjeta - 16f;

                l.Rectangulo(x, yTop - altoTarjeta, anchoTarjeta, altoTarjeta, FondoSuave);
                l.Rectangulo(x, yTop - altoTarjeta, 3f, altoTarjeta, kpis[i].Color);

                float tamTitulo = TamanoQueEntra(kpis[i].Titulo, anchoTexto, 7f, false, 5.5f);
                l.Texto(x + 10f, yTop - 16f, Ajustar(kpis[i].Titulo, anchoTexto, tamTitulo, false), tamTitulo, false, TextoSecundario);

                float tamValor = TamanoQueEntra(kpis[i].Valor, anchoTexto, 13f, true, 7.5f);
                l.Texto(x + 10f, yTop - 38f, Ajustar(kpis[i].Valor, anchoTexto, tamValor, true), tamValor, true, TextoPrimario);
            }

            l.Y = yTop - altoTarjeta - 14f;
            l.Texto(Margen, l.Y, "Nota: los ingresos son estimados según la tarifa vigente asociada al tipo de cada habitación y la cantidad de noches de cada estadía.",
                6.8f, false, TextoSecundario);
            l.Y -= 26f;
        }

        private static void DibujarSeccion(LienzoPdf l, SeccionReporte s)
        {
            bool hayValores = s.Puntos.Any(p => p.Valor > 0);

            // Título + gráfico + primeras filas van juntos: si no entran, la sección arranca en página nueva.
            l.AsegurarEspacio(AltoTituloSeccion + (hayValores ? AltoBloqueGrafico : 0f) + AltoFila * 3);
            DibujarTituloSeccion(l, s.Titulo, s.Descripcion);

            if (s.Puntos.Count == 0)
            {
                l.Texto(Margen, l.Y - 4f, "Sin registros para el período consultado.", 8.5f, false, TextoSecundario);
                l.Y -= 30f;
                return;
            }

            if (hayValores)
            {
                DibujarGrafico(l, s);
            }

            DibujarTabla(l, s);
            l.Y -= 24f;
        }

        private static void DibujarTituloSeccion(LienzoPdf l, string titulo, string? descripcion)
        {
            l.Y -= 12f;
            l.Texto(Margen, l.Y, titulo, 12f, true, TextoPrimario);
            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                l.Y -= 13f;
                l.Texto(Margen, l.Y, Ajustar(descripcion!, AnchoUtil, 8f, false), 8f, false, TextoSecundario);
            }
            l.Y -= 7f;
            l.Linea(Margen, l.Y, XDerecha, l.Y, 0.6f, Borde);
            l.Y -= 14f;
        }

        private static void DibujarGrafico(LienzoPdf l, SeccionReporte s)
        {
            const float anchoEje = 50f;
            const float tamEtiqueta = 6.5f;

            float x0 = Margen + anchoEje;
            float ancho = AnchoUtil - anchoEje - 6f;
            float yBase = l.Y - 6f - AltoAreaGrafico;

            double maxValor = s.Puntos.Max(p => p.Valor);
            double paso = PasoEje(maxValor / 4.0, s.EsEntero);
            double maxEje = paso * 4.0;

            // Grilla horizontal y valores del eje Y
            for (int i = 0; i <= 4; i++)
            {
                float y = yBase + AltoAreaGrafico * i / 4f;
                l.Linea(x0, y, x0 + ancho, y, i == 0 ? 0.8f : 0.4f, i == 0 ? TextoSecundario : Borde);
                l.TextoDerecha(x0 - 6f, y - 2.3f, s.FormatoEje(paso * i), tamEtiqueta, false, TextoSecundario);
            }

            int n = s.Puntos.Count;
            float slot = ancho / n;
            float anchoBarra = Math.Max(0.8f, Math.Min(26f, slot * 0.68f));
            bool mostrarValores = n <= 16;

            // Se muestra una etiqueta cada "salto" barras para que no se superpongan.
            float anchoEtiquetaMax = s.Puntos.Max(p => MedirTexto(p.Etiqueta, tamEtiqueta, false));
            int salto = Math.Max(1, (int)Math.Ceiling((anchoEtiquetaMax + 6f) / slot));

            for (int i = 0; i < n; i++)
            {
                double valor = Math.Max(0, s.Puntos[i].Valor);
                float centro = x0 + slot * i + slot / 2f;
                float alto = (float)(valor / maxEje * AltoAreaGrafico);

                if (alto > 0f)
                {
                    l.Rectangulo(centro - anchoBarra / 2f, yBase, anchoBarra, alto, s.Color);
                }

                if (mostrarValores && valor > 0)
                {
                    string textoValor = s.FormatoEje(valor);
                    if (MedirTexto(textoValor, tamEtiqueta, true) <= slot + 4f)
                    {
                        l.TextoCentrado(centro, yBase + alto + 3f, textoValor, tamEtiqueta, true, TextoPrimario);
                    }
                }

                if (i % salto == 0)
                {
                    string etiqueta = Ajustar(s.Puntos[i].Etiqueta, slot * salto - 2f, tamEtiqueta, false);
                    l.TextoCentrado(centro, yBase - 11f, etiqueta, tamEtiqueta, false, TextoSecundario);
                }
            }

            l.Y = yBase - 28f;
        }

        private static void DibujarTabla(LienzoPdf l, SeccionReporte s)
        {
            bool conPorcentaje = s.TotalParaPorcentaje is > 0;

            const float anchoNro = 36f;
            const float anchoValor = 130f;
            float anchoPct = conPorcentaje ? 82f : 0f;
            float anchoEtiqueta = AnchoUtil - anchoNro - anchoValor - anchoPct;

            float xEtiqueta = Margen + anchoNro;
            float xValorDer = xEtiqueta + anchoEtiqueta + anchoValor - 10f;
            float xPctDer = XDerecha - 10f;

            void DibujarEncabezado()
            {
                float yFila = l.Y - AltoFila;
                l.Rectangulo(Margen, yFila, AnchoUtil, AltoFila, PrimarioOscuro);
                float yTexto = yFila + 5.6f;
                l.Texto(Margen + 10f, yTexto, "N°", 8f, true, Blanco);
                l.Texto(xEtiqueta, yTexto, s.EncabezadoEtiqueta, 8f, true, Blanco);
                l.TextoDerecha(xValorDer, yTexto, s.EncabezadoValor, 8f, true, Blanco);
                if (conPorcentaje)
                {
                    l.TextoDerecha(xPctDer, yTexto, "% del total", 8f, true, Blanco);
                }
                l.Y = yFila;
            }

            void SaltoDePaginaSiHaceFalta()
            {
                if (l.Y - AltoFila >= LimiteInferior) return;

                l.NuevaPagina();
                l.Texto(Margen, l.Y - 10f, $"{s.Titulo} (continuación)", 9f, true, TextoSecundario);
                l.Y -= 20f;
                DibujarEncabezado();
            }

            DibujarEncabezado();

            for (int i = 0; i < s.Puntos.Count; i++)
            {
                SaltoDePaginaSiHaceFalta();

                PuntoSerie punto = s.Puntos[i];
                float yFila = l.Y - AltoFila;
                if (i % 2 == 1)
                {
                    l.Rectangulo(Margen, yFila, AnchoUtil, AltoFila, FondoZebra);
                }
                l.Linea(Margen, yFila, XDerecha, yFila, 0.4f, Borde);

                float yTexto = yFila + 5.6f;
                l.Texto(Margen + 10f, yTexto, (i + 1).ToString(CulturaAr), 8f, false, TextoSecundario);
                l.Texto(xEtiqueta, yTexto, Ajustar(s.EtiquetaTabla(i, punto), anchoEtiqueta - 10f, 8f, false), 8f, false, TextoPrimario);
                l.TextoDerecha(xValorDer, yTexto, s.FormatoValor(punto.Valor), 8f, false, TextoPrimario);
                if (conPorcentaje)
                {
                    l.TextoDerecha(xPctDer, yTexto, FormatoPorcentaje(punto.Valor, s.TotalParaPorcentaje!.Value), 8f, false, TextoSecundario);
                }

                l.Y = yFila;
            }

            if (s.EtiquetaTotal != null)
            {
                SaltoDePaginaSiHaceFalta();

                double suma = s.Puntos.Sum(p => p.Valor);
                float yFila = l.Y - AltoFila;
                l.Rectangulo(Margen, yFila, AnchoUtil, AltoFila, FondoTotal);
                l.Linea(Margen, l.Y, XDerecha, l.Y, 0.8f, PrimarioOscuro);

                float yTexto = yFila + 5.6f;
                l.Texto(xEtiqueta, yTexto, s.EtiquetaTotal, 8f, true, PrimarioOscuro);
                l.TextoDerecha(xValorDer, yTexto, s.FormatoValor(suma), 8f, true, PrimarioOscuro);
                if (conPorcentaje)
                {
                    l.TextoDerecha(xPctDer, yTexto, FormatoPorcentaje(suma, s.TotalParaPorcentaje!.Value), 8f, true, PrimarioOscuro);
                }

                l.Y = yFila;
            }

            l.Y -= 6f;
            l.Texto(Margen, l.Y - 6f, $"Registros: {s.Puntos.Count.ToString("N0", CulturaAr)}", 7f, false, TextoSecundario);
            l.Y -= 10f;
        }

        /// <summary>Encabezado compacto (desde la página 2) y pie de página con numeración "Página X de Y".</summary>
        private static void DibujarEncabezadosYPies(LienzoPdf l, ReporteHotel r, DateTime generado)
        {
            int total = l.Paginas.Count;
            string periodo = DescribirPeriodo(r);

            for (int i = 0; i < total; i++)
            {
                l.IrAPagina(i);

                if (i > 0)
                {
                    float yEnc = AltoPagina - 32f;
                    l.Texto(Margen, yEnc, NombreHotel.ToUpper(CulturaAr), 9f, true, PrimarioOscuro);
                    l.TextoDerecha(XDerecha, yEnc, $"{TituloReporte} · {periodo}", 7.5f, false, TextoSecundario);
                    l.Linea(Margen, yEnc - 8f, XDerecha, yEnc - 8f, 0.8f, Primario);
                }

                l.Linea(Margen, 44f, XDerecha, 44f, 0.5f, Borde);
                l.Texto(Margen, 31f, $"{NombreSistema} · {NombreHotel}", 7f, false, TextoSecundario);
                l.TextoCentrado(AnchoPagina / 2f, 31f, $"Generado el {generado.ToString("dd/MM/yyyy HH:mm", CulturaAr)} hs", 7f, false, TextoSecundario);
                l.TextoDerecha(XDerecha, 31f, $"Página {i + 1} de {total}", 7f, true, TextoPrimario);
            }
        }

        // ------------------------------------------------------------------
        // Formatos (Argentina)
        // ------------------------------------------------------------------

        private static string FormatoMoneda(double valor) => ((decimal)valor).ToString("C2", CulturaAr);

        private static string FormatoEntero(double valor) => valor.ToString("N0", CulturaAr);

        private static string FormatoMonedaCompacta(double valor)
        {
            if (valor >= 1_000_000) return $"$ {(valor / 1_000_000).ToString("0.#", CulturaAr)} M";
            if (valor >= 1_000) return $"$ {(valor / 1_000).ToString("0.#", CulturaAr)} mil";
            return $"$ {valor.ToString("N0", CulturaAr)}";
        }

        private static string FormatoPorcentaje(double valor, double total) =>
            total > 0 ? $"{(valor * 100.0 / total).ToString("0.0", CulturaAr)} %" : "-";

        private static string DescribirPeriodo(ReporteHotel r)
        {
            int dias = Math.Max(1, (r.Hasta.Date - r.Desde.Date).Days + 1);
            string desde = r.Desde.ToString("dd/MM/yyyy", CulturaAr);
            if (dias == 1) return $"{desde} (1 día)";
            return $"{desde} al {r.Hasta.ToString("dd/MM/yyyy", CulturaAr)} ({dias} días)";
        }

        private static string DescribirDia(DateTime fecha)
        {
            string dia = fecha.ToString("dddd", CulturaAr);
            if (dia.Length > 0) dia = char.ToUpper(dia[0], CulturaAr) + dia.Substring(1);
            return $"{fecha.ToString("dd/MM/yyyy", CulturaAr)} · {dia}";
        }

        /// <summary>Las series diarias traen etiquetas "dd/MM"; se completa el año tomando como referencia el fin del período.</summary>
        private static string CompletarAnio(string etiqueta, DateTime hasta)
        {
            for (int anio = hasta.Year; anio >= hasta.Year - 1; anio--)
            {
                if (DateTime.TryParseExact($"{etiqueta}/{anio}", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha)
                    && fecha.Date <= hasta.Date)
                {
                    return DescribirDia(fecha);
                }
            }
            return etiqueta;
        }

        /// <summary>Paso "redondo" para el eje Y (1, 2, 2,5 o 5 × 10^n).</summary>
        private static double PasoEje(double bruto, bool esEntero)
        {
            if (bruto <= 0) return 1;
            double potencia = Math.Pow(10, Math.Floor(Math.Log10(bruto)));
            double fraccion = bruto / potencia;
            double redondo = fraccion <= 1 ? 1 : fraccion <= 2 ? 2 : fraccion <= 2.5 ? 2.5 : fraccion <= 5 ? 5 : 10;
            double paso = redondo * potencia;
            return esEntero ? Math.Max(1, Math.Ceiling(paso)) : paso;
        }

        // ------------------------------------------------------------------
        // Texto: medición, ajuste y codificación WinAnsi
        // ------------------------------------------------------------------

        private static float TamanoQueEntra(string texto, float anchoMax, float tamano, bool esBold, float tamanoMinimo)
        {
            while (tamano > tamanoMinimo && MedirTexto(texto, tamano, esBold) > anchoMax)
            {
                tamano -= 0.5f;
            }
            return tamano;
        }

        private static string Ajustar(string texto, float anchoMax, float tamano, bool esBold)
        {
            if (string.IsNullOrEmpty(texto) || MedirTexto(texto, tamano, esBold) <= anchoMax) return texto ?? string.Empty;

            const string sufijo = "...";
            for (int largo = texto.Length - 1; largo > 0; largo--)
            {
                string candidato = texto.Substring(0, largo).TrimEnd() + sufijo;
                if (MedirTexto(candidato, tamano, esBold) <= anchoMax) return candidato;
            }
            return sufijo;
        }

        private static float MedirTexto(string texto, float tamano, bool esBold)
        {
            if (string.IsNullOrEmpty(texto)) return 0f;
            float unidades = 0f;
            foreach (byte b in CodificarWinAnsi(texto))
            {
                unidades += AnchoCaracter(b, esBold);
            }
            return unidades / 1000f * tamano;
        }

        // Anchos de las fuentes estándar Helvetica / Helvetica-Bold (AFM) para los caracteres 32..126.
        private static readonly short[] AnchosHelvetica =
        {
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
            278, 278, 584, 584, 584, 556, 1015,
            667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611,
            278, 278, 278, 469, 556, 333,
            556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500,
            334, 260, 334, 584
        };

        private static readonly short[] AnchosHelveticaBold =
        {
            278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
            333, 333, 584, 584, 584, 611, 975,
            722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611,
            333, 278, 333, 584, 556, 333,
            556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500,
            389, 280, 389, 584
        };

        private static float AnchoCaracter(byte b, bool esBold)
        {
            short[] tabla = esBold ? AnchosHelveticaBold : AnchosHelvetica;
            if (b >= 32 && b <= 126) return tabla[b - 32];

            switch (b)
            {
                case 0x95: return 350;   // •
                case 0x96: return 556;   // –
                case 0x97: return 1000;  // —
                case 0xA0: return 278;   // espacio duro
                case 0xB0: return 400;   // °
                case 0xB7: return 278;   // ·
                case 0xBF: return 611;   // ¿
                case 0xA1: return 333;   // ¡
            }

            // Letras acentuadas (Latin-1): se mide como su letra base.
            if (b >= 0xC0)
            {
                char baseLetra = ((char)b).ToString().Normalize(NormalizationForm.FormD)[0];
                if (baseLetra >= 32 && baseLetra <= 126) return tabla[baseLetra - 32];
            }
            return 556;
        }

        private static Encoding CrearCodificacionWinAnsi()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252, new EncoderReplacementFallback("?"), DecoderFallback.ReplacementFallback);
        }

        private static byte[] CodificarWinAnsi(string texto)
        {
            // La cultura es-AR usa espacios duros/finos en moneda y porcentajes.
            string normalizado = texto.Replace(' ', ' ').Replace(' ', ' ').Replace("\r", "").Replace('\n', ' ');
            return WinAnsi.GetBytes(normalizado);
        }

        /// <summary>Cadena literal PDF en ASCII puro: los caracteres fuera de ASCII se escriben como escapes octales WinAnsi.</summary>
        private static string EscaparPdf(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            var sb = new StringBuilder(texto.Length + 10);
            foreach (byte b in CodificarWinAnsi(texto))
            {
                switch (b)
                {
                    case (byte)'(': sb.Append(@"\("); break;
                    case (byte)')': sb.Append(@"\)"); break;
                    case (byte)'\\': sb.Append(@"\\"); break;
                    default:
                        if (b >= 32 && b <= 126) sb.Append((char)b);
                        else sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                        break;
                }
            }
            return sb.ToString();
        }

        private static string F(float valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------
        // Ensamblado del archivo PDF
        // ------------------------------------------------------------------

        private static byte[] EnsamblarDocumentoPdf(List<StringBuilder> paginas, DateTime generado)
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

            // Numeración: 1 Catalog, 2 Pages, 3-4 fuentes, 5 Info, luego (Page, Contents) por cada página.
            const int primerObjetoPagina = 6;
            string kids = string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{primerObjetoPagina + i * 2} 0 R"));
            string mediaBox = $"[0 0 {F(AnchoPagina)} {F(AltoPagina)}]";

            writer.Write("%PDF-1.4\r\n");

            RegistrarObjeto("1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n");
            RegistrarObjeto($"2 0 obj\r\n<< /Type /Pages /Kids [{kids}] /Count {paginas.Count} >>\r\nendobj\r\n");
            RegistrarObjeto("3 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\r\nendobj\r\n");
            RegistrarObjeto("4 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\r\nendobj\r\n");
            RegistrarObjeto($"5 0 obj\r\n<< /Title ({EscaparPdf($"{TituloReporte} - {NombreHotel}")}) /Author ({EscaparPdf(NombreHotel)}) " +
                            $"/Creator ({EscaparPdf(NombreSistema)}) /Producer (SGH - GeneradorReportePdf) " +
                            $"/CreationDate (D:{generado:yyyyMMddHHmmss}) >>\r\nendobj\r\n");

            for (int i = 0; i < paginas.Count; i++)
            {
                int idPagina = primerObjetoPagina + i * 2;
                int idContenido = idPagina + 1;
                string contenido = paginas[i].ToString();

                RegistrarObjeto($"{idPagina} 0 obj\r\n<< /Type /Page /Parent 2 0 R /MediaBox {mediaBox} /Contents {idContenido} 0 R " +
                                "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>\r\nendobj\r\n");
                RegistrarObjeto($"{idContenido} 0 obj\r\n<< /Length {Encoding.ASCII.GetByteCount(contenido)} >>\r\nstream\r\n{contenido}\r\nendstream\r\nendobj\r\n");
            }

            writer.Flush();
            long xrefOffset = ms.Position;

            writer.Write("xref\r\n");
            writer.Write($"0 {offsets.Count + 1}\r\n");
            writer.Write("0000000000 65535 f \r\n");
            foreach (long offset in offsets)
            {
                writer.Write($"{offset:D10} 00000 n \r\n");
            }

            writer.Write($"trailer\r\n<< /Size {offsets.Count + 1} /Root 1 0 R /Info 5 0 R >>\r\nstartxref\r\n{xrefOffset}\r\n%%EOF\r\n");
            writer.Flush();

            return ms.ToArray();
        }

        // ------------------------------------------------------------------
        // Tipos auxiliares
        // ------------------------------------------------------------------

        private readonly record struct ColorPdf(byte R, byte G, byte B)
        {
            public string Componentes =>
                $"{(R / 255f).ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"{(G / 255f).ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"{(B / 255f).ToString("0.###", CultureInfo.InvariantCulture)}";
        }

        private sealed record SeccionReporte(
            string Titulo,
            string Descripcion,
            string EncabezadoEtiqueta,
            string EncabezadoValor,
            List<PuntoSerie> Puntos,
            Func<int, PuntoSerie, string> EtiquetaTabla,
            Func<double, string> FormatoValor,
            Func<double, string> FormatoEje,
            bool EsEntero,
            ColorPdf Color,
            double? TotalParaPorcentaje,
            string? EtiquetaTotal);

        /// <summary>Acumula los operadores gráficos de cada página. Y es el cursor vertical (decrece hacia abajo).</summary>
        private sealed class LienzoPdf
        {
            private int _indice = -1;

            public List<StringBuilder> Paginas { get; } = new();
            public float Y { get; set; }

            private StringBuilder Actual => Paginas[_indice];

            public void NuevaPagina()
            {
                Paginas.Add(new StringBuilder());
                _indice = Paginas.Count - 1;
                Y = InicioContenidoContinuacion;
            }

            public void IrAPagina(int indice) => _indice = indice;

            public void AsegurarEspacio(float alto)
            {
                if (Y - alto < LimiteInferior) NuevaPagina();
            }

            public void Texto(float x, float y, string texto, float tamano, bool esBold, ColorPdf color)
            {
                if (string.IsNullOrEmpty(texto)) return;
                Actual.Append("BT\r\n")
                      .Append(esBold ? "/F2 " : "/F1 ").Append(F(tamano)).Append(" Tf\r\n")
                      .Append(color.Componentes).Append(" rg\r\n")
                      .Append(F(x)).Append(' ').Append(F(y)).Append(" Td\r\n")
                      .Append('(').Append(EscaparPdf(texto)).Append(") Tj\r\n")
                      .Append("ET\r\n");
            }

            public void TextoDerecha(float xDerecha, float y, string texto, float tamano, bool esBold, ColorPdf color) =>
                Texto(xDerecha - MedirTexto(texto, tamano, esBold), y, texto, tamano, esBold, color);

            public void TextoCentrado(float xCentro, float y, string texto, float tamano, bool esBold, ColorPdf color) =>
                Texto(xCentro - MedirTexto(texto, tamano, esBold) / 2f, y, texto, tamano, esBold, color);

            public void Rectangulo(float x, float y, float ancho, float alto, ColorPdf relleno)
            {
                Actual.Append("q ").Append(relleno.Componentes).Append(" rg ")
                      .Append(F(x)).Append(' ').Append(F(y)).Append(' ').Append(F(ancho)).Append(' ').Append(F(alto))
                      .Append(" re f Q\r\n");
            }

            public void Linea(float x1, float y1, float x2, float y2, float grosor, ColorPdf color)
            {
                Actual.Append("q ").Append(F(grosor)).Append(" w ").Append(color.Componentes).Append(" RG ")
                      .Append(F(x1)).Append(' ').Append(F(y1)).Append(" m ")
                      .Append(F(x2)).Append(' ').Append(F(y2)).Append(" l S Q\r\n");
            }
        }
    }
}
