using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok
{
    internal static class ArrayUtils
    {
        internal static string[] GetAlphabet()
        {
            return Enumerable.Range(0, 26).Select(i => ((char)('a' + i)).ToString()).ToArray();
        }
    }
}
