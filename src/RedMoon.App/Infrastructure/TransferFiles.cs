namespace RedMoon.App.Infrastructure;

/// <summary>
/// Midlertidige overførselsfiler. De lægges i appens cache-mappe (kræves af systemets del-funktion)
/// og slettes igen når eksport-siden lukkes og ved hver app-start.
/// </summary>
public static class TransferFiles
{
    private static string Directory => Path.Combine(FileSystem.CacheDirectory, "transfer");

    /// <summary>Skriver filen og returnerer den fulde sti.</summary>
    public static async Task<string> WriteAsync(string fileName, byte[] content)
    {
        CleanUp();
        System.IO.Directory.CreateDirectory(Directory);
        var path = Path.Combine(Directory, Path.GetFileName(fileName));
        await File.WriteAllBytesAsync(path, content);
        return path;
    }

    /// <summary>Sletter alle midlertidige overførselsfiler.</summary>
    public static void CleanUp()
    {
        try
        {
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // Filen kan være i brug af del-funktionen; den slettes ved næste app-start.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
