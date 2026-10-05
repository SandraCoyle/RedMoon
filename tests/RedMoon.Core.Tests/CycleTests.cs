using RedMoon.Core.Models;
using RedMoon.Core.Services;

namespace RedMoon.Core.Tests;

public class CycleTests
{
    private static DailyEntry Day(int year, int month, int day, MenstruationStatus status) =>
        new(new LocalDate(year, month, day), Mood.None, status, FlowIntensity.Medium);

    [Fact]
    public void PeriodBuilder_FirstDayStartsNewPeriod()
    {
        var periods = PeriodBuilder.Build(new[]
        {
            Day(2026, 8, 1, MenstruationStatus.FirstDay),
            Day(2026, 8, 3, MenstruationStatus.LastDay),
            Day(2026, 8, 29, MenstruationStatus.FirstDay),
            Day(2026, 8, 30, MenstruationStatus.Ongoing),
        });

        Assert.Equal(2, periods.Count);
        Assert.Equal(3, periods[0].LengthDays);
        Assert.True(periods[0].EndConfirmed);
        Assert.Equal(new LocalDate(2026, 8, 29), periods[1].Start);
        Assert.Equal(new LocalDate(2026, 8, 30), periods[1].End);
        Assert.False(periods[1].EndConfirmed);
    }

    [Fact]
    public void PeriodBuilder_ForgottenEnd_StartsNewPeriodAfterMaxLength()
    {
        var periods = PeriodBuilder.Build(new[]
        {
            Day(2026, 8, 1, MenstruationStatus.Ongoing),
            Day(2026, 8, 20, MenstruationStatus.Ongoing),
        });

        Assert.Equal(2, periods.Count);
    }

    [Fact]
    public void Prediction_WithoutData_UsesDefaults28And5()
    {
        var prediction = CyclePredictor.Predict(Array.Empty<MenstruationPeriod>(), new LocalDate(2026, 10, 5));

        Assert.Equal(28, prediction.CycleLengthDays);
        Assert.Equal(5, prediction.PeriodLengthDays);
        Assert.False(prediction.IsPersonal);
        Assert.False(prediction.HasCycleData);
        Assert.Null(prediction.NextPeriodStart);
    }

    [Fact]
    public void Prediction_WithOnePeriod_UsesDefaultCycleFromThatStart()
    {
        var periods = new[] { new MenstruationPeriod(new LocalDate(2026, 9, 20), new LocalDate(2026, 9, 24), true) };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 10, 5));

        Assert.Equal(new LocalDate(2026, 10, 18), prediction.NextPeriodStart);
        Assert.Equal(16, prediction.DayOfCycle);
        Assert.Equal(13, prediction.DaysUntilNextPeriod);
        Assert.Equal(CycleDayKind.Period, prediction.GetDayKind(new LocalDate(2026, 9, 22)));
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 10, 18)));
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 10, 22)));
        Assert.Equal(CycleDayKind.PredictedMargin, prediction.GetDayKind(new LocalDate(2026, 10, 16)));
        Assert.Equal(CycleDayKind.None, prediction.GetDayKind(new LocalDate(2026, 10, 10)));
        // Næste cyklus derefter: 18/10 + 28 = 15/11
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 11, 15)));
    }

    [Fact]
    public void Prediction_UsesPersonalAverages()
    {
        var periods = new[]
        {
            new MenstruationPeriod(new LocalDate(2026, 6, 1), new LocalDate(2026, 6, 4), true),   // 4 dage
            new MenstruationPeriod(new LocalDate(2026, 7, 1), new LocalDate(2026, 7, 4), true),   // cyklus 30
            new MenstruationPeriod(new LocalDate(2026, 7, 31), new LocalDate(2026, 8, 3), true),  // cyklus 30
            new MenstruationPeriod(new LocalDate(2026, 8, 30), new LocalDate(2026, 9, 2), true),  // cyklus 30
        };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 9, 10));

        Assert.Equal(30, prediction.CycleLengthDays);
        Assert.Equal(4, prediction.PeriodLengthDays);
        Assert.Equal(3, prediction.CyclesUsed);
        Assert.True(prediction.IsPersonal);
        Assert.Equal(new LocalDate(2026, 9, 29), prediction.NextPeriodStart);
        Assert.Equal(1, prediction.VariationDays);
    }

    [Fact]
    public void Prediction_IgnoresImplausibleCycles()
    {
        var periods = new[]
        {
            new MenstruationPeriod(new LocalDate(2026, 1, 1), new LocalDate(2026, 1, 5), true),
            new MenstruationPeriod(new LocalDate(2026, 5, 1), new LocalDate(2026, 5, 5), true), // 120 dage – ignoreres
        };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 5, 10));

        Assert.Equal(28, prediction.CycleLengthDays);
        Assert.Equal(0, prediction.CyclesUsed);
    }

    [Fact]
    public void Prediction_WhenLate_PredictsFromToday()
    {
        var periods = new[] { new MenstruationPeriod(new LocalDate(2026, 9, 1), new LocalDate(2026, 9, 5), true) };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 10, 3));

        Assert.Equal(4, prediction.DaysLate);
        Assert.Equal(new LocalDate(2026, 10, 3), prediction.NextPeriodStart);
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 10, 3)));
    }

    [Fact]
    public void Prediction_WithVeryOldData_IsStale()
    {
        var periods = new[] { new MenstruationPeriod(new LocalDate(2025, 1, 1), new LocalDate(2025, 1, 5), true) };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 10, 5));

        Assert.True(prediction.IsStale);
        Assert.False(prediction.HasCycleData);
        Assert.Equal(CycleDayKind.None, prediction.GetDayKind(new LocalDate(2026, 10, 20)));
    }

    [Fact]
    public void Prediction_OngoingPeriodWithoutEnd_ShowsRemainingDaysAsPredicted()
    {
        var periods = new[] { new MenstruationPeriod(new LocalDate(2026, 10, 4), new LocalDate(2026, 10, 4), false) };

        var prediction = CyclePredictor.Predict(periods, new LocalDate(2026, 10, 5));

        Assert.True(prediction.IsMenstruatingToday == false);
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 10, 5)));
        Assert.Equal(CycleDayKind.PredictedPeriod, prediction.GetDayKind(new LocalDate(2026, 10, 8)));
        Assert.Equal(CycleDayKind.None, prediction.GetDayKind(new LocalDate(2026, 10, 9)));
    }

    [Theory]
    [InlineData(1, CycleSeason.Winter)]
    [InlineData(5, CycleSeason.Winter)]
    [InlineData(8, CycleSeason.Spring)]
    [InlineData(14, CycleSeason.Summer)]
    [InlineData(20, CycleSeason.Autumn)]
    [InlineData(28, CycleSeason.Autumn)]
    public void Season_FollowsCycleDay(int day, CycleSeason expected)
    {
        var prediction = CyclePredictor.Predict(Array.Empty<MenstruationPeriod>(), new LocalDate(2026, 10, 5));

        Assert.Equal(expected, prediction.GetSeason(day));
    }
}
