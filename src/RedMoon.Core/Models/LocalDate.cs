using System;
using System.Globalization;

namespace RedMoon.Core.Models
{
    /// <summary>
    /// En kalenderdato uden klokkeslæt og tidszone (år, måned, dag).
    /// Bruges i stedet for DateTime for at undgå tidszone-fejl, og fordi DateOnly
    /// ikke findes i .NET Standard 2.1 (som Unity kræver).
    /// Internt gemmes datoen som et dagsnummer (dage siden 1. januar år 1).
    /// </summary>
    public readonly struct LocalDate : IEquatable<LocalDate>, IComparable<LocalDate>
    {
        private readonly int _dayNumber;

        private LocalDate(int dayNumber)
        {
            _dayNumber = dayNumber;
        }

        /// <summary>Opretter en dato. Kaster ArgumentOutOfRangeException ved ugyldig dato.</summary>
        public LocalDate(int year, int month, int day)
        {
            // DateTime-konstruktøren validerer år/måned/dag (fx 30. februar afvises).
            _dayNumber = (int)(new DateTime(year, month, day).Ticks / TimeSpan.TicksPerDay);
        }

        /// <summary>Antal dage siden 1. januar år 1. Bruges ved serialisering.</summary>
        public int DayNumber => _dayNumber;

        public int Year => ToDateTime().Year;
        public int Month => ToDateTime().Month;
        public int Day => ToDateTime().Day;

        /// <summary>Ugedag (søndag = 0 ... lørdag = 6), samme som DateTime.</summary>
        public DayOfWeek DayOfWeek => ToDateTime().DayOfWeek;

        /// <summary>Største gyldige dagsnummer (31. december 9999).</summary>
        public static int MaxDayNumber => (int)(DateTime.MaxValue.Ticks / TimeSpan.TicksPerDay);

        /// <summary>Genskaber en dato fra et dagsnummer (fx ved indlæsning fra fil).</summary>
        public static LocalDate FromDayNumber(int dayNumber)
        {
            if (dayNumber < 0 || dayNumber > MaxDayNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(dayNumber));
            }
            return new LocalDate(dayNumber);
        }

        /// <summary>Tager datodelen af et DateTime (klokkeslæt ignoreres).</summary>
        public static LocalDate FromDateTime(DateTime dateTime) => new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);

        /// <summary>Første dag i den givne måned.</summary>
        public static LocalDate FirstOfMonth(int year, int month) => new LocalDate(year, month, 1);

        public DateTime ToDateTime() => new DateTime(_dayNumber * TimeSpan.TicksPerDay, DateTimeKind.Unspecified);

        public LocalDate AddDays(int days) => FromDayNumber(_dayNumber + days);

        public LocalDate AddMonths(int months) => FromDateTime(ToDateTime().AddMonths(months));

        /// <summary>Antal dage fra <paramref name="other"/> til denne dato (positiv hvis denne er senere).</summary>
        public int DaysSince(LocalDate other) => _dayNumber - other._dayNumber;

        public bool Equals(LocalDate other) => _dayNumber == other._dayNumber;
        public override bool Equals(object? obj) => obj is LocalDate other && Equals(other);
        public override int GetHashCode() => _dayNumber;
        public int CompareTo(LocalDate other) => _dayNumber.CompareTo(other._dayNumber);

        public static bool operator ==(LocalDate a, LocalDate b) => a.Equals(b);
        public static bool operator !=(LocalDate a, LocalDate b) => !a.Equals(b);
        public static bool operator <(LocalDate a, LocalDate b) => a._dayNumber < b._dayNumber;
        public static bool operator >(LocalDate a, LocalDate b) => a._dayNumber > b._dayNumber;
        public static bool operator <=(LocalDate a, LocalDate b) => a._dayNumber <= b._dayNumber;
        public static bool operator >=(LocalDate a, LocalDate b) => a._dayNumber >= b._dayNumber;

        /// <summary>ISO-format (yyyy-MM-dd), fx 2026-10-05.</summary>
        public override string ToString() => ToDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
