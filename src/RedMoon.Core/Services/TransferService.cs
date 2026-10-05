using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Services
{
    /// <summary>Resultatet af en eksport: den krypterede fil og koden der låser den op.</summary>
    public sealed class TransferPackage
    {
        public TransferPackage(byte[] content, string code, string suggestedFileName)
        {
            Content = content;
            Code = code;
            SuggestedFileName = suggestedFileName;
        }

        /// <summary>Den krypterede fil.</summary>
        public byte[] Content { get; }

        /// <summary>Overførselskoden (vises KUN til brugeren, gemmes aldrig).</summary>
        public string Code { get; }

        public string SuggestedFileName { get; }
    }

    /// <summary>
    /// Overførsel af konto og registreringer til en ny telefon via en krypteret fil.
    ///
    /// Filen krypteres med en nøgle afledt (PBKDF2-HMAC-SHA256) af en tilfældig 16-tegns kode,
    /// som KUN vises på skærmen. Uden koden kan filen ikke læses. Appen sender intet selv –
    /// brugeren vælger selv hvordan filen flyttes (AirDrop, kabel, Nearby Share osv.).
    ///
    /// Filformat: "RMX" + version(1) | salt(16) | iterationer(int32) | IV | ciffertekst | HMAC
    /// </summary>
    public sealed class TransferService
    {
        public const string FileExtension = ".redmoon";
        private const int SaltLength = 16;
        private const int MaxFileBytes = 20 * 1024 * 1024;
        private const int MaxIterations = 10_000_000;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("RMX\u0001");
        private static readonly int HeaderLength = Magic.Length + SaltLength + sizeof(int);

        private readonly VaultRepository _repository;
        private readonly SessionState _session;
        private readonly SecurityOptions _options;
        private readonly IClock _clock;

        public TransferService(VaultRepository repository, SessionState session, SecurityOptions options, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Laver en krypteret kopi af den indloggede brugers konto og registreringer.
        /// Session og login-spærring medtages ikke.
        /// </summary>
        public async Task<TransferPackage> ExportAsync()
        {
            var vault = _session.Vault;
            var code = TransferCode.Generate();
            var payload = VaultSerializer.Serialize(vault, includeSecurityState: false);

            var salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            var iterations = _options.TransferIterations;
            var header = BuildHeader(salt, iterations);

            try
            {
                var content = await Task.Run(() =>
                {
                    var cipher = DeriveCipher(TransferCode.Normalize(code), salt, iterations);
                    return cipher.Encrypt(payload, header);
                }).ConfigureAwait(false);

                var fileName = "redmoon-" + _clock.Today + FileExtension;
                return new TransferPackage(content, code, fileName);
            }
            finally
            {
                Array.Clear(payload, 0, payload.Length);
            }
        }

        /// <summary>
        /// Importerer en overførselsfil på en telefon UDEN eksisterende konto.
        /// Bagefter logger brugeren ind med sit sædvanlige brugernavn og mønster.
        /// </summary>
        /// <exception cref="ValidationException">Forkert kode, ødelagt fil, eller der findes allerede en konto.</exception>
        public async Task ImportAsync(byte[] fileContent, string code)
        {
            if (_repository.Exists())
            {
                throw new ValidationException("Der findes allerede en bruger på denne telefon. Slet den først.");
            }
            if (fileContent == null || fileContent.Length < HeaderLength || fileContent.Length > MaxFileBytes)
            {
                throw new ValidationException("Filen er ikke en gyldig Rød Måne-fil.");
            }

            var normalizedCode = TransferCode.Normalize(code);

            for (var i = 0; i < Magic.Length; i++)
            {
                if (fileContent[i] != Magic[i]) throw new ValidationException("Filen er ikke en gyldig Rød Måne-fil.");
            }

            var salt = new byte[SaltLength];
            Buffer.BlockCopy(fileContent, Magic.Length, salt, 0, SaltLength);
            var iterations = BitConverter.ToInt32(fileContent, Magic.Length + SaltLength);
            if (!BitConverter.IsLittleEndian) iterations = ReverseEndian(iterations);
            if (iterations < 1 || iterations > MaxIterations)
            {
                throw new ValidationException("Filen er ikke en gyldig Rød Måne-fil.");
            }

            var header = new byte[HeaderLength];
            Buffer.BlockCopy(fileContent, 0, header, 0, HeaderLength);

            byte[] payload;
            try
            {
                payload = await Task.Run(() => DeriveCipher(normalizedCode, salt, iterations).Decrypt(fileContent, header)).ConfigureAwait(false);
            }
            catch (VaultCorruptedException)
            {
                throw new ValidationException("Forkert kode, eller filen er ødelagt.");
            }

            try
            {
                var vault = VaultSerializer.Deserialize(payload);
                vault.Security.Reset();
                // Menstruationer genberegnes, så de altid passer til registreringerne.
                vault.SetPeriods(PeriodBuilder.Build(vault.Entries));
                await _repository.SaveAsync(vault).ConfigureAwait(false);
            }
            catch (VaultCorruptedException)
            {
                throw new ValidationException("Filen er ødelagt og kan ikke importeres.");
            }
            finally
            {
                Array.Clear(payload, 0, payload.Length);
            }
        }

        private static byte[] BuildHeader(byte[] salt, int iterations)
        {
            using (var memory = new MemoryStream())
            {
                memory.Write(Magic, 0, Magic.Length);
                memory.Write(salt, 0, salt.Length);
                var iterationBytes = BitConverter.GetBytes(BitConverter.IsLittleEndian ? iterations : ReverseEndian(iterations));
                memory.Write(iterationBytes, 0, iterationBytes.Length);
                return memory.ToArray();
            }
        }

        private static AuthenticatedCipher DeriveCipher(string normalizedCode, byte[] salt, int iterations)
        {
            var secret = TransferCode.ToSecretBytes(normalizedCode);
            try
            {
                return AuthenticatedCipher.FromKeyMaterial(Pbkdf2.DeriveSha256(secret, salt, iterations, AuthenticatedCipher.KeyLength * 2));
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
            }
        }

        private static int ReverseEndian(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
