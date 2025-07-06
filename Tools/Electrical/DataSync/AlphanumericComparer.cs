#region Namespaces
using System.Text.RegularExpressions;
using System.Globalization;

#endregion

namespace HoloBlok.Tools.Electrical.DataSync
{
    public class AlphanumericComparer : IComparer<string>
    {
        public static readonly AlphanumericComparer Instance = new AlphanumericComparer();
        private AlphanumericComparer() { }

        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Remove whitespace
            x = x.Replace(" ", "");
            y = y.Replace(" ", "");

            List<string> tokensX = Tokenize(x);
            List<string> tokensY = Tokenize(y);

            int max = Math.Max(tokensX.Count, tokensY.Count);

            for (int i = 0; i < max; i++)
            {
                if (i >= tokensX.Count) return -1; // x is shorter
                if (i >= tokensY.Count) return 1; // y is shorter

                var tokenX = tokensX[i];
                var tokenY = tokensY[i];

                int result = CompareTokens(tokenX, tokenY);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var pattern = @"(\d+(\.\d+)?|[a-zA-Z]+|[\.\-_])";
            foreach (Match match in Regex.Matches(input, pattern))
            {
                tokens.Add(match.Value);
            }
            return tokens;
        }

        private int CompareTokens(string a, string b)
        {
            // Try comparing as numbers (including decimals
            if (double.TryParse(a, NumberStyles.Number, CultureInfo.InvariantCulture, out double numA) &&
                double.TryParse(b, NumberStyles.Number, CultureInfo.InvariantCulture, out double numB))
            {
                int numericCompare = numA.CompareTo(numB);
                if (numericCompare != 0)
                    return numericCompare;

                // If two numbers are equal but one is longer than the other (i.e. 1 vs 001), put the shorter number first
                return a.Length.CompareTo(b.Length);
            }

            // Otherwise, do case-insensitive string comparison
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

}
