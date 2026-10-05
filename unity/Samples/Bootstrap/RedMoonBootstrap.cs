using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.Core.Storage;
using UnityEngine;

namespace RedMoon.UnitySample
{
    /// <summary>
    /// Eksempel: kobler RedMoon.Core op i Unity. Læg komponenten på et GameObject i en scene.
    /// Unity-brugerfladen (UI Toolkit/uGUI) bygges oven på de services, der eksponeres her.
    /// </summary>
    public sealed class RedMoonBootstrap : MonoBehaviour
    {
        public AccountService Accounts { get; private set; } = null!;
        public DiaryService Diary { get; private set; } = null!;
        public TransferService Transfer { get; private set; } = null!;

        private void Awake()
        {
            var clock = new SystemClock();
            var options = new SecurityOptions();
            var files = new DirectoryFileStore(Application.persistentDataPath);

            // VIGTIGT: Unity har ingen indbygget Keychain/Keystore. Til produktion skal ISecureKeyStore
            // implementeres med et native plugin (iOS Keychain / Android Keystore).
            // DevelopmentOnlyKeyStore gemmer nøglen i en almindelig fil og er KUN til test i editoren.
            ISecureKeyStore keys = new DevelopmentOnlyKeyStore(Application.persistentDataPath);

            var repository = new VaultRepository(files, keys);
            var session = new SessionState();
            Accounts = new AccountService(repository, keys, session, new PasswordHasher(options), options, clock);
            Diary = new DiaryService(repository, session, clock);
            Transfer = new TransferService(repository, session, options, clock);
        }
    }
}
