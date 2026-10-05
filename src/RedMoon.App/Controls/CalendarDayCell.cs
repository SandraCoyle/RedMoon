using RedMoon.Core.Models;

namespace RedMoon.App.Controls;

/// <summary>
/// Visningsdata for én dag i månedskalenderen (beregnes af CalendarViewModel).
/// </summary>
public sealed record CalendarDayCell(
    LocalDate Date,
    bool IsInMonth,
    bool IsToday,
    bool IsSelected,
    CycleDayKind Kind,
    string MoodGlyph,
    bool HasEntry);
