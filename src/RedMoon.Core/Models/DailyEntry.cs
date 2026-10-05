namespace RedMoon.Core.Models
{
    /// <summary>
    /// Én dags registrering. En DailyEntry er altid knyttet til præcis én dato.
    /// Uforanderlig (immutable): ændringer laves ved at oprette en ny instans med With-metoderne.
    /// </summary>
    public sealed class DailyEntry
    {
        public DailyEntry(LocalDate date, Mood mood, MenstruationStatus menstruationStatus, FlowIntensity flowIntensity)
        {
            Date = date;
            Mood = mood;
            MenstruationStatus = menstruationStatus;
            // Intensitet giver kun mening hvis der er menstruation; ellers nulstilles den.
            FlowIntensity = menstruationStatus == MenstruationStatus.None ? FlowIntensity.None : flowIntensity;
        }

        /// <summary>Den dato registreringen gælder.</summary>
        public LocalDate Date { get; }

        /// <summary>Dagens humør (None = ikke registreret).</summary>
        public Mood Mood { get; }

        /// <summary>Menstruationsstatus for dagen.</summary>
        public MenstruationStatus MenstruationStatus { get; }

        /// <summary>Intensitet i blødning (kun relevant ved menstruation).</summary>
        public FlowIntensity FlowIntensity { get; }

        /// <summary>Sand hvis dagen ikke indeholder nogen registrering og derfor kan slettes.</summary>
        public bool IsEmpty => Mood == Mood.None && MenstruationStatus == MenstruationStatus.None;

        /// <summary>Sand hvis dagen er registreret som menstruationsdag.</summary>
        public bool HasMenstruation => MenstruationStatus != MenstruationStatus.None;

        /// <summary>Tom registrering for en dato.</summary>
        public static DailyEntry Empty(LocalDate date) => new DailyEntry(date, Mood.None, MenstruationStatus.None, FlowIntensity.None);

        public DailyEntry WithMood(Mood mood) => new DailyEntry(Date, mood, MenstruationStatus, FlowIntensity);

        public DailyEntry WithMenstruation(MenstruationStatus status, FlowIntensity intensity) => new DailyEntry(Date, Mood, status, intensity);
    }
}
