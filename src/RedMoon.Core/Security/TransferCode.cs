using System;
using System.Security.Cryptography;
using System.Text;
using RedMoon.Core.Common;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Engangskode til at låse en overførselsfil op på en ny telefon.
    /// 16 tegn fra et alfabet på 32 tegn = 80 bit tilfældighed, vist som XXXX-XXXX-XXXX-XXXX.
    /// Alfabetet udelader tegn der let forveksles (0/O, 1/I).
    ///
    /// Hvorfor ikke bruge mønster-adgangskoden? Et mønster har kun få tusinde muligheder og kan
    /// gættes på under et sekund, hvis nogen får fat i filen. 80 bit kan ikke gættes.
    /// </summary>
    public static class TransferCode
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int CodeLength = 16;

        /// <summary>Genererer en ny tilfældig kode, formateret med bindestreger.</summary>
        public static string Generate()
        {
            var bytes = new byte[CodeLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var builder = new StringBuilder(CodeLength + 3);
            for (var i = 0; i < CodeLength; i++)
            {
                if (i > 0 && i % 4 == 0) builder.Append('-');
                // 256 er deleligt med 32, så modulo giver ingen skævhed.
                builder.Append(Alphabet[bytes[i] % Alphabet.Length]);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Normaliserer brugerens indtastning (store bogstaver, uden mellemrum/bindestreger)
        /// og validerer at den har det rigtige format.
        /// </summary>
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ValidationException("Skriv overførselskoden.");

            var builder = new StringBuilder(CodeLength);
            foreach (var ch in input.ToUpperInvariant())
            {
                if (ch == '-' || char.IsWhiteSpace(ch)) continue;
                if (Alphabet.IndexOf(ch) < 0) throw new ValidationException("Koden indeholder et ugyldigt tegn.");
                builder.Append(ch);
            }

            if (builder.Length != CodeLength) throw new ValidationException("Koden skal have 16 tegn.");
            return builder.ToString();
        }

        /// <summary>Den byte-sekvens der bruges som input til nøgleafledning.</summary>
        internal static byte[] ToSecretBytes(string normalizedCode) => Encoding.UTF8.GetBytes("redmoon-transfer-v1:" + normalizedCode);
    }
}
