using RedMoon.Core.Security;

namespace RedMoon.Core.Tests;

public class PatternGridTests
{
    private const float Side = 300f; // 3 x 3 celler á 100: centre i 50, 150, 250

    [Theory]
    [InlineData(1, 3, 2)]
    [InlineData(1, 9, 5)]
    [InlineData(3, 7, 5)]
    [InlineData(2, 8, 5)]
    [InlineData(1, 7, 4)]
    [InlineData(4, 6, 5)]
    public void MiddlePoint_FindsPointBetween(int a, int b, int expected)
    {
        Assert.Equal(expected, PatternGrid.MiddlePoint(a, b));
        Assert.Equal(expected, PatternGrid.MiddlePoint(b, a));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 6)]
    [InlineData(1, 8)]
    [InlineData(5, 9)]
    public void MiddlePoint_NoneForNeighboursAndKnightMoves(int a, int b)
    {
        Assert.Null(PatternGrid.MiddlePoint(a, b));
    }

    [Fact]
    public void FastSwipeAcrossTopRow_TakesAllThreeInOrder()
    {
        // Ét stort ryk fra punkt 1 til punkt 3 – punkt 2 må ikke springes over.
        var selected = new List<int>();
        PatternGrid.AddPointsAlong(selected, 50, 50, 50, 50, Side);
        PatternGrid.AddPointsAlong(selected, 50, 50, 250, 50, Side);

        Assert.Equal(new[] { 1, 2, 3 }, selected);
    }

    [Fact]
    public void SwipeBackwards_KeepsOrderOfPassing()
    {
        var selected = new List<int>();
        PatternGrid.AddPointsAlong(selected, 250, 250, 50, 250, Side);

        Assert.Equal(new[] { 9, 8, 7 }, selected);
    }

    [Fact]
    public void CurvingAroundMiddle_StillIncludesMiddlePoint()
    {
        // Fra 1 til 3 uden om punkt 2 (buen går under punktet): 2 tages alligevel med først.
        var selected = new List<int> { 1 };
        PatternGrid.AddPoint(selected, 3);

        Assert.Equal(new[] { 1, 2, 3 }, selected);
    }

    [Fact]
    public void AlreadySelectedPoints_AreNotAddedTwice()
    {
        var selected = new List<int>();
        PatternGrid.AddPointsAlong(selected, 50, 50, 250, 50, Side);
        PatternGrid.AddPointsAlong(selected, 250, 50, 50, 50, Side);

        Assert.Equal(new[] { 1, 2, 3 }, selected);
    }

    [Fact]
    public void MovementBetweenDots_AddsNothing()
    {
        var selected = new List<int>();
        PatternGrid.AddPointsAlong(selected, 100, 95, 100, 105, Side);

        Assert.Empty(selected);
    }

    [Fact]
    public void Center_IsMiddleOfCell()
    {
        Assert.Equal((50f, 50f), PatternGrid.Center(1, Side));
        Assert.Equal((150f, 150f), PatternGrid.Center(5, Side));
        Assert.Equal((250f, 250f), PatternGrid.Center(9, Side));
    }
}
