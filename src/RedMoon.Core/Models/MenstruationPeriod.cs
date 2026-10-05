using System;

namespace RedMoon.Core.Models
{
    /// <summary>
    /// En samlet menstruation med startdato, slutdato og længde i dage.
    /// Bygges automatisk ud fra de daglige registreringer (se PeriodBuilder) og gemmes sammen med dem.
    /// </summary>
    public sealed class MenstruationPeriod
    {
        public MenstruationPeriod(LocalDate start, LocalDate end, bool endConfirmed)
        {
            if (end < start) throw new ArgumentException("Slutdato kan ikke ligge før startdato.", nameof(end));
            Start = start;
            End = end;
            EndConfirmed = endConfirmed;
        }

        /// <summary>Første menstruationsdag.</summary>
        public LocalDate Start { get; }

        /// <summary>Sidste registrerede menstruationsdag.</summary>
        public LocalDate End { get; }

        /// <summary>Længde i dage (inklusiv start- og slutdag).</summary>
        public int LengthDays => End.DaysSince(Start) + 1;

        /// <summary>
        /// Sand hvis brugeren har trykket "Afslut menstruation" (sidste dag).
        /// Hvis falsk, er slutdatoen blot den seneste registrerede menstruationsdag,
        /// og længden bruges derfor ikke til at beregne gennemsnitlig menstruationslængde.
        /// </summary>
        public bool EndConfirmed { get; }

        /// <summary>Sand hvis datoen ligger inden for menstruationen.</summary>
        public bool Contains(LocalDate date) => date >= Start && date <= End;
    }
}
