using System;
using RedMoon.Core.Models;

namespace RedMoon.Core.Common
{
    /// <summary>
    /// Abstraktion over systemuret, så tests kan styre "i dag" og "nu".
    /// </summary>
    public interface IClock
    {
        /// <summary>Dagens dato i telefonens lokale tidszone.</summary>
        LocalDate Today { get; }

        /// <summary>Nuværende tidspunkt i UTC (bruges til spærretid efter forkerte login-forsøg).</summary>
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Standardimplementering der bruger telefonens ur.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        public LocalDate Today => LocalDate.FromDateTime(DateTime.Now);
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
