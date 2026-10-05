using Foundation;

namespace RedMoon.App;

/// <summary>iOS app-delegate (standard MAUI).</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
