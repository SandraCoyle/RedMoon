using System.Windows.Input;
using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.Core.Security;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Skift mønster i tre trin: nuværende mønster → nyt mønster → gentag nyt mønster.
/// Kan gøres helt lokalt, fordi hashen blot erstattes i den krypterede datafil.
/// </summary>
public sealed class ChangePatternViewModel : ViewModelBase
{
    private enum Step { Current, New, Confirm }

    private readonly AccountService _accounts;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private Step _step = Step.Current;
    private PatternPassword? _current;
    private PatternPassword? _new;
    private bool _isPatternError;

    public ChangePatternViewModel(AccountService accounts, INavigationService navigation, IDialogService dialogs)
    {
        _accounts = accounts;
        _navigation = navigation;
        _dialogs = dialogs;
        PatternCompletedCommand = new AsyncCommand(p => OnPatternAsync(p as IReadOnlyList<int>));
    }

    public string Title => _step switch
    {
        Step.Current => "Tegn dit nuværende mønster",
        Step.New => "Tegn dit nye mønster",
        _ => "Gentag dit nye mønster",
    };

    public string StepText => $"Trin {(int)_step + 1} af 3";

    public bool IsPatternError { get => _isPatternError; private set => SetProperty(ref _isPatternError, value); }

    public ICommand PatternCompletedCommand { get; }

    /// <summary>Glemmer mønstrene fra hukommelsen, når siden lukkes.</summary>
    public void Reset()
    {
        _current = null;
        _new = null;
        SetStep(Step.Current);
        ErrorMessage = string.Empty;
        IsPatternError = false;
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

        switch (_step)
        {
            case Step.Current:
                _current = pattern;
                SetStep(Step.New);
                return;
            case Step.New:
                _new = pattern;
                SetStep(Step.Confirm);
                return;
        }

        if (_new == null || _current == null || !_new.SameAs(pattern))
        {
            IsPatternError = true;
            ErrorMessage = "De nye mønstre var ikke ens. Tegn det nye mønster igen.";
            _new = null;
            SetStep(Step.New);
            return;
        }

        var ok = await RunAsync(() => _accounts.ChangePatternAsync(_current, _new));
        if (!ok)
        {
            // Fx forkert nuværende mønster: start forfra.
            IsPatternError = true;
            _current = null;
            _new = null;
            SetStep(Step.Current);
            return;
        }

        Reset();
        await _dialogs.AlertAsync("Mønster skiftet ✓", "Brug dit nye mønster næste gang du logger ind.");
        await _navigation.PopAsync();
    }

    private void SetStep(Step step)
    {
        _step = step;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(StepText));
    }
}
