using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Hent konto og data fra en anden telefon.</summary>
public partial class ImportPage : ContentPage
{
    public ImportPage(ImportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
