using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.Conversions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Datums
{

    internal static class HBGridUtils
    {
        public static Curve GetCurveInView(Grid grid, View view)
        {
            //Get curve representing 2D grid geometry in view 
            Curve curGridCurve = grid.GetCurvesInView(DatumExtentType.ViewSpecific, view).First();

            return curGridCurve;
        }
        
        public static XYZ[] GetEndPoints(Grid grid, View view)
        {
            //Get curve representing 2D grid geometry in view 
            Curve curGridCurve = GetCurveInView(grid, view);

            //Get points representing grid ends
            XYZ startPoint = curGridCurve.GetEndPoint(0);
            XYZ endPoint = curGridCurve.GetEndPoint(1);

            //Get datum end that has grid bubble visible
            bool BubbleOnEnd0 = grid.IsBubbleVisibleInView(DatumEnds.End0, view);
            bool BubbleOnEnd1 = grid.IsBubbleVisibleInView(DatumEnds.End1, view);

            //Return curve endpoint corresponding to grid bubble
            //!!POTENTIAL ISSUE: If there's a group of grids that have bubbles on both sides and additional shorter grids on one side, dimensions may not work correctly. This should be addressed
            if (BubbleOnEnd0)
            {
                return new XYZ[] { startPoint, endPoint };
            }
            else if (BubbleOnEnd1)
            {
                return new XYZ[] { endPoint, startPoint };
            }

            return null;
        }

        //Get grid end points, where bubble location is index 0 and opposite end is index 1
        public enum GridBubbleVisibility
        {
            Side1,
            Side2,
            BothSides
        }

        internal static List<HBGridGroup> Group(Document doc, View view, IEnumerable<Grid> grids)
        {
            //Group grids that are parallel and have grid bubble on same side
            List<HBGridGroup> gridGroupsWithLocations = new List<HBGridGroup>();

            foreach (var curGrid in grids)
            {
                XYZ gridBubbleLocation = GetGridBubbleLocation(curGrid, view);
                XYZ gridEndOppositeBubble = GetGridEndOppositeBubble(curGrid, view);

                bool added = false;

                foreach (var groupWithLocations in gridGroupsWithLocations)
                {
                    if (IsGridMatchingGroup(curGrid, gridBubbleLocation, groupWithLocations, view, doc))
                    {
                        AddGridToGroup(curGrid, gridBubbleLocation, groupWithLocations);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    gridGroupsWithLocations.Add(CreateNewGridDataGroup(curGrid, gridBubbleLocation, gridEndOppositeBubble, view));
                }
            }

            return gridGroupsWithLocations;
        }

        private static XYZ GetGridBubbleLocation(Grid grid, View view)
        {
            return HBGridUtils.GetEndPoints(grid, view)[0];
        }

        private static XYZ GetGridEndOppositeBubble(Grid grid, View view)
        {
            return HBGridUtils.GetEndPoints(grid, view)[1];
        }

        private static bool IsGridMatchingGroup(Grid curGrid, XYZ gridBubbleLocation, HBGridGroup groupWithLocations, View view, Document doc)
        {
            Grid firstGridInGroup = groupWithLocations.Grids.FirstOrDefault();
            XYZ firstGridBubbleLocInGroup = GetGridBubbleLocation(firstGridInGroup, view);
            Plane firstTangentPlaneInGroup = groupWithLocations.BubbleLocationPlane;

            double gridBubbleOffsetTolerance = 10;
            bool areParallel = HBCurveUtils.AreLinesParallel(firstGridInGroup.Curve, curGrid.Curve);
            bool bubblesArePlanar = HBXYZUtils.IsPointWithinPlaneTolerance(gridBubbleLocation, firstTangentPlaneInGroup, HBConversionUtils.ProjectPaperspaceMMToFeet(gridBubbleOffsetTolerance, view, doc));

            return areParallel && bubblesArePlanar;
        }

        private static void AddGridToGroup(Grid curGrid, XYZ gridBubbleLocation, HBGridGroup groupWithLocations)
        {
            groupWithLocations.Grids.Add(curGrid);
            groupWithLocations.BubbleLocations.Add(gridBubbleLocation);
        }

        private static HBGridGroup CreateNewGridDataGroup(Grid curGrid, XYZ gridBubbleLocation, XYZ gridEndOppositeBubble, View view)
        {
            return new HBGridGroup
            {
                Grids = new List<Grid> { curGrid },
                BubbleLocations = new List<XYZ> { gridBubbleLocation },
                BubbleLocationPlane = HBPlaneUtils.GetTangentPlane(curGrid.GetCurvesInView(DatumExtentType.ViewSpecific, view).First(), gridBubbleLocation),
                GridVector = (gridEndOppositeBubble - gridBubbleLocation).Normalize()
            };

        }

        private static List<Grid> SortGridsByPoints(List<Grid> grids, List<XYZ> points, List<XYZ> sortedPoints)
        {
            //Assuming 'grids' and 'points' are originally in corresponding order
            //and 'sortedPoints' is 'points' sorted by some criteria
            var gridPointMap = grids.Zip(points, (grid, point) => new { grid, point }).ToDictionary(pair => pair.point, pair => pair.grid);
            return sortedPoints.Select(point => gridPointMap[point]).ToList();
        }



        private static XYZ TEST_FamilyAtGridBubble(Document doc, Grid grid, View view, string familySymbolName)
        {

            var famSymbolCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(f => f.Name.Equals(familySymbolName));

            FamilySymbol famSymbol = famSymbolCollector.FirstOrDefault();

            string levelName = "Level 0";

            //Get level
            var levelCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(f => f.Name.Equals(levelName));

            Element level = levelCollector.FirstOrDefault() as Element;

            Curve curGridCurve = grid.GetCurvesInView(DatumExtentType.ViewSpecific, view).First();
            XYZ startPoint = curGridCurve.GetEndPoint(0);
            XYZ endPoint = curGridCurve.GetEndPoint(1);

            //Get datum end that has grid bubble visible
            bool BubbleOnEnd0 = grid.IsBubbleVisibleInView(DatumEnds.End0, view);
            bool BubbleOnEnd1 = grid.IsBubbleVisibleInView(DatumEnds.End1, view);

            //Return curve endpoint corresponding to grid bubble
            //!!POTENTIAL ISSUE: If there's a group of grids that have bubbles on both sides and additional shorter grids on one side, dimensions may not work correctly. This should be addressed

            if (BubbleOnEnd0)
            {
                doc.Create.NewFamilyInstance(startPoint, famSymbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            }
            else if (BubbleOnEnd1)
            {
                doc.Create.NewFamilyInstance(endPoint, famSymbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            }

            return null;
        }


    }
}
