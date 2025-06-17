using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    public static class HBGeomInstanceUtils
    {
        public static List<GeometryObject> GetObjects(GeometryInstance geomInstance)
        {
            List<GeometryObject> geometryObjects = new List<GeometryObject>();

            //Extract geometry from the instance
            GeometryElement geometryElement = geomInstance.GetInstanceGeometry();
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject is Solid solid && solid.Volume > 0)
                {
                    geometryObjects.Add(solid);
                }
            }

            return geometryObjects;
        }

        public static double GetRotation(GeometryInstance geomInst)
        {
            Transform transform = geomInst.Transform;
            return Math.Atan2(transform.BasisX.Y, transform.BasisX.X);
        }

        public static Solid GetAlignedBoundingSolid(GeometryInstance geomInst)
        {
            GeometryElement geomElement = geomInst.GetInstanceGeometry();
            XYZ centroid = HBGeomElementUtils.GetCentroid(geomElement);
            double rotation = GetRotation(geomInst);    // NOTE: previous code was: double rotation = locPoint.Rotation;

            //Rotate geometry element to be orthagonal
            GeometryElement rotatedGeomElement = HBGeomElementUtils.Rotate(geomElement, rotation * -1, centroid);

            //Get bounding box solid
            Solid orthogonalSolid = HBSolidUtils.CreateFromGeomElement(rotatedGeomElement);

            if (orthogonalSolid != null)
            {
                //Rotate solid to original orientation
                return HBSolidUtils.Rotate(orthogonalSolid, rotation, centroid);
            }

            return null;
        }
    }
}
