using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RedMoon.Core.Storage;
using UnityEngine;

namespace RedMoon.UnityApp.Platform
{
    /// <summary>
    /// Resultatet af valget af nøglelager: selve lageret, og om nøglen er beskyttet af telefonens sikre hardware.
    /// </summary>
    internal sealed class KeyStoreSelection
    {
        public KeyStoreSelection(ISecureKeyStore store, bool isHardwareBacked, string description)
        {
            Store = store;
            IsHardwareBacked = isHardwareBacked;
            Description = description;
        }

        public ISecureKeyStore Store { get; }

        /// <summary>Sand for iOS Keychain og Android Keystore. Falsk i Unity Editor og på computer.</summary>
        public bool IsHardwareBacked { get; }

        /// <summary>Kort dansk beskrivelse til profilsiden.</summary>
        public string Description { get; }
    }

    /// <summary>
    /// Vælger det sikreste nøglelager platformen har:
    /// - iOS: Keychain (via Plugins/iOS/RedMoonKeychain.mm)
    /// - Android: Android Keystore (via Plugins/Android/RedMoonAndroid.java)
    /// - Unity Editor/computer: almindelig fil (KUN til udvikling – se FileKeyStore)
    /// Hvis Keystore/Keychain ikke virker på en telefon, bruges fil-lageret, og profilsiden viser en advarsel.
    /// </summary>
    internal static class KeyStoreFactory
    {
        public static KeyStoreSelection Create(string dataDirectory)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return new KeyStoreSelection(new IosKeychainKeyStore(), true, "Nøglen ligger i iPhonens Keychain.");

                case RuntimePlatform.Android:
                    var android = new AndroidKeystoreKeyStore(Path.Combine(dataDirectory, "keys"));
                    if (android.SelfTest(out var error))
                    {
                        return new KeyStoreSelection(android, true, "Nøglen er beskyttet af Android Keystore.");
                    }
                    Debug.LogWarning("Android Keystore virker ikke på denne telefon: " + error);
                    return new KeyStoreSelection(new FileKeyStore(Path.Combine(dataDirectory, "dev-keys")), false,
                        "Advarsel: Android Keystore virker ikke på denne telefon. Nøglen ligger i en almindelig fil.");

                default:
                    return new KeyStoreSelection(new FileKeyStore(Path.Combine(dataDirectory, "dev-keys")), false,
                        "Udviklingstilstand: nøglen ligger i en almindelig fil (kun sikkert nok til test).");
            }
        }
    }

    /// <summary>
    /// KUN TIL UDVIKLING (Unity Editor/computer): gemmer nøgler i almindelige filer i appens datamappe.
    /// Nøglen og den krypterede datafil ligger altså samme sted, så krypteringen beskytter ikke mod
    /// nogen der kan læse mappen. På iPhone/Android bruges Keychain/Keystore i stedet.
    /// </summary>
    internal sealed class FileKeyStore : ISecureKeyStore
    {
        private readonly string _directory;

        public FileKeyStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public Task<string?> GetAsync(string key)
        {
            var path = PathFor(key);
            return Task.FromResult(File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null);
        }

        public Task SetAsync(string key, string value)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(PathFor(key), value, Encoding.UTF8);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        private string PathFor(string key) => Path.Combine(_directory, SafeFileName(key) + ".txt");

        internal static string SafeFileName(string key)
        {
            var builder = new StringBuilder(key.Length);
            foreach (var ch in key)
            {
                builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Android: hver værdi krypteres med AES-256-GCM med en nøgle, der ligger i Android Keystore
    /// (nøglen kan ikke trækkes ud af telefonen). Den krypterede værdi gemmes i appens interne mappe.
    /// Selve kryptografien sker i Java (Plugins/Android/RedMoonAndroid.java); her kaldes den via JNI på hovedtråden.
    /// </summary>
    internal sealed class AndroidKeystoreKeyStore : ISecureKeyStore
    {
        private const string JavaClass = "dk.roedmaane.unity.RedMoonAndroid";
        private readonly string _directory;

        public AndroidKeystoreKeyStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        /// <summary>Krypterer og dekrypterer en testværdi. Falsk hvis Keystore ikke virker.</summary>
        public bool SelfTest(out string error)
        {
            try
            {
                var sample = new byte[] { 1, 2, 3, 4 };
                var roundTrip = Decrypt(Encrypt(sample));
                error = string.Empty;
                return roundTrip.Length == sample.Length && roundTrip[3] == 4;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public Task<string?> GetAsync(string key) => MainThread.RunAsync<string?>(() =>
        {
            var path = PathFor(key);
            if (!File.Exists(path)) return null;
            return Encoding.UTF8.GetString(Decrypt(File.ReadAllBytes(path)));
        });

        public Task SetAsync(string key, string value) => MainThread.RunAsync(() =>
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllBytes(PathFor(key), Encrypt(Encoding.UTF8.GetBytes(value)));
        });

        public Task RemoveAsync(string key) => MainThread.RunAsync(() =>
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
        });

        private string PathFor(string key) => Path.Combine(_directory, FileKeyStore.SafeFileName(key) + ".bin");

        private static byte[] Encrypt(byte[] plain) => CallBytes("encrypt", plain);

        private static byte[] Decrypt(byte[] blob) => CallBytes("decrypt", blob);

        private static byte[] CallBytes(string method, byte[] input)
        {
            using (var plugin = new AndroidJavaClass(JavaClass))
            {
                // Unity overfører Java byte[] som sbyte[].
                var result = plugin.CallStatic<sbyte[]>(method, ToSigned(input));
                return ToUnsigned(result);
            }
        }

        private static sbyte[] ToSigned(byte[] bytes)
        {
            var result = new sbyte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        private static byte[] ToUnsigned(sbyte[] bytes)
        {
            var result = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }
    }

    /// <summary>
    /// iOS: værdier gemmes i Keychain med "AfterFirstUnlockThisDeviceOnly"
    /// (følger aldrig med i backup eller til en anden telefon). Native kode: Plugins/iOS/RedMoonKeychain.mm.
    /// </summary>
    internal sealed class IosKeychainKeyStore : ISecureKeyStore
    {
        [DllImport("__Internal")] private static extern int RedMoonKeychain_Set(string key, string value);
        [DllImport("__Internal")] private static extern IntPtr RedMoonKeychain_Get(string key);
        [DllImport("__Internal")] private static extern int RedMoonKeychain_Remove(string key);
        [DllImport("__Internal")] private static extern void RedMoonKeychain_Free(IntPtr pointer);

        public Task<string?> GetAsync(string key)
        {
            var pointer = RedMoonKeychain_Get(key);
            if (pointer == IntPtr.Zero) return Task.FromResult<string?>(null);
            try
            {
                return Task.FromResult<string?>(Marshal.PtrToStringAnsi(pointer));
            }
            finally
            {
                RedMoonKeychain_Free(pointer);
            }
        }

        public Task SetAsync(string key, string value)
        {
            var status = RedMoonKeychain_Set(key, value);
            if (status != 0) throw new InvalidOperationException("Keychain-fejl " + status);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            RedMoonKeychain_Remove(key);
            return Task.CompletedTask;
        }
    }
}
