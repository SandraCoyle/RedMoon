using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.Core.Models;
using RedMoon.Core.Security;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Services
{
    /// <summary>
    /// Alt om kontoen: opret bruger, log ind/ud, husket session, skift mønster/brugernavn og slet alle data.
    /// Der er én konto per installation – alt foregår lokalt uden server.
    /// </summary>
    public sealed class AccountService
    {
        /// <summary>Maks. længde på brugernavn.</summary>
        public const int MaxUsernameLength = 24;

        /// <summary>Yngste tilladte alder ved oprettelse (sikrer et fornuftigt fødselsår).</summary>
        public const int MinimumAgeYears = 6;

        /// <summary>Ældste fødselsår der kan vælges.</summary>
        public const int OldestBirthYear = 1940;

        private readonly VaultRepository _repository;
        private readonly ISecureKeyStore _keyStore;
        private readonly SessionState _session;
        private readonly PasswordHasher _hasher;
        private readonly SecurityOptions _options;
        private readonly IClock _clock;

        public AccountService(VaultRepository repository, ISecureKeyStore keyStore, SessionState session,
            PasswordHasher hasher, SecurityOptions options, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _options.Validate();
        }

        /// <summary>Sand hvis der allerede er oprettet en konto på telefonen.</summary>
        public bool HasAccount => _repository.Exists();

        /// <summary>Sand hvis en bruger er logget ind.</summary>
        public bool IsLoggedIn => _session.IsLoggedIn;

        /// <summary>Den indloggede brugers konto.</summary>
        public UserAccount CurrentAccount => _session.Vault.Account;

        /// <summary>
        /// Opretter en ny konto og logger brugeren ind.
        /// </summary>
        /// <exception cref="ValidationException">Ved ugyldigt input, eller hvis der allerede findes en konto.</exception>
        public async Task CreateAccountAsync(string? username, PatternPassword pattern, int birthYear, int birthMonth)
        {
            if (pattern == null) throw new ValidationException("Tegn et mønster.");
            if (_repository.Exists()) throw new ValidationException("Der findes allerede en bruger på denne telefon.");

            var cleanUsername = NormalizeUsername(username);
            ValidateBirth(birthYear, birthMonth, _clock.Today);

            var hash = await Task.Run(() => _hasher.Hash(pattern)).ConfigureAwait(false);
            var vault = new UserVault(new UserAccount(cleanUsername, hash, birthYear, birthMonth));

            await StartSessionAsync(vault).ConfigureAwait(false);
        }

        /// <summary>
        /// Logger ind med brugernavn og mønster. Efter for mange forkerte forsøg spærres login
        /// i stigende tid (30 sek., 1 min., 2 min. ... op til 15 min.).
        /// </summary>
        public async Task<LoginResult> LoginAsync(string? username, PatternPassword pattern)
        {
            if (pattern == null) throw new ValidationException("Tegn dit mønster.");

            var vault = await _repository.LoadAsync().ConfigureAwait(false);
            if (vault == null) return LoginResult.NoAccount();

            var now = _clock.UtcNow;
            var security = vault.Security;
            if (security.LockoutUntilUtc > now)
            {
                return LoginResult.Locked(security.LockoutUntilUtc, now);
            }

            // Mønsteret verificeres ALTID (også ved forkert brugernavn), så svartiden ikke afslører noget.
            var patternOk = await Task.Run(() => _hasher.Verify(pattern, vault.Account.PasswordHash)).ConfigureAwait(false);
            var usernameOk = UsernamesMatch(username, vault.Account.Username);

            if (patternOk && usernameOk)
            {
                security.FailedLoginAttempts = 0;
                security.LockoutUntilUtc = DateTime.MinValue;
                await StartSessionAsync(vault).ConfigureAwait(false);
                return LoginResult.Success();
            }

            security.FailedLoginAttempts++;
            var overLimit = security.FailedLoginAttempts - _options.MaxFailedAttemptsBeforeLockout;
            if (overLimit >= 0)
            {
                security.LockoutUntilUtc = now + LockoutDuration(overLimit);
                await _repository.SaveAsync(vault).ConfigureAwait(false);
                return LoginResult.Locked(security.LockoutUntilUtc, now);
            }

            await _repository.SaveAsync(vault).ConfigureAwait(false);
            return LoginResult.Wrong(_options.MaxFailedAttemptsBeforeLockout - security.FailedLoginAttempts);
        }

        /// <summary>
        /// Gendanner en husket session ved app-start. Returnerer sand hvis brugeren stadig er logget ind.
        /// Sessionen er gyldig når token i Keychain/Keystore matcher den hash, der ligger i den krypterede datafil.
        /// </summary>
        public async Task<bool> TryRestoreSessionAsync()
        {
            var token = await _keyStore.GetAsync(SecureKeyNames.SessionToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(token)) return false;

            UserVault? vault;
            try
            {
                vault = await _repository.LoadAsync().ConfigureAwait(false);
            }
            catch (VaultCorruptedException)
            {
                return false;
            }
            if (vault == null) return false;

            byte[] tokenBytes;
            try
            {
                tokenBytes = Convert.FromBase64String(token);
            }
            catch (FormatException)
            {
                return false;
            }

            if (vault.Security.SessionTokenHash.Length == 0 || !ConstantTime.AreEqual(Sha256(tokenBytes), vault.Security.SessionTokenHash))
            {
                return false;
            }

            _session.Begin(vault);
            return true;
        }

        /// <summary>Logger ud: sletter session-token og glemmer data i hukommelsen.</summary>
        public async Task LogoutAsync()
        {
            await _keyStore.RemoveAsync(SecureKeyNames.SessionToken).ConfigureAwait(false);
            if (_session.IsLoggedIn)
            {
                var vault = _session.Vault;
                vault.Security.SessionTokenHash = Array.Empty<byte>();
                try
                {
                    await _repository.SaveAsync(vault).ConfigureAwait(false);
                }
                finally
                {
                    _session.End();
                }
            }
        }

        /// <summary>Skifter mønster. Kræver det nuværende mønster.</summary>
        public async Task ChangePatternAsync(PatternPassword currentPattern, PatternPassword newPattern)
        {
            if (currentPattern == null || newPattern == null) throw new ValidationException("Tegn begge mønstre.");
            var vault = _session.Vault;

            var ok = await Task.Run(() => _hasher.Verify(currentPattern, vault.Account.PasswordHash)).ConfigureAwait(false);
            if (!ok) throw new ValidationException("Det nuværende mønster er forkert.");
            if (currentPattern.SameAs(newPattern)) throw new ValidationException("Det nye mønster skal være anderledes end det gamle.");

            var previous = vault.Account.PasswordHash;
            vault.Account.PasswordHash = await Task.Run(() => _hasher.Hash(newPattern)).ConfigureAwait(false);
            try
            {
                await _repository.SaveAsync(vault).ConfigureAwait(false);
            }
            catch
            {
                vault.Account.PasswordHash = previous;
                throw;
            }
        }

        /// <summary>Skifter brugernavn (må gerne være tomt).</summary>
        public async Task ChangeUsernameAsync(string? username)
        {
            var clean = NormalizeUsername(username);
            var vault = _session.Vault;
            var previous = vault.Account.Username;
            vault.Account.Username = clean;
            try
            {
                await _repository.SaveAsync(vault).ConfigureAwait(false);
            }
            catch
            {
                vault.Account.Username = previous;
                throw;
            }
        }

        /// <summary>
        /// Sletter ALT: datafil, krypteringsnøgle og session. Kan ikke fortrydes.
        /// Virker også uden login (bruges ved "Glemt mønster").
        /// </summary>
        public async Task DeleteAllDataAsync()
        {
            await _repository.DeleteAllAsync().ConfigureAwait(false);
            if (_session.IsLoggedIn) _session.End();
        }

        /// <summary>
        /// Validerer og renser et brugernavn. Tomt er tilladt (brugernavn er valgfrit).
        /// Tilladt: bogstaver, tal, mellemrum, punktum, bindestreg og understreg.
        /// </summary>
        public static string NormalizeUsername(string? username)
        {
            var trimmed = (username ?? string.Empty).Trim();
            if (trimmed.Length > MaxUsernameLength)
            {
                throw new ValidationException($"Brugernavnet må højst have {MaxUsernameLength} tegn.");
            }
            foreach (var ch in trimmed)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == ' ' || ch == '.' || ch == '-' || ch == '_'))
                {
                    throw new ValidationException("Brugernavnet må kun indeholde bogstaver, tal, mellemrum og . - _");
                }
            }
            return trimmed;
        }

        /// <summary>Validerer fødselsår og -måned. Fremtidige datoer og urealistiske år afvises.</summary>
        public static void ValidateBirth(int birthYear, int birthMonth, LocalDate today)
        {
            if (birthMonth < 1 || birthMonth > 12) throw new ValidationException("Vælg en fødselsmåned.");
            if (birthYear < OldestBirthYear || birthYear > today.Year - MinimumAgeYears)
            {
                throw new ValidationException("Vælg et gyldigt fødselsår.");
            }
        }

        private static bool UsernamesMatch(string? input, string stored)
        {
            string clean;
            try
            {
                clean = NormalizeUsername(input);
            }
            catch (ValidationException)
            {
                return false;
            }
            return string.Equals(clean, stored, StringComparison.OrdinalIgnoreCase);
        }

        private TimeSpan LockoutDuration(int stepsOverLimit)
        {
            // Fordobling per ekstra forkert forsøg, begrænset af MaxLockout.
            var factor = Math.Pow(2, Math.Min(stepsOverLimit, 20));
            var ticks = Math.Min(_options.InitialLockout.Ticks * factor, _options.MaxLockout.Ticks);
            return TimeSpan.FromTicks((long)ticks);
        }

        private async Task StartSessionAsync(UserVault vault)
        {
            var token = AuthenticatedCipher.GenerateKey();
            vault.Security.SessionTokenHash = Sha256(token);
            await _repository.SaveAsync(vault).ConfigureAwait(false);
            await _keyStore.SetAsync(SecureKeyNames.SessionToken, Convert.ToBase64String(token)).ConfigureAwait(false);
            _session.Begin(vault);
        }

        private static byte[] Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(data);
            }
        }
    }
}
