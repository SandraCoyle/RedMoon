using System.Collections;
using System.Windows.Input;
using RedMoon.App.Theme;

namespace RedMoon.App.Controls;

/// <summary>
/// Genanvendelig gruppe af valgknapper i et gitter (bruges til humør, menstruationsstatus og intensitet).
/// Viser hvilket element der er valgt (SelectedValue) og udfører Command med elementets Value ved tryk.
/// Kontrollen ændrer ikke selv SelectedValue – det gør ViewModel'en, når valget er gemt.
/// </summary>
public sealed class ChoiceGroupView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ChoiceGroupView), propertyChanged: Rebuild);

    public static readonly BindableProperty SelectedValueProperty =
        BindableProperty.Create(nameof(SelectedValue), typeof(object), typeof(ChoiceGroupView), propertyChanged: Rebuild);

    public static readonly BindableProperty ColumnsProperty =
        BindableProperty.Create(nameof(Columns), typeof(int), typeof(ChoiceGroupView), 3, propertyChanged: Rebuild);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ChoiceGroupView));

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    public object? SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((ChoiceGroupView)bindable).Build();

    private void Build()
    {
        var items = ItemsSource?.OfType<ChoiceItem>().ToList() ?? new List<ChoiceItem>();
        var columns = Math.Max(1, Columns);
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        for (var c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        var rows = (items.Count + columns - 1) / columns;
        for (var r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var i = 0; i < items.Count; i++)
        {
            var cell = CreateCell(items[i], Equals(items[i].Value, SelectedValue));
            grid.Add(cell, i % columns, i / columns);
        }
        Content = grid;
    }

    private View CreateCell(ChoiceItem item, bool selected)
    {
        var stack = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        if (item.FillLevel.HasValue)
        {
            stack.Add(new FillLevelIcon(item.FillLevel.Value) { HorizontalOptions = LayoutOptions.Center });
        }
        else if (!string.IsNullOrEmpty(item.Glyph))
        {
            stack.Add(new Label { Text = item.Glyph, FontSize = 26, HorizontalTextAlignment = TextAlignment.Center });
        }

        var title = new Label
        {
            Text = item.Title,
            FontSize = 13,
            FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular",
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        if (selected) title.SetAppThemeColor(Label.TextColorProperty, Palette.OnAccentLight, Palette.OnAccentDark);
        stack.Add(title);

        var border = new Border
        {
            Content = stack,
            Padding = new Thickness(6, 10),
            MinimumHeightRequest = 52,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 1.5,
        };
        if (selected)
        {
            border.SetAppThemeColor(Border.BackgroundColorProperty, Palette.AccentLight, Palette.AccentDark);
            border.SetAppThemeColor(Border.StrokeProperty, Palette.AccentLight, Palette.AccentDark);
        }
        else
        {
            border.SetAppThemeColor(Border.BackgroundColorProperty, Palette.SurfaceAltLight, Palette.SurfaceAltDark);
            border.SetAppThemeColor(Border.StrokeProperty, Palette.BorderLight, Palette.BorderDark);
        }

        SemanticProperties.SetDescription(border, selected ? $"{item.Title}, valgt" : item.Title);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Command?.CanExecute(item.Value) == true) Command.Execute(item.Value);
        };
        border.GestureRecognizers.Add(tap);
        return border;
    }
}
