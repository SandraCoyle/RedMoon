using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using RedMoon.App.Presentation;
using RedMoon.App.Theme;
using RedMoon.Core.Models;

namespace RedMoon.App.Controls;

/// <summary>
/// Månedskalender (6 uger x 7 dage, mandag først).
/// Farver: fyldt = registreret menstruation, lys = forudsagt menstruation, svag = usikkerhedsmargen.
/// I dag har en ring; valgt dag har en tyk kant. Humør vises som lille emoji under datoen.
/// </summary>
public sealed class MonthCalendarView : ContentView
{
    public static readonly BindableProperty DaysProperty =
        BindableProperty.Create(nameof(Days), typeof(IReadOnlyList<CalendarDayCell>), typeof(MonthCalendarView),
            propertyChanged: (b, _, _) => ((MonthCalendarView)b).Build());

    public static readonly BindableProperty DayTappedCommandProperty =
        BindableProperty.Create(nameof(DayTappedCommand), typeof(ICommand), typeof(MonthCalendarView));

    public IReadOnlyList<CalendarDayCell>? Days
    {
        get => (IReadOnlyList<CalendarDayCell>?)GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    /// <summary>Udføres med den trykkede dags LocalDate.</summary>
    public ICommand? DayTappedCommand
    {
        get => (ICommand?)GetValue(DayTappedCommandProperty);
        set => SetValue(DayTappedCommandProperty, value);
    }

    private void Build()
    {
        var grid = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        for (var c = 0; c < 7; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var c = 0; c < 7; c++)
        {
            var header = new Label
            {
                Text = DisplayText.WeekdayInitials[c],
                FontSize = 12,
                FontFamily = "OpenSansSemibold",
                HorizontalTextAlignment = TextAlignment.Center,
            };
            header.SetAppThemeColor(Label.TextColorProperty, Palette.TextSecondaryLight, Palette.TextSecondaryDark);
            grid.Add(header, c, 0);
        }

        var days = Days ?? Array.Empty<CalendarDayCell>();
        var weeks = (days.Count + 6) / 7;
        for (var r = 0; r < weeks; r++) grid.RowDefinitions.Add(new RowDefinition(new GridLength(52)));

        for (var i = 0; i < days.Count; i++)
        {
            grid.Add(CreateCell(days[i]), i % 7, 1 + i / 7);
        }
        Content = grid;
    }

    private View CreateCell(CalendarDayCell day)
    {
        var number = new Label
        {
            Text = day.Date.Day.ToString(DisplayText.Danish),
            FontSize = 15,
            FontFamily = day.IsToday || day.IsSelected ? "OpenSansSemibold" : "OpenSansRegular",
            HorizontalTextAlignment = TextAlignment.Center,
        };
        var mood = new Label
        {
            Text = day.MoodGlyph,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center,
            IsVisible = !string.IsNullOrEmpty(day.MoodGlyph),
        };
        var stack = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { number, mood } };

        var border = new Border
        {
            Content = stack,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 0,
            Opacity = day.IsInMonth ? 1 : 0.4,
        };

        switch (day.Kind)
        {
            case CycleDayKind.Period:
                border.SetAppThemeColor(Border.BackgroundColorProperty, Palette.PeriodLight, Palette.PeriodDark);
                number.SetAppThemeColor(Label.TextColorProperty, Palette.OnPeriodLight, Palette.OnPeriodDark);
                break;
            case CycleDayKind.PredictedPeriod:
                border.SetAppThemeColor(Border.BackgroundColorProperty, Palette.PredictedLight, Palette.PredictedDark);
                break;
            case CycleDayKind.PredictedMargin:
                border.SetAppThemeColor(Border.BackgroundColorProperty, Palette.MarginLight, Palette.MarginDark);
                break;
            default:
                border.BackgroundColor = Colors.Transparent;
                break;
        }

        if (day.IsSelected)
        {
            border.StrokeThickness = 3;
            border.SetAppThemeColor(Border.StrokeProperty, Palette.TextPrimaryLight, Palette.TextPrimaryDark);
        }
        else if (day.IsToday)
        {
            border.StrokeThickness = 2;
            border.SetAppThemeColor(Border.StrokeProperty, Palette.AccentLight, Palette.AccentDark);
        }
        else
        {
            border.StrokeThickness = 0;
            border.Stroke = Colors.Transparent;
        }

        SemanticProperties.SetDescription(border, Describe(day));

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (DayTappedCommand?.CanExecute(day.Date) == true) DayTappedCommand.Execute(day.Date);
        };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    /// <summary>Skærmlæser-tekst for en dag.</summary>
    private static string Describe(CalendarDayCell day)
    {
        var parts = new List<string> { DisplayText.LongDate(day.Date) };
        if (day.IsToday) parts.Add("i dag");
        parts.Add(day.Kind switch
        {
            CycleDayKind.Period => "menstruation",
            CycleDayKind.PredictedPeriod => "forventet menstruation",
            CycleDayKind.PredictedMargin => "måske menstruation",
            _ => string.Empty,
        });
        if (day.HasEntry) parts.Add("har registrering");
        return string.Join(", ", parts.Where(p => p.Length > 0));
    }
}
