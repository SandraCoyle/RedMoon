using System.Collections.Generic;
using RedMoon.Core.Models;

namespace RedMoon.UnityApp.UI
{
    /// <summary>
    /// Danske tekster for Core-værdier. Navne på måneder og ugedage står direkte her
    /// (i stedet for CultureInfo), så de virker ens på alle Unity-platforme.
    /// </summary>
    internal static class Texts
    {
        private static readonly string[] MonthNames =
        {
            "januar", "februar", "marts", "april", "maj", "juni",
            "juli", "august", "september", "oktober", "november", "december",
        };

        private static readonly string[] WeekdayNames = { "Søndag", "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag", "Lørdag" };

        /// <summary>Ugedagsforkortelser, mandag først (dansk standard).</summary>
        public static readonly IReadOnlyList<string> WeekdayInitials = new[] { "M", "T", "O", "T", "F", "L", "S" };

        /// <summary>Humør i den rækkefølge de vises.</summary>
        public static readonly IReadOnlyList<Mood> Moods = new[]
        {
            Mood.Outgoing, Mood.Happy, Mood.InBetween, Mood.Sad, Mood.Introverted, Mood.Powerful,
        };

        /// <summary>Månedsnavn med stort forbogstav, fx "Oktober".</summary>
        public static string MonthName(int month) => Capitalize(MonthNames[month - 1]);

        /// <summary>Fx "Mandag 5. oktober".</summary>
        public static string LongDate(LocalDate date) =>
            WeekdayNames[(int)date.DayOfWeek] + " " + date.Day + ". " + MonthNames[date.Month - 1];

        public static string MoodName(Mood mood)
        {
            switch (mood)
            {
                case Mood.Outgoing: return "Udadvendt";
                case Mood.Happy: return "Glad";
                case Mood.InBetween: return "Midtimellem";
                case Mood.Sad: return "Trist";
                case Mood.Introverted: return "Indadvendt";
                case Mood.Powerful: return "Powerful";
                default: return "Ikke valgt";
            }
        }

        public static string StatusName(MenstruationStatus status)
        {
            switch (status)
            {
                case MenstruationStatus.FirstDay: return "Første dag";
                case MenstruationStatus.Ongoing: return "Har mens";
                case MenstruationStatus.LastDay: return "Sidste dag";
                default: return "Ingen";
            }
        }

        public static string FlowName(FlowIntensity flow)
        {
            switch (flow)
            {
                case FlowIntensity.Light: return "Lidt";
                case FlowIntensity.Medium: return "Noget";
                case FlowIntensity.Heavy: return "Meget";
                default: return "Ikke valgt";
            }
        }

        public static string SeasonName(CycleSeason season)
        {
            switch (season)
            {
                case CycleSeason.Winter: return "Vinter";
                case CycleSeason.Spring: return "Forår";
                case CycleSeason.Summer: return "Sommer";
                case CycleSeason.Autumn: return "Efterår";
                default: return string.Empty;
            }
        }

        private static string Capitalize(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
