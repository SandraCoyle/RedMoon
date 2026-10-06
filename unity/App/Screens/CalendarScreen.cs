using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedMoon.Core.Models;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Graphics;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Kalender: månedsvisning med registrerede og forudsagte menstruationsdage,
    /// samt registrering/redigering af menstruation, intensitet og humør for den valgte dato.
    /// Hvert tryk gemmes med det samme (krypteret).
    /// </summary>
    internal sealed class CalendarScreen : ScreenBase
    {
        private readonly Label _monthTitle;
        private readonly CalendarGrid _grid;
        private readonly Label _selectedTitle;
        private readonly Label _selectedInfo;
        private readonly VisualElement _editor;
        private readonly VisualElement _flowSection;
        private readonly Button _clearButton;
        private readonly ChoiceRow<MenstruationStatus> _status;
        private readonly ChoiceRow<FlowIntensity> _flow;
        private readonly ChoiceRow<Mood> _mood;
        private LocalDate _displayedMonth;
        private LocalDate _selectedDate;
        private bool _saving;

        public CalendarScreen(ScreenContext context) : base(context, "Kalender")
        {
            _selectedDate = context.Services.Diary.Today;
            _displayedMonth = LocalDate.FirstOfMonth(_selectedDate.Year, _selectedDate.Month);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rm-stretch");
            Root.Add(scroll);

            var header = Ui.Box("rm-cal-header");
            header.Add(Ui.Button("‹", () => ShowMonth(_displayedMonth.AddMonths(-1)), "rm-btn--round"));
            _monthTitle = Ui.Label(string.Empty, "rm-cal-title");
            header.Add(_monthTitle);
            header.Add(Ui.Button("›", () => ShowMonth(_displayedMonth.AddMonths(1)), "rm-btn--round"));
            scroll.Add(header);
            scroll.Add(Ui.Button("I dag", GoToToday, "rm-btn--link"));

            _grid = new CalendarGrid();
            _grid.DayTapped += SelectDate;
            scroll.Add(_grid);
            scroll.Add(Legend());

            var card = Ui.Box("rm-card");
            scroll.Add(card);
            _selectedTitle = Ui.Label(string.Empty, "rm-section-title");
            _selectedInfo = Ui.Label(string.Empty, "rm-muted");
            card.Add(_selectedTitle);
            card.Add(_selectedInfo);

            _editor = Ui.Box();
            card.Add(_editor);

            _editor.Add(Ui.Label("Menstruation", "rm-field-caption"));
            _status = new ChoiceRow<MenstruationStatus>(new[]
            {
                new Choice<MenstruationStatus>(MenstruationStatus.None, Texts.StatusName(MenstruationStatus.None), Icons.Status(MenstruationStatus.None)),
                new Choice<MenstruationStatus>(MenstruationStatus.FirstDay, Texts.StatusName(MenstruationStatus.FirstDay), Icons.Status(MenstruationStatus.FirstDay)),
                new Choice<MenstruationStatus>(MenstruationStatus.Ongoing, Texts.StatusName(MenstruationStatus.Ongoing), Icons.Status(MenstruationStatus.Ongoing)),
                new Choice<MenstruationStatus>(MenstruationStatus.LastDay, Texts.StatusName(MenstruationStatus.LastDay), Icons.Status(MenstruationStatus.LastDay)),
            });
            _status.Selected += value => Fire(() => SetStatusAsync(value));
            _editor.Add(_status);

            _flowSection = Ui.Box();
            _flowSection.Add(Ui.Label("Blødning", "rm-field-caption"));
            _flow = new ChoiceRow<FlowIntensity>(new[] { FlowIntensity.Light, FlowIntensity.Medium, FlowIntensity.Heavy }
                .Select(f => new Choice<FlowIntensity>(f, Texts.FlowName(f), Icons.Flow(f))));
            _flow.Selected += value => Fire(() => SetFlowAsync(value));
            _flowSection.Add(_flow);
            _editor.Add(_flowSection);

            _editor.Add(Ui.Label("Humør", "rm-field-caption"));
            _mood = new ChoiceRow<Mood>(Texts.Moods.Select(m => new Choice<Mood>(m, Texts.MoodName(m), Icons.Mood(m))));
            _mood.Selected += value => Fire(() => SetMoodAsync(value));
            _editor.Add(_mood);

            CreateErrorLabel(card);
            _clearButton = Ui.Button("Ryd dagen", () => Fire(ClearDayAsync), "rm-btn--danger", "rm-btn--small");
            card.Add(_clearButton);

            scroll.Add(Ui.Label("Forudsigelser er kun et skøn og kan ikke bruges som prævention.", "rm-muted", "rm-center"));
        }

        public override void OnShow()
        {
            if (!Context.Services.Accounts.IsLoggedIn) return;
            ClearError();
            Rebuild();
        }

        private void GoToToday()
        {
            _selectedDate = Context.Services.Diary.Today;
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
            ClearError();
            Rebuild();
        }

        private void Rebuild()
        {
            var diary = Context.Services.Diary;
            var today = diary.Today;
            var prediction = diary.GetPrediction();

            // Mandag først: antal dage fra forrige måned i første uge.
            var leading = ((int)_displayedMonth.DayOfWeek + 6) % 7;
            var start = _displayedMonth.AddDays(-leading);
            var end = start.AddDays(41);
            var entries = diary.GetEntries(start, end).ToDictionary(e => e.Date);

            var cells = new List<CalendarCell>(42);
            for (var i = 0; i < 42; i++)
            {
                var date = start.AddDays(i);
                entries.TryGetValue(date, out var entry);
                cells.Add(new CalendarCell(date, date.Month == _displayedMonth.Month, date == today, date == _selectedDate,
                    prediction.GetDayKind(date), entry?.Mood ?? Mood.None));
            }

            _grid.SetDays(cells);
            _monthTitle.text = Texts.MonthName(_displayedMonth.Month) + " " + _displayedMonth.Year;
            UpdateSelected(prediction);
        }

        private void UpdateSelected(CyclePrediction prediction)
        {
            var diary = Context.Services.Diary;
            var entry = diary.GetEntry(_selectedDate);
            var editable = _selectedDate <= diary.Today;

            _selectedTitle.text = Texts.LongDate(_selectedDate) + (_selectedDate == diary.Today ? " · i dag" : string.Empty);
            _status.SetSelected(entry.MenstruationStatus);
            _flow.SetSelected(entry.FlowIntensity == FlowIntensity.None ? (FlowIntensity?)null : entry.FlowIntensity);
            _mood.SetSelected(entry.Mood == Mood.None ? (Mood?)null : entry.Mood);
            Ui.SetVisible(_editor, editable);
            Ui.SetVisible(_flowSection, entry.HasMenstruation);
            Ui.SetVisible(_clearButton, editable && !entry.IsEmpty);

            switch (prediction.GetDayKind(_selectedDate))
            {
                case CycleDayKind.Period:
                    _selectedInfo.text = "Menstruation";
                    break;
                case CycleDayKind.PredictedPeriod:
                    _selectedInfo.text = "Forventet mens (skøn)";
                    break;
                case CycleDayKind.PredictedMargin:
                    _selectedInfo.text = "Mens kan måske komme her (skøn)";
                    break;
                default:
                    _selectedInfo.text = editable ? string.Empty : "Ingen forventet mens";
                    break;
            }
            if (!editable) _selectedInfo.text += (_selectedInfo.text.Length > 0 ? ". " : string.Empty) + "Fremtidige dage kan ikke registreres.";
        }

        private Task SetStatusAsync(MenstruationStatus status)
        {
            var current = Context.Services.Diary.GetEntry(_selectedDate);
            return SaveAsync(() => Context.Services.Diary.SetMenstruationAsync(_selectedDate, status, current.FlowIntensity));
        }

        private Task SetFlowAsync(FlowIntensity flow)
        {
            var current = Context.Services.Diary.GetEntry(_selectedDate);
            if (!current.HasMenstruation) return Task.CompletedTask;
            // Tryk på det allerede valgte fjerner valget igen.
            var next = current.FlowIntensity == flow ? FlowIntensity.None : flow;
            return SaveAsync(() => Context.Services.Diary.SetMenstruationAsync(_selectedDate, current.MenstruationStatus, next));
        }

        private Task SetMoodAsync(Mood mood)
        {
            var current = Context.Services.Diary.GetEntry(_selectedDate);
            var next = current.Mood == mood ? Mood.None : mood;
            return SaveAsync(() => Context.Services.Diary.SetMoodAsync(_selectedDate, next));
        }

        private Task ClearDayAsync() => SaveAsync(() => Context.Services.Diary.DeleteEntryAsync(_selectedDate));

        private async Task SaveAsync(Func<Task> save)
        {
            if (_saving) return;
            _saving = true;
            try
            {
                ClearError();
                await RunAsync(save, showBusy: false);
                Rebuild();
            }
            finally
            {
                _saving = false;
            }
        }

        private static VisualElement Legend()
        {
            var legend = Ui.Box("rm-legend");
            legend.Add(LegendItem("rm-cal-day--period", "Mens"));
            legend.Add(LegendItem("rm-cal-day--predicted", "Forventet"));
            legend.Add(LegendItem("rm-cal-day--margin", "Måske"));
            return legend;
        }

        private static VisualElement LegendItem(string dayClass, string text)
        {
            var item = Ui.Box("rm-legend-item");
            item.Add(Ui.Box("rm-legend-swatch", dayClass));
            item.Add(Ui.Label(text, "rm-muted"));
            return item;
        }
    }
}
