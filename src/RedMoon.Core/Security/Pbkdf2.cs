using System.Security.Cryptography;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// PBKDF2-HMAC-SHA256 nøgleafledning (RFC 8018).
    /// På .NET 10 bruges den native one-shot implementering (hurtigere),
    /// på .NET Standard 2.1 (Unity) bruges Rfc2898DeriveBytes. Begge giver identisk output.
    /// </summary>
    internal static class Pbkdf2
    {
        public static byte[] DeriveSha256(byte[] secret, byte[] salt, int iterations, int outputLength)
        {
#if NET
            return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA256, outputLength);
#else
            using (var kdf = new Rfc2898DeriveBytes(secret, salt, iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(outputLength);
            }
#endif
        }
    }
}
