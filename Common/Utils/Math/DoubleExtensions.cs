using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoloBlok.Common.DataSets;

namespace HoloBlok.Common.MathExtensions
{
    public static class DoubleExtensions
    {
        public static bool IsAlmostEqualTo(this double a, double b, double tolerance = Constants.DefaultTolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}
