using Autodesk.Revit.DB;
using HoloBlok;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBXYZUtils
    {
        public static List<XYZ> SortAlongVector(List<XYZ> points, XYZ vector)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            //Sort the points along the vector
            points.Sort((p1, p2) => ComparePointsAlongVector(p1, p2, vector));
            return points;
        }

        public static int ComparePointsAlongVector(XYZ point1, XYZ point2, XYZ vector)
        {
            double scalarProjection1 = point1.DotProduct(vector);
            double scalarProjection2 = point2.DotProduct(vector);

            return scalarProjection1.CompareTo(scalarProjection2);
        }

        public static List<int> SortIndicesByLocalXAtTransform(List<XYZ> points, Transform planeTransform)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (planeTransform == null) throw new ArgumentNullException(nameof(planeTransform));

            List<XYZ> localPoints = TransformPointsToLocal(points, planeTransform);
            bool allSameX = AreXCoordinatesEqual(localPoints);

            // Sort by X or Y based on the check
            return localPoints
                .Select((point, index) => new { Point = point, Index = index })
                .OrderBy(p => allSameX ? p.Point.Y : p.Point.X)
                .Select(p => p.Index)
                .ToList();
        }

        private static List<XYZ> TransformPointsToLocal(List<XYZ> points, Transform transform)
        {
            return points.Select(p => transform.Inverse.OfPoint(p)).ToList();
        }

        private static bool AreXCoordinatesEqual(List<XYZ> localPoints)
        {
            return localPoints.All(p => Math.Abs(p.X - localPoints[0].X) < HBConstantValues.Tolerance);
        }

        public static bool IsPointWithinPlaneTolerance(XYZ pointToCheck, Plane plane, double tolerance)
        {
            if (pointToCheck == null) throw new ArgumentNullException(nameof(pointToCheck));
            if (plane == null) throw new ArgumentNullException(nameof(plane));

            XYZ vectorToNewPoint = pointToCheck - plane.Origin;
            double distanceToPlane = plane.Normal.DotProduct(vectorToNewPoint);

            return Math.Abs(distanceToPlane) <= tolerance;
        }

        public static List<XYZ> ProjectPointsOntoOffsetPlane(List<XYZ> points, XYZ planeOrigin, XYZ planeNormal)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (planeOrigin == null) throw new ArgumentNullException(nameof(planeOrigin));
            if (planeNormal == null) throw new ArgumentNullException(nameof(planeNormal));

            return points.Select(point =>
            {
                double distanceToPlane = (point - planeOrigin).DotProduct(planeNormal);
                return point - planeNormal * distanceToPlane;
            }).ToList();
        }

        public static List<XYZ> ProjectPointsToPlaneWithOffset(Plane plane, List<XYZ> sortedPoints, XYZ vector, double distance)
        {
            if (plane == null) throw new ArgumentNullException(nameof(plane));
            if (sortedPoints == null) throw new ArgumentNullException(nameof(sortedPoints));
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            XYZ newOrigin = HBPlaneUtils.MoveOrigin(plane, vector, distance);

            return ProjectPointsOntoOffsetPlane(sortedPoints, newOrigin, plane.Normal);
        }

        public static XYZ ProjectVectorOntoViewPlane(XYZ vector, View view)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (view == null) throw new ArgumentNullException(nameof(view));

            XYZ viewNormal = view.ViewDirection.Normalize();
            return vector - viewNormal * vector.DotProduct(viewNormal);
        }
        
        public static double GetAngleFromViewRight(XYZ directionInPlane, View view)
        {
            if (directionInPlane == null) throw new ArgumentNullException(nameof(directionInPlane));
            if (view == null) throw new ArgumentNullException(nameof(view));

            XYZ viewRight = view.RightDirection;
            XYZ viewUp = view.UpDirection;
            return CalculateAngleBetweenVectors(directionInPlane, viewRight, viewUp);
        }

        public static double CalculateAngleBetweenVectors(XYZ directionVector, XYZ rightVector, XYZ upVector)
        {
            return Math.Atan2(directionVector.DotProduct(upVector), directionVector.DotProduct(rightVector));
        }

        public static ModelCurve VisualizePointWithLine(Document doc, XYZ point)
        {
            Line line = Line.CreateBound(point, new XYZ(point.X + 1, point.Y + 1, point.Z));
            return doc.Create.NewModelCurve(line, SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, point)));
        }
    }
}
