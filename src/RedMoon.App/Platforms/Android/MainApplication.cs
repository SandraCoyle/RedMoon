using Android.App;
using Android.Runtime;

namespace RedMoon.App;

/// <summary>Android-applikationsklasse (standard MAUI).</summary>
[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
