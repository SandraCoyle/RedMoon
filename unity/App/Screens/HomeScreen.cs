using RedMoon.Core.Models;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Hjem: månen der viser hvor brugeren er i sin cyklus, samt dagens humør og tid til næste menstruation.
    /// </summary>
    internal sealed class HomeScreen : ScreenBase
    {
        private readonly UnityEngine.UIElements.Label _greeting;
        private readonly UnityEngine.UIElements.Label _today;
        private readonly MoonElement _moon;
        private readonly UnityEngine.UIElements.Label _day;
        private readonly UnityEngine.UIElements.Label _season;
        private readonly UnityEngine.UIElements.Label _next;
        private readonly UnityEngine.UIElements.Label _mood;
        private readonly UnityEngine.UIElements.Label _basis;

        public HomeScreen(ScreenContext context) : base(context, "Hjem")
        {
            Root.AddToClassList("rm-screen--center");
            _greeting = Ui.Label(string.Empty, "rm-title", "rm-center");
            _today = Ui.Label(string.Empty, "rm-subtitle", "rm-center");
            _moon = new MoonElement(300);
            _day = Ui.Label(string.Empty, "rm-big", "rm-center");
            _season = Ui.Label(string.Empty, "rm-subtitle", "rm-center");

            var card = Ui.Box("rm-card");
            _next = Ui.Label(string.Empty, "rm-text", "rm-center");
            _mood = Ui.Label(string.Empty, "rm-text", "rm-center");
            card.Add(_next);
            card.Add(Ui.Spacer(6));
            card.Add(_mood);

            _basis = Ui.Label(string.Empty, "rm-muted", "rm-center");

            Root.Add(_greeting);
            Root.Add(_today);
            Root.Add(Ui.Spacer(8));
            Root.Add(_moon);
            Root.Add(_day);
            Root.Add(_season);
            Root.Add(card);
            Root.Add(Ui.Spacer(8));
            Root.Add(Ui.Button("Registrér i kalenderen", () => Context.Navigator.GoToTab(TabRegistry.Calendar), "rm-btn--secondary"));
            Root.Add(_basis);
        }

        /// <summary>Genberegner alt ud fra de gemte data. Kaldes hver gang skærmen vises.</summary>
        public override void OnShow()
        {
            var accounts = Context.Services.Accounts;
            var diary = Context.Services.Diary;
            if (!accounts.IsLoggedIn) return;

            var name = accounts.CurrentAccount.Username;
            _greeting.text = string.IsNullOrEmpty(name) ? "Hej" : "Hej " + name;
            _today.text = Texts.LongDate(diary.Today);

            var prediction = diary.GetPrediction();
            _moon.SetCycle(prediction.CycleLengthDays, prediction.PeriodLengthDays, prediction.DayOfCycle ?? 0);

            if (prediction.HasCycleData)
            {
                _day.text = "Dag " + prediction.DayOfCycle;
                _season.text = Texts.SeasonName(prediction.SeasonToday);
            }
            else
            {
                _day.text = "Ingen cyklus endnu";
                _season.text = string.Empty;
            }

            _next.text = DescribeNextPeriod(prediction);

            var todayEntry = diary.GetEntry(diary.Today);
            _mood.text = todayEntry.Mood == Mood.None
                ? "Dagens humør: ikke valgt"
                : "Dagens humør: " + Texts.MoodName(todayEntry.Mood);

            _basis.text = prediction.CyclesUsed > 0
                ? $"Skøn ud fra dine sidste {prediction.CyclesUsed} cyklusser. Ikke prævention."
                : "Skøn ud fra en standardcyklus på 28 dage. Ikke prævention.";
        }

        private static string DescribeNextPeriod(CyclePrediction prediction)
        {
            if (prediction.IsMenstruatingToday) return "Du har mens i dag. Pas godt på dig selv.";
            if (prediction.LastPeriod == null) return "Registrér din mens i kalenderen, så kan månen følge din cyklus.";
            if (prediction.IsStale) return "Det er længe siden, du har registreret mens. Registrér den næste i kalenderen.";
            if (prediction.GetDayKind(prediction.Today) == CycleDayKind.PredictedPeriod && prediction.DaysLate == 0)
            {
                return "Din mens er måske stadig i gang. Registrér den eller vælg \"Sidste dag\" i kalenderen.";
            }
            if (prediction.DaysLate > 0) return $"Din mens er {prediction.DaysLate} dage senere end forventet. Det er helt almindeligt.";

            var days = prediction.DaysUntilNextPeriod;
            if (!days.HasValue) return string.Empty;
            if (days.Value == 0) return "Din mens kan starte i dag.";
            if (days.Value == 1) return "Ca. 1 dag til næste mens.";
            return $"Ca. {days.Value} dage til næste mens.";
        }
    }
}
