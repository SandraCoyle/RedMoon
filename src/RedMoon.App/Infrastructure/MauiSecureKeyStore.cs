using RedMoon.Core.Storage;

namespace RedMoon.App.Infrastructure;

/// <summary>
/// Kobler Core's ISecureKeyStore til MAUI SecureStorage:
/// - iOS: Keychain (kun denne enhed, se konstruktøren)
/// - Android: Android Keystore (værdier krypteres med en hardware-beskyttet nøgle)
/// Kun tilfældige nøgler og session-token gemmes her – aldrig brugerdata.
/// </summary>
public sealed class MauiSecureKeyStore : ISecureKeyStore
{
    public MauiSecureKeyStore()
    {
#if IOS
        // Keychain-værdier må kun kunne læses på DENNE telefon, efter den er låst op første gang efter genstart.
        // "ThisDeviceOnly" betyder at nøglen aldrig følger med i iCloud-/computer-backup eller til en ny telefon.
        SecureStorage.DefaultAccessible = Security.SecAccessible.AfterFirstUnlockThisDeviceOnly;
#endif
    }

    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);

    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
