using System.Globalization;
using RedMoon.Core.Models;

namespace RedMoon.App.Presentation;

/// <summary>
/// Danske tekster og ikoner for Core-værdier. Holdes adskilt fra Core,
/// så forretningslogikken er uafhængig af sprog og design.
/// </summary>
public static class DisplayText
{
    /// <summary>Dansk kultur til datoer og månedsnavne.</summary>
    public static readonly CultureInfo Danish = new("da-DK");

    /// <summary>Humør-valgmuligheder i den rækkefølge de vises.</summary>
    public static readonly IReadOnlyList<Mood> Moods = new[]
    {
        Mood.Outgoing, Mood.Happy, Mood.InBetween, Mood.Sad, Mood.Introverted, Mood.Powerful,
    };

    public static string MoodEmoji(Mood mood) => mood switch
    {
        Mood.Outgoing => "🤩",
        Mood.Happy => "😊",
        Mood.InBetween => "😐",
        Mood.Sad => "😢",
        Mood.Introverted => "😶",
        Mood.Powerful => "⚡",
        _ => string.Empty,
    };

    public static string MoodName(Mood mood) => mood switch
    {
        Mood.Outgoing => "Udadvendt",
        Mood.Happy => "Glad",
        Mood.InBetween => "Midtimellem",
        Mood.Sad => "Trist",
        Mood.Introverted => "Indadvendt",
        Mood.Powerful => "Powerful",
        _ => "Ikke valgt",
    };

    public static string StatusName(MenstruationStatus status) => status switch
    {
        MenstruationStatus.FirstDay => "Første dag",
        MenstruationStatus.Ongoing => "Har mens",
        MenstruationStatus.LastDay => "Sidste dag",
        _ => "Ingen",
    };

    public static string FlowName(FlowIntensity flow) => flow switch
    {
        FlowIntensity.Light => "Lidt",
        FlowIntensity.Medium => "Noget",
        FlowIntensity.Heavy => "Meget",
        _ => "Ikke valgt",
    };

    public static string SeasonName(CycleSeason season) => season switch
    {
        CycleSeason.Winter => "Vinter ❄️",
        CycleSeason.Spring => "Forår 🌱",
        CycleSeason.Summer => "Sommer ☀️",
        CycleSeason.Autumn => "Efterår 🍂",
        _ => string.Empty,
    };

    /// <summary>Månedsnavn med stort forbogstav, fx "Oktober".</summary>
    public static string MonthName(int month)
    {
        var name = Danish.DateTimeFormat.GetMonthName(month);
        return char.ToUpper(name[0], Danish) + name[1..];
    }

    /// <summary>Fx "Lørdag 3. oktober".</summary>
    public static string LongDate(LocalDate date)
    {
        var text = date.ToDateTime().ToString("dddd d. MMMM", Danish);
        return char.ToUpper(text[0], Danish) + text[1..];
    }

    /// <summary>Ugedagsforkortelser, mandag først (dansk standard).</summary>
    public static readonly IReadOnlyList<string> WeekdayInitials = new[] { "M", "T", "O", "T", "F", "L", "S" };
}
