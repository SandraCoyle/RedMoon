using RedMoon.App.Controls;
using RedMoon.App.Presentation;
using RedMoon.Core.Models;

namespace RedMoon.App.ViewModels;

/// <summary>Faste valgmuligheder til ChoiceGroupView (humør, status, intensitet).</summary>
public static class ChoiceLists
{
    public static IReadOnlyList<ChoiceItem> Moods { get; } =
        DisplayText.Moods.Select(m => new ChoiceItem(m, DisplayText.MoodName(m), DisplayText.MoodEmoji(m))).ToList();

    public static IReadOnlyList<ChoiceItem> Statuses { get; } = new[]
    {
        new ChoiceItem(MenstruationStatus.None, DisplayText.StatusName(MenstruationStatus.None), "○"),
        new ChoiceItem(MenstruationStatus.FirstDay, DisplayText.StatusName(MenstruationStatus.FirstDay), "🌕"),
        new ChoiceItem(MenstruationStatus.Ongoing, DisplayText.StatusName(MenstruationStatus.Ongoing), "🌖"),
        new ChoiceItem(MenstruationStatus.LastDay, DisplayText.StatusName(MenstruationStatus.LastDay), "🌘"),
    };

    public static IReadOnlyList<ChoiceItem> Flows { get; } = new[]
    {
        new ChoiceItem(FlowIntensity.Light, DisplayText.FlowName(FlowIntensity.Light), FillLevel: 1),
        new ChoiceItem(FlowIntensity.Medium, DisplayText.FlowName(FlowIntensity.Medium), FillLevel: 2),
        new ChoiceItem(FlowIntensity.Heavy, DisplayText.FlowName(FlowIntensity.Heavy), FillLevel: 3),
    };
}
