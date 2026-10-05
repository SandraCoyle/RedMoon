using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.Core.Services;

namespace RedMoon.App.Views;

/// <summary>
/// Kort opstartsskærm: rydder midlertidige filer og tjekker om der er en husket session.
/// </summary>
public sealed class StartupPage : ContentPage
{
    private readonly AccountService _accounts;
    private readonly INavigationService _navigation;
    private bool _started;

    public StartupPage(AccountService accounts, INavigationService navigation)
    {
        _accounts = accounts;
        _navigation = navigation;
        Content = new ActivityIndicator { IsRunning = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        TransferFiles.CleanUp();

        bool restored;
        try
        {
            restored = await _accounts.TryRestoreSessionAsync();
        }
        catch (Exception)
        {
            // Fx utilgængeligt nøglelager: vis login i stedet for at crashe.
            restored = false;
        }

        if (restored) _navigation.ShowMain(); else _navigation.ShowLogin();
    }
}
