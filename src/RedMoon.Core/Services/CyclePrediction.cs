using System;
using System.Collections.Generic;
using RedMoon.Core.Models;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Resultatet af en cyklusberegning for en given dag ("i dag").
    /// Alle tal er SKØN baseret på brugerens egne registreringer (eller standardværdier 28/5 dage).
    /// Det er IKKE egnet som prævention.
    /// </summary>
    public sealed class CyclePrediction
    {
        /// <summary>Hvor mange cyklusser frem kalenderen viser forudsigelser.</summary>
        public const int CyclesAhead = 12;

        private readonly IReadOnlyList<MenstruationPeriod> _periods;

        internal CyclePrediction(
            IReadOnlyList<MenstruationPeriod> periods,
            LocalDate today,
            int cycleLengthDays,
            int periodLengthDays,
            int variationDays,
            int cyclesUsed,
            int periodsUsed)
        {
            _periods = periods;
            Today = today;
            CycleLengthDays = cycleLengthDays;
            PeriodLengthDays = periodLengthDays;
            VariationDays = variationDays;
            CyclesUsed = cyclesUsed;
            PeriodsUsed = periodsUsed;
            LastPeriod = periods.Count > 0 ? periods[periods.Count - 1] : null;

            if (LastPeriod != null)
            {
                var expected = LastPeriod.Start.AddDays(CycleLengthDays);
                var daysLate = Today.DaysSince(expected);
                if (daysLate > CycleLengthDays)
                {
                    // Mere end en hel cyklus uden registrering: data er for gamle til at forudsige ud fra.
                    IsStale = true;
                }
                else if (daysLate > 0)
                {
                    DaysLate = daysLate;
                    NextPeriodStart = Today;
                }
                else
                {
                    NextPeriodStart = expected;
                }
            }
        }

        /// <summary>Datoen beregningen er lavet for.</summary>
        public LocalDate Today { get; }

        /// <summary>Gennemsnitlig cykluslængde i dage (standard 28).</summary>
        public int CycleLengthDays { get; }

        /// <summary>Gennemsnitlig menstruationslængde i dage (standard 5).</summary>
        public int PeriodLengthDays { get; }

        /// <summary>Usikkerhed på starttidspunktet i dage (± dage).</summary>
        public int VariationDays { get; }

        /// <summary>Antal af brugerens egne cyklusser der indgår i gennemsnittet (0 = standardværdier).</summary>
        public int CyclesUsed { get; }

        /// <summary>Antal afsluttede menstruationer der indgår i gennemsnitslængden.</summary>
        public int PeriodsUsed { get; }

        /// <summary>Sand hvis beregningen bygger på brugerens egne data frem for standardværdier.</summary>
        public bool IsPersonal => CyclesUsed > 0 || PeriodsUsed > 0;

        /// <summary>Seneste registrerede menstruation, eller null.</summary>
        public MenstruationPeriod? LastPeriod { get; }

        /// <summary>Forventet start på næste menstruation, eller null hvis den ikke kan beregnes.</summary>
        public LocalDate? NextPeriodStart { get; }

        /// <summary>Antal dage menstruationen er forsinket ift. forventet (0 hvis ikke forsinket).</summary>
        public int DaysLate { get; }

        /// <summary>Sand hvis seneste registrering er så gammel, at der ikke kan forudsiges noget.</summary>
        public bool IsStale { get; }

        /// <summary>Sand hvis der er nok data til at vise hvor i cyklussen brugeren er.</summary>
        public bool HasCycleData => LastPeriod != null && !IsStale;

        /// <summary>Hvilken dag i cyklussen det er i dag (1 = første menstruationsdag), eller null.</summary>
        public int? DayOfCycle => HasCycleData ? Today.DaysSince(LastPeriod!.Start) + 1 : (int?)null;

        /// <summary>Dage til næste forventede menstruation (0 = i dag), eller null.</summary>
        public int? DaysUntilNextPeriod => NextPeriodStart.HasValue ? NextPeriodStart.Value.DaysSince(Today) : (int?)null;

        /// <summary>Sand hvis i dag ligger i en registreret menstruation.</summary>
        public bool IsMenstruatingToday => GetDayKind(Today) == CycleDayKind.Period;

        /// <summary>Cyklussens "årstid" i dag.</summary>
        public CycleSeason SeasonToday => DayOfCycle.HasValue ? GetSeason(DayOfCycle.Value) : CycleSeason.Unknown;

        /// <summary>
        /// Omtrentlig årstid for en cyklusdag. Ægløsning antages ca. 14 dage før næste menstruation
        /// (lærebogsantagelse – varierer meget fra person til person, især for unge).
        /// </summary>
        public CycleSeason GetSeason(int dayOfCycle)
        {
            if (dayOfCycle < 1) return CycleSeason.Unknown;
            if (dayOfCycle <= PeriodLengthDays) return CycleSeason.Winter;

            var ovulationDay = Math.Max(PeriodLengthDays + 2, CycleLengthDays - 14);
            if (dayOfCycle < ovulationDay - 2) return CycleSeason.Spring;
            if (dayOfCycle <= ovulationDay + 1) return CycleSeason.Summer;
            return CycleSeason.Autumn;
        }

        /// <summary>
        /// Hvordan en dato skal vises i kalenderen: registreret menstruation, forudsagt menstruation,
        /// usikkerhedsmargen eller intet. Forudsigelser vises kun fra i dag og frem.
        /// </summary>
        public CycleDayKind GetDayKind(LocalDate date)
        {
            foreach (var period in _periods)
            {
                if (period.Contains(date)) return CycleDayKind.Period;
            }

            if (LastPeriod == null || IsStale) return CycleDayKind.None;

            // Igangværende menstruation uden "afslut": vis de forventede resterende dage.
            if (!LastPeriod.EndConfirmed && date > LastPeriod.End && date <= LastPeriod.Start.AddDays(PeriodLengthDays - 1) && date >= Today)
            {
                return CycleDayKind.PredictedPeriod;
            }

            if (!NextPeriodStart.HasValue || date < Today) return CycleDayKind.None;

            var first = NextPeriodStart.Value;
            var offset = date.DaysSince(first);
            if (offset < -VariationDays) return CycleDayKind.None;

            // Find nærmeste forudsagte cyklus (k = 0 er næste menstruation).
            var k = offset < 0 ? 0 : offset / CycleLengthDays;
            if (k >= CyclesAhead) return CycleDayKind.None;

            for (var candidate = Math.Max(0, k - 1); candidate <= k + 1 && candidate < CyclesAhead; candidate++)
            {
                var start = first.AddDays(candidate * CycleLengthDays);
                var end = start.AddDays(PeriodLengthDays - 1);
                if (date >= start && date <= end) return CycleDayKind.PredictedPeriod;
            }
            for (var candidate = Math.Max(0, k - 1); candidate <= k + 1 && candidate < CyclesAhead; candidate++)
            {
                var start = first.AddDays(candidate * CycleLengthDays);
                var end = start.AddDays(PeriodLengthDays - 1);
                if (date >= start.AddDays(-VariationDays) && date <= end.AddDays(VariationDays)) return CycleDayKind.PredictedMargin;
            }
            return CycleDayKind.None;
        }
    }
}
