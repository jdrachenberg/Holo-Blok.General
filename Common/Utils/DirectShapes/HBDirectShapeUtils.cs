using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok
{
    internal static class HBDirectShapeUtils
    {
        internal static DirectShape CreateFromSolid(Document doc, Solid solid, BuiltInCategory category)
        {
            //Create a DirectShape element
            DirectShape ds = CreateDirectShapeElement(doc, category);
            ds.SetShape(new GeometryObject[] { solid });
            return ds;
        }

        internal static DirectShape CreateFromGeometryElement(Document doc, GeometryElement geomElement, BuiltInCategory category)
        {
            //Create a DirectShape element
            DirectShape ds = CreateDirectShapeElement(doc, category);
            ds.SetShape(new GeometryObject[] { geomElement });
            return ds;
        }

        internal static DirectShape CreateFromGeometryInstance(Document doc, GeometryInstance geomInstance, BuiltInCategory category)
        {
            //Create a DirectShape element
            DirectShape ds = CreateDirectShapeElement(doc, category);
            ds.SetShape(new GeometryObject[] { geomInstance });
            return ds;
        }

        internal static DirectShape CreateDirectShapeFromGeometryInstance(Document doc, GeometryInstance geomInstance, BuiltInCategory category)
        {
            
            List<GeometryObject> geometryObjects = HBGeomInstanceUtils.GetObjects(geomInstance); //DEPENDENCY

            //If we have valid geometry objects, create a DirectShape
            if (geometryObjects.Count > 0)
            {
                //Create a DirectShape element
                DirectShape ds = CreateDirectShapeElement(doc, category);
                ds.SetShape(geometryObjects);
                return ds;
            }

            else
            {
                return null;
            }
        }

        private static DirectShape CreateDirectShapeElement(Document doc, BuiltInCategory category)
        {
            return DirectShape.CreateElement(doc, new ElementId(category));
        }

    }

}
