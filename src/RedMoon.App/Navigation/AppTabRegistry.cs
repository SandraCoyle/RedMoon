using RedMoon.App.Views;

namespace RedMoon.App.Navigation;

/// <summary>
/// Beskriver én knap i bundmenuen.
/// </summary>
/// <param name="Title">Tekst under ikonet.</param>
/// <param name="Icon">Billedfil i Resources/Images (SVG konverteres til PNG ved build).</param>
/// <param name="Route">Unikt navn til navigation, fx "calendar".</param>
/// <param name="PageType">Siden der vises. Skal være registreret i MauiProgram.</param>
public sealed record AppTab(string Title, string Icon, string Route, Type PageType);

/// <summary>
/// ÉT sted der definerer bundmenuen.
///
/// Sådan tilføjer du en ny underside i menuen:
///   1. Lav siden (fx Views/MinNyeSide.xaml + ViewModel).
///   2. Registrér den i MauiProgram.RegisterPages: services.AddTransient&lt;MinNyeSide&gt;();
///   3. Tilføj en linje her: new AppTab("Navn", "tab_ikon.png", "route", typeof(MinNyeSide)).
/// Sider der ikke skal i menuen (fx "Skift mønster") åbnes med INavigationService.PushAsync&lt;T&gt;().
/// </summary>
public static class AppTabRegistry
{
    public static IReadOnlyList<AppTab> Tabs { get; } = new[]
    {
        new AppTab("Hjem", "tab_home.png", Routes.Home, typeof(HomePage)),
        new AppTab("Kalender", "tab_calendar.png", Routes.Calendar, typeof(CalendarPage)),
        new AppTab("Profil", "tab_profile.png", Routes.Profile, typeof(ProfilePage)),
    };
}

/// <summary>Navne på menu-ruter.</summary>
public static class Routes
{
    public const string Home = "home";
    public const string Calendar = "calendar";
    public const string Profile = "profile";
}
