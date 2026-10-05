namespace RedMoon.App.Infrastructure;

/// <summary>
/// Simple app-hændelser som skærme kan lytte på (fx når appen vender tilbage fra baggrunden,
/// så "i dag" opdateres hvis datoen er skiftet).
/// </summary>
public static class AppEvents
{
    public static event EventHandler? Resumed;

    internal static void RaiseResumed() => Resumed?.Invoke(null, EventArgs.Empty);
}
