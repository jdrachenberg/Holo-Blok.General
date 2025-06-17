#region Namespaces
using Autodesk.Revit.DB;
using HoloBlok.Utils.Conversions;
using HoloBlok.Utils.Datums;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace HoloBlok.Utils.RevitElements
{
    public static class HBDimensionCreator
    {
        private static Dimension CreateDimension(Document doc, View view, List<XYZ> locations, ReferenceArray refs)
        {
            Line dimLine = Line.CreateBound(locations.First(), locations.Last());
            return doc.Create.NewDimension(view, dimLine, refs);
        }

        public static Dimension AllGrids(Document doc, View view, List<XYZ> dimensionLocations, ReferenceArray gridReferences)
        {
            return CreateDimension(doc, view, dimensionLocations, gridReferences);
        }

        public static Dimension OverallGrids(Document doc, View view, List<XYZ> dimensionLocations, ReferenceArray gridReferences)
        {
            var refs = new ReferenceArray();
            refs.Append(gridReferences.get_Item(0));
            refs.Append(gridReferences.get_Item(gridReferences.Size - 1));
            return CreateDimension(doc, view, dimensionLocations, refs);
        }

        internal static bool DimensionGridGroup(Document doc, View view, HBGridGroup gridGroup, double offsetDistance)
        {
            int gridCount = gridGroup.Grids.Count;
            if (gridCount < 2) return true;

            double convertedOffsetDistance = HBConversionUtils.ProjectPaperspaceMMToFeet(offsetDistance, view, doc);

            List<XYZ> dimensionLocations = gridGroup.GetDimensionLocations(convertedOffsetDistance);
            ReferenceArray gridReferences = gridGroup.GetReferenceArray();

            Dimension allGridsDim = HBDimensionCreator.AllGrids(doc, view, dimensionLocations, gridReferences);
            if (allGridsDim == null) return false;

            if (gridCount >= 3)
            {
                MoveGridDimensionString(doc, gridGroup, convertedOffsetDistance, allGridsDim);

                //Create overall dimension
                Dimension overallGridsDim = HBDimensionCreator.OverallGrids(doc, view, dimensionLocations, gridReferences);
                if (overallGridsDim == null) return false;
            }
            return true;
        }

        private static void MoveGridDimensionString(Document doc, HBGridGroup gridGroup, double translationDistance, Dimension dimension)
        {
            XYZ moveVector = gridGroup.GridVector.Multiply(translationDistance);
            ElementTransformUtils.MoveElement(doc, dimension.Id, moveVector);
        }
    }
}
