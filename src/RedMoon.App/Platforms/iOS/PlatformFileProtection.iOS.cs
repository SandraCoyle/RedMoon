using Foundation;

namespace RedMoon.App.Infrastructure;

/// <summary>iOS-del af PlatformFileProtection (se den delte fil for forklaring).</summary>
public static partial class PlatformFileProtection
{
    static partial void ApplyPlatform(string path)
    {
        // 1) Ikke med i iCloud-/computer-backup: data skal kun ligge på denne telefon.
        var url = NSUrl.FromFilename(path);
        url.SetResource(NSUrl.IsExcludedFromBackupKey, NSNumber.FromBoolean(true), out _);

        // 2) Højeste filbeskyttelse: iOS krypterer filen med en nøgle der kun er tilgængelig mens telefonen er låst op.
        var attributes = new NSFileAttributes { ProtectionKey = NSFileProtection.Complete };
        NSFileManager.DefaultManager.SetAttributes(attributes, path, out _);
    }
}
