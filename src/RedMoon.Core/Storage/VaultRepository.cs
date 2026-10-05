using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.Core.Models;
using RedMoon.Core.Security;

namespace RedMoon.Core.Storage
{
    /// <summary>
    /// Gemmer og indlæser den krypterede datafil.
    ///
    /// Sikkerhedsmodel:
    /// - Al brugerdata ligger i ÉN fil i appens private mappe, krypteret med AES-256 + HMAC-SHA256.
    /// - Nøglen er 256 tilfældige bits, genereret på telefonen og gemt i Keychain (iOS) / Keystore (Android).
    /// - Uden nøglen fra telefonens sikre hardware-lager er filen ulæselig.
    /// </summary>
    public sealed class VaultRepository
    {
        /// <summary>Navnet på den krypterede datafil.</summary>
        public const string VaultFileName = "redmoon.vault";

        // Fast header: "RMV" + formatversion. Autentificeres af HMAC'en.
        private static readonly byte[] Header = Encoding.ASCII.GetBytes("RMV\u0001");

        private readonly IFileStore _fileStore;
        private readonly ISecureKeyStore _keyStore;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public VaultRepository(IFileStore fileStore, ISecureKeyStore keyStore)
        {
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
        }

        /// <summary>Sand hvis der findes en konto på telefonen.</summary>
        public bool Exists() => _fileStore.Exists(VaultFileName);

        /// <summary>
        /// Indlæser og dekrypterer data. Returnerer null hvis der ingen konto er.
        /// Kaster VaultCorruptedException hvis filen findes, men ikke kan læses.
        /// </summary>
        public async Task<UserVault?> LoadAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var data = await _fileStore.ReadAsync(VaultFileName).ConfigureAwait(false);
                if (data == null) return null;

                var key = await ReadKeyAsync().ConfigureAwait(false);
                if (key == null)
                {
                    throw new VaultCorruptedException("Krypteringsnøglen mangler på denne telefon. Data kan ikke læses.");
                }

                var plaintext = AuthenticatedCipher.FromMasterKey(key).Decrypt(data, Header);
                try
                {
                    return VaultSerializer.Deserialize(plaintext);
                }
                finally
                {
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>Krypterer og gemmer data. Opretter en ny nøgle første gang.</summary>
        public async Task SaveAsync(UserVault vault)
        {
            if (vault == null) throw new ArgumentNullException(nameof(vault));

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var key = await ReadKeyAsync().ConfigureAwait(false);
                if (key == null || !_fileStore.Exists(VaultFileName))
                {
                    // Ny konto: altid en frisk nøgle (også hvis en gammel nøgle ligger tilbage i iOS Keychain efter geninstallation).
                    key = AuthenticatedCipher.GenerateKey();
                    await _keyStore.SetAsync(SecureKeyNames.VaultKey, Convert.ToBase64String(key)).ConfigureAwait(false);
                }

                var plaintext = VaultSerializer.Serialize(vault);
                try
                {
                    var encrypted = AuthenticatedCipher.FromMasterKey(key).Encrypt(plaintext, Header);
                    await _fileStore.WriteAtomicAsync(VaultFileName, encrypted).ConfigureAwait(false);
                }
                finally
                {
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>Sletter datafilen og alle nøgler permanent.</summary>
        public async Task DeleteAllAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                _fileStore.Delete(VaultFileName);
                await _keyStore.RemoveAsync(SecureKeyNames.VaultKey).ConfigureAwait(false);
                await _keyStore.RemoveAsync(SecureKeyNames.SessionToken).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<byte[]?> ReadKeyAsync()
        {
            var encoded = await _keyStore.GetAsync(SecureKeyNames.VaultKey).ConfigureAwait(false);
            if (string.IsNullOrEmpty(encoded)) return null;
            try
            {
                var key = Convert.FromBase64String(encoded);
                return key.Length == AuthenticatedCipher.KeyLength ? key : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
