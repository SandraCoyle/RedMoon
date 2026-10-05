using RedMoon.Core.Common;
using RedMoon.Core.Models;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Tests;

/// <summary>Filsystem i hukommelsen, så tests ikke rører disken.</summary>
internal sealed class InMemoryFileStore : IFileStore
{
    public Dictionary<string, byte[]> Files { get; } = new();
    public bool FailWrites { get; set; }

    public bool Exists(string fileName) => Files.ContainsKey(fileName);
    public Task<byte[]?> ReadAsync(string fileName) => Task.FromResult(Files.TryGetValue(fileName, out var b) ? (byte[]?)b.ToArray() : null);

    public Task WriteAtomicAsync(string fileName, byte[] content)
    {
        if (FailWrites) throw new IOException("Disken er fuld (simuleret).");
        Files[fileName] = content.ToArray();
        return Task.CompletedTask;
    }

    public void Delete(string fileName) => Files.Remove(fileName);
}

/// <summary>Erstatning for Keychain/Keystore i tests.</summary>
internal sealed class InMemoryKeyStore : ISecureKeyStore
{
    public Dictionary<string, string> Values { get; } = new();
    public Task<string?> GetAsync(string key) => Task.FromResult(Values.TryGetValue(key, out var v) ? v : null);
    public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
    public Task RemoveAsync(string key) { Values.Remove(key); return Task.CompletedTask; }
}

/// <summary>Ur som testen styrer.</summary>
internal sealed class FakeClock : IClock
{
    public LocalDate Today { get; set; } = new LocalDate(2026, 10, 5);
    public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// Samler alle services som én "telefon". Lave iterationstal gør testene hurtige
/// (algoritmen er den samme som i appen).
/// </summary>
internal sealed class TestPhone
{
    public TestPhone(InMemoryFileStore? files = null, InMemoryKeyStore? keys = null, FakeClock? clock = null)
    {
        Files = files ?? new InMemoryFileStore();
        Keys = keys ?? new InMemoryKeyStore();
        Clock = clock ?? new FakeClock();
        Options = new SecurityOptions { PasswordIterations = 1_000, TransferIterations = 1_000 };
        Repository = new VaultRepository(Files, Keys);
        Session = new SessionState();
        Accounts = new AccountService(Repository, Keys, Session, new PasswordHasher(Options), Options, Clock);
        Diary = new DiaryService(Repository, Session, Clock);
        Transfer = new TransferService(Repository, Session, Options, Clock);
    }

    public InMemoryFileStore Files { get; }
    public InMemoryKeyStore Keys { get; }
    public FakeClock Clock { get; }
    public SecurityOptions Options { get; }
    public VaultRepository Repository { get; }
    public SessionState Session { get; }
    public AccountService Accounts { get; }
    public DiaryService Diary { get; }
    public TransferService Transfer { get; }

    /// <summary>"Genstarter appen": nye services, men samme filer og nøglelager.</summary>
    public TestPhone Restart() => new TestPhone(Files, Keys, Clock);

    public static PatternPassword Pattern(params int[] points) => PatternPassword.Create(points);

    public static readonly PatternPassword DefaultPattern = Pattern(1, 5, 9, 6);

    public Task CreateDefaultUserAsync() => Accounts.CreateAccountAsync("Luna", DefaultPattern, 2012, 3);
}
