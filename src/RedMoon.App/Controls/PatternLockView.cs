using System.Windows.Input;
using RedMoon.App.Theme;

namespace RedMoon.App.Controls;

/// <summary>
/// Mønsterlås: 3 x 3 punkter (1-9) som brugeren forbinder ved at trække fingeren.
/// Punkter fingeren passerer over tages med (også ved hurtige swipes), så samme mønster altid giver samme talrække.
/// Når fingeren løftes, udføres <see cref="PatternCompletedCommand"/> med listen af punkter (IReadOnlyList&lt;int&gt;).
/// Mønsteret ryddes automatisk kort efter, så det ikke bliver stående synligt på skærmen.
/// </summary>
public sealed class PatternLockView : GraphicsView
{
    public static readonly BindableProperty PatternCompletedCommandProperty =
        BindableProperty.Create(nameof(PatternCompletedCommand), typeof(ICommand), typeof(PatternLockView));

    public static readonly BindableProperty IsErrorProperty =
        BindableProperty.Create(nameof(IsError), typeof(bool), typeof(PatternLockView), false,
            propertyChanged: (b, _, _) => ((PatternLockView)b).Invalidate());

    private readonly PatternLockDrawable _drawable = new();
    private CancellationTokenSource? _clearTimer;

    public PatternLockView()
    {
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        HeightRequest = 300;
        WidthRequest = 300;
        SemanticProperties.SetDescription(this, "Mønsterlås med 9 punkter. Træk fingeren gennem mindst 4 punkter.");

        StartInteraction += OnStart;
        DragInteraction += OnDrag;
        EndInteraction += OnEnd;
        CancelInteraction += (_, _) => Clear();

        Loaded += (_, _) => { if (Application.Current != null) Application.Current.RequestedThemeChanged += OnThemeChanged; };
        Unloaded += (_, _) => { if (Application.Current != null) Application.Current.RequestedThemeChanged -= OnThemeChanged; };
    }

    /// <summary>Udføres med IReadOnlyList&lt;int&gt; (punkter 1-9) når brugeren løfter fingeren.</summary>
    public ICommand? PatternCompletedCommand
    {
        get => (ICommand?)GetValue(PatternCompletedCommandProperty);
        set => SetValue(PatternCompletedCommandProperty, value);
    }

    /// <summary>Viser stregen i fejlfarve (fx efter forkert mønster).</summary>
    public bool IsError
    {
        get => (bool)GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    private void OnStart(object? sender, TouchEventArgs e)
    {
        _clearTimer?.Cancel();
        _drawable.Points.Clear();
        _drawable.FingerPosition = null;
        _drawable.IsError = false;
        Track(e);
    }

    private void OnDrag(object? sender, TouchEventArgs e) => Track(e);

    private void OnEnd(object? sender, TouchEventArgs e)
    {
        _drawable.FingerPosition = null;
        var points = _drawable.Points.ToList();
        Invalidate();

        if (points.Count > 0 && PatternCompletedCommand?.CanExecute(points) == true)
        {
            PatternCompletedCommand.Execute(points);
        }
        ScheduleClear();
    }

    private void Track(TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        var touch = e.Touches[0];

        // Hele stykket fingeren har bevæget sig siden sidst testes – ellers kan et hurtigt swipe
        // springe et punkt over, og samme mønster ville give forskellige talrækker (= fejlede logins).
        var from = _drawable.FingerPosition ?? touch;
        _drawable.FingerPosition = touch;
        _drawable.AddPointsAlong(from, touch, (float)Width, (float)Height);
        Invalidate();
    }

    private async void ScheduleClear()
    {
        _clearTimer?.Cancel();
        var cts = _clearTimer = new CancellationTokenSource();
        try
        {
            await Task.Delay(700, cts.Token);
            Clear();
        }
        catch (TaskCanceledException)
        {
            // En ny berøring startede – lad mønsteret stå.
        }
    }

    private void Clear()
    {
        _drawable.Points.Clear();
        _drawable.FingerPosition = null;
        Invalidate();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

    /// <summary>Tegner punkter, forbindelseslinjer og tal.</summary>
    private sealed class PatternLockDrawable : IDrawable
    {
        public List<int> Points { get; } = new();
        public PointF? FingerPosition { get; set; }
        public bool IsError { get; set; }

        private static (float Size, float OffsetX, float OffsetY) Layout(float width, float height)
        {
            var size = Math.Min(width, height);
            return (size, (width - size) / 2, (height - size) / 2);
        }

        /// <summary>Centrum af punkt 1-9.</summary>
        private static PointF Center(int point, float width, float height)
        {
            var (size, ox, oy) = Layout(width, height);
            var cell = size / 3;
            var index = point - 1;
            return new PointF(ox + cell * (index % 3) + cell / 2, oy + cell * (index / 3) + cell / 2);
        }

        /// <summary>
        /// Tilføjer de punkter fingeren har passeret på vej fra <paramref name="from"/> til <paramref name="to"/>,
        /// i den rækkefølge de blev passeret.
        /// </summary>
        public void AddPointsAlong(PointF from, PointF to, float width, float height)
        {
            var (size, _, _) = Layout(width, height);
            var hitRadius = size / 3 * 0.32f;

            var hits = new List<(int Point, float Position)>();
            for (var point = 1; point <= 9; point++)
            {
                if (Points.Contains(point)) continue;
                var (distance, position) = DistanceToSegment(Center(point, width, height), from, to);
                if (distance <= hitRadius) hits.Add((point, position));
            }

            foreach (var (point, _) in hits.OrderBy(h => h.Position))
            {
                AddPoint(point);
            }
        }

        /// <summary>
        /// Tilføjer et punkt. Som på Android tages et uvalgt punkt, der ligger midt imellem
        /// det forrige og det nye (fx 2 mellem 1 og 3), automatisk med først.
        /// </summary>
        private void AddPoint(int point)
        {
            if (Points.Contains(point)) return;
            if (Points.Count > 0)
            {
                var middle = MiddlePoint(Points[^1], point);
                if (middle.HasValue && !Points.Contains(middle.Value)) Points.Add(middle.Value);
            }
            Points.Add(point);
        }

        /// <summary>Punktet præcis midt mellem a og b i gitteret, eller null hvis der ikke er et.</summary>
        private static int? MiddlePoint(int a, int b)
        {
            int rowA = (a - 1) / 3, colA = (a - 1) % 3, rowB = (b - 1) / 3, colB = (b - 1) % 3;
            if ((rowA + rowB) % 2 != 0 || (colA + colB) % 2 != 0) return null;
            return (rowA + rowB) / 2 * 3 + (colA + colB) / 2 + 1;
        }

        /// <summary>Afstand fra et punkt til linjestykket a-b, og hvor langt (0-1) ad stykket det nærmeste sted ligger.</summary>
        private static (float Distance, float Position) DistanceToSegment(PointF p, PointF a, PointF b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 0.0001f) return (p.Distance(a), 0);

            var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0f, 1f);
            var closest = new PointF(a.X + t * dx, a.Y + t * dy);
            return (p.Distance(closest), t);
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            var (size, _, _) = Layout(rect.Width, rect.Height);
            var dotRadius = size / 3 * 0.17f;
            var lineColor = IsError ? Palette.Pick(Palette.ErrorLight, Palette.ErrorDark) : Palette.Pick(Palette.AccentLight, Palette.AccentDark);

            // Linjer mellem valgte punkter (+ til fingeren).
            canvas.StrokeColor = lineColor.WithAlpha(0.8f);
            canvas.StrokeSize = Math.Max(4, size * 0.02f);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            for (var i = 1; i < Points.Count; i++)
            {
                canvas.DrawLine(Center(Points[i - 1], rect.Width, rect.Height), Center(Points[i], rect.Width, rect.Height));
            }
            if (FingerPosition.HasValue && Points.Count > 0)
            {
                canvas.DrawLine(Center(Points[^1], rect.Width, rect.Height), FingerPosition.Value);
            }

            // Punkterne.
            for (var point = 1; point <= 9; point++)
            {
                var c = Center(point, rect.Width, rect.Height);
                var selected = Points.Contains(point);

                canvas.FillColor = selected ? lineColor : Palette.Pick(Palette.SurfaceAltLight, Palette.SurfaceAltDark);
                canvas.FillCircle(c, selected ? dotRadius * 1.15f : dotRadius);
                canvas.StrokeColor = selected ? lineColor : Palette.Pick(Palette.BorderLight, Palette.BorderDark);
                canvas.StrokeSize = 2;
                canvas.DrawCircle(c, selected ? dotRadius * 1.15f : dotRadius);

                canvas.FontColor = selected
                    ? Palette.Pick(Palette.OnAccentLight, Palette.OnAccentDark)
                    : Palette.Pick(Palette.TextSecondaryLight, Palette.TextSecondaryDark);
                canvas.FontSize = Math.Max(12, dotRadius * 0.8f);
                canvas.DrawString(point.ToString(), c.X - dotRadius, c.Y - dotRadius, dotRadius * 2, dotRadius * 2,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}
