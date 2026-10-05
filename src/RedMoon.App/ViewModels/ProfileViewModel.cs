using System.Windows.Input;
using RedMoon.App.Controls;
using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.App.Presentation;
using RedMoon.App.Views;
using RedMoon.Core.Models;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Profil/indstillinger: dagens humør, brugernavn, fødselsår/-måned, skift mønster,
/// overfør til ny telefon, log ud og slet alle data.
/// </summary>
public sealed class ProfileViewModel : ViewModelBase
{
    /// <summary>Ordet brugeren skal skrive for at bekræfte sletning.</summary>
    public const string DeleteConfirmationWord = "SLET";

    private readonly AccountService _accounts;
    private readonly DiaryService _diary;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    private string _username = string.Empty;
    private string _birthYearText = string.Empty;
    private string _birthMonthText = string.Empty;
    private object _todayMood = Mood.None;
    private string _statusMessage = string.Empty;

    public ProfileViewModel(AccountService accounts, DiaryService diary, INavigationService navigation, IDialogService dialogs)
    {
        _accounts = accounts;
        _diary = diary;
        _navigation = navigation;
        _dialogs = dialogs;

        SetMoodCommand = new AsyncCommand(SetMoodAsync);
        SaveUsernameCommand = new AsyncCommand(SaveUsernameAsync);
        ChangePatternCommand = new AsyncCommand(() => _navigation.PushAsync<ChangePatternPage>());
        ExportCommand = new AsyncCommand(() => _navigation.PushAsync<ExportPage>());
        LogoutCommand = new AsyncCommand(LogoutAsync);
        DeleteAllCommand = new AsyncCommand(DeleteAllAsync);
    }

    public IReadOnlyList<ChoiceItem> MoodChoices => ChoiceLists.Moods;

    public string Username { get => _username; set => SetProperty(ref _username, value ?? string.Empty); }
    public string BirthYearText { get => _birthYearText; private set => SetProperty(ref _birthYearText, value); }
    public string BirthMonthText { get => _birthMonthText; private set => SetProperty(ref _birthMonthText, value); }
    public object TodayMood { get => _todayMood; private set => SetProperty(ref _todayMood, value); }

    /// <summary>Kort bekræftelse, fx "Gemt ✓".</summary>
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    public ICommand SetMoodCommand { get; }
    public ICommand SaveUsernameCommand { get; }
    public ICommand ChangePatternCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DeleteAllCommand { get; }

    public void Refresh()
    {
        if (!_accounts.IsLoggedIn) return;
        var account = _accounts.CurrentAccount;
        Username = account.Username;
        BirthYearText = account.BirthYear.ToString(DisplayText.Danish);
        BirthMonthText = DisplayText.MonthName(account.BirthMonth);
        TodayMood = _diary.GetEntry(_diary.Today).Mood;
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;
    }

    private async Task SetMoodAsync(object? value)
    {
        if (value is not Mood mood) return;
        var current = _diary.GetEntry(_diary.Today).Mood;
        var next = current == mood ? Mood.None : mood;
        if (await RunAsync(() => _diary.SetMoodAsync(_diary.Today, next), showBusy: false))
        {
            TodayMood = next;
            StatusMessage = next == Mood.None ? "Humør fjernet" : $"Dagens humør: {DisplayText.MoodName(next)} ✓";
        }
    }

    private async Task SaveUsernameAsync()
    {
        StatusMessage = string.Empty;
        if (await RunAsync(() => _accounts.ChangeUsernameAsync(Username)))
        {
            Username = _accounts.CurrentAccount.Username;
            StatusMessage = "Brugernavn gemt ✓";
        }
    }

    private async Task LogoutAsync()
    {
        if (await RunAsync(() => _accounts.LogoutAsync()))
        {
            _navigation.ShowLogin();
        }
    }

    private async Task DeleteAllAsync()
    {
        var answer = await _dialogs.PromptAsync(
            "Slet alle data?",
            "Dette sletter din bruger og ALLE registreringer af humør og menstruation fra telefonen. " +
            "Det kan ikke fortrydes.\n\n" +
            $"Skriv {DeleteConfirmationWord} for at bekræfte.",
            "Slet permanent");

        if (answer == null) return;
        if (!string.Equals(answer.Trim(), DeleteConfirmationWord, StringComparison.OrdinalIgnoreCase))
        {
            await _dialogs.AlertAsync("Ikke slettet", $"Du skrev ikke {DeleteConfirmationWord}, så intet er slettet.");
            return;
        }

        if (await RunAsync(() => _accounts.DeleteAllDataAsync()))
        {
            await _dialogs.AlertAsync("Slettet", "Alle data er slettet fra telefonen.");
            _navigation.ShowLogin();
        }
    }
}
