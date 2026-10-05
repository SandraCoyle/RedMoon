using System;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Samlede sikkerhedsindstillinger. Standardværdierne bruges i appen;
    /// tests kan sænke antallet af iterationer, så de kører hurtigt.
    /// </summary>
    public sealed class SecurityOptions
    {
        /// <summary>
        /// PBKDF2-HMAC-SHA256-iterationer for mønster-adgangskoden.
        /// 600.000 følger OWASP Password Storage Cheat Sheet (anbefaling for PBKDF2-HMAC-SHA256).
        /// </summary>
        public int PasswordIterations { get; set; } = 600_000;

        /// <summary>PBKDF2-iterationer for overførselskoden ved eksport/import af data.</summary>
        public int TransferIterations { get; set; } = 600_000;

        /// <summary>Antal forkerte forsøg før login spærres midlertidigt.</summary>
        public int MaxFailedAttemptsBeforeLockout { get; set; } = 5;

        /// <summary>Første spærretid. Fordobles for hvert yderligere forkert forsøg.</summary>
        public TimeSpan InitialLockout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Længste spærretid.</summary>
        public TimeSpan MaxLockout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>Kaster hvis indstillingerne er ugyldige.</summary>
        public void Validate()
        {
            if (PasswordIterations < 1 || TransferIterations < 1) throw new ArgumentOutOfRangeException(nameof(PasswordIterations));
            if (MaxFailedAttemptsBeforeLockout < 1) throw new ArgumentOutOfRangeException(nameof(MaxFailedAttemptsBeforeLockout));
            if (InitialLockout <= TimeSpan.Zero || MaxLockout < InitialLockout) throw new ArgumentOutOfRangeException(nameof(InitialLockout));
        }
    }
}
