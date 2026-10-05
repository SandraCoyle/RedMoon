using System;
using System.Collections.Generic;
using System.Linq;

namespace RedMoon.Core.Models
{
    /// <summary>
    /// Sikkerhedsrelateret tilstand, der er nødvendig for at login kan fungere sikkert.
    /// Indeholder ingen personoplysninger.
    /// </summary>
    public sealed class SecurityState
    {
        /// <summary>Antal forkerte login-forsøg i træk.</summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>Login er spærret indtil dette tidspunkt (UTC). DateTime.MinValue = ingen spærring.</summary>
        public DateTime LockoutUntilUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// SHA-256 af den aktive session-token. Selve tokenet ligger i telefonens Keychain/Keystore.
        /// Tom = ingen aktiv session.
        /// </summary>
        public byte[] SessionTokenHash { get; set; } = Array.Empty<byte>();

        /// <summary>Nulstiller sessionen og spærringer (bruges ved eksport/import).</summary>
        public void Reset()
        {
            FailedLoginAttempts = 0;
            LockoutUntilUtc = DateTime.MinValue;
            SessionTokenHash = Array.Empty<byte>();
        }
    }

    /// <summary>
    /// "Pengeskabet": ALT hvad appen gemmer om brugeren, samlet i ét objekt.
    /// Hele objektet krypteres og gemmes som én fil på telefonen.
    /// </summary>
    public sealed class UserVault
    {
        private readonly SortedDictionary<LocalDate, DailyEntry> _entries = new SortedDictionary<LocalDate, DailyEntry>();
        private List<MenstruationPeriod> _periods = new List<MenstruationPeriod>();

        public UserVault(UserAccount account)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
        }

        /// <summary>Brugerens konto.</summary>
        public UserAccount Account { get; }

        /// <summary>Login-sikkerhed (forkerte forsøg, spærring, session).</summary>
        public SecurityState Security { get; } = new SecurityState();

        /// <summary>Alle daglige registreringer sorteret efter dato.</summary>
        public IEnumerable<DailyEntry> Entries => _entries.Values;

        /// <summary>Antal daglige registreringer.</summary>
        public int EntryCount => _entries.Count;

        /// <summary>Alle menstruationer sorteret efter startdato.</summary>
        public IReadOnlyList<MenstruationPeriod> Periods => _periods;

        /// <summary>Henter registreringen for en dato, eller null hvis der ikke er nogen.</summary>
        public DailyEntry? GetEntry(LocalDate date) => _entries.TryGetValue(date, out var entry) ? entry : null;

        /// <summary>Indsætter eller erstatter en registrering. Tomme registreringer fjernes i stedet.</summary>
        internal void SetEntry(DailyEntry entry)
        {
            if (entry.IsEmpty)
            {
                _entries.Remove(entry.Date);
            }
            else
            {
                _entries[entry.Date] = entry;
            }
        }

        /// <summary>Fjerner registreringen for en dato. Returnerer sand hvis der var en.</summary>
        internal bool RemoveEntry(LocalDate date) => _entries.Remove(date);

        /// <summary>Erstatter listen af menstruationer (efter genberegning).</summary>
        internal void SetPeriods(IEnumerable<MenstruationPeriod> periods)
        {
            _periods = periods.OrderBy(p => p.Start).ToList();
        }
    }
}
