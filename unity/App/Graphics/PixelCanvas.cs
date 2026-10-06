using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedMoon.UnityApp.Graphics
{
    /// <summary>
    /// Lille tegneflade der tegner kantudglattede (anti-aliased) figurer i en pixelbuffer og laver en Texture2D.
    /// Bruges til månen og ikonerne, så appen ikke afhænger af billedfiler eller emoji-skrifttyper.
    /// Koordinater: (0,0) er øverst til venstre, y vokser nedad (som i UI'et).
    /// </summary>
    internal sealed class PixelCanvas
    {
        private readonly Color[] _pixels;

        public PixelCanvas(int size)
        {
            if (size < 4 || size > 2048) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            _pixels = new Color[size * size];
        }

        public int Size { get; }

        /// <summary>
        /// Farver alle pixels i et område med en dækningsfunktion (0 = intet, 1 = fuldt dækket).
        /// Funktionen får pixelens centrum (x + 0.5, y + 0.5).
        /// </summary>
        public void Shade(float left, float top, float right, float bottom, Color color, Func<float, float, float> coverage)
        {
            var x0 = Math.Max(0, (int)Math.Floor(left));
            var y0 = Math.Max(0, (int)Math.Floor(top));
            var x1 = Math.Min(Size - 1, (int)Math.Ceiling(right));
            var y1 = Math.Min(Size - 1, (int)Math.Ceiling(bottom));
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var c = coverage(x + 0.5f, y + 0.5f);
                    if (c > 0f) Blend(x, y, color, Mathf.Clamp01(c));
                }
            }
        }

        public void FillCircle(float cx, float cy, float radius, Color color)
        {
            Shade(cx - radius - 1, cy - radius - 1, cx + radius + 1, cy + radius + 1, color,
                (x, y) => radius - Distance(x, y, cx, cy) + 0.5f);
        }

        public void StrokeCircle(float cx, float cy, float radius, float width, Color color)
        {
            var half = width / 2f;
            Shade(cx - radius - half - 1, cy - radius - half - 1, cx + radius + half + 1, cy + radius + half + 1, color,
                (x, y) => half - Math.Abs(Distance(x, y, cx, cy) - radius) + 0.5f);
        }

        /// <summary>Blød glorie fra <paramref name="innerRadius"/> (fuld styrke) ud til <paramref name="outerRadius"/> (usynlig).</summary>
        public void Glow(float cx, float cy, float innerRadius, float outerRadius, Color color)
        {
            Shade(cx - outerRadius, cy - outerRadius, cx + outerRadius, cy + outerRadius, color, (x, y) =>
            {
                var d = Distance(x, y, cx, cy);
                if (d <= innerRadius || d >= outerRadius) return 0f;
                var t = 1f - (d - innerRadius) / (outerRadius - innerRadius);
                return t * t;
            });
        }

        /// <summary>Tegner en streg gennem punkterne med runde ender.</summary>
        public void StrokePolyline(IReadOnlyList<Vector2> points, float width, Color color)
        {
            if (points.Count == 0) return;
            var half = width / 2f;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Math.Min(minX, p.x); minY = Math.Min(minY, p.y);
                maxX = Math.Max(maxX, p.x); maxY = Math.Max(maxY, p.y);
            }

            Shade(minX - half - 1, minY - half - 1, maxX + half + 1, maxY + half + 1, color, (x, y) =>
            {
                var best = float.MaxValue;
                if (points.Count == 1) best = Distance(x, y, points[0].x, points[0].y);
                for (var i = 1; i < points.Count; i++)
                {
                    best = Math.Min(best, DistanceToSegment(x, y, points[i - 1], points[i]));
                }
                return half - best + 0.5f;
            });
        }

        /// <summary>Udfylder en polygon (lige-ulige-reglen) med udglattede kanter.</summary>
        public void FillPolygon(IReadOnlyList<Vector2> points, Color color)
        {
            if (points.Count < 3) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Math.Min(minX, p.x); minY = Math.Min(minY, p.y);
                maxX = Math.Max(maxX, p.x); maxY = Math.Max(maxY, p.y);
            }

            Shade(minX - 1, minY - 1, maxX + 1, maxY + 1, color, (x, y) =>
            {
                var inside = false;
                var edge = float.MaxValue;
                for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
                {
                    var a = points[i];
                    var b = points[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    edge = Math.Min(edge, DistanceToSegment(x, y, a, b));
                }
                return inside ? edge + 0.5f : 0.5f - edge;
            });
        }

        /// <summary>Laver en Texture2D. Den kaldende kode ejer teksturen og skal selv destruere den.</summary>
        public Texture2D ToTexture(string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            // Unity-teksturer har (0,0) nederst til venstre, så rækkerne vendes.
            var flipped = new Color[_pixels.Length];
            for (var y = 0; y < Size; y++)
            {
                Array.Copy(_pixels, y * Size, flipped, (Size - 1 - y) * Size, Size);
            }
            texture.SetPixels(flipped);
            texture.Apply(false, true);
            return texture;
        }

        public static float Distance(float x1, float y1, float x2, float y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float DistanceToSegment(float px, float py, Vector2 a, Vector2 b)
        {
            var dx = b.x - a.x;
            var dy = b.y - a.y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 0.0001f) return Distance(px, py, a.x, a.y);
            var t = Mathf.Clamp01(((px - a.x) * dx + (py - a.y) * dy) / lengthSquared);
            return Distance(px, py, a.x + t * dx, a.y + t * dy);
        }

        /// <summary>Lægger farven oven på pixelen ("over"-blanding) med den givne dækning.</summary>
        private void Blend(int x, int y, Color color, float coverage)
        {
            var index = y * Size + x;
            var dst = _pixels[index];
            var srcA = color.a * coverage;
            var outA = srcA + dst.a * (1f - srcA);
            if (outA <= 0f)
            {
                _pixels[index] = new Color(0, 0, 0, 0);
                return;
            }
            var r = (color.r * srcA + dst.r * dst.a * (1f - srcA)) / outA;
            var g = (color.g * srcA + dst.g * dst.a * (1f - srcA)) / outA;
            var b = (color.b * srcA + dst.b * dst.a * (1f - srcA)) / outA;
            _pixels[index] = new Color(r, g, b, outA);
        }
    }
}
