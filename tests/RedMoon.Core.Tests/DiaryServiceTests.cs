using RedMoon.Core.Common;
using RedMoon.Core.Models;

namespace RedMoon.Core.Tests;

public class DiaryServiceTests
{
    private static async Task<TestPhone> LoggedInPhoneAsync()
    {
        var phone = new TestPhone();
        await phone.CreateDefaultUserAsync();
        return phone;
    }

    [Fact]
    public async Task SaveEntry_ThenGetEntry_ReturnsSameData()
    {
        var phone = await LoggedInPhoneAsync();
        var date = new LocalDate(2026, 10, 1);

        await phone.Diary.SaveEntryAsync(new DailyEntry(date, Mood.Powerful, MenstruationStatus.FirstDay, FlowIntensity.Heavy));
        var loaded = phone.Diary.GetEntry(date);

        Assert.Equal(Mood.Powerful, loaded.Mood);
        Assert.Equal(MenstruationStatus.FirstDay, loaded.MenstruationStatus);
        Assert.Equal(FlowIntensity.Heavy, loaded.FlowIntensity);
    }

    [Fact]
    public async Task SavedEntries_SurviveRestart()
    {
        var phone = await LoggedInPhoneAsync();
        var date = new LocalDate(2026, 9, 20);
        await phone.Diary.SetMoodAsync(date, Mood.Sad);
        await phone.Diary.SetMenstruationAsync(date, MenstruationStatus.Ongoing, FlowIntensity.Light);

        var restarted = phone.Restart();
        Assert.True(await restarted.Accounts.TryRestoreSessionAsync());
        var loaded = restarted.Diary.GetEntry(date);

        Assert.Equal(Mood.Sad, loaded.Mood);
        Assert.Equal(MenstruationStatus.Ongoing, loaded.MenstruationStatus);
        Assert.Equal(FlowIntensity.Light, loaded.FlowIntensity);
    }

    [Fact]
    public async Task GetEntry_ForEmptyDay_ReturnsEmptyEntry()
    {
        var phone = await LoggedInPhoneAsync();

        var entry = phone.Diary.GetEntry(new LocalDate(2026, 1, 1));

        Assert.True(entry.IsEmpty);
    }

    [Fact]
    public async Task ChangeEntry_UpdatesOnlyTheChangedField()
    {
        var phone = await LoggedInPhoneAsync();
        var date = phone.Clock.Today;
        await phone.Diary.SetMoodAsync(date, Mood.Happy);
        await phone.Diary.SetMenstruationAsync(date, MenstruationStatus.FirstDay, FlowIntensity.Medium);

        await phone.Diary.SetMoodAsync(date, Mood.Introverted);

        var entry = phone.Diary.GetEntry(date);
        Assert.Equal(Mood.Introverted, entry.Mood);
        Assert.Equal(MenstruationStatus.FirstDay, entry.MenstruationStatus);
        Assert.Equal(FlowIntensity.Medium, entry.FlowIntensity);
    }

    [Fact]
    public async Task RemovingMenstruation_AlsoClearsIntensity()
    {
        var phone = await LoggedInPhoneAsync();
        var date = phone.Clock.Today;
        await phone.Diary.SetMenstruationAsync(date, MenstruationStatus.Ongoing, FlowIntensity.Heavy);

        await phone.Diary.SetMenstruationAsync(date, MenstruationStatus.None, FlowIntensity.Heavy);

        Assert.True(phone.Diary.GetEntry(date).IsEmpty);
        Assert.Empty(phone.Diary.Periods);
    }

    [Fact]
    public async Task DeleteEntry_RemovesDayAndPeriods()
    {
        var phone = await LoggedInPhoneAsync();
        var date = new LocalDate(2026, 10, 2);
        await phone.Diary.SetMenstruationAsync(date, MenstruationStatus.FirstDay, FlowIntensity.Light);
        Assert.Single(phone.Diary.Periods);

        Assert.True(await phone.Diary.DeleteEntryAsync(date));

        Assert.True(phone.Diary.GetEntry(date).IsEmpty);
        Assert.Empty(phone.Diary.Periods);
        Assert.False(await phone.Diary.DeleteEntryAsync(date));
    }

    [Fact]
    public async Task SaveEntry_InTheFuture_IsRejected()
    {
        var phone = await LoggedInPhoneAsync();

        await Assert.ThrowsAsync<ValidationException>(() => phone.Diary.SetMoodAsync(phone.Clock.Today.AddDays(1), Mood.Happy));
    }

    [Fact]
    public async Task SaveEntry_WithUndefinedEnum_IsRejected()
    {
        var phone = await LoggedInPhoneAsync();

        await Assert.ThrowsAsync<ValidationException>(() => phone.Diary.SetMoodAsync(phone.Clock.Today, (Mood)99));
    }

    [Fact]
    public async Task SaveEntry_WhenDiskFails_RollsBackInMemory()
    {
        var phone = await LoggedInPhoneAsync();
        var date = phone.Clock.Today;
        await phone.Diary.SetMoodAsync(date, Mood.Happy);
        phone.Files.FailWrites = true;

        await Assert.ThrowsAsync<IOException>(() => phone.Diary.SetMoodAsync(date, Mood.Sad));

        Assert.Equal(Mood.Happy, phone.Diary.GetEntry(date).Mood);
    }

    [Fact]
    public async Task Diary_RequiresLogin()
    {
        var phone = new TestPhone();

        await Assert.ThrowsAsync<NotLoggedInException>(() => phone.Diary.SetMoodAsync(phone.Clock.Today, Mood.Happy));
    }

    [Fact]
    public async Task Periods_AreStoredWithStartEndAndLength()
    {
        var phone = await LoggedInPhoneAsync();
        await phone.Diary.SetMenstruationAsync(new LocalDate(2026, 9, 1), MenstruationStatus.FirstDay, FlowIntensity.Heavy);
        await phone.Diary.SetMenstruationAsync(new LocalDate(2026, 9, 2), MenstruationStatus.Ongoing, FlowIntensity.Medium);
        await phone.Diary.SetMenstruationAsync(new LocalDate(2026, 9, 4), MenstruationStatus.LastDay, FlowIntensity.Light);

        var restarted = phone.Restart();
        await restarted.Accounts.TryRestoreSessionAsync();
        var period = Assert.Single(restarted.Diary.Periods);

        Assert.Equal(new LocalDate(2026, 9, 1), period.Start);
        Assert.Equal(new LocalDate(2026, 9, 4), period.End);
        Assert.Equal(4, period.LengthDays);
        Assert.True(period.EndConfirmed);
    }
}
