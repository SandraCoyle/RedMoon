using System.Windows.Input;
using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.App.Views;
using RedMoon.Core.Security;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Velkomst/login. Har telefonen en konto, logger brugeren ind med brugernavn + mønster.
/// Ellers vises "Opret bruger" og "Hent fra anden telefon".
/// </summary>
public sealed class LoginViewModel : ViewModelBase
{
    private readonly AccountService _accounts;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private bool _hasAccount;
    private string _username = string.Empty;
    private bool _isPatternError;

    public LoginViewModel(AccountService accounts, INavigationService navigation, IDialogService dialogs)
    {
        _accounts = accounts;
        _navigation = navigation;
        _dialogs = dialogs;

        PatternCompletedCommand = new AsyncCommand(p => LoginAsync(p as IReadOnlyList<int>));
        CreateAccountCommand = new AsyncCommand(() => _navigation.PushAsync<RegisterPage>());
        ImportCommand = new AsyncCommand(() => _navigation.PushAsync<ImportPage>());
        ForgotPatternCommand = new AsyncCommand(ForgotPatternAsync);
    }

    /// <summary>Sand hvis der findes en konto på telefonen.</summary>
    public bool HasAccount
    {
        get => _hasAccount;
        private set
        {
            if (SetProperty(ref _hasAccount, value)) OnPropertyChanged(nameof(HasNoAccount));
        }
    }

    public bool HasNoAccount => !HasAccount;

    /// <summary>Brugernavn (lades tomt hvis kontoen ikke har et).</summary>
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value ?? string.Empty);
    }

    /// <summary>Viser mønsteret i fejlfarve efter forkert login.</summary>
    public bool IsPatternError
    {
        get => _isPatternError;
        private set => SetProperty(ref _isPatternError, value);
    }

    public ICommand PatternCompletedCommand { get; }
    public ICommand CreateAccountCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ForgotPatternCommand { get; }

    /// <summary>Kaldes når skærmen vises.</summary>
    public void Refresh()
    {
        HasAccount = _accounts.HasAccount;
        IsPatternError = false;
        ErrorMessage = string.Empty;
    }

    private async Task LoginAsync(IReadOnlyList<int>? points)
    {
        if (IsBusy || points == null) return;
        IsPatternError = false;

        var pattern = PatternPassword.TryCreate(points);
        if (pattern == null)
        {
            IsPatternError = true;
            ErrorMessage = $"Forbind mindst {PatternPassword.MinimumLength} punkter.";
            return;
        }

        LoginResult? result = null;
        var ok = await RunAsync(async () => result = await _accounts.LoginAsync(Username, pattern));
        if (!ok || result == null)
        {
            IsPatternError = true;
            return;
        }

        if (result.IsSuccess)
        {
            Username = string.Empty;
            _navigation.ShowMain();
            return;
        }

        IsPatternError = true;
        ErrorMessage = result.Message;
        if (result.Outcome == LoginOutcome.NoAccount) HasAccount = false;
    }

    private async Task ForgotPatternAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Glemt mønster?",
            "Dine data er kun gemt på denne telefon og kan ikke gendannes uden dit mønster.\n\n" +
            "Du kan nulstille appen, men så bliver ALLE dine registreringer slettet permanent.",
            "Slet alt og start forfra");
        if (!confirmed) return;

        if (await RunAsync(() => _accounts.DeleteAllDataAsync()))
        {
            Refresh();
            await _dialogs.AlertAsync("Nulstillet", "Alle data er slettet. Du kan nu oprette en ny bruger.");
        }
    }
}
