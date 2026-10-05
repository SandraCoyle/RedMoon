using System;

namespace RedMoon.Core.Models
{
    /// <summary>
    /// Resultatet af at hashe brugerens mønster-adgangskode med PBKDF2-HMAC-SHA256.
    /// Selve mønsteret gemmes ALDRIG – kun salt, hash og antal iterationer.
    /// Antal iterationer gemmes per bruger, så det kan hæves i fremtidige versioner.
    /// </summary>
    public sealed class PasswordHash
    {
        public PasswordHash(byte[] salt, byte[] hash, int iterations)
        {
            if (salt == null || salt.Length < 16) throw new ArgumentException("Salt skal være mindst 16 bytes.", nameof(salt));
            if (hash == null || hash.Length < 32) throw new ArgumentException("Hash skal være mindst 32 bytes.", nameof(hash));
            if (iterations < 1) throw new ArgumentOutOfRangeException(nameof(iterations));

            Salt = (byte[])salt.Clone();
            Hash = (byte[])hash.Clone();
            Iterations = iterations;
        }

        /// <summary>Tilfældigt salt (16 bytes), unikt per bruger.</summary>
        public byte[] Salt { get; }

        /// <summary>PBKDF2-output (32 bytes).</summary>
        public byte[] Hash { get; }

        /// <summary>Antal PBKDF2-iterationer brugt da hashen blev lavet.</summary>
        public int Iterations { get; }
    }
}
