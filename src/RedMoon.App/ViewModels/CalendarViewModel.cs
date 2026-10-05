using System.Windows.Input;
using RedMoon.App.Controls;
using RedMoon.App.Presentation;
using RedMoon.Core.Models;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Kalender: månedsvisning med registrerede og forudsagte menstruationsdage,
/// samt registrering/redigering af menstruation, intensitet og humør for den valgte dato.
/// Hvert tryk gemmes med det samme.
/// </summary>
public sealed class CalendarViewModel : ViewModelBase
{
    private readonly DiaryService _diary;
    private readonly AccountService _accounts;

    private LocalDate _displayedMonth;
    private LocalDate _selectedDate;
    private IReadOnlyList<CalendarDayCell> _days = Array.Empty<CalendarDayCell>();
    private string _monthTitle = string.Empty;
    private string _selectedTitle = string.Empty;
    private string _selectedInfo = string.Empty;
    private object _selectedStatus = MenstruationStatus.None;
    private object _selectedFlow = FlowIntensity.None;
    private object _selectedMood = Mood.None;
    private bool _isSelectedEditable;
    private bool _showFlow;
    private bool _hasEntry;

    public CalendarViewModel(DiaryService diary, AccountService accounts)
    {
        _diary = diary;
        _accounts = accounts;
        _selectedDate = diary.Today;
        _displayedMonth = LocalDate.FirstOfMonth(_selectedDate.Year, _selectedDate.Month);

        PreviousMonthCommand = new Command(() => ShowMonth(_displayedMonth.AddMonths(-1)));
        NextMonthCommand = new Command(() => ShowMonth(_displayedMonth.AddMonths(1)));
        TodayCommand = new Command(GoToToday);
        DayTappedCommand = new Command<LocalDate>(SelectDate);
        SetStatusCommand = new AsyncCommand(SetStatusAsync);
        SetFlowCommand = new AsyncCommand(SetFlowAsync);
        SetMoodCommand = new AsyncCommand(SetMoodAsync);
        ClearDayCommand = new AsyncCommand(ClearDayAsync);
    }

    public IReadOnlyList<ChoiceItem> StatusChoices => ChoiceLists.Statuses;
    public IReadOnlyList<ChoiceItem> FlowChoices => ChoiceLists.Flows;
    public IReadOnlyList<ChoiceItem> MoodChoices => ChoiceLists.Moods;

    /// <summary>De 42 dage (6 uger) der vises i kalendergitteret.</summary>
    public IReadOnlyList<CalendarDayCell> Days { get => _days; private set => SetProperty(ref _days, value); }

    /// <summary>Fx "Oktober 2026".</summary>
    public string MonthTitle { get => _monthTitle; private set => SetProperty(ref _monthTitle, value); }

    /// <summary>Fx "Mandag 5. oktober".</summary>
    public string SelectedTitle { get => _selectedTitle; private set => SetProperty(ref _selectedTitle, value); }

    /// <summary>Kort status for den valgte dag (fx "Forventet mens").</summary>
    public string SelectedInfo { get => _selectedInfo; private set => SetProperty(ref _selectedInfo, value); }

    public object SelectedStatus { get => _selectedStatus; private set => SetProperty(ref _selectedStatus, value); }
    public object SelectedFlow { get => _selectedFlow; private set => SetProperty(ref _selectedFlow, value); }
    public object SelectedMood { get => _selectedMood; private set => SetProperty(ref _selectedMood, value); }

    /// <summary>Kun dage til og med i dag kan registreres.</summary>
    public bool IsSelectedEditable
    {
        get => _isSelectedEditable;
        private set
        {
            if (SetProperty(ref _isSelectedEditable, value)) OnPropertyChanged(nameof(IsSelectedInFuture));
        }
    }

    public bool IsSelectedInFuture => !IsSelectedEditable;

    /// <summary>Intensitet vises kun når der er valgt menstruation.</summary>
    public bool ShowFlow { get => _showFlow; private set => SetProperty(ref _showFlow, value); }

    /// <summary>Sand hvis den valgte dag har en registrering (viser "Ryd dag").</summary>
    public bool HasEntry { get => _hasEntry; private set => SetProperty(ref _hasEntry, value); }

    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand TodayCommand { get; }
    public ICommand DayTappedCommand { get; }
    public ICommand SetStatusCommand { get; }
    public ICommand SetFlowCommand { get; }
    public ICommand SetMoodCommand { get; }
    public ICommand ClearDayCommand { get; }

    /// <summary>Kaldes når skærmen vises. Hopper til i dag første gang og opdaterer data.</summary>
    public void Refresh()
    {
        if (!_accounts.IsLoggedIn) return;
        Rebuild();
    }

    /// <summary>Går til dagens dato.</summary>
    public void GoToToday()
    {
        _selectedDate = _diary.Today;
        ShowMonth(LocalDate.FirstOfMonth(_selectedDate.Year, _selectedDate.Month));
    }

    private void ShowMonth(LocalDate firstOfMonth)
    {
        _displayedMonth = firstOfMonth;
        Rebuild();
    }

    private void SelectDate(LocalDate date)
    {
        _selectedDate = date;
        if (date.Year != _displayedMonth.Year || date.Month != _displayedMonth.Month)
        {
            _displayedMonth = LocalDate.FirstOfMonth(date.Year, date.Month);
        }
        Rebuild();
    }

    private void Rebuild()
    {
        var today = _diary.Today;
        var prediction = _diary.GetPrediction();

        // Mandag-først: hvor mange dage fra forrige måned der skal med i første uge.
        var leading = ((int)_displayedMonth.DayOfWeek + 6) % 7;
        var start = _displayedMonth.AddDays(-leading);
        var end = start.AddDays(41);
        var entries = _diary.GetEntries(start, end).ToDictionary(e => e.Date);

        var cells = new List<CalendarDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            entries.TryGetValue(date, out var entry);
            cells.Add(new CalendarDayCell(
                date,
                IsInMonth: date.Month == _displayedMonth.Month,
                IsToday: date == today,
                IsSelected: date == _selectedDate,
                Kind: prediction.GetDayKind(date),
                MoodGlyph: entry == null ? string.Empty : DisplayText.MoodEmoji(entry.Mood),
                HasEntry: entry != null));
        }

        Days = cells;
        MonthTitle = $"{DisplayText.MonthName(_displayedMonth.Month)} {_displayedMonth.Year}";
        UpdateSelected(prediction);
    }

    private void UpdateSelected(CyclePrediction prediction)
    {
        var entry = _diary.GetEntry(_selectedDate);
        SelectedTitle = DisplayText.LongDate(_selectedDate) + (_selectedDate == _diary.Today ? " · i dag" : string.Empty);
        SelectedStatus = entry.MenstruationStatus;
        SelectedFlow = entry.FlowIntensity;
        SelectedMood = entry.Mood;
        ShowFlow = entry.HasMenstruation;
        HasEntry = !entry.IsEmpty;
        IsSelectedEditable = _selectedDate <= _diary.Today;

        SelectedInfo = prediction.GetDayKind(_selectedDate) switch
        {
            CycleDayKind.Period => "Menstruation",
            CycleDayKind.PredictedPeriod => "Forventet mens (skøn)",
            CycleDayKind.PredictedMargin => "Mens kan måske komme her (skøn)",
            _ => IsSelectedEditable ? string.Empty : "Ingen forventet mens",
        };
    }

    private async Task SetStatusAsync(object? value)
    {
        if (value is not MenstruationStatus status) return;
        var current = _diary.GetEntry(_selectedDate);
        await SaveAsync(() => _diary.SetMenstruationAsync(_selectedDate, status, current.FlowIntensity));
    }

    private async Task SetFlowAsync(object? value)
    {
        if (value is not FlowIntensity flow) return;
        var current = _diary.GetEntry(_selectedDate);
        if (!current.HasMenstruation) return;
        // Tryk på det allerede valgte fjerner valget igen.
        var next = current.FlowIntensity == flow ? FlowIntensity.None : flow;
        await SaveAsync(() => _diary.SetMenstruationAsync(_selectedDate, current.MenstruationStatus, next));
    }

    private async Task SetMoodAsync(object? value)
    {
        if (value is not Mood mood) return;
        var current = _diary.GetEntry(_selectedDate);
        var next = current.Mood == mood ? Mood.None : mood;
        await SaveAsync(() => _diary.SetMoodAsync(_selectedDate, next));
    }

    private Task ClearDayAsync() => SaveAsync(() => _diary.DeleteEntryAsync(_selectedDate));

    private async Task SaveAsync(Func<Task> save)
    {
        await RunAsync(save, showBusy: false);
        Rebuild();
    }
}
