using RedMoon.App.Views;

namespace RedMoon.App.Navigation;

/// <summary>
/// Navigation mellem appens to "verdener":
/// - Logget ud: login/opret/import i en NavigationPage.
/// - Logget ind: AppShell med bundmenu.
/// Desuden åbning af undersider (push) og skift mellem menupunkter.
/// </summary>
public interface INavigationService
{
    /// <summary>Viser login-flowet (og fjerner alt andet fra skærmen).</summary>
    void ShowLogin();

    /// <summary>Viser hovedappen med bundmenu.</summary>
    void ShowMain();

    /// <summary>Åbner en underside oven på den aktuelle.</summary>
    Task PushAsync<TPage>() where TPage : Page;

    /// <summary>Lukker den øverste underside.</summary>
    Task PopAsync();

    /// <summary>Skifter til et menupunkt, fx Routes.Calendar.</summary>
    Task GoToTabAsync(string route);
}

/// <summary>Standardimplementering. Sider hentes fra DI-containeren, så de får deres afhængigheder.</summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void ShowLogin()
    {
        SetRoot(new NavigationPage(_services.GetRequiredService<LoginPage>()));
    }

    public void ShowMain()
    {
        SetRoot(new AppShell(_services));
    }

    public Task PushAsync<TPage>() where TPage : Page
    {
        var navigation = CurrentNavigation ?? throw new InvalidOperationException("Ingen aktiv side.");
        return navigation.PushAsync(_services.GetRequiredService<TPage>());
    }

    public Task PopAsync() => CurrentNavigation?.PopAsync() ?? Task.CompletedTask;

    public Task GoToTabAsync(string route) => Shell.Current?.GoToAsync("//" + route) ?? Task.CompletedTask;

    private static INavigation? CurrentNavigation
    {
        get
        {
            var root = Application.Current?.Windows.FirstOrDefault()?.Page;
            return root switch
            {
                Shell shell => shell.Navigation,
                null => null,
                _ => root.Navigation,
            };
        }
    }

    private static void SetRoot(Page page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            window.Page = page;
        }
    }
}
