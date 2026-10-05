using System.Threading.Tasks;

namespace RedMoon.Core.Storage
{
    /// <summary>
    /// Telefonens sikre nøglelager: iOS Keychain / Android Keystore.
    /// Bruges KUN til tilfældige nøgler og session-token – aldrig til brugerdata.
    /// </summary>
    public interface ISecureKeyStore
    {
        /// <summary>Henter en værdi, eller null hvis den ikke findes.</summary>
        Task<string?> GetAsync(string key);

        Task SetAsync(string key, string value);

        /// <summary>Fjerner en værdi. Gør intet hvis den ikke findes.</summary>
        Task RemoveAsync(string key);
    }

    /// <summary>Navnene på de værdier appen gemmer i det sikre nøglelager.</summary>
    public static class SecureKeyNames
    {
        /// <summary>256-bit hovednøgle der krypterer datafilen (Base64).</summary>
        public const string VaultKey = "redmoon.vault.key.v1";

        /// <summary>Tilfældigt session-token (Base64), der holder brugeren logget ind.</summary>
        public const string SessionToken = "redmoon.session.token.v1";
    }
}
