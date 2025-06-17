using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBCurveUtils
    {
        public static bool AreLinesParallel(Curve line1, Curve line2)
        {
            //Extract direction vectors of the two lines
            XYZ dir1 = (line1.GetEndPoint(1) - line1.GetEndPoint(0)).Normalize();
            XYZ dir2 = (line2.GetEndPoint(1) - line2.GetEndPoint(0)).Normalize();

            //Check if vectors are parallel by cross product
            XYZ crossProduct = dir1.CrossProduct(dir2);
            bool areParallel = crossProduct.IsAlmostEqualTo(XYZ.Zero);

            return areParallel;
        }

        public static double GetRotationInView(Curve curve, View view)
        {
            XYZ direction = GetCurveDirection(curve);
            XYZ directionInPlane = HBXYZUtils.ProjectVectorOntoViewPlane(direction, view);

            return HBXYZUtils.GetAngleFromViewRight(directionInPlane, view);
        }

        public static XYZ GetCurveDirection(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            return endPoint - startPoint;
        }

        public static XYZ GetMidpoint(Curve curve)
        {
            return curve.Evaluate(0.5, true);
        }


        public static Line CreateFromStartEndPoints(List<XYZ> points)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Point list is null or empty", nameof(points));

            XYZ startPoint = points.First();
            XYZ endPoint = points.Last();

            return Line.CreateBound(startPoint, endPoint);
        }

        internal static Curve GetSolidIntersectionCurve(Curve curve, IEnumerable<Solid> solids)
        {
            List<Curve> intersectingSegments = new List<Curve>();
            foreach (Solid solid in solids)
            {
                try
                {
                    SolidCurveIntersection intersection = solid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions());
                    for (int i = 0; i < intersection.SegmentCount; i++)
                    {
                        intersectingSegments.Add(intersection.GetCurveSegment(i));
                    }
                }

                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }

            if (intersectingSegments.Count == 0) return null;
            if (intersectingSegments.Count == 1) return intersectingSegments[0];
            return HBCurveListUtils.MergeCollinearCurves(intersectingSegments);
        }
    }
}
