using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HoloBlok.Utils.RevitElements;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBSolidUtils
    {
        public static Solid CreateFromGeomElement(GeometryElement geomElement)
        {
            if (geomElement ==  null) throw new ArgumentNullException(nameof(geomElement));
            
            BoundingBoxXYZ bbox = geomElement.GetBoundingBox();
            Solid boundingSolid = HBBoundingBoxUtils.GetSolid(bbox);

            return boundingSolid;
        }

        public static Solid CombineSolids(List<Solid> solids)
        {
            if (solids == null || solids.Count == 0) throw new ArgumentException("Solids list cannot be null or empty.", nameof(solids));
            
            Solid combinedSolid = solids[0];

            //If there are multiple solids, combine them
            for (int i = 1; i < solids.Count; i++)
            {
                try
                {
                    combinedSolid = BooleanOperationsUtils.ExecuteBooleanOperation(combinedSolid, solids[i], BooleanOperationsType.Union);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to union solids.", ex);
                }
            }
            return combinedSolid;
        }

        public static bool IsValidSolid(Solid solid)
        {
            if (solid == null) return false;
            return solid.Faces.Size > 0 && solid.Volume > 0;
        }

        public static Solid CreateFromCurveLoop(CurveLoop curves, double height)
        {
            if (height < 0.001)
            {
                return null;
            }
            else
            {
                Solid extrusion = GeometryCreationUtilities.CreateExtrusionGeometry(
                    new List<CurveLoop> { curves }, // list of CurveLoops to extrude
                    XYZ.BasisZ, // direction to extrude in
                    height); // height to extrude

                return extrusion;
            }
        }


        public static Solid CreateFromElement(Element element)
        {
            //Get the geometry of that element
            GeometryElement geomElement = element.get_Geometry(new Options());

            //Make sure there actually is geometry
            if (geomElement != null)
            {
                Location loc = element.Location;
                Solid geomSolid = HBGeomElementUtils.CombineSolids(geomElement, loc); // DEPENDENCY

                return geomSolid;
            }
            //If there is no solid geometry, return null
            return null;
        }


        public static Solid Rotate(Solid solid, double rotation, XYZ centroid)
        {
            if (solid == null) throw new ArgumentNullException(nameof(solid));
            if (centroid == null) throw new ArgumentNullException(nameof(centroid));

            Transform rotate = Transform.CreateRotationAtPoint(XYZ.BasisZ, rotation, centroid);
            Solid rotatedSolid = SolidUtils.CreateTransformed(solid, rotate);

            return rotatedSolid;
        }

        internal static IList<IList<Curve>> GetCropCurvesIntersectingSolids(View activeView, IEnumerable<Solid> modelElementSolids)
        {
            IList<CurveLoop> adjustedCropLoops = HBViewUtils.GetAdjustedCropLoops(activeView);
            IList<IList<Curve>> intersectingCurvesList = new List<IList<Curve>>();

            foreach (var cropLoop in adjustedCropLoops)
            {
                IList<Curve> curvesIntersectingLoop = new List<Curve>();
                foreach (var cropCurve in cropLoop)
                {
                    Curve intersectingCurve = HBCurveUtils.GetSolidIntersectionCurve(cropCurve, modelElementSolids);
                    if (intersectingCurve != null)
                    {
                        curvesIntersectingLoop.Add(intersectingCurve);
                    }
                }
                intersectingCurvesList.Add(curvesIntersectingLoop);
            }

            return intersectingCurvesList;
        }
    }
}
