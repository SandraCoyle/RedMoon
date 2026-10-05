using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedMoon.Core.Common;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Mønster-adgangskode: brugeren trækker en streg gennem mindst 4 af 9 punkter (nummereret 1-9).
    /// Rækkefølgen betyder noget, og hvert punkt må kun bruges én gang.
    ///
    /// Antal mulige mønstre: 4 punkter = 3.024, 5 = 15.120, 6 = 60.480 ... (permutationer af 9).
    /// Det er MEGET få sammenlignet med en almindelig adgangskode – se README for konsekvenserne.
    /// </summary>
    public sealed class PatternPassword
    {
        /// <summary>Mindste antal punkter i et mønster.</summary>
        public const int MinimumLength = 4;

        /// <summary>Antal punkter i gitteret (3 x 3).</summary>
        public const int GridSize = 9;

        private readonly int[] _points;

        private PatternPassword(int[] points)
        {
            _points = points;
        }

        /// <summary>Punkterne i den rækkefølge de blev valgt (værdier 1-9).</summary>
        public IReadOnlyList<int> Points => _points;

        /// <summary>
        /// Validerer og opretter et mønster. Kaster ValidationException ved ugyldigt mønster.
        /// </summary>
        public static PatternPassword Create(IEnumerable<int> points)
        {
            if (points == null) throw new ValidationException("Tegn et mønster.");

            var list = points.ToArray();
            if (list.Length < MinimumLength)
            {
                throw new ValidationException($"Mønsteret skal forbinde mindst {MinimumLength} punkter.");
            }
            if (list.Length > GridSize)
            {
                throw new ValidationException("Mønsteret har for mange punkter.");
            }
            if (list.Any(p => p < 1 || p > GridSize))
            {
                throw new ValidationException("Mønsteret indeholder et ugyldigt punkt.");
            }
            if (list.Distinct().Count() != list.Length)
            {
                throw new ValidationException("Hvert punkt må kun bruges én gang.");
            }
            return new PatternPassword(list);
        }

        /// <summary>
        /// Prøver at oprette et mønster uden at kaste. Returnerer null ved ugyldigt input.
        /// </summary>
        public static PatternPassword? TryCreate(IEnumerable<int> points)
        {
            try
            {
                return Create(points);
            }
            catch (ValidationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Den byte-sekvens der hashes. Indeholder et fast præfiks (domæneadskillelse),
        /// så samme bytes aldrig kan forveksles med andre hemmeligheder i appen.
        /// </summary>
        internal byte[] ToSecretBytes()
        {
            var text = "redmoon-pattern-v1:" + string.Join("-", _points);
            return Encoding.UTF8.GetBytes(text);
        }

        /// <summary>Sand hvis to mønstre er ens (bruges ved "gentag mønster").</summary>
        public bool SameAs(PatternPassword other) => other != null && _points.SequenceEqual(other._points);

        /// <summary>Viser aldrig selve mønsteret, så det ikke havner i logs ved en fejl.</summary>
        public override string ToString() => "PatternPassword(***)";
    }
}
