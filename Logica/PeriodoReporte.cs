namespace Logica
{
    /// <summary>Rangos de fecha que se ofrecen como botones rápidos en Reportes.</summary>
    public enum RangoPredefinido
    {
        Hoy,
        EstaSemana,
        EsteMes,
        UltimoTrimestre,
        AnioEnCurso,
        Personalizado
    }

    /// <summary>
    /// Período consultado y el período "equivalente anterior" contra el que se comparan los números
    /// (hoy vs. ayer, este mes vs. los mismos días del mes pasado, etc.).
    /// </summary>
    public sealed class PeriodoReporte
    {
        public RangoPredefinido Rango { get; }
        public DateTime Desde { get; }
        public DateTime Hasta { get; }
        public DateTime DesdeAnterior { get; }
        public DateTime HastaAnterior { get; }

        /// <summary>Nombre corto para mostrar ("Este mes").</summary>
        public string Nombre { get; }

        /// <summary>Con qué se compara, para armar frases: "más que <b>el mes pasado</b>".</summary>
        public string EtiquetaComparacion { get; }

        public int Dias => (Hasta - Desde).Days + 1;

        private PeriodoReporte(RangoPredefinido rango, DateTime desde, DateTime hasta, DateTime desdeAnterior, DateTime hastaAnterior, string nombre, string etiquetaComparacion)
        {
            Rango = rango;
            Desde = desde.Date;
            Hasta = hasta.Date;
            DesdeAnterior = desdeAnterior.Date;
            HastaAnterior = hastaAnterior.Date;
            Nombre = nombre;
            EtiquetaComparacion = etiquetaComparacion;
        }

        public static PeriodoReporte Crear(RangoPredefinido rango, DateTime hoy, DateTime? desdePersonalizado = null, DateTime? hastaPersonalizado = null)
        {
            hoy = hoy.Date;

            switch (rango)
            {
                case RangoPredefinido.Hoy:
                    return new(rango, hoy, hoy, hoy.AddDays(-1), hoy.AddDays(-1), "Hoy", "ayer");

                case RangoPredefinido.EstaSemana:
                {
                    DateTime lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
                    return new(rango, lunes, hoy, lunes.AddDays(-7), hoy.AddDays(-7), "Esta semana", "la semana pasada");
                }

                case RangoPredefinido.EsteMes:
                {
                    var inicio = new DateTime(hoy.Year, hoy.Month, 1);
                    // Mismos días del mes anterior (AddMonths ajusta solo el 31 → 30/28).
                    return new(rango, inicio, hoy, inicio.AddMonths(-1), hoy.AddMonths(-1), "Este mes", "el mes pasado");
                }

                case RangoPredefinido.UltimoTrimestre:
                {
                    DateTime inicio = hoy.AddMonths(-3).AddDays(1);
                    return new(rango, inicio, hoy, inicio.AddMonths(-3), inicio.AddDays(-1), "Último trimestre", "el trimestre anterior");
                }

                case RangoPredefinido.AnioEnCurso:
                {
                    var inicio = new DateTime(hoy.Year, 1, 1);
                    return new(rango, inicio, hoy, inicio.AddYears(-1), hoy.AddYears(-1), "Año en curso", "el año pasado");
                }

                default:
                {
                    DateTime desde = (desdePersonalizado ?? hoy).Date;
                    DateTime hasta = (hastaPersonalizado ?? hoy).Date;
                    if (desde > hasta) (desde, hasta) = (hasta, desde);

                    int dias = (hasta - desde).Days + 1;
                    return new(RangoPredefinido.Personalizado, desde, hasta, desde.AddDays(-dias), desde.AddDays(-1),
                        "Rango personalizado", dias == 1 ? "el día anterior" : $"los {dias} días anteriores");
                }
            }
        }
    }
}
