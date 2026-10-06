using System;
using System.Collections.Generic;
using RedMoon.UnityApp.Graphics;
using RedMoon.UnityApp.Screens;

namespace RedMoon.UnityApp.Navigation
{
    /// <summary>Én knap i bundmenuen.</summary>
    internal sealed class TabDefinition
    {
        public TabDefinition(string id, string title, TabIcon icon, Func<ScreenContext, ScreenBase> create)
        {
            Id = id;
            Title = title;
            Icon = icon;
            Create = create;
        }

        /// <summary>Unikt navn, fx "calendar" (bruges af Navigator.GoToTab).</summary>
        public string Id { get; }
        public string Title { get; }
        public TabIcon Icon { get; }

        /// <summary>Opretter skærmen (første gang fanen åbnes).</summary>
        public Func<ScreenContext, ScreenBase> Create { get; }
    }

    /// <summary>
    /// ÉT sted der definerer bundmenuen.
    ///
    /// Sådan tilføjer du en ny underside i menuen:
    ///   1. Lav en klasse der arver fra ScreenBase (fx Screens/GameScreen.cs).
    ///   2. Tilføj en linje her: new TabDefinition("spil", "Spil", TabIcon.Home, c => new GameScreen(c)).
    ///      (Tilføj evt. et nyt ikon i Graphics/Icons.cs.)
    /// Sider der ikke skal i menuen åbnes med Context.Navigator.Push(new MinSkærm(Context)).
    /// </summary>
    internal static class TabRegistry
    {
        public const string Home = "home";
        public const string Calendar = "calendar";
        public const string Profile = "profile";

        public static IReadOnlyList<TabDefinition> Tabs { get; } = new[]
        {
            new TabDefinition(Home, "Hjem", TabIcon.Home, c => new HomeScreen(c)),
            new TabDefinition(Calendar, "Kalender", TabIcon.Calendar, c => new CalendarScreen(c)),
            new TabDefinition(Profile, "Profil", TabIcon.Profile, c => new ProfileScreen(c)),
        };
    }
}
