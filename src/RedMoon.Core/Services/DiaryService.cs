using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.Core.Models;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Dagbogen: læs, gem, ret og slet daglige registreringer (humør og menstruation),
    /// samt beregning af cyklus og forudsigelser.
    /// Hver ændring gemmes straks krypteret. Fejler gemningen, rulles ændringen tilbage i hukommelsen.
    /// </summary>
    public sealed class DiaryService
    {
        private readonly VaultRepository _repository;
        private readonly SessionState _session;
        private readonly IClock _clock;

        public DiaryService(VaultRepository repository, SessionState session, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Udløses efter hver gemt ændring, så skærme kan opdatere sig.</summary>
        public event EventHandler? EntriesChanged;

        /// <summary>Dagens dato.</summary>
        public LocalDate Today => _clock.Today;

        /// <summary>Henter registreringen for en dato. Returnerer en tom registrering hvis der ingen er.</summary>
        public DailyEntry GetEntry(LocalDate date) => _session.Vault.GetEntry(date) ?? DailyEntry.Empty(date);

        /// <summary>Henter alle registreringer i et datointerval (begge datoer inklusive).</summary>
        public IReadOnlyList<DailyEntry> GetEntries(LocalDate from, LocalDate to) =>
            _session.Vault.Entries.Where(e => e.Date >= from && e.Date <= to).ToList();

        /// <summary>Alle registrerede menstruationer.</summary>
        public IReadOnlyList<MenstruationPeriod> Periods => _session.Vault.Periods;

        /// <summary>Beregner cyklus og forudsigelser for i dag.</summary>
        public CyclePrediction GetPrediction() => CyclePredictor.Predict(_session.Vault.Periods, _clock.Today);

        /// <summary>Sætter humøret for en dato (Mood.None fjerner det).</summary>
        public Task<DailyEntry> SetMoodAsync(LocalDate date, Mood mood)
        {
            ValidateEnum(mood);
            return SaveEntryAsync(GetEntry(date).WithMood(mood));
        }

        /// <summary>Sætter menstruationsstatus og intensitet for en dato.</summary>
        public Task<DailyEntry> SetMenstruationAsync(LocalDate date, MenstruationStatus status, FlowIntensity intensity)
        {
            ValidateEnum(status);
            ValidateEnum(intensity);
            return SaveEntryAsync(GetEntry(date).WithMenstruation(status, intensity));
        }

        /// <summary>
        /// Gemmer (opretter eller erstatter) en registrering. En tom registrering sletter dagen.
        /// </summary>
        /// <exception cref="ValidationException">Hvis datoen ligger i fremtiden.</exception>
        public async Task<DailyEntry> SaveEntryAsync(DailyEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            ValidateDate(entry.Date);
            ValidateEnum(entry.Mood);
            ValidateEnum(entry.MenstruationStatus);
            ValidateEnum(entry.FlowIntensity);

            var vault = _session.Vault;
            var previous = vault.GetEntry(entry.Date);

            vault.SetEntry(entry);
            await PersistOrRollbackAsync(vault, entry.Date, previous).ConfigureAwait(false);
            return entry;
        }

        /// <summary>Sletter registreringen for en dato. Returnerer sand hvis der var noget at slette.</summary>
        public async Task<bool> DeleteEntryAsync(LocalDate date)
        {
            var vault = _session.Vault;
            var previous = vault.GetEntry(date);
            if (previous == null) return false;

            vault.RemoveEntry(date);
            await PersistOrRollbackAsync(vault, date, previous).ConfigureAwait(false);
            return true;
        }

        private async Task PersistOrRollbackAsync(UserVault vault, LocalDate date, DailyEntry? previous)
        {
            vault.SetPeriods(PeriodBuilder.Build(vault.Entries));
            try
            {
                await _repository.SaveAsync(vault).ConfigureAwait(false);
            }
            catch
            {
                // Gendan tilstanden i hukommelsen, så den matcher det der ligger på disken.
                vault.RemoveEntry(date);
                if (previous != null) vault.SetEntry(previous);
                vault.SetPeriods(PeriodBuilder.Build(vault.Entries));
                throw;
            }
            EntriesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateDate(LocalDate date)
        {
            if (date > _clock.Today) throw new ValidationException("Du kan ikke registrere noget for en dag i fremtiden.");
            if (date.Year < 1900) throw new ValidationException("Ugyldig dato.");
        }

        private static void ValidateEnum<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value)) throw new ValidationException("Ugyldig værdi.");
        }
    }
}
