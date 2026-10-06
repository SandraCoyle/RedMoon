using System;
using RedMoon.UnityApp.UI;
using UnityEngine;

namespace RedMoon.UnityApp.Graphics
{
    /// <summary>
    /// Tegner "månen" der viser hvor brugeren er i sin cyklus (samme design som MAUI-appens MoonView):
    /// - Ringen af prikker er cyklussens dage; røde prikker er menstruationsdage; i dag er fremhævet.
    /// - Månens fase følger cyklussen: fuld, rød måne på første menstruationsdag, ny måne midt i cyklussen.
    /// </summary>
    internal static class MoonRenderer
    {
        // Faste "kratere" (relativ position og størrelse) – som i skitserne.
        private static readonly (float X, float Y, float R)[] Craters =
        {
            (-0.35f, -0.30f, 0.16f), (0.25f, -0.45f, 0.10f), (0.05f, 0.05f, 0.20f),
            (-0.45f, 0.30f, 0.12f), (0.40f, 0.30f, 0.14f), (-0.05f, 0.55f, 0.09f),
        };

        /// <summary>
        /// Tegner måne + ring. <paramref name="dayOfCycle"/> 0 = ukendt (neutral fuldmåne uden markering).
        /// </summary>
        public static Texture2D RenderCycle(int size, int cycleLength, int periodLength, int dayOfCycle)
        {
            var canvas = new PixelCanvas(size);
            var cycle = Mathf.Clamp(cycleLength, 15, 60);
            var period = Mathf.Clamp(periodLength, 1, cycle - 1);
            var known = dayOfCycle > 0;
            var center = size / 2f;
            var ringRadius = size / 2f - size * 0.04f;
            var moonRadius = ringRadius * 0.74f;

            DrawRing(canvas, center, ringRadius, size, cycle, period, dayOfCycle);

            var inPeriod = known && dayOfCycle <= period;
            var lit = inPeriod ? Palette.Period : Palette.MoonLit;
            canvas.Glow(center, center, moonRadius, moonRadius * 1.18f, new Color(lit.r, lit.g, lit.b, 0.45f));
            DrawMoon(canvas, center, center, moonRadius, known ? PhaseForDay(dayOfCycle, cycle) : 0.5, lit, true);
            canvas.StrokeCircle(center, center, moonRadius, Math.Max(1.5f, size * 0.003f), Palette.Border);

            return canvas.ToTexture("RedMoon.Moon");
        }

        /// <summary>Månefase 0-1 (0 = ny måne, 0.5 = fuldmåne). Dag 1 = fuldmåne.</summary>
        public static double PhaseForDay(int dayOfCycle, int cycleLength) => (0.5 + (dayOfCycle - 1) / (double)cycleLength) % 1.0;

        /// <summary>Tegner en måne med fase. Skyggen er en halv-ellipse med x-radius r·cos(2π·fase).</summary>
        public static void DrawMoon(PixelCanvas canvas, float cx, float cy, float r, double phase, Color lit, bool craters)
        {
            canvas.FillCircle(cx, cy, r, lit);

            if (craters)
            {
                var craterColor = new Color(Palette.Crater.r, Palette.Crater.g, Palette.Crater.b, 0.5f);
                foreach (var (x, y, cr) in Craters)
                {
                    canvas.FillCircle(cx + x * r, cy + y * r, cr * r, craterColor);
                }
            }

            if (Math.Abs(phase - 0.5) < 0.01) return; // Fuldmåne: ingen skygge.

            var cos = (float)Math.Cos(2 * Math.PI * phase);
            var waxing = phase < 0.5;
            var shadow = new Color(Palette.MoonShadow.r, Palette.MoonShadow.g, Palette.MoonShadow.b, 0.9f);
            canvas.Shade(cx - r - 1, cy - r - 1, cx + r + 1, cy + r + 1, shadow, (x, y) =>
            {
                var disc = r - PixelCanvas.Distance(x, y, cx, cy) + 0.5f;
                if (disc <= 0f) return 0f;
                var nx = (x - cx) / r;
                var ny = (y - cy) / r;
                if (Math.Abs(ny) >= 1f) return 0f;
                var w = (float)Math.Sqrt(1f - ny * ny);
                var left = waxing ? -w : -w * cos;
                var right = waxing ? w * cos : w;
                var coverage = Mathf.Clamp01((nx - left) * r + 0.5f) * Mathf.Clamp01((right - nx) * r + 0.5f);
                return coverage * Mathf.Clamp01(disc);
            });
        }

        private static void DrawRing(PixelCanvas canvas, float center, float radius, int size, int cycle, int period, int day)
        {
            var dotRadius = Math.Max(2.5f, size * 0.013f);
            for (var i = 0; i < cycle; i++)
            {
                var angle = -Math.PI / 2 + i * 2 * Math.PI / cycle; // Start øverst, med uret.
                var x = center + radius * (float)Math.Cos(angle);
                var y = center + radius * (float)Math.Sin(angle);
                var isToday = day == i + 1 || (day > cycle && i == cycle - 1);

                if (isToday)
                {
                    canvas.FillCircle(x, y, dotRadius * 2.4f, Palette.Accent);
                    canvas.StrokeCircle(x, y, dotRadius * 2.4f, Math.Max(1.5f, size * 0.004f), Palette.TextPrimary);
                }
                else if (i < period)
                {
                    canvas.FillCircle(x, y, dotRadius * 1.3f, Palette.Period);
                }
                else
                {
                    canvas.FillCircle(x, y, dotRadius, Palette.RingDot);
                }
            }
        }
    }
}
