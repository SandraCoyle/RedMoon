using RedMoon.App.Infrastructure;
using RedMoon.App.Views;

namespace RedMoon.App;

/// <summary>
/// Appens indgang. Starter med en kort opstartsside, der afgør om brugeren
/// stadig er logget ind (husket session) eller skal se login-skærmen.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_services.GetRequiredService<StartupPage>()) { Title = "Rød Måne" };
    }

    protected override void OnResume()
    {
        base.OnResume();
        AppEvents.RaiseResumed();
    }
}
