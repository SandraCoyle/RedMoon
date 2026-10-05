using RedMoon.Core.Common;
using RedMoon.Core.Models;

namespace RedMoon.Core.Tests;

public class TransferServiceTests
{
    [Fact]
    public async Task ExportImport_MovesAccountAndEntriesToNewPhone()
    {
        var oldPhone = new TestPhone();
        await oldPhone.CreateDefaultUserAsync();
        await oldPhone.Diary.SetMenstruationAsync(new LocalDate(2026, 9, 10), MenstruationStatus.FirstDay, FlowIntensity.Heavy);
        await oldPhone.Diary.SetMoodAsync(new LocalDate(2026, 9, 10), Mood.Powerful);

        var package = await oldPhone.Transfer.ExportAsync();

        var newPhone = new TestPhone();
        await newPhone.Transfer.ImportAsync(package.Content, package.Code);

        Assert.True(newPhone.Accounts.HasAccount);
        Assert.False(newPhone.Accounts.IsLoggedIn);
        Assert.True((await newPhone.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).IsSuccess);
        var entry = newPhone.Diary.GetEntry(new LocalDate(2026, 9, 10));
        Assert.Equal(Mood.Powerful, entry.Mood);
        Assert.Equal(FlowIntensity.Heavy, entry.FlowIntensity);
        Assert.Single(newPhone.Diary.Periods);
    }

    [Fact]
    public async Task Import_WithWrongCode_Fails()
    {
        var oldPhone = new TestPhone();
        await oldPhone.CreateDefaultUserAsync();
        var package = await oldPhone.Transfer.ExportAsync();

        var newPhone = new TestPhone();
        var wrongCode = package.Code[0] == 'A' ? "B" + package.Code[1..] : "A" + package.Code[1..];

        var ex = await Assert.ThrowsAsync<ValidationException>(() => newPhone.Transfer.ImportAsync(package.Content, wrongCode));
        Assert.Contains("Forkert kode", ex.Message);
        Assert.False(newPhone.Accounts.HasAccount);
    }

    [Fact]
    public async Task Import_IsRefusedWhenAccountExists()
    {
        var oldPhone = new TestPhone();
        await oldPhone.CreateDefaultUserAsync();
        var package = await oldPhone.Transfer.ExportAsync();

        var newPhone = new TestPhone();
        await newPhone.Accounts.CreateAccountAsync("Anden", TestPhone.Pattern(2, 5, 8, 9), 2011, 2);

        await Assert.ThrowsAsync<ValidationException>(() => newPhone.Transfer.ImportAsync(package.Content, package.Code));
    }

    [Fact]
    public async Task Import_RejectsGarbageFile()
    {
        var newPhone = new TestPhone();

        await Assert.ThrowsAsync<ValidationException>(() => newPhone.Transfer.ImportAsync(new byte[200], "AAAA-AAAA-AAAA-AAAA"));
    }

    [Fact]
    public async Task Export_DoesNotContainSessionState()
    {
        var oldPhone = new TestPhone();
        await oldPhone.CreateDefaultUserAsync();
        var package = await oldPhone.Transfer.ExportAsync();

        var newPhone = new TestPhone();
        await newPhone.Transfer.ImportAsync(package.Content, package.Code);

        // Den gamle telefons session-token må ikke kunne bruges på den nye.
        newPhone.Keys.Values[Storage.SecureKeyNames.SessionToken] = oldPhone.Keys.Values[Storage.SecureKeyNames.SessionToken];
        Assert.False(await newPhone.Accounts.TryRestoreSessionAsync());
    }
}
