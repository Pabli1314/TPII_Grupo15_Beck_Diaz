using System.Drawing;

namespace Presentacion.Administrador.UI
{
    /// <summary>Paleta y tipografía únicas del panel de Administrador. Un solo lugar de verdad
    /// para que todas las vistas y componentes se vean parte del mismo sistema.</summary>
    internal static class Paleta
    {
        public static readonly Color FondoApp = Color.FromArgb(242, 245, 249);
        public static readonly Color FondoTarjeta = Color.White;
        public static readonly Color Borde = Color.FromArgb(228, 232, 240);
        public static readonly Color BordeSuave = Color.FromArgb(238, 241, 246);

        public static readonly Color Sidebar = Color.FromArgb(20, 24, 35);
        public static readonly Color SidebarHover = Color.FromArgb(30, 35, 50);
        public static readonly Color SidebarTexto = Color.FromArgb(180, 188, 204);
        public static readonly Color SidebarTextoActivo = Color.White;

        public static readonly Color TextoPrimario = Color.FromArgb(17, 24, 39);
        public static readonly Color TextoSecundario = Color.FromArgb(100, 112, 134);
        public static readonly Color TextoTerciario = Color.FromArgb(148, 158, 176);

        public static readonly Color Primario = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimarioHover = Color.FromArgb(29, 82, 209);
        public static readonly Color PrimarioSuave = Color.FromArgb(224, 234, 253);

        // Colores de estado de habitación (deben coincidir siempre con Presentacion.Recepcionista.DibujoUtil)
        public static readonly Color Disponible = Color.FromArgb(40, 199, 111);
        public static readonly Color Ocupada = Color.FromArgb(234, 84, 85);
        public static readonly Color Limpieza = Color.FromArgb(255, 193, 7);
        public static readonly Color Mantenimiento = Color.FromArgb(79, 134, 247);

        public static readonly Color Peligro = Color.FromArgb(220, 53, 69);
        public static readonly Color PeligroSuave = Color.FromArgb(253, 232, 234);
        public static readonly Color Exito = Color.FromArgb(25, 135, 84);
        public static readonly Color ExitoSuave = Color.FromArgb(224, 246, 234);
        public static readonly Color AdvertenciaSuave = Color.FromArgb(255, 243, 205);

        public static Font FuenteTitulo => new Font("Segoe UI Semibold", 17f, FontStyle.Regular);
        public static Font FuenteSeccion => new Font("Segoe UI Semibold", 13f, FontStyle.Regular);
        public static Font FuenteBase => new Font("Segoe UI", 9.75f, FontStyle.Regular);
        public static Font FuenteBaseNegrita => new Font("Segoe UI Semibold", 9.75f, FontStyle.Regular);
        public static Font FuenteChica => new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static Font FuenteValor => new Font("Segoe UI Semibold", 22f, FontStyle.Regular);
    }
}
