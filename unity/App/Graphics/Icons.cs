using System;
using System.Collections.Generic;
using RedMoon.Core.Models;
using RedMoon.UnityApp.UI;
using UnityEngine;

namespace RedMoon.UnityApp.Graphics
{
    /// <summary>Ikoner i bundmenuen.</summary>
    internal enum TabIcon
    {
        Home,
        Calendar,
        Profile,
    }

    /// <summary>
    /// Tegnede ikoner (humør-ansigter, blødnings-måner, menu-ikoner). Tegnes én gang og genbruges.
    /// Tegnes i kode, fordi Unitys standardskrifttype ikke kan vise emoji.
    /// </summary>
    internal static class Icons
    {
        private const int Size = 96;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly Color Ink = new Color(0.10f, 0.04f, 0.14f, 1f);

        /// <summary>Humør-ikon: et ansigt i humørets farve (Powerful er et lyn).</summary>
        public static Texture2D Mood(Mood mood) => Cached("mood-" + mood, () =>
        {
            var canvas = new PixelCanvas(Size);
            var s = (float)Size;
            var cx = s / 2f;
            var cy = s / 2f;
            var color = MoodColor(mood);

            if (mood == Core.Models.Mood.Powerful)
            {
                canvas.FillPolygon(Points(s, (0.58f, 0.08f), (0.26f, 0.56f), (0.47f, 0.56f), (0.38f, 0.92f), (0.74f, 0.42f), (0.53f, 0.42f), (0.66f, 0.08f)), color);
                return canvas.ToTexture("RedMoon.Mood.Powerful");
            }

            canvas.FillCircle(cx, cy, s * 0.42f, color);
            var eyeY = cy - s * 0.08f;
            var eyeDx = s * 0.15f;
            var stroke = s * 0.055f;

            if (mood == Core.Models.Mood.Introverted)
            {
                // Lukkede øjne.
                canvas.StrokePolyline(Points(s, (0.30f, 0.42f), (0.40f, 0.42f)), stroke, Ink);
                canvas.StrokePolyline(Points(s, (0.60f, 0.42f), (0.70f, 0.42f)), stroke, Ink);
            }
            else
            {
                canvas.FillCircle(cx - eyeDx, eyeY, s * 0.045f, Ink);
                canvas.FillCircle(cx + eyeDx, eyeY, s * 0.045f, Ink);
            }

            switch (mood)
            {
                case Core.Models.Mood.Outgoing:
                    canvas.FillPolygon(Arc(cx, cy + s * 0.04f, s * 0.2f, 0f, (float)Math.PI, closed: true), Ink);
                    break;
                case Core.Models.Mood.Happy:
                    canvas.StrokePolyline(Arc(cx, cy + s * 0.02f, s * 0.2f, 0.2f * (float)Math.PI, 0.8f * (float)Math.PI, closed: false), stroke, Ink);
                    break;
                case Core.Models.Mood.InBetween:
                    canvas.StrokePolyline(Points(s, (0.35f, 0.66f), (0.65f, 0.66f)), stroke, Ink);
                    break;
                case Core.Models.Mood.Sad:
                    canvas.StrokePolyline(Arc(cx, cy + s * 0.33f, s * 0.18f, 1.2f * (float)Math.PI, 1.8f * (float)Math.PI, closed: false), stroke, Ink);
                    break;
                case Core.Models.Mood.Introverted:
                    canvas.StrokePolyline(Points(s, (0.45f, 0.66f), (0.55f, 0.66f)), stroke, Ink);
                    break;
            }
            return canvas.ToTexture("RedMoon.Mood." + mood);
        });

        /// <summary>Blødningsintensitet: en cirkel der er fyldt 1/3, 2/3 eller helt op.</summary>
        public static Texture2D Flow(FlowIntensity flow) => Cached("flow-" + flow, () =>
        {
            var canvas = new PixelCanvas(Size);
            var s = (float)Size;
            var cx = s / 2f;
            var cy = s / 2f;
            var r = s * 0.38f;
            var fraction = flow == FlowIntensity.Light ? 0.34f : flow == FlowIntensity.Medium ? 0.67f : 1f;
            var level = cy + r - 2f * r * fraction;

            canvas.Shade(cx - r - 1, cy - r - 1, cx + r + 1, cy + r + 1, Palette.Period, (x, y) =>
                Mathf.Clamp01(r - PixelCanvas.Distance(x, y, cx, cy) + 0.5f) * Mathf.Clamp01(y - level + 0.5f));
            canvas.StrokeCircle(cx, cy, r, s * 0.06f, Palette.Period);
            return canvas.ToTexture("RedMoon.Flow." + flow);
        });

        /// <summary>Menstruationsstatus som måne: fuld (første dag), aftagende (har mens), sidste kvarter (sidste dag).</summary>
        public static Texture2D Status(MenstruationStatus status) => Cached("status-" + status, () =>
        {
            var canvas = new PixelCanvas(Size);
            var s = (float)Size;
            var r = s * 0.38f;
            switch (status)
            {
                case MenstruationStatus.FirstDay:
                    MoonRenderer.DrawMoon(canvas, s / 2, s / 2, r, 0.5, Palette.Period, false);
                    break;
                case MenstruationStatus.Ongoing:
                    MoonRenderer.DrawMoon(canvas, s / 2, s / 2, r, 0.64, Palette.Period, false);
                    break;
                case MenstruationStatus.LastDay:
                    MoonRenderer.DrawMoon(canvas, s / 2, s / 2, r, 0.84, Palette.Period, false);
                    break;
                default:
                    canvas.StrokeCircle(s / 2, s / 2, r, s * 0.05f, Palette.TextSecondary);
                    break;
            }
            return canvas.ToTexture("RedMoon.Status." + status);
        });

        /// <summary>Ikon til bundmenuen i den givne farve.</summary>
        public static Texture2D Tab(TabIcon icon, Color color) => Cached("tab-" + icon + "-" + ColorUtility.ToHtmlStringRGBA(color), () =>
        {
            var canvas = new PixelCanvas(Size);
            var s = (float)Size;
            var stroke = s * 0.075f;
            switch (icon)
            {
                case TabIcon.Home:
                {
                    // Halvmåne: en cirkel minus en forskudt cirkel.
                    float cx = s * 0.48f, cy = s * 0.52f, r = s * 0.36f;
                    float ox = cx + s * 0.2f, oy = cy - s * 0.12f, or = r * 0.86f;
                    canvas.Shade(0, 0, s, s, color, (x, y) =>
                        Mathf.Clamp01(r - PixelCanvas.Distance(x, y, cx, cy) + 0.5f) *
                        Mathf.Clamp01(PixelCanvas.Distance(x, y, ox, oy) - or + 0.5f));
                    break;
                }
                case TabIcon.Calendar:
                    canvas.StrokePolyline(Points(s, (0.16f, 0.24f), (0.84f, 0.24f), (0.84f, 0.86f), (0.16f, 0.86f), (0.16f, 0.24f)), stroke, color);
                    canvas.StrokePolyline(Points(s, (0.16f, 0.40f), (0.84f, 0.40f)), stroke, color);
                    canvas.StrokePolyline(Points(s, (0.34f, 0.12f), (0.34f, 0.28f)), stroke, color);
                    canvas.StrokePolyline(Points(s, (0.66f, 0.12f), (0.66f, 0.28f)), stroke, color);
                    foreach (var y in new[] { 0.56f, 0.72f })
                    {
                        foreach (var x in new[] { 0.33f, 0.5f, 0.67f })
                        {
                            canvas.FillCircle(x * s, y * s, s * 0.045f, color);
                        }
                    }
                    break;
                case TabIcon.Profile:
                    canvas.StrokeCircle(s * 0.5f, s * 0.34f, s * 0.16f, stroke, color);
                    canvas.StrokePolyline(Arc(s * 0.5f, s * 0.92f, s * 0.32f, (float)Math.PI * 1.08f, (float)Math.PI * 1.92f, closed: false), stroke, color);
                    break;
            }
            return canvas.ToTexture("RedMoon.Tab." + icon);
        });

        private static Color MoodColor(Mood mood)
        {
            switch (mood)
            {
                case Core.Models.Mood.Outgoing: return new Color(1f, 0.78f, 0.34f);
                case Core.Models.Mood.Happy: return new Color(1f, 0.62f, 0.77f);
                case Core.Models.Mood.InBetween: return new Color(0.79f, 0.71f, 0.86f);
                case Core.Models.Mood.Sad: return new Color(0.50f, 0.70f, 1f);
                case Core.Models.Mood.Introverted: return new Color(0.71f, 0.55f, 1f);
                case Core.Models.Mood.Powerful: return new Color(1f, 0.78f, 0.34f);
                default: return Palette.TextSecondary;
            }
        }

        private static Texture2D Cached(string key, Func<Texture2D> create)
        {
            if (!Cache.TryGetValue(key, out var texture) || texture == null)
            {
                texture = create();
                Cache[key] = texture;
            }
            return texture;
        }

        /// <summary>Relative punkter (0-1) omregnet til pixels.</summary>
        private static List<Vector2> Points(float size, params (float X, float Y)[] relative)
        {
            var list = new List<Vector2>(relative.Length);
            foreach (var (x, y) in relative) list.Add(new Vector2(x * size, y * size));
            return list;
        }

        /// <summary>Punkter langs en cirkelbue (vinkler i radianer, y nedad). closed = bue + korde (til udfyldning).</summary>
        private static List<Vector2> Arc(float cx, float cy, float radius, float fromAngle, float toAngle, bool closed)
        {
            const int steps = 24;
            var list = new List<Vector2>(steps + 2);
            for (var i = 0; i <= steps; i++)
            {
                var t = fromAngle + (toAngle - fromAngle) * i / steps;
                list.Add(new Vector2(cx + radius * (float)Math.Cos(t), cy + radius * (float)Math.Sin(t)));
            }
            if (closed) list.Add(list[0]);
            return list;
        }
    }
}
