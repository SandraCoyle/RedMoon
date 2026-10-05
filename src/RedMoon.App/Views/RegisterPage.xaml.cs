using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Opret bruger.</summary>
public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
