using System.Text;
using RedMoon.Core.Common;
using RedMoon.Core.Services;
using RedMoon.Core.Storage;

namespace RedMoon.Core.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task CreateAccount_StoresUserAndLogsIn()
    {
        var phone = new TestPhone();

        await phone.CreateDefaultUserAsync();

        Assert.True(phone.Accounts.HasAccount);
        Assert.True(phone.Accounts.IsLoggedIn);
        Assert.Equal("Luna", phone.Accounts.CurrentAccount.Username);
        Assert.Equal(2012, phone.Accounts.CurrentAccount.BirthYear);
        Assert.Equal(3, phone.Accounts.CurrentAccount.BirthMonth);
    }

    [Fact]
    public async Task CreateAccount_UsernameIsOptional()
    {
        var phone = new TestPhone();

        await phone.Accounts.CreateAccountAsync("   ", TestPhone.DefaultPattern, 2011, 12);

        Assert.Equal(string.Empty, phone.Accounts.CurrentAccount.Username);
    }

    [Fact]
    public async Task CreateAccount_DoesNotStorePatternOrUsernameInPlainText()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();

        var file = phone.Files.Files[VaultRepository.VaultFileName];
        var asText = Encoding.UTF8.GetString(file);

        Assert.DoesNotContain("Luna", asText);
        Assert.DoesNotContain("1-5-9-6", asText);
        Assert.DoesNotContain("redmoon-pattern", asText);
    }

    [Fact]
    public async Task CreateAccount_FailsWhenAccountAlreadyExists()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();

        await Assert.ThrowsAsync<ValidationException>(() => phone.Accounts.CreateAccountAsync("Anden", TestPhone.DefaultPattern, 2010, 1));
    }

    [Theory]
    [InlineData(2012, 0)]
    [InlineData(2012, 13)]
    [InlineData(1900, 5)]
    [InlineData(2026, 5)]
    public async Task CreateAccount_RejectsInvalidBirth(int year, int month)
    {
        var phone = new TestPhone();

        await Assert.ThrowsAsync<ValidationException>(() => phone.Accounts.CreateAccountAsync("Luna", TestPhone.DefaultPattern, year, month));
        Assert.False(phone.Accounts.HasAccount);
    }

    [Theory]
    [InlineData("Luna<script>")]
    [InlineData("navn@mail.dk")]
    [InlineData("1234567890123456789012345")]
    public async Task CreateAccount_RejectsInvalidUsername(string username)
    {
        var phone = new TestPhone();

        await Assert.ThrowsAsync<ValidationException>(() => phone.Accounts.CreateAccountAsync(username, TestPhone.DefaultPattern, 2012, 3));
    }

    [Fact]
    public async Task Login_SucceedsWithCorrectCredentials_CaseInsensitiveUsername()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        await phone.Accounts.LogoutAsync();

        var result = await phone.Accounts.LoginAsync("luna", TestPhone.Pattern(1, 5, 9, 6));

        Assert.True(result.IsSuccess);
        Assert.True(phone.Accounts.IsLoggedIn);
    }

    [Fact]
    public async Task Login_FailsWithWrongPattern()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        await phone.Accounts.LogoutAsync();

        var result = await phone.Accounts.LoginAsync("Luna", TestPhone.Pattern(1, 5, 9, 8));

        Assert.Equal(LoginOutcome.WrongCredentials, result.Outcome);
        Assert.False(phone.Accounts.IsLoggedIn);
        Assert.Contains("Forkert brugernavn eller mønster", result.Message);
    }

    [Fact]
    public async Task Login_FailsWithWrongUsername_SameMessageAsWrongPattern()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        await phone.Accounts.LogoutAsync();

        var result = await phone.Accounts.LoginAsync("Sol", TestPhone.DefaultPattern);

        Assert.Equal(LoginOutcome.WrongCredentials, result.Outcome);
        Assert.False(phone.Accounts.IsLoggedIn);
    }

    [Fact]
    public async Task Login_WithoutAccount_ReturnsNoAccount()
    {
        var phone = new TestPhone();

        var result = await phone.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern);

        Assert.Equal(LoginOutcome.NoAccount, result.Outcome);
    }

    [Fact]
    public async Task Login_LocksOutAfterTooManyFailures_AndUnlocksAfterWaiting()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        await phone.Accounts.LogoutAsync();
        var wrong = TestPhone.Pattern(2, 4, 6, 8);

        for (var i = 0; i < phone.Options.MaxFailedAttemptsBeforeLockout - 1; i++)
        {
            Assert.Equal(LoginOutcome.WrongCredentials, (await phone.Accounts.LoginAsync("Luna", wrong)).Outcome);
        }
        Assert.Equal(LoginOutcome.LockedOut, (await phone.Accounts.LoginAsync("Luna", wrong)).Outcome);

        // Selv det rigtige mønster afvises under spærringen.
        Assert.Equal(LoginOutcome.LockedOut, (await phone.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).Outcome);

        // Spærringen overlever en genstart af appen.
        var restarted = phone.Restart();
        Assert.Equal(LoginOutcome.LockedOut, (await restarted.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).Outcome);

        phone.Clock.UtcNow = phone.Clock.UtcNow.AddMinutes(1);
        Assert.True((await restarted.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).IsSuccess);
    }

    [Fact]
    public async Task Session_IsRestoredAfterRestart_UntilLogout()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();

        var restarted = phone.Restart();
        Assert.True(await restarted.Accounts.TryRestoreSessionAsync());
        Assert.Equal("Luna", restarted.Accounts.CurrentAccount.Username);

        await restarted.Accounts.LogoutAsync();
        Assert.False(restarted.Accounts.IsLoggedIn);
        Assert.False(await phone.Restart().Accounts.TryRestoreSessionAsync());
    }

    [Fact]
    public async Task Session_IsRejectedWhenTokenDoesNotMatch()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        phone.Keys.Values[SecureKeyNames.SessionToken] = Convert.ToBase64String(new byte[32]);

        Assert.False(await phone.Restart().Accounts.TryRestoreSessionAsync());
    }

    [Fact]
    public async Task ChangePattern_RequiresCurrentPattern_AndNewPatternWorks()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        var newPattern = TestPhone.Pattern(7, 8, 9, 6, 3);

        await Assert.ThrowsAsync<ValidationException>(() => phone.Accounts.ChangePatternAsync(TestPhone.Pattern(1, 2, 3, 4), newPattern));

        await phone.Accounts.ChangePatternAsync(TestPhone.DefaultPattern, newPattern);
        await phone.Accounts.LogoutAsync();

        Assert.False((await phone.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).IsSuccess);
        Assert.True((await phone.Accounts.LoginAsync("Luna", newPattern)).IsSuccess);
    }

    [Fact]
    public async Task ChangeUsername_IsPersisted()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();

        await phone.Accounts.ChangeUsernameAsync("Stjerne");

        var restarted = phone.Restart();
        Assert.True(await restarted.Accounts.TryRestoreSessionAsync());
        Assert.Equal("Stjerne", restarted.Accounts.CurrentAccount.Username);
    }

    [Fact]
    public async Task DeleteAllData_RemovesFileKeysAndSession()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        await phone.Diary.SetMoodAsync(phone.Clock.Today, Models.Mood.Happy);

        await phone.Accounts.DeleteAllDataAsync();

        Assert.False(phone.Accounts.HasAccount);
        Assert.False(phone.Accounts.IsLoggedIn);
        Assert.Empty(phone.Files.Files);
        Assert.Empty(phone.Keys.Values);
        Assert.Equal(LoginOutcome.NoAccount, (await phone.Accounts.LoginAsync("Luna", TestPhone.DefaultPattern)).Outcome);

        // En ny konto kan oprettes bagefter.
        await phone.CreateDefaultUserAsync();
        Assert.Equal(Models.Mood.None, phone.Diary.GetEntry(phone.Clock.Today).Mood);
    }
}
