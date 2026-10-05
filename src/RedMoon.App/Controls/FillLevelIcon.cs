using RedMoon.App.Theme;

namespace RedMoon.App.Controls;

/// <summary>
/// Lille rund "måne" der er fyldt nedefra i 0-3 trin – ikon for blødningsintensitet
/// (inspireret af skitsen "Tryk på månen og vælg": lidt / noget / meget).
/// </summary>
public sealed class FillLevelIcon : GraphicsView
{
    public FillLevelIcon(int level)
    {
        Drawable = new FillLevelDrawable(Math.Clamp(level, 0, 3));
        WidthRequest = 26;
        HeightRequest = 26;
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
    }

    private sealed class FillLevelDrawable : IDrawable
    {
        private readonly int _level;

        public FillLevelDrawable(int level) => _level = level;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var radius = Math.Min(rect.Width, rect.Height) / 2 - 2;
            var center = rect.Center;
            var color = Palette.Pick(Palette.PeriodLight, Palette.PeriodDark);

            if (_level > 0)
            {
                canvas.SaveState();
                var clip = new PathF();
                clip.AppendCircle(center.X, center.Y, radius);
                canvas.ClipPath(clip);
                var fillHeight = 2 * radius * _level / 3f;
                canvas.FillColor = color;
                canvas.FillRectangle(center.X - radius, center.Y + radius - fillHeight, radius * 2, fillHeight);
                canvas.RestoreState();
            }

            canvas.StrokeColor = color;
            canvas.StrokeSize = 2;
            canvas.DrawCircle(center, radius);
        }
    }
}
