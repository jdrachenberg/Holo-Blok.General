using HoloBlok.Common.DataSets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class XYZExtensions
    {
        public static XYZ Absolute(this XYZ xyz)
        {
            return new XYZ(Math.Abs(xyz.X), Math.Abs(xyz.Y), Math.Abs(xyz.Z));
        }

    }


}
