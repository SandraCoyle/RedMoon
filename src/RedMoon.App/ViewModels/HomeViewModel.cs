using System.Windows.Input;
using RedMoon.App.Navigation;
using RedMoon.App.Presentation;
using RedMoon.Core.Models;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Hjem: månen der viser hvor brugeren er i sin cyklus, samt dagens humør og tid til næste menstruation.
/// </summary>
public sealed class HomeViewModel : ViewModelBase
{
    private readonly DiaryService _diary;
    private readonly AccountService _accounts;
    private readonly INavigationService _navigation;

    private string _greeting = string.Empty;
    private string _todayText = string.Empty;
    private int _cycleLength = 28;
    private int _periodLength = 5;
    private int _dayOfCycle;
    private string _dayText = string.Empty;
    private string _seasonText = string.Empty;
    private string _nextPeriodText = string.Empty;
    private string _moodText = string.Empty;
    private string _basisText = string.Empty;

    public HomeViewModel(DiaryService diary, AccountService accounts, INavigationService navigation)
    {
        _diary = diary;
        _accounts = accounts;
        _navigation = navigation;
        LogTodayCommand = new AsyncCommand(() => _navigation.GoToTabAsync(Routes.Calendar));
        ChangeMoodCommand = new AsyncCommand(() => _navigation.GoToTabAsync(Routes.Profile));
    }

    public string Greeting { get => _greeting; private set => SetProperty(ref _greeting, value); }
    public string TodayText { get => _todayText; private set => SetProperty(ref _todayText, value); }
    public int CycleLength { get => _cycleLength; private set => SetProperty(ref _cycleLength, value); }
    public int PeriodLength { get => _periodLength; private set => SetProperty(ref _periodLength, value); }

    /// <summary>Dag i cyklussen (0 = ukendt).</summary>
    public int DayOfCycle { get => _dayOfCycle; private set => SetProperty(ref _dayOfCycle, value); }

    public string DayText { get => _dayText; private set => SetProperty(ref _dayText, value); }
    public string SeasonText { get => _seasonText; private set => SetProperty(ref _seasonText, value); }
    public string NextPeriodText { get => _nextPeriodText; private set => SetProperty(ref _nextPeriodText, value); }
    public string MoodText { get => _moodText; private set => SetProperty(ref _moodText, value); }

    /// <summary>Forklarer om beregningen bygger på egne data eller standardværdier.</summary>
    public string BasisText { get => _basisText; private set => SetProperty(ref _basisText, value); }

    public ICommand LogTodayCommand { get; }
    public ICommand ChangeMoodCommand { get; }

    /// <summary>Genberegner alt ud fra de gemte data. Kaldes hver gang skærmen vises.</summary>
    public void Refresh()
    {
        if (!_accounts.IsLoggedIn) return;

        var name = _accounts.CurrentAccount.Username;
        Greeting = string.IsNullOrEmpty(name) ? "Hej 🌙" : $"Hej {name} 🌙";
        TodayText = DisplayText.LongDate(_diary.Today);

        var prediction = _diary.GetPrediction();
        CycleLength = prediction.CycleLengthDays;
        PeriodLength = prediction.PeriodLengthDays;
        DayOfCycle = prediction.DayOfCycle ?? 0;

        if (prediction.HasCycleData)
        {
            DayText = $"Dag {prediction.DayOfCycle}";
            SeasonText = DisplayText.SeasonName(prediction.SeasonToday);
        }
        else
        {
            DayText = "Ingen cyklus endnu";
            SeasonText = string.Empty;
        }

        NextPeriodText = DescribeNextPeriod(prediction);

        var todayEntry = _diary.GetEntry(_diary.Today);
        MoodText = todayEntry.Mood == Mood.None
            ? "Dagens humør: ikke valgt"
            : $"Dagens humør: {DisplayText.MoodEmoji(todayEntry.Mood)} {DisplayText.MoodName(todayEntry.Mood)}";

        BasisText = prediction.CyclesUsed > 0
            ? $"Skøn ud fra dine sidste {prediction.CyclesUsed} cyklusser. Ikke prævention."
            : "Skøn ud fra en standardcyklus på 28 dage. Ikke prævention.";
    }

    private static string DescribeNextPeriod(CyclePrediction prediction)
    {
        if (prediction.IsMenstruatingToday) return "Du har mens i dag. Pas godt på dig selv 💗";
        if (prediction.LastPeriod == null) return "Registrér din mens i kalenderen, så kan månen følge din cyklus.";
        if (prediction.IsStale) return "Det er længe siden, du har registreret mens. Registrér den næste i kalenderen.";
        if (prediction.GetDayKind(prediction.Today) == CycleDayKind.PredictedPeriod && prediction.DaysLate == 0)
        {
            return "Din mens er måske stadig i gang. Registrér den eller tryk \"Sidste dag\" i kalenderen.";
        }
        if (prediction.DaysLate > 0) return $"Din mens er {prediction.DaysLate} dage senere end forventet. Det er helt almindeligt.";

        return prediction.DaysUntilNextPeriod switch
        {
            0 => "Din mens kan starte i dag.",
            1 => "Ca. 1 dag til næste mens.",
            int days => $"Ca. {days} dage til næste mens.",
            _ => string.Empty,
        };
    }
}
