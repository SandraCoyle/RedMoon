using RedMoon.App.Theme;

namespace RedMoon.App.Controls;

/// <summary>
/// "Månen" der symboliserer hvor brugeren er i sin cyklus.
/// - Ringen af prikker er cyklussens dage (røde = menstruationsdage); den aktuelle dag er fremhævet.
/// - Månens fase følger cyklussen: fuld, rød måne på første menstruationsdag, aftagende mod
///   midten af cyklussen og tiltagende igen frem mod næste menstruation.
/// Hvis DayOfCycle er 0, vises en neutral fuldmåne uden markering (ingen data endnu).
/// </summary>
public sealed class MoonView : GraphicsView
{
    public static readonly BindableProperty CycleLengthProperty =
        BindableProperty.Create(nameof(CycleLength), typeof(int), typeof(MoonView), 28, propertyChanged: Redraw);

    public static readonly BindableProperty PeriodLengthProperty =
        BindableProperty.Create(nameof(PeriodLength), typeof(int), typeof(MoonView), 5, propertyChanged: Redraw);

    public static readonly BindableProperty DayOfCycleProperty =
        BindableProperty.Create(nameof(DayOfCycle), typeof(int), typeof(MoonView), 0, propertyChanged: Redraw);

    public static readonly BindableProperty ShowRingProperty =
        BindableProperty.Create(nameof(ShowRing), typeof(bool), typeof(MoonView), true, propertyChanged: Redraw);

    private readonly MoonDrawable _drawable;

    public MoonView()
    {
        _drawable = new MoonDrawable(this);
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;

        Loaded += (_, _) => { if (Application.Current != null) Application.Current.RequestedThemeChanged += OnThemeChanged; };
        Unloaded += (_, _) => { if (Application.Current != null) Application.Current.RequestedThemeChanged -= OnThemeChanged; };
    }

    /// <summary>Cyklussens længde i dage (antal prikker i ringen).</summary>
    public int CycleLength { get => (int)GetValue(CycleLengthProperty); set => SetValue(CycleLengthProperty, value); }

    /// <summary>Menstruationens længde i dage (antal røde prikker).</summary>
    public int PeriodLength { get => (int)GetValue(PeriodLengthProperty); set => SetValue(PeriodLengthProperty, value); }

    /// <summary>Dag i cyklussen (1 = første menstruationsdag). 0 = ukendt.</summary>
    public int DayOfCycle { get => (int)GetValue(DayOfCycleProperty); set => SetValue(DayOfCycleProperty, value); }

    /// <summary>Vis ringen af dage omkring månen.</summary>
    public bool ShowRing { get => (bool)GetValue(ShowRingProperty); set => SetValue(ShowRingProperty, value); }

    private static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((MoonView)bindable).Invalidate();

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

    private sealed class MoonDrawable : IDrawable
    {
        // Faste "kratere" (relativ position og størrelse) – giver månen karakter som i skitserne.
        private static readonly (float X, float Y, float R)[] Craters =
        {
            (-0.35f, -0.30f, 0.16f), (0.25f, -0.45f, 0.10f), (0.05f, 0.05f, 0.20f),
            (-0.45f, 0.30f, 0.12f), (0.40f, 0.30f, 0.14f), (-0.05f, 0.55f, 0.09f),
        };

        private readonly MoonView _owner;

        public MoonDrawable(MoonView owner) => _owner = owner;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var size = Math.Min(rect.Width, rect.Height);
            if (size <= 0) return;

            var center = rect.Center;
            var cycle = Math.Clamp(_owner.CycleLength, 15, 60);
            var periodLength = Math.Clamp(_owner.PeriodLength, 1, cycle - 1);
            var day = _owner.DayOfCycle;
            var known = day > 0;

            var ringRadius = size / 2 - size * 0.04f;
            var moonRadius = _owner.ShowRing ? ringRadius * 0.74f : size / 2 * 0.92f;

            if (_owner.ShowRing) DrawRing(canvas, center, ringRadius, size, cycle, periodLength, day);

            var inPeriod = known && day <= periodLength;
            var litColor = inPeriod ? Palette.Pick(Palette.PeriodLight, Palette.PeriodDark) : Palette.Pick(Palette.MoonLitLight, Palette.MoonLitDark);

            // Glød bag månen.
            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 0), size * 0.08f, litColor.WithAlpha(0.6f));
            canvas.FillColor = litColor;
            canvas.FillCircle(center, moonRadius);
            canvas.RestoreState();

            // Kratere.
            canvas.FillColor = Palette.Pick(Palette.CraterLight, Palette.CraterDark).WithAlpha(inPeriod ? 0.35f : 0.55f);
            foreach (var (x, y, r) in Craters)
            {
                canvas.FillCircle(center.X + x * moonRadius, center.Y + y * moonRadius, r * moonRadius);
            }

            // Skygge (månefase). Fase 0 = nymåne, 0.5 = fuldmåne. Dag 1 = fuldmåne.
            if (known)
            {
                var phase = (0.5 + (day - 1) / (double)cycle) % 1.0;
                var shadow = BuildShadowPath(center, moonRadius, phase);
                if (shadow != null)
                {
                    canvas.FillColor = Palette.Pick(Palette.MoonShadowLight, Palette.MoonShadowDark).WithAlpha(0.88f);
                    canvas.FillPath(shadow);
                }
            }

            // Kant, så månen også står skarpt på mørk baggrund.
            canvas.StrokeColor = Palette.Pick(Palette.BorderLight, Palette.BorderDark);
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(center, moonRadius);
        }

        private static void DrawRing(ICanvas canvas, PointF center, float radius, float size, int cycle, int periodLength, int day)
        {
            var dotRadius = Math.Max(2.5f, size * 0.013f);
            for (var i = 0; i < cycle; i++)
            {
                var angle = -Math.PI / 2 + i * 2 * Math.PI / cycle; // Start øverst, med uret.
                var p = new PointF(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
                var isPeriod = i < periodLength;
                var isToday = day == i + 1 || (day > cycle && i == cycle - 1);

                if (isToday)
                {
                    canvas.FillColor = Palette.Pick(Palette.AccentLight, Palette.AccentDark);
                    canvas.FillCircle(p, dotRadius * 2.4f);
                    canvas.StrokeColor = Palette.Pick(Palette.SurfaceLight, Palette.TextPrimaryDark);
                    canvas.StrokeSize = 2;
                    canvas.DrawCircle(p, dotRadius * 2.4f);
                }
                else
                {
                    canvas.FillColor = isPeriod ? Palette.Pick(Palette.PeriodLight, Palette.PeriodDark) : Palette.Pick(Palette.RingDotLight, Palette.RingDotDark);
                    canvas.FillCircle(p, isPeriod ? dotRadius * 1.3f : dotRadius);
                }
            }
        }

        /// <summary>
        /// Bygger skyggens form. Terminatoren (grænsen mellem lys og skygge) er en halv-ellipse
        /// med x-radius r·cos(2π·fase). Tiltagende (fase &lt; 0.5): lyset er til højre; aftagende: til venstre.
        /// </summary>
        private static PathF? BuildShadowPath(PointF c, float r, double phase)
        {
            const int steps = 48;
            var cos = (float)Math.Cos(2 * Math.PI * phase);
            if (Math.Abs(phase - 0.5) < 0.01) return null; // Fuldmåne: ingen skygge.

            var waxing = phase < 0.5;
            var path = new PathF();

            // Venstre kant oppefra og ned.
            for (var i = 0; i <= steps; i++)
            {
                var y = -r + 2 * r * i / steps;
                var w = (float)Math.Sqrt(Math.Max(0, r * r - y * y));
                var x = waxing ? -w : -w * cos;
                if (i == 0) path.MoveTo(c.X + x, c.Y + y); else path.LineTo(c.X + x, c.Y + y);
            }
            // Højre kant nedefra og op.
            for (var i = steps; i >= 0; i--)
            {
                var y = -r + 2 * r * i / steps;
                var w = (float)Math.Sqrt(Math.Max(0, r * r - y * y));
                var x = waxing ? w * cos : w;
                path.LineTo(c.X + x, c.Y + y);
            }
            path.Close();
            return path;
        }
    }
}
