namespace RedMoon.Core.Models
{
    /// <summary>
    /// Dagens humør. Værdierne gemmes som tal i datafilen, så rækkefølgen må ALDRIG ændres –
    /// nye humør skal tilføjes med nye tal i slutningen.
    /// </summary>
    public enum Mood : byte
    {
        /// <summary>Intet humør registreret.</summary>
        None = 0,
        Outgoing = 1,     // Udadvendt
        Happy = 2,        // Glad
        InBetween = 3,    // Midtimellem
        Sad = 4,          // Trist
        Introverted = 5,  // Indadvendt
        Powerful = 6,     // Powerful
    }

    /// <summary>
    /// Brugerens egen registrering af menstruation for en bestemt dag.
    /// Gemmes som tal – rækkefølgen må ikke ændres.
    /// </summary>
    public enum MenstruationStatus : byte
    {
        /// <summary>Ingen menstruation registreret denne dag.</summary>
        None = 0,
        /// <summary>"Ja" – første menstruationsdag.</summary>
        FirstDay = 1,
        /// <summary>Efterfølgende menstruationsdag.</summary>
        Ongoing = 2,
        /// <summary>Sidste dag – afslutter menstruationen.</summary>
        LastDay = 3,
    }

    /// <summary>
    /// Intensitet i blødning. Kan kun sættes når der er registreret menstruation.
    /// Gemmes som tal – rækkefølgen må ikke ændres.
    /// </summary>
    public enum FlowIntensity : byte
    {
        None = 0,
        Light = 1,   // Lidt
        Medium = 2,  // Noget
        Heavy = 3,   // Meget
    }

    /// <summary>
    /// Hvordan en kalenderdag skal vises ift. menstruation (beregnet, ikke gemt).
    /// </summary>
    public enum CycleDayKind
    {
        /// <summary>Ingen menstruation og ingen forudsigelse.</summary>
        None,
        /// <summary>Del af en registreret menstruation.</summary>
        Period,
        /// <summary>Forudsagt menstruationsdag (størst sandsynlighed).</summary>
        PredictedPeriod,
        /// <summary>Dag i usikkerhedsmargenen omkring en forudsagt menstruation.</summary>
        PredictedMargin,
    }

    /// <summary>
    /// Cyklussens "årstider" – en ikke-medicinsk og letforståelig måde at beskrive faserne på
    /// (vinter = menstruation, forår, sommer = omkring ægløsning, efterår = op til næste menstruation).
    /// Ren visning; gemmes ikke.
    /// </summary>
    public enum CycleSeason
    {
        Unknown,
        Winter,
        Spring,
        Summer,
        Autumn,
    }
}
