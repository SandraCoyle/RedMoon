using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.App.ViewModels;
using RedMoon.App.Views;
using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.Core.Storage;

namespace RedMoon.App;

/// <summary>
/// Opsætning af appen: skrifttyper, platform-hooks og dependency injection.
/// Bemærk: Der registreres INGEN analytics, crash-rapportering, netværksklienter eller reklamer.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureLifecycleEvents(PrivacyScreen.Configure);

#if DEBUG
        // Kun i Debug og kun lokalt (Visual Studio output). Aldrig brugerdata i logbeskeder.
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

        RegisterCore(builder.Services);
        RegisterPages(builder.Services);

        return builder.Build();
    }

    /// <summary>Core-services. Singletons: der er én bruger og én datafil per installation.</summary>
    private static void RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(new SecurityOptions());
        services.AddSingleton<IFileStore>(_ => new DirectoryFileStore(FileSystem.AppDataDirectory, PlatformFileProtection.Apply));
        services.AddSingleton<ISecureKeyStore, MauiSecureKeyStore>();
        services.AddSingleton<VaultRepository>();
        services.AddSingleton<SessionState>();
        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<AccountService>();
        services.AddSingleton<DiaryService>();
        services.AddSingleton<TransferService>();

        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();
    }

    /// <summary>
    /// Sider og ViewModels. Ny side = tilføj de to linjer her
    /// (og evt. en linje i Navigation/AppTabRegistry.cs, hvis den skal i bundmenuen).
    /// </summary>
    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<StartupPage>();

        services.AddTransient<LoginPage>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ImportPage>();
        services.AddTransient<ImportViewModel>();

        services.AddTransient<HomePage>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CalendarPage>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<ProfileViewModel>();

        services.AddTransient<ChangePatternPage>();
        services.AddTransient<ChangePatternViewModel>();
        services.AddTransient<ExportPage>();
        services.AddTransient<ExportViewModel>();
    }
}
