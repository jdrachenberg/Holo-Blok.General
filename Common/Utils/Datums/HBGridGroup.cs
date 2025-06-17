using Autodesk.Revit.DB;
using HoloBlok.Lists;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Datums
{
    public class HBGridGroup
    {
        public List<Grid> Grids { get; set; } = new List<Grid>();
        public List<XYZ> BubbleLocations { get; set; } = new List<XYZ>();
        public XYZ GridVector { get; set; }
        public Plane BubbleLocationPlane { get; set; }

        public List<XYZ> GetSortedBubbleLocations()
        {
            return HBListUtils.SortListByPointsRelativeX(BubbleLocationPlane, BubbleLocations);
        }

        public List<Grid> GetSortedGrids()
        {
            return HBListUtils.SortListByPointsRelativeX(BubbleLocationPlane, BubbleLocations, Grids);
        }

        public List<XYZ> GetDimensionLocations(double offsetDistance)
        {
            return HBXYZUtils.ProjectPointsToPlaneWithOffset(BubbleLocationPlane, GetSortedBubbleLocations(), GridVector, offsetDistance);
        }

        public ReferenceArray GetReferenceArray()
        {
            var allGridRefs = new ReferenceArray();
            foreach (var grid in GetSortedGrids())
                allGridRefs.Append(new Reference(grid));

            return allGridRefs;
        }
    }
}
