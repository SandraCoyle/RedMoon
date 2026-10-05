using RedMoon.Core.Common;
using RedMoon.Core.Models;
using RedMoon.Core.Security;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Tests;

public class SecurityTests
{
    [Theory]
    [InlineData(new[] { 1, 2, 3 })]          // for kort
    [InlineData(new[] { 1, 2, 2, 3 })]       // gentaget punkt
    [InlineData(new[] { 0, 1, 2, 3 })]       // uden for 1-9
    [InlineData(new[] { 1, 2, 3, 10 })]
    public void Pattern_RejectsInvalid(int[] points)
    {
        Assert.Throws<ValidationException>(() => PatternPassword.Create(points));
    }

    [Fact]
    public void Pattern_AcceptsFourToNinePoints()
    {
        Assert.NotNull(PatternPassword.Create(new[] { 1, 2, 3, 4 }));
        Assert.NotNull(PatternPassword.Create(new[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 }));
    }

    [Fact]
    public void PasswordHasher_VerifiesCorrectAndRejectsWrong_WithUniqueSalts()
    {
        var hasher = new PasswordHasher(new SecurityOptions { PasswordIterations = 1_000 });
        var pattern = PatternPassword.Create(new[] { 3, 5, 7, 8 });

        var first = hasher.Hash(pattern);
        var second = hasher.Hash(pattern);

        Assert.True(hasher.Verify(pattern, first));
        Assert.False(hasher.Verify(PatternPassword.Create(new[] { 3, 5, 7, 9 }), first));
        Assert.NotEqual(first.Salt, second.Salt);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void PasswordHasher_MatchesKnownPbkdf2Vector()
    {
        // RFC 7914 afsnit 11 testvektor for PBKDF2-HMAC-SHA256 (P="passwd", S="salt", c=1, dkLen=64).
        var output = Pbkdf2.DeriveSha256("passwd"u8.ToArray(), "salt"u8.ToArray(), 1, 64);
        Assert.Equal("55ac046e56e3089fec1691c22544b605f94185216dde0465e68b9d57c20dacbc49ca9cccf179b645991664b39d77ef317c71b845b1e30bd509112041d3a19783",
            Convert.ToHexString(output).ToLowerInvariant());
    }

    [Fact]
    public void Cipher_DetectsTampering()
    {
        var cipher = AuthenticatedCipher.FromMasterKey(AuthenticatedCipher.GenerateKey());
        var encrypted = cipher.Encrypt("hemmelig"u8.ToArray(), "H"u8.ToArray());

        Assert.Equal("hemmelig"u8.ToArray(), cipher.Decrypt(encrypted, "H"u8.ToArray()));

        encrypted[encrypted.Length / 2] ^= 0x01;
        Assert.Throws<VaultCorruptedException>(() => cipher.Decrypt(encrypted, "H"u8.ToArray()));
    }

    [Fact]
    public void Cipher_WrongKeyFails()
    {
        var encrypted = AuthenticatedCipher.FromMasterKey(AuthenticatedCipher.GenerateKey()).Encrypt(new byte[] { 1, 2, 3 }, Array.Empty<byte>());

        Assert.Throws<VaultCorruptedException>(() =>
            AuthenticatedCipher.FromMasterKey(AuthenticatedCipher.GenerateKey()).Decrypt(encrypted, Array.Empty<byte>()));
    }

    [Fact]
    public async Task Vault_CannotBeReadWithoutKeyFromSecureStore()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        phone.Keys.Values.Remove(SecureKeyNames.VaultKey);

        await Assert.ThrowsAsync<VaultCorruptedException>(() => phone.Restart().Repository.LoadAsync());
    }

    [Fact]
    public async Task Vault_TamperedFileIsRejected()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        var file = phone.Files.Files[VaultRepository.VaultFileName];
        file[^5] ^= 0xFF;

        await Assert.ThrowsAsync<VaultCorruptedException>(() => phone.Restart().Repository.LoadAsync());
    }

    [Fact]
    public void Serializer_RoundTripsAllFields()
    {
        var hasher = new PasswordHasher(new SecurityOptions { PasswordIterations = 1_000 });
        var vault = new UserVault(new UserAccount("Æble Ø", hasher.Hash(PatternPassword.Create(new[] { 1, 2, 3, 4 })), 2010, 11));
        vault.SetEntry(new DailyEntry(new LocalDate(2026, 5, 1), Mood.Outgoing, MenstruationStatus.FirstDay, FlowIntensity.Heavy));
        vault.SetEntry(new DailyEntry(new LocalDate(2026, 5, 2), Mood.None, MenstruationStatus.LastDay, FlowIntensity.Light));
        vault.SetPeriods(Core.Services.PeriodBuilder.Build(vault.Entries));
        vault.Security.FailedLoginAttempts = 2;

        var copy = VaultSerializer.Deserialize(VaultSerializer.Serialize(vault));

        Assert.Equal("Æble Ø", copy.Account.Username);
        Assert.Equal(2010, copy.Account.BirthYear);
        Assert.Equal(11, copy.Account.BirthMonth);
        Assert.Equal(vault.Account.PasswordHash.Hash, copy.Account.PasswordHash.Hash);
        Assert.Equal(2, copy.EntryCount);
        Assert.Equal(Mood.Outgoing, copy.GetEntry(new LocalDate(2026, 5, 1))!.Mood);
        Assert.Equal(2, Assert.Single(copy.Periods).LengthDays);
        Assert.Equal(2, copy.Security.FailedLoginAttempts);
    }

    [Fact]
    public void TransferCode_HasExpectedFormat_AndNormalizes()
    {
        var code = TransferCode.Generate();

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code);
        Assert.Equal(code.Replace("-", ""), TransferCode.Normalize(" " + code.ToLowerInvariant().Replace("-", " ") + " "));
        Assert.Throws<ValidationException>(() => TransferCode.Normalize("ABC"));
    }
}
