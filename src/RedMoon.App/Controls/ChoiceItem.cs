namespace RedMoon.App.Controls;

/// <summary>
/// Én valgmulighed i en <see cref="ChoiceGroupView"/>.
/// </summary>
/// <param name="Value">Værdien der sendes til kommandoen ved tryk (fx en Mood).</param>
/// <param name="Title">Tekst under ikonet.</param>
/// <param name="Glyph">Valgfrit tegn/emoji der vises som ikon.</param>
/// <param name="FillLevel">Valgfrit: tegner en lille måne fyldt 0-3 (bruges til blødningsintensitet).</param>
public sealed record ChoiceItem(object Value, string Title, string? Glyph = null, int? FillLevel = null);
