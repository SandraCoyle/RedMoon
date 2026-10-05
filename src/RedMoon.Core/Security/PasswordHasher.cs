using System;
using System.Security.Cryptography;
using RedMoon.Core.Models;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Hasher og verificerer mønster-adgangskoder med PBKDF2-HMAC-SHA256,
    /// et unikt tilfældigt salt per bruger og konstant-tids sammenligning.
    /// </summary>
    public sealed class PasswordHasher
    {
        private const int SaltLength = 16;
        private const int HashLength = 32;

        private readonly SecurityOptions _options;

        public PasswordHasher(SecurityOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Laver en ny hash med nyt tilfældigt salt.</summary>
        public PasswordHash Hash(PatternPassword pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            var salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var secret = pattern.ToSecretBytes();
            try
            {
                var hash = Pbkdf2.DeriveSha256(secret, salt, _options.PasswordIterations, HashLength);
                return new PasswordHash(salt, hash, _options.PasswordIterations);
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
            }
        }

        /// <summary>Sand hvis mønsteret matcher den gemte hash.</summary>
        public bool Verify(PatternPassword pattern, PasswordHash stored)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (stored == null) throw new ArgumentNullException(nameof(stored));

            var secret = pattern.ToSecretBytes();
            try
            {
                var candidate = Pbkdf2.DeriveSha256(secret, stored.Salt, stored.Iterations, stored.Hash.Length);
                return ConstantTime.AreEqual(candidate, stored.Hash);
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
            }
        }
    }
}
