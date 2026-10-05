namespace RedMoon.Core.Models
{
    /// <summary>
    /// Brugerens konto. Indeholder KUN de tilladte oplysninger:
    /// brugernavn (valgfrit), adgangskode (som hash), fødselsår og fødselsmåned.
    /// </summary>
    public sealed class UserAccount
    {
        public UserAccount(string username, PasswordHash passwordHash, int birthYear, int birthMonth)
        {
            Username = username ?? string.Empty;
            PasswordHash = passwordHash;
            BirthYear = birthYear;
            BirthMonth = birthMonth;
        }

        /// <summary>Brugernavn. Kan være tomt, da brugernavn er valgfrit.</summary>
        public string Username { get; internal set; }

        /// <summary>Hash af mønster-adgangskoden.</summary>
        public PasswordHash PasswordHash { get; internal set; }

        /// <summary>Fødselsår, fx 2011.</summary>
        public int BirthYear { get; }

        /// <summary>Fødselsmåned 1-12.</summary>
        public int BirthMonth { get; }
    }
}
