using System;
using System.IO;
using System.Threading.Tasks;

namespace RedMoon.Core.Storage
{
    /// <summary>
    /// Abstraktion over appens private filområde. Gør det muligt at teste uden rigtigt filsystem
    /// og at genbruge koden i Unity (Application.persistentDataPath).
    /// </summary>
    public interface IFileStore
    {
        bool Exists(string fileName);
        Task<byte[]?> ReadAsync(string fileName);

        /// <summary>Skriver atomisk: enten er hele den nye fil skrevet, eller den gamle er urørt.</summary>
        Task WriteAtomicAsync(string fileName, byte[] content);

        void Delete(string fileName);
    }

    /// <summary>
    /// Filbaseret lager i en bestemt mappe (på telefonen: appens private sandbox-mappe).
    /// </summary>
    public sealed class DirectoryFileStore : IFileStore
    {
        private readonly string _directory;
        private readonly Action<string>? _afterWrite;

        /// <param name="directory">Mappen filerne gemmes i. Oprettes hvis den ikke findes.</param>
        /// <param name="afterWrite">
        /// Valgfri platform-hook der kaldes med den fulde sti efter hver skrivning
        /// (på iOS bruges den til at udelukke filen fra backup og slå filbeskyttelse til).
        /// </param>
        public DirectoryFileStore(string directory, Action<string>? afterWrite = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Mappe mangler.", nameof(directory));
            _directory = directory;
            _afterWrite = afterWrite;
        }

        public bool Exists(string fileName) => File.Exists(PathFor(fileName));

        public async Task<byte[]?> ReadAsync(string fileName)
        {
            var path = PathFor(fileName);
            if (!File.Exists(path)) return null;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory).ConfigureAwait(false);
                return memory.ToArray();
            }
        }

        public async Task WriteAtomicAsync(string fileName, byte[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            Directory.CreateDirectory(_directory);

            var path = PathFor(fileName);
            var tempPath = path + ".tmp";

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await stream.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }

            // Erstat den gamle fil i ét trin, så en crash midt i skrivningen ikke ødelægger data.
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }

            _afterWrite?.Invoke(path);
        }

        public void Delete(string fileName)
        {
            var path = PathFor(fileName);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }

        private string PathFor(string fileName)
        {
            // Kun simple filnavne er tilladt – forhindrer at nogen skriver uden for mappen.
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
            {
                throw new ArgumentException("Ugyldigt filnavn.", nameof(fileName));
            }
            return Path.Combine(_directory, fileName);
        }
    }
}
