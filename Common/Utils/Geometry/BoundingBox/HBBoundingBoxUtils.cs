using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBBoundingBoxUtils
    {
        public static Solid GetSolid(BoundingBoxXYZ bbox)
        {
            CurveLoop baseCurves = GetBaseCurves(bbox);
            double height = GetHeight(bbox);

            return HBSolidUtils.CreateFromCurveLoop(baseCurves, height);
        }

        public static double GetHeight(BoundingBoxXYZ bbox)
        {
            return Math.Abs(bbox.Max.Z - bbox.Min.Z);
        }

        public static CurveLoop GetBaseCurves(BoundingBoxXYZ bbox)
        {
            //Get the corners of the bounding box
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            //Create bottom profile
            CurveLoop bottomProfile = new CurveLoop();

            //Bottom profile
            bottomProfile.Append(Line.CreateBound(new XYZ(min.X, min.Y, min.Z), new XYZ(max.X, min.Y, min.Z)));
            bottomProfile.Append(Line.CreateBound(new XYZ(max.X, min.Y, min.Z), new XYZ(max.X, max.Y, min.Z)));
            bottomProfile.Append(Line.CreateBound(new XYZ(max.X, max.Y, min.Z), new XYZ(min.X, max.Y, min.Z)));
            bottomProfile.Append(Line.CreateBound(new XYZ(min.X, max.Y, min.Z), new XYZ(min.X, min.Y, min.Z)));

            return bottomProfile;
        }
    }
}
