using UnityEngine;

namespace RedMoon.UnityApp.UI
{
    /// <summary>
    /// Farver til de tegnede grafikker (månen, ikoner, mønsterlås).
    /// Samme natlige palet som MAUI-appens mørke tilstand. Layoutfarver står i Resources/RedMoon/RedMoonStyles.uss.
    /// </summary>
    internal static class Palette
    {
        public static readonly Color Background = Hex("#140B22");
        public static readonly Color Surface = Hex("#21132F");
        public static readonly Color SurfaceAlt = Hex("#2E1B43");
        public static readonly Color Border = Hex("#433059");
        public static readonly Color TextPrimary = Hex("#F7EEFF");
        public static readonly Color TextSecondary = Hex("#C9B6DC");
        public static readonly Color Accent = Hex("#FF6FA0");
        public static readonly Color OnAccent = Hex("#1A0B23");
        public static readonly Color Period = Hex("#FF4D86");
        public static readonly Color RingDot = Hex("#5E4880");
        public static readonly Color MoonLit = Hex("#F6A58E");
        public static readonly Color MoonShadow = Hex("#0A0512");
        public static readonly Color Crater = Hex("#DD7C62");
        public static readonly Color Error = Hex("#FF8A9A");

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
    }
}
