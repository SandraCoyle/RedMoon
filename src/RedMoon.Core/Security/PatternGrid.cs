using System;
using System.Collections.Generic;
using System.Linq;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Geometrien bag mønsterlåsen (3 x 3 punkter, nummereret 1-9 række for række).
    /// Ligger i Core, så MAUI-appen og Unity-appen opfører sig ens, og så reglerne kan testes.
    ///
    /// Regler:
    /// - Alle punkter fingeren passerer på et stykke, tages med i den rækkefølge de passeres
    ///   (et hurtigt swipe må ikke springe et punkt over).
    /// - Som på Android tages et uvalgt punkt, der ligger midt imellem det forrige og det nye
    ///   (fx 2 mellem 1 og 3), automatisk med først.
    /// </summary>
    public static class PatternGrid
    {
        /// <summary>Antal punkter per række/kolonne.</summary>
        public const int Size = 3;

        /// <summary>Hvor stor en del af en celle (målt fra centrum) der rammer et punkt.</summary>
        public const float HitRadiusFraction = 0.32f;

        /// <summary>Centrum af punkt 1-9 i et kvadrat med sidelængde <paramref name="side"/> (origo øverst til venstre).</summary>
        public static (float X, float Y) Center(int point, float side)
        {
            if (point < 1 || point > Size * Size) throw new ArgumentOutOfRangeException(nameof(point));
            var cell = side / Size;
            var index = point - 1;
            return (cell * (index % Size) + cell / 2, cell * (index / Size) + cell / 2);
        }

        /// <summary>Punktet præcis midt mellem a og b i gitteret, eller null hvis der ikke er et.</summary>
        public static int? MiddlePoint(int a, int b)
        {
            int rowA = (a - 1) / Size, colA = (a - 1) % Size, rowB = (b - 1) / Size, colB = (b - 1) % Size;
            if ((rowA + rowB) % 2 != 0 || (colA + colB) % 2 != 0) return null;
            var middle = (rowA + rowB) / 2 * Size + (colA + colB) / 2 + 1;
            return middle == a || middle == b ? (int?)null : middle;
        }

        /// <summary>
        /// Tilføjer et punkt til <paramref name="selected"/> (og et evt. overset midterpunkt før det).
        /// Gør intet hvis punktet allerede er valgt.
        /// </summary>
        public static void AddPoint(List<int> selected, int point)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            if (selected.Contains(point)) return;
            if (selected.Count > 0)
            {
                var middle = MiddlePoint(selected[selected.Count - 1], point);
                if (middle.HasValue && !selected.Contains(middle.Value)) selected.Add(middle.Value);
            }
            selected.Add(point);
        }

        /// <summary>
        /// Tilføjer alle punkter fingeren har passeret på vej fra (fromX, fromY) til (toX, toY),
        /// i den rækkefølge de blev passeret. Koordinater er i et kvadrat med sidelængde <paramref name="side"/>.
        /// </summary>
        public static void AddPointsAlong(List<int> selected, float fromX, float fromY, float toX, float toY, float side)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            if (side <= 0) return;

            var hitRadius = side / Size * HitRadiusFraction;
            var hits = new List<(int Point, float Position)>();
            for (var point = 1; point <= Size * Size; point++)
            {
                if (selected.Contains(point)) continue;
                var (cx, cy) = Center(point, side);
                var (distance, position) = DistanceToSegment(cx, cy, fromX, fromY, toX, toY);
                if (distance <= hitRadius) hits.Add((point, position));
            }

            foreach (var hit in hits.OrderBy(h => h.Position))
            {
                AddPoint(selected, hit.Point);
            }
        }

        /// <summary>Afstand fra punktet (px, py) til linjestykket a-b, og hvor langt (0-1) ad stykket det nærmeste sted ligger.</summary>
        private static (float Distance, float Position) DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 0.0001f) return (Distance(px, py, ax, ay), 0f);

            var t = ((px - ax) * dx + (py - ay) * dy) / lengthSquared;
            t = Math.Max(0f, Math.Min(1f, t));
            return (Distance(px, py, ax + t * dx, ay + t * dy), t);
        }

        private static float Distance(float x1, float y1, float x2, float y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
