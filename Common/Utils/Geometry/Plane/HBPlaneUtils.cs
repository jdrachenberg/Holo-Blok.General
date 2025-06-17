using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBPlaneUtils
    {
        public static XYZ MoveOrigin(Plane plane, XYZ vector, double distance)
        {
            return plane.Origin + vector.Normalize() * distance;
        }

        public static IEnumerable<Solid> GetSolidsIntersectingPlane(List<Solid> solids, Plane plane)
        {
            List<Solid> intersectingElementGeometry = new List<Solid>();

            foreach (Solid solid in solids)
            {
                if (solid != null && DoesSolidIntersectPlane(solid, plane))
                {
                    intersectingElementGeometry.Add(solid);
                }
            }

            return intersectingElementGeometry;
        }

        public static bool DoesSolidIntersectPlane(Solid solid, Plane plane)
        {
            FaceArray faces = solid.Faces;
            foreach (Face face in faces)
            {
                EdgeArrayArray edgeLoops = face.EdgeLoops;
                foreach (EdgeArray edgeLoop in edgeLoops)
                {
                    foreach (Edge edge in edgeLoop)
                    {
                        Curve curve = edge.AsCurve();
                        if (curve != null)
                        {
                            XYZ point1 = curve.GetEndPoint(0);
                            XYZ point2 = curve.GetEndPoint(1);

                            //Check if the endpoints are on opposite sides of the plane
                            bool point1Above = IsAbovePlane(point1, plane);
                            bool point2Above = IsAbovePlane(point2, plane);

                            if (point1Above != point2Above)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool DoesLineIntersectPlane(XYZ point1, XYZ point2, Plane plane)
        {
            bool point1Above = IsAbovePlane(point1, plane);
            bool point2Above = IsAbovePlane(point2, plane);
            return point1Above != point2Above;
        }

        public static bool IsAbovePlane(XYZ point, Plane plane)
        {
            XYZ v = point - plane.Origin;
            double distance = plane.Normal.DotProduct(v);

            return distance > 0;
        }

        public static Plane GetTangentPlane(Curve curve, XYZ point)
        {
            XYZ tangent = curve.ComputeDerivatives(0, true).BasisX.Normalize();

            // The cross product of the tangent with the global Z axis gives a horizontal vector perpendicular to the tangent.
            XYZ xDirection = tangent.CrossProduct(XYZ.BasisZ).Normalize();

            // Check if the xDirection is a zero vector which can happen if the tangent is vertical.
            if (xDirection.IsZeroLength())
            {
                // The tangent is vertical, use global X axis as the plane's X direction.
                xDirection = XYZ.BasisX;
            }

            // which will be perpendicular to both, ensuring we have a proper right-handed coordinate system.
            XYZ upDirection = xDirection.CrossProduct(tangent).Normalize();

            // Create a plane using the point, the horizontal xDirection, and the corrected upDirection.
            Plane plane = Plane.CreateByOriginAndBasis(point, xDirection, upDirection);

            return plane;
        }
    }
}
