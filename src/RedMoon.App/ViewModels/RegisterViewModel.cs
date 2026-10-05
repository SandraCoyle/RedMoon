using System.Windows.Input;
using RedMoon.App.Navigation;
using RedMoon.App.Presentation;
using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Opret bruger i tre trin:
/// 1) brugernavn (valgfrit), fødselsår og fødselsmåned,
/// 2) tegn mønster,
/// 3) gentag mønster.
/// </summary>
public sealed class RegisterViewModel : ViewModelBase
{
    private enum Step { Details, Draw, Confirm }

    private readonly AccountService _accounts;
    private readonly INavigationService _navigation;
    private readonly IClock _clock;
    private Step _step = Step.Details;
    private string _username = string.Empty;
    private int _birthYearIndex = -1;
    private int _birthMonthIndex = -1;
    private PatternPassword? _firstPattern;
    private bool _isPatternError;

    public RegisterViewModel(AccountService accounts, INavigationService navigation, IClock clock)
    {
        _accounts = accounts;
        _navigation = navigation;
        _clock = clock;

        var newest = clock.Today.Year - AccountService.MinimumAgeYears;
        BirthYears = Enumerable.Range(AccountService.OldestBirthYear, newest - AccountService.OldestBirthYear + 1).Reverse().ToList();
        BirthMonths = Enumerable.Range(1, 12).Select(DisplayText.MonthName).ToList();

        NextCommand = new AsyncCommand(GoToPatternAsync);
        BackCommand = new AsyncCommand(GoBackAsync);
        PatternCompletedCommand = new AsyncCommand(p => OnPatternAsync(p as IReadOnlyList<int>));
    }

    /// <summary>Valgmuligheder i rullemenuen for fødselsår (nyeste først).</summary>
    public List<int> BirthYears { get; }

    /// <summary>De 12 måneder på dansk.</summary>
    public List<string> BirthMonths { get; }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value ?? string.Empty);
    }

    public int BirthYearIndex
    {
        get => _birthYearIndex;
        set => SetProperty(ref _birthYearIndex, value);
    }

    public int BirthMonthIndex
    {
        get => _birthMonthIndex;
        set => SetProperty(ref _birthMonthIndex, value);
    }

    public bool IsDetailsStep => _step == Step.Details;
    public bool IsPatternStep => _step != Step.Details;

    public string PatternTitle => _step == Step.Confirm ? "Tegn mønsteret igen" : "Tegn dit hemmelige mønster";

    public string PatternHint => _step == Step.Confirm
        ? "Så er vi sikre på, at du kan huske det."
        : $"Forbind mindst {PatternPassword.MinimumLength} punkter. Flere punkter = sværere at gætte.";

    public bool IsPatternError
    {
        get => _isPatternError;
        private set => SetProperty(ref _isPatternError, value);
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand PatternCompletedCommand { get; }

    private Task GoToPatternAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            Username = AccountService.NormalizeUsername(Username);
            if (BirthYearIndex < 0) throw new ValidationException("Vælg dit fødselsår.");
            if (BirthMonthIndex < 0) throw new ValidationException("Vælg din fødselsmåned.");
            AccountService.ValidateBirth(BirthYears[BirthYearIndex], BirthMonthIndex + 1, _clock.Today);
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
            return Task.CompletedTask;
        }

        SetStep(Step.Draw);
        return Task.CompletedTask;
    }

    private Task GoBackAsync()
    {
        ErrorMessage = string.Empty;
        _firstPattern = null;
        if (_step == Step.Details) return _navigation.PopAsync();
        SetStep(_step == Step.Confirm ? Step.Draw : Step.Details);
        return Task.CompletedTask;
    }

    private async Task OnPatternAsync(IReadOnlyList<int>? points)
    {
        if (IsBusy || points == null) return;
        ErrorMessage = string.Empty;
        IsPatternError = false;

        var pattern = PatternPassword.TryCreate(points);
        if (pattern == null)
        {
            IsPatternError = true;
            ErrorMessage = $"Forbind mindst {PatternPassword.MinimumLength} forskellige punkter.";
            return;
        }

        if (_step == Step.Draw)
        {
            _firstPattern = pattern;
            SetStep(Step.Confirm);
            return;
        }

        if (_firstPattern == null || !_firstPattern.SameAs(pattern))
        {
            IsPatternError = true;
            ErrorMessage = "Mønstrene var ikke ens. Prøv igen fra start.";
            _firstPattern = null;
            SetStep(Step.Draw);
            return;
        }

        var created = await RunAsync(() => _accounts.CreateAccountAsync(Username, pattern, BirthYears[BirthYearIndex], BirthMonthIndex + 1));
        _firstPattern = null;
        if (created) _navigation.ShowMain();
    }

    private void SetStep(Step step)
    {
        _step = step;
        OnPropertyChanged(nameof(IsDetailsStep));
        OnPropertyChanged(nameof(IsPatternStep));
        OnPropertyChanged(nameof(PatternTitle));
        OnPropertyChanged(nameof(PatternHint));
    }
}
