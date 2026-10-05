using System;
using System.Collections.Generic;
using System.Linq;
using RedMoon.Core.Models;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Beregner gennemsnitlig cykluslængde, menstruationslængde og usikkerhed ud fra brugerens
    /// registrerede menstruationer. Uden egne data bruges standardværdierne 28 og 5 dage.
    /// </summary>
    public static class CyclePredictor
    {
        public const int DefaultCycleLengthDays = 28;
        public const int DefaultPeriodLengthDays = 5;
        public const int DefaultVariationDays = 2;

        /// <summary>Hvor mange af de seneste cyklusser der bruges i gennemsnittet.</summary>
        public const int HistorySize = 6;

        /// <summary>Cyklusser kortere/længere end dette betragtes som fejlregistreringer og ignoreres.</summary>
        public const int MinPlausibleCycleDays = 15;
        public const int MaxPlausibleCycleDays = 60;

        public static CyclePrediction Predict(IReadOnlyList<MenstruationPeriod> periods, LocalDate today)
        {
            if (periods == null) throw new ArgumentNullException(nameof(periods));

            var ordered = periods.OrderBy(p => p.Start).ToList();

            // Cykluslængde = antal dage fra én menstruations start til den næstes start.
            var cycleLengths = new List<int>();
            for (var i = 1; i < ordered.Count; i++)
            {
                var length = ordered[i].Start.DaysSince(ordered[i - 1].Start);
                if (length >= MinPlausibleCycleDays && length <= MaxPlausibleCycleDays)
                {
                    cycleLengths.Add(length);
                }
            }
            cycleLengths = cycleLengths.Skip(Math.Max(0, cycleLengths.Count - HistorySize)).ToList();

            // Menstruationslængde: kun menstruationer hvor brugeren har bekræftet sidste dag.
            var periodLengths = ordered
                .Where(p => p.EndConfirmed && p.LengthDays >= 1 && p.LengthDays <= PeriodBuilder.MaxPeriodLengthDays)
                .Select(p => p.LengthDays)
                .ToList();
            periodLengths = periodLengths.Skip(Math.Max(0, periodLengths.Count - HistorySize)).ToList();

            var cycleLength = cycleLengths.Count > 0 ? (int)Math.Round(cycleLengths.Average()) : DefaultCycleLengthDays;
            var periodLength = periodLengths.Count > 0 ? (int)Math.Round(periodLengths.Average()) : DefaultPeriodLengthDays;

            // Menstruationen kan ikke vare længere end selve cyklussen.
            periodLength = Math.Min(periodLength, cycleLength - 1);

            var variation = DefaultVariationDays;
            if (cycleLengths.Count >= 2)
            {
                var mean = cycleLengths.Average();
                var stdDev = Math.Sqrt(cycleLengths.Sum(c => (c - mean) * (c - mean)) / (cycleLengths.Count - 1));
                variation = Math.Max(1, Math.Min(7, (int)Math.Round(stdDev)));
            }

            return new CyclePrediction(ordered, today, cycleLength, periodLength, variation, cycleLengths.Count, periodLengths.Count);
        }
    }
}
