using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.Core.Storage;
using RedMoon.UnityApp.Platform;

namespace RedMoon.UnityApp.Bootstrap
{
    /// <summary>
    /// Sammenkobling af RedMoon.Core til Unity (svarer til MauiProgram i MAUI-appen).
    /// Al forretningslogik – login, kryptering, kalender og forudsigelse – er den SAMME kode som i mobilappen.
    /// </summary>
    internal sealed class AppServices
    {
        public AppServices()
        {
            DataDirectory = PlatformStorage.DataDirectory();
            KeyStore = KeyStoreFactory.Create(DataDirectory);

            Clock = new SystemClock();
            Options = new SecurityOptions();
            var files = new DirectoryFileStore(DataDirectory, PlatformStorage.AfterWrite);
            var repository = new VaultRepository(files, KeyStore.Store);
            var session = new SessionState();

            Accounts = new AccountService(repository, KeyStore.Store, session, new PasswordHasher(Options), Options, Clock);
            Diary = new DiaryService(repository, session, Clock);
            Transfer = new TransferService(repository, session, Options, Clock);
        }

        /// <summary>Mappen med den krypterede datafil.</summary>
        public string DataDirectory { get; }

        /// <summary>Det valgte nøglelager (Keychain/Keystore, eller fil i udviklingstilstand).</summary>
        public KeyStoreSelection KeyStore { get; }

        public IClock Clock { get; }
        public SecurityOptions Options { get; }
        public AccountService Accounts { get; }
        public DiaryService Diary { get; }
        public TransferService Transfer { get; }
    }
}
