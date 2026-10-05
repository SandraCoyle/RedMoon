using RedMoon.App.ViewModels;

namespace RedMoon.App.Views;

/// <summary>Velkomst/login-skærm.</summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }
}
