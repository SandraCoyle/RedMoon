namespace RedMoon.App.Infrastructure;

/// <summary>
/// Platform-specifik beskyttelse af datafilen efter hver skrivning.
/// iOS: udelukker filen fra iCloud/iTunes-backup og slår NSFileProtectionComplete til
///      (filen er krypteret af iOS og ulæselig mens telefonen er låst).
/// Android: intet nødvendigt her – backup er slået fra i AndroidManifest.xml,
///          og appens private mappe er krypteret af systemet (file-based encryption).
/// </summary>
public static partial class PlatformFileProtection
{
    /// <summary>Kaldes med den fulde sti efter datafilen er skrevet.</summary>
    public static void Apply(string path) => ApplyPlatform(path);

    static partial void ApplyPlatform(string path);
}
