using RedMoon.App.Infrastructure;
using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Hjemmeskærm med månen.</summary>
public partial class HomePage : ContentPage
{
    private const double MaxMoonSize = 340;
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
        AppEvents.Resumed += OnResumed;
    }

    protected override void OnDisappearing()
    {
        AppEvents.Resumed -= OnResumed;
        base.OnDisappearing();
    }

    /// <summary>Gør månen kvadratisk og så stor som skærmbredden tillader.</summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;
        var size = Math.Min(MaxMoonSize, width - 48);
        Moon.WidthRequest = size;
        Moon.HeightRequest = size;
    }

    private void OnResumed(object? sender, EventArgs e) => _viewModel.Refresh();
}
