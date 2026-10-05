using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Skift mønster (undersideside, åbnes fra Profil).</summary>
public partial class ChangePatternPage : ContentPage
{
    private readonly ChangePatternViewModel _viewModel;

    public ChangePatternPage(ChangePatternViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnDisappearing()
    {
        _viewModel.Reset();
        base.OnDisappearing();
    }
}
