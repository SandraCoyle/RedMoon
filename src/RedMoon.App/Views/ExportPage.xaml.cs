using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Overfør til ny telefon (underside, åbnes fra Profil).</summary>
public partial class ExportPage : ContentPage
{
    private readonly ExportViewModel _viewModel;

    public ExportPage(ExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnDisappearing()
    {
        // Koden glemmes og filen slettes, når brugeren forlader siden.
        _viewModel.Reset();
        base.OnDisappearing();
    }
}
