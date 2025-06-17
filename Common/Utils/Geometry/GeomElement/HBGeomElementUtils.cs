using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System.Collections.Generic;

namespace HoloBlok.Utils.Geometry
{
    public static class HBGeomElementUtils
    {
        
        public static XYZ GetCentroid(GeometryElement geomElem)
        {
            //Get centroid of instance geometry
            BoundingBoxXYZ bb = geomElem.GetBoundingBox();
            XYZ centroid = (bb.Max + bb.Min) / 2;

            return centroid;
        }

        public static GeometryElement Rotate(GeometryElement geomElement, double rotation, XYZ centroid)
        {
            Transform rotate = Transform.CreateRotationAtPoint(XYZ.BasisZ, rotation, centroid);
            GeometryElement rotatedGeomElement = geomElement.GetTransformed(rotate);

            return rotatedGeomElement;
        }

        public static List<Solid> ExtractSolids(GeometryElement geomElement, Location loc)
        {
            //Get list of solids from GeometryElement
            List<Solid> solids = new List<Solid>();

            foreach (object obj in geomElement)
            {
                if (obj is Solid solid && HBSolidUtils.IsValidSolid(solid))
                {
                    solids.Add(solid);
                }

                else if (obj is GeometryInstance geomInst && loc is LocationPoint)
                {
                    Solid alignedSolid = HBGeomInstanceUtils.GetAlignedBoundingSolid(geomInst);

                    if (alignedSolid != null)
                    {
                        solids.Add(alignedSolid);
                    }
                }
            }

            return solids;
        }

        public static Solid CombineSolids(GeometryElement geomElement, Location loc)
        {
            List<Solid> solids = HBGeomElementUtils.ExtractSolids(geomElement, loc);
            if (solids.Count == 0) return null;

            return HBSolidUtils.CombineSolids(solids);
        }
    }
}
