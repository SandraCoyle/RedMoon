using Microsoft.Maui.LifecycleEvents;
#if ANDROID
using Android.Views;
#endif
#if IOS
using UIKit;
#endif

namespace RedMoon.App.Infrastructure;

/// <summary>
/// Skjuler appens indhold når den ikke er i forgrunden, så menstruationsdata ikke kan ses
/// i "seneste apps"/app-switcheren.
/// - Android: FLAG_SECURE (skjuler preview og blokerer skærmbilleder af appen).
/// - iOS: lægger en ensfarvet flade over vinduet når appen mister fokus.
/// </summary>
public static class PrivacyScreen
{
    public static void Configure(ILifecycleBuilder events)
    {
#if ANDROID
        events.AddAndroid(android => android.OnCreate((activity, _) =>
            activity.Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure)));
#elif IOS
        events.AddiOS(ios => ios
            .SceneOnResignActivation(Cover)
            .SceneOnActivated(_ => Uncover()));
#endif
    }

#if IOS
    private static UIView? _cover;

    private static void Cover(UIScene scene)
    {
        if (_cover != null) return;
        var window = (scene as UIWindowScene)?.Windows.FirstOrDefault(w => w.IsKeyWindow)
                     ?? (scene as UIWindowScene)?.Windows.FirstOrDefault();
        if (window == null) return;

        _cover = new UIView(window.Bounds)
        {
            BackgroundColor = UIColor.FromRGB(0x14, 0x0B, 0x22),
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
        };
        window.AddSubview(_cover);
    }

    private static void Uncover()
    {
        _cover?.RemoveFromSuperview();
        _cover = null;
    }
#endif
}
