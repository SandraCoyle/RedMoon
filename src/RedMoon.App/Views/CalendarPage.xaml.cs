using RedMoon.App.Infrastructure;
using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Kalender med registrering af menstruation og humør.</summary>
public partial class CalendarPage : ContentPage
{
    private readonly CalendarViewModel _viewModel;
    private bool _hasAppeared;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_hasAppeared) _viewModel.Refresh(); else _viewModel.GoToToday();
        _hasAppeared = true;
        AppEvents.Resumed += OnResumed;
    }

    protected override void OnDisappearing()
    {
        AppEvents.Resumed -= OnResumed;
        base.OnDisappearing();
    }

    private void OnResumed(object? sender, EventArgs e) => _viewModel.Refresh();
}
