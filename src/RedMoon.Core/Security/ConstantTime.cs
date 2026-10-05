namespace RedMoon.Core.Security
{
    /// <summary>
    /// Sammenligning af hemmelige bytes i konstant tid, så en angriber ikke kan
    /// gætte indholdet ud fra hvor lang tid sammenligningen tager (timing-angreb).
    /// </summary>
    internal static class ConstantTime
    {
        public static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < a.Length; i++)
            {
                difference |= a[i] ^ b[i];
            }
            return difference == 0;
        }
    }
}
