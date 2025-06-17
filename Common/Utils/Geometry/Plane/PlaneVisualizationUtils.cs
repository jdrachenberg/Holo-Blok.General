using Autodesk.Revit.DB;

namespace HoloBlok.Utils.Geometry
{
    public static class PlaneVisualizationUtils
    {
        private static void VisualizeWithModelCurves(Document doc, Plane plane)
        {
            using (Transaction t = new Transaction(doc))
            {
                t.Start("plane test");
                SketchPlane skPlane = SketchPlane.Create(doc, plane);

                XYZ startPoint1 = plane.Origin;
                XYZ endPoint1 = plane.Origin.Add(plane.XVec * 20);

                XYZ startPoint2 = plane.Origin;
                XYZ endPoint2 = plane.Origin.Add(plane.YVec * 5);

                Curve line1 = Line.CreateBound(startPoint1, endPoint1);
                ModelCurve modelCurv1 = doc.Create.NewModelCurve(line1, skPlane);

                Curve line2 = Line.CreateBound(startPoint2, endPoint2);
                ModelCurve modelCurv2 = doc.Create.NewModelCurve(line2, skPlane);

                t.Commit();
            }
        }
    }
}
