using RedMoon.App.Navigation;
using RedMoon.App.Theme;

namespace RedMoon.App;

/// <summary>
/// Hovedappen med bundmenu. Menuen bygges ud fra AppTabRegistry,
/// så nye menupunkter kun kræver én linje dér.
/// </summary>
public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetNavBarIsVisible(this, false);

        this.SetAppThemeColor(Shell.BackgroundColorProperty, Palette.BackgroundLight, Palette.BackgroundDark);
        this.SetAppThemeColor(Shell.TabBarBackgroundColorProperty, Palette.SurfaceLight, Palette.SurfaceDark);
        this.SetAppThemeColor(Shell.TabBarForegroundColorProperty, Palette.AccentLight, Palette.AccentDark);
        this.SetAppThemeColor(Shell.TabBarTitleColorProperty, Palette.AccentLight, Palette.AccentDark);
        this.SetAppThemeColor(Shell.TabBarUnselectedColorProperty, Palette.TextSecondaryLight, Palette.TextSecondaryDark);
        this.SetAppThemeColor(Shell.ForegroundColorProperty, Palette.TextPrimaryLight, Palette.TextPrimaryDark);
        this.SetAppThemeColor(Shell.TitleColorProperty, Palette.TextPrimaryLight, Palette.TextPrimaryDark);

        var tabBar = new TabBar();
        foreach (var tab in AppTabRegistry.Tabs)
        {
            var pageType = tab.PageType;
            tabBar.Items.Add(new ShellContent
            {
                Title = tab.Title,
                Icon = tab.Icon,
                Route = tab.Route,
                ContentTemplate = new DataTemplate(() => services.GetRequiredService(pageType)),
            });
        }
        Items.Add(tabBar);
    }
}
