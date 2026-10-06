using System;
using System.Collections.Generic;
using RedMoon.Core.Models;
using RedMoon.UnityApp.Graphics;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Controls
{
    /// <summary>Det der vises i én kalenderdag.</summary>
    internal sealed class CalendarCell
    {
        public CalendarCell(LocalDate date, bool isInMonth, bool isToday, bool isSelected, CycleDayKind kind, Mood mood)
        {
            Date = date;
            IsInMonth = isInMonth;
            IsToday = isToday;
            IsSelected = isSelected;
            Kind = kind;
            Mood = mood;
        }

        public LocalDate Date { get; }
        public bool IsInMonth { get; }
        public bool IsToday { get; }
        public bool IsSelected { get; }
        public CycleDayKind Kind { get; }
        public Mood Mood { get; }
    }

    /// <summary>
    /// Månedskalender: 7 kolonner (mandag først) x 6 uger.
    /// Farver: fyldt = registreret menstruation, lys = forventet menstruation, svag = måske.
    /// I dag har en kant; valgt dag en tyk hvid kant. Humør vises som et lille ikon.
    /// </summary>
    internal sealed class CalendarGrid : VisualElement
    {
        public CalendarGrid()
        {
            AddToClassList("rm-cal-grid");
        }

        /// <summary>Udløses med datoen når brugeren trykker på en dag.</summary>
        public event Action<LocalDate>? DayTapped;

        public void SetDays(IReadOnlyList<CalendarCell> cells)
        {
            Clear();
            foreach (var initial in Texts.WeekdayInitials)
            {
                var header = Ui.Label(initial, "rm-cal-weekday");
                Add(header);
            }

            foreach (var cell in cells)
            {
                Add(CreateCell(cell));
            }
        }

        private VisualElement CreateCell(CalendarCell cell)
        {
            var wrapper = Ui.Box("rm-cal-cell");
            var date = cell.Date;
            var day = new Button(() => DayTapped?.Invoke(date)) { text = string.Empty };
            day.AddToClassList("rm-cal-day");
            wrapper.Add(day);

            day.Add(Ui.Label(cell.Date.Day.ToString(), "rm-cal-num"));
            if (cell.Mood != Mood.None)
            {
                day.Add(Ui.Icon(Icons.Mood(cell.Mood), 14));
            }

            Ui.SetClass(day, "rm-cal-day--period", cell.Kind == CycleDayKind.Period);
            Ui.SetClass(day, "rm-cal-day--predicted", cell.Kind == CycleDayKind.PredictedPeriod);
            Ui.SetClass(day, "rm-cal-day--margin", cell.Kind == CycleDayKind.PredictedMargin);
            Ui.SetClass(day, "rm-cal-day--today", cell.IsToday);
            Ui.SetClass(day, "rm-cal-day--selected", cell.IsSelected);
            Ui.SetClass(wrapper, "rm-cal-cell--outside", !cell.IsInMonth);
            return wrapper;
        }
    }
}
