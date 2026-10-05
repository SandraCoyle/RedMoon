using Android.App;
using Android.Content.PM;

namespace RedMoon.App;

/// <summary>Androids hovedaktivitet (standard MAUI). FLAG_SECURE sættes i Infrastructure/PrivacyScreen.cs.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
