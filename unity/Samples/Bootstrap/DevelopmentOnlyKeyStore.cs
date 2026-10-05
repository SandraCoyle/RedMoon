using System;
using System.IO;
using System.Threading.Tasks;
using RedMoon.Core.Storage;

namespace RedMoon.UnitySample
{
    /// <summary>
    /// KUN TIL UDVIKLING: gemmer "sikre" værdier i en almindelig fil ved siden af datafilen.
    /// Det betyder at nøglen og den krypterede datafil ligger samme sted – krypteringen beskytter så
    /// reelt ikke mod nogen der kan læse appens filer. Udskift med Keychain/Keystore før udgivelse.
    /// </summary>
    public sealed class DevelopmentOnlyKeyStore : ISecureKeyStore
    {
        private readonly string _directory;

        public DevelopmentOnlyKeyStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public Task<string?> GetAsync(string key)
        {
            var path = PathFor(key);
            return Task.FromResult(File.Exists(path) ? File.ReadAllText(path) : null);
        }

        public Task SetAsync(string key, string value)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(PathFor(key), value);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        private string PathFor(string key) => Path.Combine(_directory, "devkey-" + key.Replace('.', '_') + ".txt");
    }
}
